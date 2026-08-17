using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using DesfudenciFy.IntegrationTests.Infrastructure;

namespace DesfudenciFy.IntegrationTests;

public class DashboardTotalsTests
{
    [Fact]
    public async Task Monthly_balance_should_subtract_next_unpaid_installment_per_purchase()
    {
        await using var fx = new TestDbFixture();
        var incomeType = fx.Db.IncomeTypes.First(t => t.IsActive);
        await fx.IncomeSources.CreateAsync(new UpsertIncomeSourceRequest("Salário", 5000m, "", true, incomeType.Id));
        await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "TV",
            null,
            1200m,
            12,
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)));

        var dashboard = new DashboardService(fx.AppDb, fx.Balance);
        var totals = await dashboard.GetTotalsAsync();

        Assert.Equal(100m, 1200m / 12m);
        Assert.Equal(4900m, totals.MonthlyBalance);
    }

    [Fact]
    public async Task Monthly_balance_should_ignore_paid_purchases_and_count_each_active_purchase_once()
    {
        await using var fx = new TestDbFixture();
        var incomeType = fx.Db.IncomeTypes.First(t => t.IsActive);
        await fx.IncomeSources.CreateAsync(new UpsertIncomeSourceRequest("Salário", 5000m, "", true, incomeType.Id));

        var paid = await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "Fone",
            null,
            90m,
            1,
            DateTime.UtcNow.Date));
        await fx.Purchases.PayInstallmentAsync(paid.Id, paid.Installments[0].Id);

        await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "Notebook",
            null,
            300m,
            3,
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)));

        var dashboard = new DashboardService(fx.AppDb, fx.Balance);
        var totals = await dashboard.GetTotalsAsync();

        Assert.Equal(4900m, totals.MonthlyBalance);
    }
}
