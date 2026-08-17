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
        Assert.Equal(5000m, totals.TotalIncome);
        Assert.Equal(100m, totals.TotalMonthlyCosts);
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

        Assert.Equal(100m, totals.TotalMonthlyCosts);
        Assert.Equal(4900m, totals.MonthlyBalance);
    }

    [Fact]
    public async Task Accumulated_wealth_should_add_financial_capital_and_property_appraised_values()
    {
        await using var fx = new TestDbFixture();
        await fx.CreditFreeAsync(1000m);
        var reserve = await fx.SeedReserveAsync();
        await fx.CreditReserveAsync(reserve.Id, 400m);
        await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Apto",
            "Rua A",
            300_000m,
            0m,
            200_000m,
            1_500m,
            120,
            180_000m));
        await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Casa",
            "Rua B",
            150_000m,
            0m,
            0m,
            0m,
            0,
            0m));

        var dashboard = new DashboardService(fx.AppDb, fx.Balance);
        var totals = await dashboard.GetTotalsAsync();

        Assert.Equal(1_400m, totals.TotalFinancialCapital);
        Assert.Equal(450_000m, totals.TotalPropertyAppraisedValue);
        Assert.Equal(451_400m, totals.TotalAccumulated);
    }

    [Fact]
    public async Task Invested_totals_should_split_free_reserves_and_retained_profit_of_active_investments()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        var (bank, type) = await fx.SeedInvestmentCatalogAsync();
        await fx.CreditReserveAsync(reserve.Id, 700m);
        await fx.CreditFreeAsync(300m);

        var investment = await fx.Investments.CreateAsync(new CreateInvestmentRequest(
            "CDB",
            null,
            DateTime.UtcNow.Date,
            null,
            bank.Id,
            type.Id,
            [
                new ReserveAllocationDto(reserve.Id, 700m),
                new ReserveAllocationDto(null, 300m),
            ]));
        await fx.Investments.UpdateCurrentAmountAsync(investment.Id, new UpdateCurrentAmountRequest(1100m));

        var dashboard = new DashboardService(fx.AppDb, fx.Balance);
        var totals = await dashboard.GetTotalsAsync();

        Assert.Equal(1000m, totals.TotalInvested);
        Assert.Equal(300m, totals.TotalInvestedFromFree);
        Assert.Equal(700m, totals.TotalInvestedFromReserves);
        Assert.Equal(100m, totals.RetainedProfit);
    }
}
