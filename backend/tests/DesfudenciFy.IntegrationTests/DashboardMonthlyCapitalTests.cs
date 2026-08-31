using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using DesfudenciFy.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.IntegrationTests;

public class DashboardMonthlyCapitalTests
{
    [Fact]
    public async Task Property_value_should_start_contributing_from_creation_month()
    {
        await using var fx = new TestDbFixture();

        var now = DateTime.UtcNow.Date;
        var start = now.AddMonths(-11);
        start = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var createdInMonthIndex = 5;
        var monthStart = start.AddMonths(createdInMonthIndex);
        var createdDate = monthStart.AddDays(10); // Still inside the same month bucket.

        var property = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            Name: "Imovel A",
            Address: "Rua A",
            AppraisedValue: 100m,
            RentalAmount: 0m,
            InitialFinancingAmount: 0m,
            InstallmentAmount: 0m,
            RemainingInstallments: 0,
            RemainingBalance: 0m));

        var createdEntity = await fx.Db.Properties.SingleAsync(p => p.Id == property.Id);
        createdEntity.DateCreated = createdDate;
        await fx.Db.SaveChangesAsync();

        var dashboard = new DashboardService(fx.AppDb, fx.Balance);
        var monthly = await dashboard.GetMonthlyCapitalAsync();

        Assert.Equal(12, monthly.Count);

        for (var i = 0; i < monthly.Count; i++)
        {
            var expected = i < createdInMonthIndex ? 0m : 100m;
            Assert.Equal(expected, monthly[i].PropertyValue);
        }
    }

    [Fact]
    public async Task Property_value_should_accumulate_multiple_properties_by_creation_date()
    {
        await using var fx = new TestDbFixture();

        var now = DateTime.UtcNow.Date;
        var start = now.AddMonths(-11);
        start = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var createdInMonthIndex1 = 2;
        var createdInMonthIndex2 = 7;

        var monthStart1 = start.AddMonths(createdInMonthIndex1);
        var createdDate1 = monthStart1.AddDays(3);

        var monthStart2 = start.AddMonths(createdInMonthIndex2);
        var createdDate2 = monthStart2.AddDays(12);

        var property1 = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            Name: "Imovel B",
            Address: "Rua B",
            AppraisedValue: 100m,
            RentalAmount: 0m,
            InitialFinancingAmount: 0m,
            InstallmentAmount: 0m,
            RemainingInstallments: 0,
            RemainingBalance: 0m));

        var property2 = await fx.Properties.CreateAsync(new CreatePropertyRequest(
            Name: "Imovel C",
            Address: "Rua C",
            AppraisedValue: 50m,
            RentalAmount: 0m,
            InitialFinancingAmount: 0m,
            InstallmentAmount: 0m,
            RemainingInstallments: 0,
            RemainingBalance: 0m));

        var entity1 = await fx.Db.Properties.SingleAsync(p => p.Id == property1.Id);
        entity1.DateCreated = createdDate1;

        var entity2 = await fx.Db.Properties.SingleAsync(p => p.Id == property2.Id);
        entity2.DateCreated = createdDate2;

        await fx.Db.SaveChangesAsync();

        var dashboard = new DashboardService(fx.AppDb, fx.Balance);
        var monthly = await dashboard.GetMonthlyCapitalAsync();

        Assert.Equal(12, monthly.Count);

        for (var i = 0; i < monthly.Count; i++)
        {
            var expected = 0m;
            if (i >= createdInMonthIndex1) expected += 100m;
            if (i >= createdInMonthIndex2) expected += 50m;

            Assert.Equal(expected, monthly[i].PropertyValue);
        }
    }

    [Fact]
    public async Task Vehicle_value_should_start_contributing_from_creation_month()
    {
        await using var fx = new TestDbFixture();

        var now = DateTime.UtcNow.Date;
        var start = now.AddMonths(-11);
        start = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var createdInMonthIndex = 4;
        var monthStart = start.AddMonths(createdInMonthIndex);
        var createdDate = monthStart.AddDays(8);

        var vehicle = await fx.Vehicles.CreateAsync(new CreateVehicleRequest(
            Name: "Civic",
            Model: "Honda Civic",
            Year: 2020,
            PaidValue: 90_000m,
            FipeValue: 75_000m));

        var createdEntity = await fx.Db.Vehicles.SingleAsync(v => v.Id == vehicle.Id);
        createdEntity.DateCreated = createdDate;
        await fx.Db.SaveChangesAsync();

        var dashboard = new DashboardService(fx.AppDb, fx.Balance);
        var monthly = await dashboard.GetMonthlyCapitalAsync();

        Assert.Equal(12, monthly.Count);

        for (var i = 0; i < monthly.Count; i++)
        {
            var expected = i < createdInMonthIndex ? 0m : 75_000m;
            Assert.Equal(expected, monthly[i].VehicleValue);
        }
    }
}

