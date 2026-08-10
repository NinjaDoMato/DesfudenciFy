using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Enums;
using DesfudenciFy.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.IntegrationTests;

public class BalanceAndTransferTests
{
    [Fact]
    public async Task Free_balance_available_should_exclude_amounts_locked_in_investments()
    {
        await using var fx = new TestDbFixture();
        var (bank, type) = await fx.SeedInvestmentCatalogAsync();
        await fx.CreditFreeAsync(1000m);

        await fx.Investments.CreateAsync(new CreateInvestmentRequest(
            "CDB Livre",
            null,
            DateTime.UtcNow.Date,
            null,
            bank.Id,
            type.Id,
            [new ReserveAllocationDto(null, 400m)]));

        var available = await fx.Balance.GetFreeBalanceAvailableAsync();
        var invested = await fx.Balance.GetFreeBalanceInvestedAsync();
        var current = await fx.Balance.GetFreeBalanceAsync();

        Assert.Equal(1000m, current);
        Assert.Equal(400m, invested);
        Assert.Equal(600m, available);
        Assert.Equal(600m, await fx.Entries.GetFreeBalanceAsync());
    }

    [Fact]
    public async Task Reserve_available_should_exclude_amounts_locked_in_investments()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        var (bank, type) = await fx.SeedInvestmentCatalogAsync();
        await fx.CreditReserveAsync(reserve.Id, 800m);

        await fx.Investments.CreateAsync(new CreateInvestmentRequest(
            "LCI",
            null,
            DateTime.UtcNow.Date,
            null,
            bank.Id,
            type.Id,
            [new ReserveAllocationDto(reserve.Id, 300m)]));

        var dto = await fx.Reserves.GetAsync(reserve.Id);

        Assert.Equal(800m, dto.CurrentValue);
        Assert.Equal(300m, dto.InvestedValue);
        Assert.Equal(500m, dto.AvailableValue);
    }

    [Fact]
    public async Task Transfer_should_move_available_balance_from_free_to_reserve()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        await fx.CreditFreeAsync(500m);

        await fx.Entries.TransferAsync(new TransferRequest(
            EntryDestination.FreeBalance,
            null,
            EntryDestination.Reserve,
            reserve.Id,
            200m,
            "Alocação"));

        Assert.Equal(300m, await fx.Balance.GetFreeBalanceAvailableAsync());
        Assert.Equal(200m, await fx.Balance.GetReserveAvailableAsync(reserve.Id));
    }

    [Fact]
    public async Task Transfer_should_fail_when_source_available_is_insufficient()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        await fx.CreditFreeAsync(100m);

        var exception = await Assert.ThrowsAsync<AppException>(() => fx.Entries.TransferAsync(new TransferRequest(
            EntryDestination.FreeBalance,
            null,
            EntryDestination.Reserve,
            reserve.Id,
            150m,
            null)));

        Assert.Equal("Saldo livre insuficiente.", exception.Message);
        Assert.Equal(100m, await fx.Balance.GetFreeBalanceAvailableAsync());
        Assert.Equal(0m, await fx.Balance.GetReserveAvailableAsync(reserve.Id));
    }
}
