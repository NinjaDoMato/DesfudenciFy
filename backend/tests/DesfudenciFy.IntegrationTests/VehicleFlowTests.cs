using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Enums;
using DesfudenciFy.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.IntegrationTests;

public class VehicleFlowTests
{
    [Fact]
    public async Task Create_and_update_vehicle_should_persist_fields_and_totals()
    {
        await using var fx = new TestDbFixture();

        var created = await fx.Vehicles.CreateAsync(new CreateVehicleRequest(
            "Civic",
            "Honda Civic EXL",
            2020,
            95_000m,
            88_000m));

        Assert.Equal("Civic", created.Name);
        Assert.Equal("Honda Civic EXL", created.Model);
        Assert.Equal(2020, created.Year);
        Assert.Equal(95_000m, created.PaidValue);
        Assert.Equal(88_000m, created.FipeValue);
        Assert.Equal(0m, created.TotalExpenses);
        Assert.Equal(7_000m, created.FipeVariance);

        var updated = await fx.Vehicles.UpdateAsync(created.Id, new UpdateVehicleRequest(
            "Civic Touring",
            "Honda Civic Touring",
            2021,
            100_000m,
            92_000m));

        Assert.Equal("Civic Touring", updated.Name);
        Assert.Equal("Honda Civic Touring", updated.Model);
        Assert.Equal(2021, updated.Year);
        Assert.Equal(8_000m, updated.FipeVariance);
    }

    [Fact]
    public async Task Add_expense_with_debit_should_create_entry_and_update_totals()
    {
        await using var fx = new TestDbFixture();
        await fx.CreditFreeAsync(5_000m);

        var vehicle = await fx.Vehicles.CreateAsync(new CreateVehicleRequest(
            "Corolla",
            "Toyota Corolla XEi",
            2019,
            80_000m,
            78_000m));

        Assert.Equal(2_000m, vehicle.FipeVariance);

        var expenseType = await fx.Db.VehicleExpenseTypes.SingleAsync(t => t.Name == "Revisão");
        var expense = await fx.Vehicles.AddExpenseAsync(vehicle.Id, new CreateVehicleExpenseRequest(
            1_500m,
            expenseType.Id,
            "Revisão dos 40 mil",
            null,
            true,
            EntryDestination.FreeBalance,
            null));

        Assert.Equal(1_500m, expense.Amount);
        Assert.Equal(expenseType.Id, expense.ExpenseTypeId);
        Assert.Equal("Revisão", expense.ExpenseTypeName);
        Assert.NotNull(expense.EntryId);

        var updated = await fx.Vehicles.GetAsync(vehicle.Id);
        Assert.Equal(1_500m, updated.TotalExpenses);
        Assert.Equal(3_500m, updated.FipeVariance);
        Assert.Equal(3_500m, await fx.Balance.GetFreeBalanceAvailableAsync());
    }

    [Fact]
    public async Task Add_expense_without_debit_should_not_create_entry()
    {
        await using var fx = new TestDbFixture();

        var vehicle = await fx.Vehicles.CreateAsync(new CreateVehicleRequest(
            "Gol",
            "VW Gol 1.0",
            2015,
            30_000m,
            28_000m));

        var expenseType = await fx.Db.VehicleExpenseTypes.SingleAsync(t => t.Name == "Documentação");
        var expense = await fx.Vehicles.AddExpenseAsync(vehicle.Id, new CreateVehicleExpenseRequest(
            800m,
            expenseType.Id,
            "Transferência",
            null,
            false,
            null,
            null));

        Assert.Null(expense.EntryId);
        Assert.Empty(await fx.Db.Entries.ToListAsync());

        var updated = await fx.Vehicles.GetAsync(vehicle.Id);
        Assert.Equal(800m, updated.TotalExpenses);
        Assert.Equal(2_800m, updated.FipeVariance);
    }

    [Fact]
    public async Task Delete_expense_with_entry_should_remove_entry_and_recalculate()
    {
        await using var fx = new TestDbFixture();
        await fx.CreditFreeAsync(3_000m);

        var vehicle = await fx.Vehicles.CreateAsync(new CreateVehicleRequest(
            "Onix",
            "Chevrolet Onix LT",
            2018,
            45_000m,
            42_000m));

        var expenseType = await fx.Db.VehicleExpenseTypes.SingleAsync(t => t.Name == "Reparos");
        var expense = await fx.Vehicles.AddExpenseAsync(vehicle.Id, new CreateVehicleExpenseRequest(
            1_200m,
            expenseType.Id,
            "Troca de embreagem",
            null,
            true,
            EntryDestination.FreeBalance,
            null));

        await fx.Vehicles.DeleteExpenseAsync(vehicle.Id, expense.Id);

        Assert.Empty(await fx.Db.Entries.Where(e => e.Amount < 0).ToListAsync());
        var updated = await fx.Vehicles.GetAsync(vehicle.Id);
        Assert.Equal(0m, updated.TotalExpenses);
        Assert.Equal(3_000m, updated.FipeVariance);
        Assert.Equal(3_000m, await fx.Balance.GetFreeBalanceAvailableAsync());
    }

    [Fact]
    public async Task Delete_vehicle_should_remove_it()
    {
        await using var fx = new TestDbFixture();

        var vehicle = await fx.Vehicles.CreateAsync(new CreateVehicleRequest(
            "Fiesta",
            "Ford Fiesta SE",
            2014,
            25_000m,
            22_000m));

        await fx.Vehicles.DeleteAsync(vehicle.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => fx.Vehicles.GetAsync(vehicle.Id));
    }
}
