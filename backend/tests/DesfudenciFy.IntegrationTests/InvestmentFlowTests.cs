using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Enums;
using DesfudenciFy.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.IntegrationTests;

public class InvestmentFlowTests
{
    [Fact]
    public async Task Create_investment_from_reserve_should_lock_available_without_creating_debit_entry()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        var (bank, type) = await fx.SeedInvestmentCatalogAsync();
        await fx.CreditReserveAsync(reserve.Id, 1000m);

        var investment = await fx.Investments.CreateAsync(new CreateInvestmentRequest(
            "Tesouro",
            "100% CDI",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddMonths(6),
            bank.Id,
            type.Id,
            [new ReserveAllocationDto(reserve.Id, 700m)]));

        Assert.Equal(700m, investment.StartAmount);
        Assert.Equal(700m, investment.CurrentAmount);
        Assert.Equal("100% CDI", investment.Rentability);
        Assert.Single(investment.SourceReserves);

        var entriesSum = await fx.Db.Entries
            .Where(e => e.ReserveId == reserve.Id)
            .SumAsync(e => e.Amount);
        Assert.Equal(1000m, entriesSum);

        var reserveDto = await fx.Reserves.GetAsync(reserve.Id);
        Assert.Equal(300m, reserveDto.AvailableValue);
    }

    [Fact]
    public async Task Create_investment_should_reject_amount_above_available_balance()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        var (bank, type) = await fx.SeedInvestmentCatalogAsync();
        await fx.CreditReserveAsync(reserve.Id, 200m);

        var exception = await Assert.ThrowsAsync<AppException>(() => fx.Investments.CreateAsync(new CreateInvestmentRequest(
            "CDB",
            null,
            DateTime.UtcNow.Date,
            null,
            bank.Id,
            type.Id,
            [new ReserveAllocationDto(reserve.Id, 250m)])));

        Assert.Equal("Saldo disponível insuficiente na reserva.", exception.Message);
        Assert.Empty(await fx.Db.Investments.ToListAsync());
    }

    [Fact]
    public async Task Liquidation_should_post_proportional_profit_and_unlock_principal()
    {
        await using var fx = new TestDbFixture();
        var reserveA = await fx.SeedReserveAsync("A");
        var reserveB = await fx.SeedReserveAsync("B");
        var (bank, type) = await fx.SeedInvestmentCatalogAsync();
        await fx.CreditReserveAsync(reserveA.Id, 700m);
        await fx.CreditReserveAsync(reserveB.Id, 300m);
        await fx.CreditFreeAsync(200m);

        var investment = await fx.Investments.CreateAsync(new CreateInvestmentRequest(
            "Mix",
            null,
            DateTime.UtcNow.Date,
            null,
            bank.Id,
            type.Id,
            [
                new ReserveAllocationDto(reserveA.Id, 700m),
                new ReserveAllocationDto(reserveB.Id, 300m),
                new ReserveAllocationDto(null, 200m),
            ]));

        await fx.Investments.UpdateCurrentAmountAsync(investment.Id, new UpdateCurrentAmountRequest(1440m));
        await fx.Investments.LiquidateAsync(investment.Id);

        var liquidated = await fx.Db.Investments.SingleAsync(i => i.Id == investment.Id);
        Assert.Equal(InvestmentStatus.Liquidated, liquidated.Status);
        Assert.Empty(await fx.Db.ReserveInvestments.Where(ri => ri.InvestmentId == investment.Id).ToListAsync());

        // Profit = 240 over 1200 start → 700/1200*240=140, 300/1200*240=60, 200/1200*240=40
        Assert.Equal(840m, await fx.Balance.GetReserveCurrentAsync(reserveA.Id));
        Assert.Equal(360m, await fx.Balance.GetReserveCurrentAsync(reserveB.Id));
        Assert.Equal(240m, await fx.Balance.GetFreeBalanceAsync());

        Assert.Equal(840m, await fx.Balance.GetReserveAvailableAsync(reserveA.Id));
        Assert.Equal(360m, await fx.Balance.GetReserveAvailableAsync(reserveB.Id));
        Assert.Equal(240m, await fx.Balance.GetFreeBalanceAvailableAsync());
    }

    [Fact]
    public async Task Liquidation_without_profit_should_only_unlock_principal()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        var (bank, type) = await fx.SeedInvestmentCatalogAsync();
        await fx.CreditReserveAsync(reserve.Id, 500m);

        var investment = await fx.Investments.CreateAsync(new CreateInvestmentRequest(
            "Flat",
            null,
            DateTime.UtcNow.Date,
            null,
            bank.Id,
            type.Id,
            [new ReserveAllocationDto(reserve.Id, 500m)]));

        await fx.Investments.LiquidateAsync(investment.Id);

        var profitEntries = await fx.Db.Entries
            .Where(e => e.Observation.Contains("Distribuição de lucros"))
            .ToListAsync();
        Assert.Empty(profitEntries);
        Assert.Equal(500m, await fx.Balance.GetReserveAvailableAsync(reserve.Id));
    }

    [Fact]
    public async Task Update_current_amount_should_not_go_below_start_amount()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        var (bank, type) = await fx.SeedInvestmentCatalogAsync();
        await fx.CreditReserveAsync(reserve.Id, 500m);

        var investment = await fx.Investments.CreateAsync(new CreateInvestmentRequest(
            "Floor",
            null,
            DateTime.UtcNow.Date,
            null,
            bank.Id,
            type.Id,
            [new ReserveAllocationDto(reserve.Id, 500m)]));

        await fx.Investments.UpdateCurrentAmountAsync(investment.Id, new UpdateCurrentAmountRequest(100m));
        var updated = await fx.Investments.GetAsync(investment.Id);

        Assert.Equal(500m, updated.CurrentAmount);
    }
}
