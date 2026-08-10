using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.Infrastructure.Persistence;

public class AppDbContextAdapter : IAppDbContext
{
    private readonly AppDbContext _db;

    public AppDbContextAdapter(AppDbContext db)
    {
        _db = db;
    }

    public IQueryable<User> Users => _db.Users;
    public IQueryable<BankAccount> BankAccounts => _db.BankAccounts;
    public IQueryable<InvestmentType> InvestmentTypes => _db.InvestmentTypes;
    public IQueryable<Reserve> Reserves => _db.Reserves;
    public IQueryable<Entry> Entries => _db.Entries;
    public IQueryable<Investment> Investments => _db.Investments;
    public IQueryable<ReserveInvestment> ReserveInvestments => _db.ReserveInvestments;
    public IQueryable<Property> Properties => _db.Properties;
    public IQueryable<PropertyAmortization> PropertyAmortizations => _db.PropertyAmortizations;
    public IQueryable<FixedCost> FixedCosts => _db.FixedCosts;
    public IQueryable<CostPayment> CostPayments => _db.CostPayments;
    public IQueryable<IncomeSource> IncomeSources => _db.IncomeSources;
    public IQueryable<Purchase> Purchases => _db.Purchases;
    public IQueryable<Installment> Installments => _db.Installments;

    public void Add<TEntity>(TEntity entity) where TEntity : class => _db.Set<TEntity>().Add(entity);

    public void Remove<TEntity>(TEntity entity) where TEntity : class => _db.Set<TEntity>().Remove(entity);

    public void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class =>
        _db.Set<TEntity>().RemoveRange(entities);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
