using DesfudenciFy.Domain.Common;
using DesfudenciFy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<InvestmentType> InvestmentTypes => Set<InvestmentType>();
    public DbSet<IncomeType> IncomeTypes => Set<IncomeType>();
    public DbSet<PropertyExpenseType> PropertyExpenseTypes => Set<PropertyExpenseType>();
    public DbSet<VehicleExpenseType> VehicleExpenseTypes => Set<VehicleExpenseType>();
    public DbSet<Reserve> Reserves => Set<Reserve>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<ReserveInvestment> ReserveInvestments => Set<ReserveInvestment>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyAmortization> PropertyAmortizations => Set<PropertyAmortization>();
    public DbSet<PropertyExpense> PropertyExpenses => Set<PropertyExpense>();
    public DbSet<PropertyRentPayment> PropertyRentPayments => Set<PropertyRentPayment>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleExpense> VehicleExpenses => Set<VehicleExpense>();
    public DbSet<FixedCost> FixedCosts => Set<FixedCost>();
    public DbSet<CostPayment> CostPayments => Set<CostPayment>();
    public DbSet<IncomeSource> IncomeSources => Set<IncomeSource>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<Installment> Installments => Set<Installment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.DateCreated = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastUpdate = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
