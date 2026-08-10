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

        var payment = await fx.FixedCosts.PayAsync(cost.Id, new CreateCostPaymentRequest(100m, null));
        Assert.Equal(100m, payment.PaidAmount);

        var updated = (await fx.FixedCosts.ListAsync()).Single(c => c.Id == cost.Id);
        Assert.Equal(due.AddMonths(1), updated.DueDate);
        Assert.Equal(200m, await fx.Balance.GetReserveAvailableAsync(reserve.Id));

        var debit = await fx.Db.Entries.SingleAsync(e => e.Id == payment.EntryId);
        Assert.Equal(-100m, debit.Amount);
        Assert.Equal(reserve.Id, debit.ReserveId);
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
}
