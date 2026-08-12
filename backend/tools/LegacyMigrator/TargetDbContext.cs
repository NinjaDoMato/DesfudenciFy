using DesfudenciFy.Domain.Entities;
using DesfudenciFy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegacyMigrator;

/// <summary>
/// Contexto de escrita que reutiliza as configurações do app, sem sobrescrever DateCreated/LastUpdate.
/// </summary>
internal sealed class TargetDbContext : DbContext
{
    public TargetDbContext(DbContextOptions<TargetDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<InvestmentType> InvestmentTypes => Set<InvestmentType>();
    public DbSet<IncomeType> IncomeTypes => Set<IncomeType>();
    public DbSet<Reserve> Reserves => Set<Reserve>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<ReserveInvestment> ReserveInvestments => Set<ReserveInvestment>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyAmortization> PropertyAmortizations => Set<PropertyAmortization>();
    public DbSet<PropertyExpense> PropertyExpenses => Set<PropertyExpense>();
    public DbSet<PropertyRentPayment> PropertyRentPayments => Set<PropertyRentPayment>();
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
}
