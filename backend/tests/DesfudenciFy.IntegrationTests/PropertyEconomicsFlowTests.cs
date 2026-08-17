using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Enums;
using DesfudenciFy.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.IntegrationTests;

public class PropertyEconomicsFlowTests
{
    [Fact]
    public async Task Create_property_with_installments_should_create_monthly_fixed_cost()
    {
        await using var fx = new TestDbFixture();

        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Apto Centro",
            "Rua A",
            300_000m,
            0m,
            200_000m,
            1_500m,
            120,
            180_000m));

        var cost = await fx.Db.FixedCosts.SingleAsync(c => c.PropertyId == property.Id);
        Assert.True(cost.IsActive);
        Assert.Equal(1_500m, cost.Amount);
        Assert.Equal(CostRecurrence.Month, cost.Recurrence);
        Assert.Equal("Parcela - Apto Centro", cost.Name);
    }

    [Fact]
    public async Task Update_installment_amount_should_update_linked_fixed_cost()
    {
        await using var fx = new TestDbFixture();

        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Casa",
            "Rua B",
            400_000m,
            0m,
            250_000m,
            2_000m,
            100,
            200_000m));

        await fx.Properties.UpdateAsync(property.Id, new UpdatePropertyRequest(
            "Casa",
            "Rua B",
            false,
            400_000m,
            0m,
            250_000m,
            2_200m,
            100,
            220_000m));

        var cost = await fx.Db.FixedCosts.SingleAsync(c => c.PropertyId == property.Id && c.IsActive);
        Assert.Equal(2_200m, cost.Amount);
    }

    [Fact]
    public async Task Amortize_until_no_remaining_installments_should_finalize_fixed_cost()
    {
        await using var fx = new TestDbFixture();
        await fx.CreditFreeAsync(10_000m);

        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Studio",
            "Rua C",
            180_000m,
            0m,
            100_000m,
            1_000m,
            2,
            2_000m));

        Assert.True(await fx.Db.FixedCosts.AnyAsync(c => c.PropertyId == property.Id && c.IsActive));

        await fx.Properties.AmortizeAsync(property.Id, new CreateAmortizationRequest(
            0m, 2, null, null, true, EntryDestination.FreeBalance, null));

        var cost = await fx.Db.FixedCosts.SingleAsync(c => c.PropertyId == property.Id);
        Assert.False(cost.IsActive);
        var updated = await fx.Properties.GetAsync(property.Id);
        Assert.Equal(0, updated.RemainingInstallments);
    }

    [Fact]
    public async Task Marking_property_as_rented_should_create_and_deactivate_rental_income_source()
    {
        await using var fx = new TestDbFixture();

        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Loft",
            "Rua D",
            350_000m,
            2_500m,
            200_000m,
            0m,
            0,
            0m));

        await fx.Properties.UpdateAsync(property.Id, new UpdatePropertyRequest(
            "Loft",
            "Rua D",
            true,
            350_000m,
            2_500m,
            200_000m,
            0m,
            0,
            0m));

        var income = await fx.Db.IncomeSources
            .Include(i => i.IncomeType)
            .SingleAsync(i => i.PropertyId == property.Id);
        Assert.True(income.IsActive);
        Assert.Equal(2_500m, income.Amount);
        Assert.Equal("Aluguel", income.IncomeType.Name);
        Assert.Equal("Aluguel - Loft", income.Name);

        await fx.Properties.UpdateAsync(property.Id, new UpdatePropertyRequest(
            "Loft",
            "Rua D",
            false,
            350_000m,
            2_500m,
            200_000m,
            0m,
            0,
            0m));

        income = await fx.Db.IncomeSources.SingleAsync(i => i.PropertyId == property.Id);
        Assert.False(income.IsActive);
    }

    [Fact]
    public async Task Marking_rented_without_rental_amount_should_fail()
    {
        await using var fx = new TestDbFixture();

        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Kitnet",
            "Rua E",
            120_000m,
            0m,
            80_000m,
            0m,
            0,
            0m));

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            fx.Properties.UpdateAsync(property.Id, new UpdatePropertyRequest(
                "Kitnet",
                "Rua E",
                true,
                120_000m,
                0m,
                80_000m,
                0m,
                0,
                0m)));

        Assert.Equal("Informe o valor do aluguel ao marcar o imóvel como alugado.", exception.Message);
    }

    [Fact]
    public async Task Add_expense_with_debit_should_create_entry_and_increase_property_cost()
    {
        await using var fx = new TestDbFixture();
        await fx.CreditFreeAsync(5_000m);

        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Casa Verde",
            "Rua F",
            400_000m,
            0m,
            200_000m,
            0m,
            0,
            0m));

        Assert.Equal(200_000m, property.PropertyCost);

        var expenseType = await fx.Db.PropertyExpenseTypes.SingleAsync(t => t.Name == "Serviços");
        var expense = await fx.Properties.AddExpenseAsync(property.Id, new CreatePropertyExpenseRequest(
            1_500m,
            expenseType.Id,
            "Contratado eletricista",
            null,
            true,
            EntryDestination.FreeBalance,
            null));

        Assert.Equal(1_500m, expense.Amount);
        Assert.Equal(expenseType.Id, expense.ExpenseTypeId);
        Assert.Equal("Serviços", expense.ExpenseTypeName);
        Assert.NotNull(expense.EntryId);

        var updated = await fx.Properties.GetAsync(property.Id);
        Assert.Equal(1_500m, updated.TotalExpenses);
        Assert.Equal(201_500m, updated.PropertyCost);
        Assert.Equal(3_500m, await fx.Balance.GetFreeBalanceAvailableAsync());
    }

    [Fact]
    public async Task Add_rent_payment_should_credit_free_balance_and_increase_return()
    {
        await using var fx = new TestDbFixture();

        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Apto Sol",
            "Rua G",
            300_000m,
            2_000m,
            200_000m,
            0m,
            0,
            0m));

        Assert.Equal(100_000m, property.PropertyReturn);

        var payment = await fx.Properties.AddRentPaymentAsync(property.Id, new CreatePropertyRentPaymentRequest(
            2_000m,
            "Março",
            null));

        Assert.Equal(2_000m, payment.Amount);
        var entry = await fx.Db.Entries.SingleAsync(e => e.Id == payment.EntryId);
        Assert.Equal(2_000m, entry.Amount);
        Assert.Equal(EntryDestination.FreeBalance, entry.Destination);

        var updated = await fx.Properties.GetAsync(property.Id);
        Assert.Equal(2_000m, updated.TotalRentPaid);
        Assert.Equal(102_000m, updated.PropertyReturn);
        Assert.Equal(2_000m, await fx.Balance.GetFreeBalanceAvailableAsync());
    }

    [Fact]
    public async Task Income_source_create_should_require_income_type()
    {
        await using var fx = new TestDbFixture();
        var type = await fx.Db.IncomeTypes.SingleAsync(t => t.Name == "Salário");

        var created = await fx.IncomeSources.CreateAsync(new UpsertIncomeSourceRequest(
            "Salário CLT",
            8_000m,
            "Mensal",
            true,
            type.Id));

        Assert.Equal(type.Id, created.IncomeTypeId);
        Assert.Equal("Salário", created.IncomeTypeName);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            fx.IncomeSources.CreateAsync(new UpsertIncomeSourceRequest(
                "Sem tipo",
                100m,
                "",
                true,
                Guid.NewGuid())));

        Assert.Equal("Tipo de entrada inválido ou inativo.", exception.Message);
    }

    [Fact]
    public async Task Seeded_income_types_should_include_defaults()
    {
        await using var fx = new TestDbFixture();
        var names = await fx.Db.IncomeTypes.Select(t => t.Name).OrderBy(n => n).ToListAsync();

        Assert.Contains("Aluguel", names);
        Assert.Contains("Renda extra", names);
        Assert.Contains("Salário", names);
        Assert.Contains("Vale Alimentação", names);
        Assert.Contains("Vale Refeição", names);
    }

    [Fact]
    public async Task Seeded_property_expense_types_should_include_defaults()
    {
        await using var fx = new TestDbFixture();
        var names = await fx.Db.PropertyExpenseTypes.Select(t => t.Name).OrderBy(n => n).ToListAsync();

        Assert.Equal(["Documentação", "Leilão", "Material", "Serviços"], names);
    }

    [Fact]
    public async Task Add_expense_should_require_expense_type()
    {
        await using var fx = new TestDbFixture();

        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            "Casa Azul",
            "Rua H",
            300_000m,
            0m,
            180_000m,
            0m,
            0,
            0m));

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            fx.Properties.AddExpenseAsync(property.Id, new CreatePropertyExpenseRequest(
                500m,
                Guid.NewGuid(),
                "Pintura",
                null,
                false,
                null,
                null)));

        Assert.Equal("Tipo de custo inválido ou inativo.", exception.Message);
    }
}
