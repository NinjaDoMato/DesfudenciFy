using DesfudenciFy.Domain.Entities;

namespace DesfudenciFy.Application.Abstractions;

public interface IAppDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<BankAccount> BankAccounts { get; }
    IQueryable<InvestmentType> InvestmentTypes { get; }
    IQueryable<IncomeType> IncomeTypes { get; }
    IQueryable<Reserve> Reserves { get; }
    IQueryable<Entry> Entries { get; }
    IQueryable<Investment> Investments { get; }
    IQueryable<ReserveInvestment> ReserveInvestments { get; }
    IQueryable<Property> Properties { get; }
    IQueryable<PropertyAmortization> PropertyAmortizations { get; }
    IQueryable<PropertyExpense> PropertyExpenses { get; }
    IQueryable<PropertyRentPayment> PropertyRentPayments { get; }
    IQueryable<FixedCost> FixedCosts { get; }
    IQueryable<CostPayment> CostPayments { get; }
    IQueryable<IncomeSource> IncomeSources { get; }
    IQueryable<Purchase> Purchases { get; }
    IQueryable<Installment> Installments { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    string GenerateToken(User user);
}

public interface IFileStorage
{
    Task<string> SavePropertyPhotoAsync(Guid propertyId, Stream content, string fileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
    string GetAbsolutePath(string relativePath);
}
