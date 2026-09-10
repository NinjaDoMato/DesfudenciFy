using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Entities;
using DesfudenciFy.Domain.Enums;
using DesfudenciFy.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.IntegrationTests;

public class PropertyAndCostFlowTests
{
    [Fact]
    public async Task Amortize_should_reduce_remaining_balance_and_installments_and_debit_free_balance()
    {
        await using var fx = new TestDbFixture();
        await fx.CreditFreeAsync(5000m);

        var property = new Property
        {
            Name = "Apto",
            Address = "Rua 1",
            InitialFinancingAmount = 100000m,
            InstallmentAmount = 1000m,
            RemainingInstallments = 10,
            RemainingBalance = 10000m,
            IsRented = false
        };
        fx.Db.Properties.Add(property);
        await fx.Db.SaveChangesAsync();

        var amortization = await fx.Properties.AmortizeAsync(property.Id, new CreateAmortizationRequest(
            0m,
            1,
            null,
            "Parcela",
            true,
            EntryDestination.FreeBalance,
            null));

        Assert.Equal(1000m, amortization.Amount);
        Assert.Equal(1, amortization.InstallmentsAmortized);

        var updated = await fx.Properties.GetAsync(property.Id);
        Assert.Equal(9000m, updated.RemainingBalance);
        Assert.Equal(9, updated.RemainingInstallments);
        Assert.Equal(4000m, await fx.Balance.GetFreeBalanceAvailableAsync());
    }

    [Fact]
    public async Task Amortize_should_reject_amount_greater_than_remaining_balance()
    {
        await using var fx = new TestDbFixture();
        var property = new Property
        {
            Name = "Casa",
            Address = "Rua 2",
            InitialFinancingAmount = 50000m,
            InstallmentAmount = 1000m,
            RemainingInstallments = 2,
            RemainingBalance = 2000m,
            IsRented = false
        };
        fx.Db.Properties.Add(property);
        await fx.Db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<AppException>(() => fx.Properties.AmortizeAsync(property.Id, new CreateAmortizationRequest(
            2500m,
            1,
            null,
            null,
            false,
            null,
            null)));

        Assert.Equal("O valor da amortização não pode ser maior que o saldo restante.", exception.Message);
        var unchanged = await fx.Properties.GetAsync(property.Id);
        Assert.Equal(2000m, unchanged.RemainingBalance);
        Assert.Equal(2, unchanged.RemainingInstallments);
    }

    [Fact]
    public async Task Paying_fixed_cost_should_debit_reserve_and_advance_due_date()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        await fx.CreditReserveAsync(reserve.Id, 300m);

