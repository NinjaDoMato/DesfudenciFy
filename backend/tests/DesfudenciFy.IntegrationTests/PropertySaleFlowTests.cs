using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using DesfudenciFy.Domain.Enums;
using DesfudenciFy.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.IntegrationTests;

public class PropertySaleFlowTests
{
    [Fact]
    public async Task Sell_should_zero_debt_post_profit_and_mark_sold()
    {
        await using var fx = new TestDbFixture();
        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Apto Venda",
            "Rua V",
            300_000m,
            0m,
            200_000m,
            1_500m,
            80,
            120_000m));

        var sold = await fx.Properties.SellAsync(property.Id, new SellPropertyRequest(
            280_000m,
            EntryDestination.FreeBalance,
            null));

        Assert.Equal("Sold", sold.Status);
        Assert.Equal(280_000m, sold.SaleAmount);
        Assert.Equal(0m, sold.RemainingBalance);
        Assert.Equal(0, sold.RemainingInstallments);
        Assert.False(sold.IsRented);
        Assert.NotNull(sold.SoldAt);
        Assert.Equal(80_000m, sold.PropertyReturn);

        var entry = await fx.Db.Entries.SingleAsync(e => e.Observation == "Venda do imóvel - Apto Venda");
        Assert.Equal(80_000m, entry.Amount);
        Assert.Equal(EntryDestination.FreeBalance, entry.Destination);
        Assert.Null(entry.ReserveId);
        Assert.Equal(80_000m, await fx.Balance.GetFreeBalanceAvailableAsync());

        var installmentCost = await fx.Db.FixedCosts.SingleAsync(c => c.PropertyId == property.Id);
        Assert.False(installmentCost.IsActive);
    }

    [Fact]
    public async Task Sell_should_credit_reserve_when_chosen_as_destination()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync("Reserva Imóvel");
        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Casa Venda",
            "Rua C",
            400_000m,
            0m,
            250_000m,
            0m,
            0,
            0m));

        var sold = await fx.Properties.SellAsync(property.Id, new SellPropertyRequest(
            310_000m,
            EntryDestination.Reserve,
            reserve.Id));

        Assert.Equal("Sold", sold.Status);
        var entry = await fx.Db.Entries.SingleAsync();
        Assert.Equal(60_000m, entry.Amount);
        Assert.Equal(EntryDestination.Reserve, entry.Destination);
        Assert.Equal(reserve.Id, entry.ReserveId);
        Assert.Equal(60_000m, await fx.Balance.GetReserveAvailableAsync(reserve.Id));
    }

    [Fact]
    public async Task Sell_rented_property_should_deactivate_rental_income_like_manual_unrent()
    {
        await using var fx = new TestDbFixture();
        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Loft Venda",
            "Rua L",
            350_000m,
            2_500m,
            200_000m,
            0m,
            0,
            0m));
        await fx.Properties.UpdateAsync(property.Id, new UpdatePropertyRequest(
            "Loft Venda",
            "Rua L",
            true,
            350_000m,
            2_500m,
            200_000m,
            0m,
            0,
            0m));

        Assert.True(await fx.Db.IncomeSources.AnyAsync(i => i.PropertyId == property.Id && i.IsActive));

        await fx.Properties.SellAsync(property.Id, new SellPropertyRequest(
            360_000m,
            EntryDestination.FreeBalance,
            null));

        var income = await fx.Db.IncomeSources.SingleAsync(i => i.PropertyId == property.Id);
        Assert.False(income.IsActive);
        var sold = await fx.Properties.GetAsync(property.Id);
        Assert.False(sold.IsRented);
    }

    [Fact]
    public async Task Sell_should_include_collected_rents_in_posted_profit()
    {
        await using var fx = new TestDbFixture();
        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Apto Sol",
            "Rua G",
            300_000m,
            2_000m,
            200_000m,
            0m,
            0,
            0m));
        await fx.Properties.AddRentPaymentAsync(property.Id, new CreatePropertyRentPaymentRequest(2_000m, "Março", null));

        var sold = await fx.Properties.SellAsync(property.Id, new SellPropertyRequest(
            300_000m,
            EntryDestination.FreeBalance,
            null));

        Assert.Equal(102_000m, sold.PropertyReturn);
        var saleEntry = await fx.Db.Entries.SingleAsync(e => e.Observation == "Venda do imóvel - Apto Sol");
        Assert.Equal(102_000m, saleEntry.Amount);
    }

    [Fact]
    public async Task Sell_should_reject_already_sold_property()
    {
        await using var fx = new TestDbFixture();
        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Kit",
            "Rua K",
            120_000m,
            0m,
            80_000m,
            0m,
            0,
            0m));
        await fx.Properties.SellAsync(property.Id, new SellPropertyRequest(130_000m, EntryDestination.FreeBalance, null));

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            fx.Properties.SellAsync(property.Id, new SellPropertyRequest(140_000m, EntryDestination.FreeBalance, null)));
        Assert.Equal("Este imóvel já foi vendido.", exception.Message);
    }

    [Fact]
    public async Task Sold_property_should_drop_out_of_dashboard_appraised_total()
    {
        await using var fx = new TestDbFixture();
        await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Keep",
            "Rua A",
            150_000m,
            0m,
            0m,
            0m,
            0,
            0m));
        var sold = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Sell",
            "Rua B",
            300_000m,
            0m,
            200_000m,
            0m,
            0,
            0m));

        var dashboard = new DashboardService(fx.AppDb, fx.Balance);
        var before = await dashboard.GetTotalsAsync();
        Assert.Equal(450_000m, before.TotalPropertyAppraisedValue);

        await fx.Properties.SellAsync(sold.Id, new SellPropertyRequest(280_000m, EntryDestination.FreeBalance, null));

        var after = await dashboard.GetTotalsAsync();
        Assert.Equal(150_000m, after.TotalPropertyAppraisedValue);
        Assert.Equal(80_000m, after.TotalFinancialCapital);
    }

    [Fact]
    public async Task Sell_should_reject_zero_sale_amount()
    {
        await using var fx = new TestDbFixture();
        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Zero",
            "Rua Z",
            100_000m,
            0m,
            50_000m,
            0m,
            0,
            0m));

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            fx.Properties.SellAsync(property.Id, new SellPropertyRequest(0m, EntryDestination.FreeBalance, null)));
        Assert.Equal("O valor da venda deve ser maior que zero.", exception.Message);
    }
}
