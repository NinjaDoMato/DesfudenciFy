using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Enums;
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

    [Fact]
    public async Task Get_and_update_purchase_should_return_mapped_installments()
    {
        await using var fx = new TestDbFixture();
        var created = await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "Monitor",
            "https://example.com/old",
            200m,
            2,
            DateTime.UtcNow.Date));

        var loaded = await fx.Purchases.GetAsync(created.Id);
        Assert.Equal("Monitor", loaded.Name);
        Assert.Equal(2, loaded.Installments.Count);

        var updated = await fx.Purchases.UpdateAsync(created.Id, new UpdatePurchaseRequest(
            "Monitor 27",
            "https://example.com/new"));

        Assert.Equal("Monitor 27", updated.Name);
        Assert.Equal("https://example.com/new", updated.ProductUrl);
        Assert.Equal(2, updated.Installments.Count);
    }

    [Fact]
    public async Task Pay_installment_with_reserve_should_debit_and_unpay_should_restore()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        await fx.CreditFreeAsync(200m);
        await fx.CreditReserveAsync(reserve.Id, 200m);

        var purchase = await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "TV",
            null,
            90m,
            3,
            DateTime.UtcNow.Date,
            reserve.Id));

        var first = purchase.Installments[0];
        var paid = await fx.Purchases.PayInstallmentAsync(purchase.Id, first.Id);

        Assert.True(paid.Paid);
        Assert.NotNull(paid.EntryId);
        Assert.Equal(170m, await fx.Balance.GetReserveAvailableAsync(reserve.Id));

        var debit = await fx.Db.Entries.SingleAsync(e => e.Id == paid.EntryId);
        Assert.Equal(-30m, debit.Amount);
        Assert.Equal(reserve.Id, debit.ReserveId);

        var reversed = await fx.Purchases.UnpayInstallmentAsync(purchase.Id, first.Id);
        Assert.False(reversed.Paid);
        Assert.Null(reversed.PaidDate);
        Assert.Null(reversed.EntryId);
        Assert.Equal(200m, await fx.Balance.GetReserveAvailableAsync(reserve.Id));
        Assert.Empty(await fx.Db.Entries.Where(e => e.Amount < 0 && e.ReserveId == reserve.Id).ToListAsync());
    }

    [Fact]
    public async Task Pay_installment_should_fail_when_reserve_available_is_insufficient()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        await fx.CreditReserveAsync(reserve.Id, 10m);

        var purchase = await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "Celular",
            null,
            90m,
            3,
            DateTime.UtcNow.Date,
            reserve.Id));

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            fx.Purchases.PayInstallmentAsync(purchase.Id, purchase.Installments[0].Id));

        Assert.Equal("Saldo disponível insuficiente na reserva.", exception.Message);
        Assert.Equal(10m, await fx.Balance.GetReserveAvailableAsync(reserve.Id));
        Assert.False((await fx.Db.Installments.SingleAsync(i => i.Id == purchase.Installments[0].Id)).Paid);
    }

    [Fact]
    public async Task Pay_installment_with_free_balance_should_debit_and_unpay_should_restore()
    {
        await using var fx = new TestDbFixture();
        await fx.CreditFreeAsync(200m);

        var purchase = await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "TV",
            null,
            90m,
            3,
            DateTime.UtcNow.Date,
            DebitSource: "FreeBalance"));

        Assert.Equal("FreeBalance", purchase.DebitSource);

        var first = purchase.Installments[0];
        var paid = await fx.Purchases.PayInstallmentAsync(purchase.Id, first.Id);

        Assert.True(paid.Paid);
        Assert.NotNull(paid.EntryId);
        Assert.Equal(170m, await fx.Balance.GetFreeBalanceAvailableAsync());

        var debit = await fx.Db.Entries.SingleAsync(e => e.Id == paid.EntryId);
        Assert.Equal(-30m, debit.Amount);
        Assert.Equal(EntryDestination.FreeBalance, debit.Destination);
        Assert.Null(debit.ReserveId);

        var reversed = await fx.Purchases.UnpayInstallmentAsync(purchase.Id, first.Id);
        Assert.False(reversed.Paid);
        Assert.Null(reversed.EntryId);
        Assert.Equal(200m, await fx.Balance.GetFreeBalanceAvailableAsync());
    }

    [Fact]
    public async Task Pay_installment_should_fail_when_free_balance_is_insufficient()
    {
        await using var fx = new TestDbFixture();
        await fx.CreditFreeAsync(10m);

        var purchase = await fx.Purchases.CreateAsync(new CreatePurchaseRequest(
            "Celular",
            null,
            90m,
            3,
            DateTime.UtcNow.Date,
            DebitSource: "FreeBalance"));

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            fx.Purchases.PayInstallmentAsync(purchase.Id, purchase.Installments[0].Id));

        Assert.Equal("Saldo livre insuficiente.", exception.Message);
        Assert.Equal(10m, await fx.Balance.GetFreeBalanceAvailableAsync());
        Assert.False((await fx.Db.Installments.SingleAsync(i => i.Id == purchase.Installments[0].Id)).Paid);
    }
}
