using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.IntegrationTests.Infrastructure;

namespace DesfudenciFy.IntegrationTests;

public class ReserveRulesTests
{
    [Fact]
    public async Task Delete_reserve_should_fail_when_linked_to_active_investment()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        var (bank, type) = await fx.SeedInvestmentCatalogAsync();
        await fx.CreditReserveAsync(reserve.Id, 500m);

        await fx.Investments.CreateAsync(new CreateInvestmentRequest(
            "Bloqueio",
            null,
            DateTime.UtcNow.Date,
            null,
            bank.Id,
            type.Id,
            [new ReserveAllocationDto(reserve.Id, 100m)]));

        var exception = await Assert.ThrowsAsync<AppException>(() => fx.Reserves.DeleteAsync(reserve.Id));
        Assert.Equal("Não é possível excluir uma reserva vinculada a investimentos.", exception.Message);
        Assert.NotNull(await fx.Reserves.GetAsync(reserve.Id));
    }

    [Fact]
    public async Task Delete_reserve_should_remove_its_entries_when_not_invested()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        await fx.CreditReserveAsync(reserve.Id, 250m);

        await fx.Reserves.DeleteAsync(reserve.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => fx.Reserves.GetAsync(reserve.Id));
        Assert.Empty(fx.Db.Entries.Where(e => e.ReserveId == reserve.Id).ToList());
        Assert.Equal(0m, await fx.Balance.GetFreeBalanceAsync());
    }
}
