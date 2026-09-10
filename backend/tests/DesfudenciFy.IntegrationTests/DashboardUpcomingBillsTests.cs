using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using DesfudenciFy.IntegrationTests.Infrastructure;

namespace DesfudenciFy.IntegrationTests;

public class DashboardUpcomingBillsTests
{
    [Fact]
    public async Task Upcoming_bills_should_include_overdue_and_items_due_within_next_15_days_ordered_nearest_first()
    {
        await using var fx = new TestDbFixture();
        var today = DateTime.UtcNow.Date;

        await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "Overdue",
            "",
            10m,
            "Month",
            today.AddDays(-1),
            null));
        await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "Today",
            "",
            20m,
            "Month",
            today,
            null));
        await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "In15",
            "",
            30m,
            "Month",
            today.AddDays(15),
            null));
        await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "In16",
            "",
            40m,
            "Month",
            today.AddDays(16),
            null));
        await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "In7",
            "",
            50m,
            "Month",
            today.AddDays(7),
            null));

        var dashboard = new DashboardService(fx.AppDb, fx.Balance);
        var bills = await dashboard.GetUpcomingBillsAsync();

        Assert.Equal(new[] { "Overdue", "Today", "In7", "In15" }, bills.Select(b => b.Name).ToArray());
        Assert.All(bills, b => Assert.Equal("FixedCost", b.Kind));
        Assert.True(bills.Select(b => b.DueDate).SequenceEqual(bills.Select(b => b.DueDate).OrderBy(d => d)));
    }

    [Fact]
    public async Task Upcoming_bills_should_apply_same_window_to_installments_including_overdue()
    {
        await using var fx = new TestDbFixture();
        var today = DateTime.UtcNow.Date;

        await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "OverdueInstallment",
            null,
            50m,
            1,
            today.AddDays(-3)));
        await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "WithinWindow",
            null,
            100m,
            1,
            today.AddDays(10)));
        await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "OutsideWindow",
            null,
            200m,
            1,
            today.AddDays(20)));

        var dashboard = new DashboardService(fx.AppDb, fx.Balance);
        var bills = await dashboard.GetUpcomingBillsAsync();

        Assert.Contains(bills, b => b.Kind == "Installment" && b.Name == "OverdueInstallment");
        Assert.Contains(bills, b => b.Kind == "Installment" && b.Name == "WithinWindow");
        Assert.DoesNotContain(bills, b => b.Name == "OutsideWindow");
        Assert.Equal("OverdueInstallment", bills.First().Name);
    }
}
