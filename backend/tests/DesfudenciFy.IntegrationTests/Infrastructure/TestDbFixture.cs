using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Application.Services;
using DesfudenciFy.Domain.Entities;
using DesfudenciFy.Domain.Enums;
using DesfudenciFy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.IntegrationTests.Infrastructure;

public sealed class TestDbFixture : IAsyncDisposable
{
    public AppDbContext Db { get; }
    public IAppDbContext AppDb { get; }
    public BalanceService Balance { get; }
    public EntryService Entries { get; }
    public ReserveService Reserves { get; }
    public InvestmentService Investments { get; }
    public PropertyService Properties { get; }
    public VehicleService Vehicles { get; }
    public FixedCostService FixedCosts { get; }
    public PurchaseService Purchases { get; }
    public IncomeSourceService IncomeSources { get; }

    public TestDbFixture()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"desfudencify-tests-{Guid.NewGuid()}")
            .Options;

        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();

        AppDb = new AppDbContextAdapter(Db);
        Balance = new BalanceService(AppDb);
        Entries = new EntryService(AppDb, Balance);
        Reserves = new ReserveService(AppDb, Balance);
        Investments = new InvestmentService(AppDb, Balance);
        Properties = new PropertyService(AppDb, new NoOpFileStorage(), Balance);
        Vehicles = new VehicleService(AppDb, new NoOpFileStorage(), Balance);
        FixedCosts = new FixedCostService(AppDb, Balance);
        Purchases = new PurchaseService(AppDb, Balance);
        IncomeSources = new IncomeSourceService(AppDb);

        SeedCatalogTypes();
    }

    private void SeedCatalogTypes()
    {
        Db.IncomeTypes.AddRange(
            new IncomeType { Name = "Salário", IsActive = true },
            new IncomeType { Name = "Vale Refeição", IsActive = true },
            new IncomeType { Name = "Vale Alimentação", IsActive = true },
            new IncomeType { Name = "Aluguel", IsActive = true },
            new IncomeType { Name = "Renda extra", IsActive = true });
        Db.PropertyExpenseTypes.AddRange(
            new PropertyExpenseType { Name = "Leilão", IsActive = true },
            new PropertyExpenseType { Name = "Material", IsActive = true },
            new PropertyExpenseType { Name = "Serviços", IsActive = true },
            new PropertyExpenseType { Name = "Documentação", IsActive = true });
        Db.VehicleExpenseTypes.AddRange(
            new VehicleExpenseType { Name = "Documentação", IsActive = true },
            new VehicleExpenseType { Name = "Impostos", IsActive = true },
            new VehicleExpenseType { Name = "Revisão", IsActive = true },
            new VehicleExpenseType { Name = "Reparos", IsActive = true });
        Db.SaveChanges();
    }

    public async Task<Reserve> SeedReserveAsync(string name = "Reserva")
    {
        var reserve = new Reserve { Name = name, Description = "", Goal = 0 };
        Db.Reserves.Add(reserve);
        await Db.SaveChangesAsync();
        return reserve;
    }

    public async Task CreditFreeAsync(decimal amount, string observation = "Crédito livre")
    {
        Db.Entries.Add(new Entry
        {
            Amount = amount,
            Observation = observation,
            OccurredAt = DateTime.UtcNow,
            Destination = EntryDestination.FreeBalance
        });
        await Db.SaveChangesAsync();
    }

    public async Task CreditReserveAsync(Guid reserveId, decimal amount, string observation = "Crédito reserva")
    {
        var occurredAt = DateTime.UtcNow;
        Db.Entries.Add(new Entry
        {
            Amount = -amount,
            Observation = $"{observation} (saldo livre)",
            OccurredAt = occurredAt,
            Destination = EntryDestination.FreeBalance
        });
        Db.Entries.Add(new Entry
        {
            Amount = amount,
            Observation = observation,
            OccurredAt = occurredAt,
            Destination = EntryDestination.Reserve,
            ReserveId = reserveId
        });
        await Db.SaveChangesAsync();
    }

    public async Task<(BankAccount Bank, InvestmentType Type)> SeedInvestmentCatalogAsync()
    {
        var bank = new BankAccount { Name = "XP", Description = "", IsActive = true };
        var type = new InvestmentType { Name = "CDB", Description = "", IsActive = true };
        Db.BankAccounts.Add(bank);
        Db.InvestmentTypes.Add(type);
        await Db.SaveChangesAsync();
        return (bank, type);
    }

    public ValueTask DisposeAsync()
    {
        Db.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class NoOpFileStorage : IFileStorage
    {
        public Task<string> SavePropertyPhotoAsync(Guid propertyId, Stream content, string fileName, CancellationToken cancellationToken = default) =>
            Task.FromResult($"properties/{propertyId}/{fileName}");

        public Task<string> SaveVehiclePhotoAsync(Guid vehicleId, Stream content, string fileName, CancellationToken cancellationToken = default) =>
            Task.FromResult($"vehicles/{vehicleId}/{fileName}");

        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public string GetAbsolutePath(string relativePath) => relativePath;
    }
}
