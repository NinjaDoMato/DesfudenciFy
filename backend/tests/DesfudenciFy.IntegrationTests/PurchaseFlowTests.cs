using DesfudenciFy.Application.DTOs;
using DesfudenciFy.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.IntegrationTests;

public class PurchaseFlowTests
{
    [Fact]
    public async Task Create_purchase_should_generate_installments_that_sum_to_total()
    {
        await using var fx = new TestDbFixture();
        var firstDue = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        var purchase = await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "Notebook",
            "https://example.com",
            100m,
            3,
            firstDue));

        Assert.Equal(3, purchase.Installments.Count);
        Assert.Equal(100m, purchase.Installments.Sum(i => i.Amount));
        Assert.Equal(33.33m, purchase.Installments[0].Amount);
        Assert.Equal(33.33m, purchase.Installments[1].Amount);
        Assert.Equal(33.34m, purchase.Installments[2].Amount);
        Assert.Equal(firstDue, purchase.Installments[0].DueDate);
        Assert.Equal(firstDue.AddMonths(2), purchase.Installments[2].DueDate);
        Assert.All(purchase.Installments, i => Assert.False(i.Paid));
    }

    [Fact]
    public async Task Pay_installment_should_mark_only_that_installment_as_paid()
    {
        await using var fx = new TestDbFixture();
        var purchase = await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "Fone",
            null,
            90m,
            3,
            DateTime.UtcNow.Date));

        var first = purchase.Installments[0];
        var paid = await fx.Purchases.PayInstallmentAsync(purchase.Id, first.Id);

        Assert.True(paid.Paid);
        Assert.NotNull(paid.PaidDate);

        var reloaded = await fx.Db.Purchases.Include(p => p.Installments).SingleAsync(p => p.Id == purchase.Id);
        Assert.True(reloaded.Installments.Single(i => i.Id == first.Id).Paid);
        Assert.Equal(2, reloaded.Installments.Count(i => !i.Paid));
    }
}