        var due = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var cost = await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "Internet",
            "Fibra",
            100m,
            "Month",
            due,
            reserve.Id));

        var payment = await fx.FixedCosts.PayAsync(cost.Id, new CreateCostPaymentRequest(100m, due));
        Assert.Equal(100m, payment.PaidAmount);

        var updated = (await fx.FixedCosts.ListAsync()).Single(c => c.Id == cost.Id);
        Assert.Equal(due.AddMonths(1), updated.DueDate);
        Assert.Equal(200m, await fx.Balance.GetReserveAvailableAsync(reserve.Id));

        var debit = await fx.Db.Entries.SingleAsync(e => e.Id == payment.EntryId);
        Assert.Equal(-100m, debit.Amount);
        Assert.Equal(reserve.Id, debit.ReserveId);
    }

    [Fact]
    public async Task Paying_fixed_cost_should_advance_due_date_from_last_payment_date()
    {
        await using var fx = new TestDbFixture();
        var due = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var paidOn = new DateTime(2026, 8, 20, 15, 30, 0, DateTimeKind.Utc);
        var cost = await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "Internet",
            "",
            100m,
            "Month",
            due,
            null));

        await fx.FixedCosts.PayAsync(cost.Id, new CreateCostPaymentRequest(100m, paidOn));

        var updated = await fx.FixedCosts.GetAsync(cost.Id);
        Assert.Equal(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc), updated.DueDate);
    }

    [Fact]
    public async Task Deleting_last_fixed_cost_payment_should_restore_due_date_to_payment_date()
    {
        await using var fx = new TestDbFixture();
        var due = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var paidOn = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);
        var cost = await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "Internet",
            "",
            100m,
            "Month",
            due,
            null));

        var payment = await fx.FixedCosts.PayAsync(cost.Id, new CreateCostPaymentRequest(100m, paidOn));
        Assert.Equal(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc),
            (await fx.FixedCosts.GetAsync(cost.Id)).DueDate);

        await fx.FixedCosts.DeletePaymentAsync(cost.Id, payment.Id);

        var afterDelete = await fx.FixedCosts.GetAsync(cost.Id);
        Assert.Equal(paidOn, afterDelete.DueDate);
        Assert.Empty(afterDelete.Payments);
    }

    [Fact]
    public async Task Deleting_latest_fixed_cost_payment_should_resync_due_date_from_remaining_last_payment()
    {
        await using var fx = new TestDbFixture();
        var due = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var firstPaid = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var secondPaid = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);
        var cost = await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "Internet",
            "",
            100m,
            "Month",
            due,
            null));

        await fx.FixedCosts.PayAsync(cost.Id, new CreateCostPaymentRequest(100m, firstPaid));
        var second = await fx.FixedCosts.PayAsync(cost.Id, new CreateCostPaymentRequest(100m, secondPaid));
        Assert.Equal(new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc),
            (await fx.FixedCosts.GetAsync(cost.Id)).DueDate);

        await fx.FixedCosts.DeletePaymentAsync(cost.Id, second.Id);

        var afterDelete = await fx.FixedCosts.GetAsync(cost.Id);
        Assert.Equal(new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc), afterDelete.DueDate);
        Assert.Single(afterDelete.Payments);
    }

    [Fact]
    public async Task Paying_fixed_cost_should_fail_when_reserve_available_is_insufficient()
    {
        await using var fx = new TestDbFixture();
        var reserve = await fx.SeedReserveAsync();
        await fx.CreditReserveAsync(reserve.Id, 50m);

        var cost = await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "Luz",
            "",
            100m,
            "Month",
            DateTime.UtcNow.Date,
            reserve.Id));

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            fx.FixedCosts.PayAsync(cost.Id, new CreateCostPaymentRequest(100m, null)));

        Assert.Equal("Saldo disponível insuficiente na reserva.", exception.Message);
        Assert.Equal(50m, await fx.Balance.GetReserveAvailableAsync(reserve.Id));
        Assert.Empty(await fx.Db.CostPayments.ToListAsync());
    }

    [Fact]
    public async Task Paying_fixed_cost_without_reserve_can_debit_free_balance()
    {
        await using var fx = new TestDbFixture();
        await fx.CreditFreeAsync(250m);

        var due = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var cost = await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "Água",
            "",
            80m,
            "Month",
            due,
            null));

        var payment = await fx.FixedCosts.PayAsync(
            cost.Id,
            new CreateCostPaymentRequest(80m, due, DebitFromFreeBalance: true));

        Assert.NotNull(payment.EntryId);
        Assert.Equal(170m, await fx.Balance.GetFreeBalanceAvailableAsync());

        var debit = await fx.Db.Entries.SingleAsync(e => e.Id == payment.EntryId);
        Assert.Equal(-80m, debit.Amount);
        Assert.Equal(EntryDestination.FreeBalance, debit.Destination);
        Assert.Null(debit.ReserveId);

        var updated = (await fx.FixedCosts.ListAsync()).Single(c => c.Id == cost.Id);
        Assert.Equal(due.AddMonths(1), updated.DueDate);
    }

    [Fact]
    public async Task Paying_fixed_cost_without_reserve_should_fail_when_free_balance_is_insufficient()
    {
        await using var fx = new TestDbFixture();
        await fx.CreditFreeAsync(20m);

        var cost = await fx.FixedCosts.CreateAsync(new UpsertFixedCostRequest(
            "Gás",
            "",
            80m,
            "Month",
            DateTime.UtcNow.Date,
            null));

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            fx.FixedCosts.PayAsync(
                cost.Id,
                new CreateCostPaymentRequest(80m, null, DebitFromFreeBalance: true)));

        Assert.Equal("Saldo livre insuficiente.", exception.Message);
        Assert.Equal(20m, await fx.Balance.GetFreeBalanceAvailableAsync());
        Assert.Empty(await fx.Db.CostPayments.ToListAsync());
    }
}
