namespace LegacyMigrator;

internal sealed record LegacyUserRow(
    Guid Id,
    string Email,
    string PasswordHash,
    DateTime? LastLoginAt,
    DateTime DateCreated,
    DateTime? LastUpdate);

internal sealed record LegacyReserveRow(
    Guid Id,
    string Name,
    string Description,
    int Owner,
    decimal Goal,
    string? DisplayColor,
    decimal? MonthlyGoal,
    DateTime DateCreated,
    DateTime? LastUpdate);

internal sealed record LegacyEntryRow(
    Guid Id,
    decimal Amount,
    string Observation,
    Guid ReserveId,
    DateTime DateCreated,
    DateTime? LastUpdate);

internal sealed record LegacyInvestmentRow(
    Guid Id,
    string Name,
    decimal StartAmount,
    decimal CurrentAmount,
    decimal Rentability,
    int Type,
    DateTime EndDate,
    int Account,
    DateTime DateCreated,
    DateTime? LastUpdate);

internal sealed record LegacyReserveInvestmentRow(
    Guid Id,
    Guid ReserveId,
    Guid InvestmentId,
    decimal Amount,
    DateTime DateCreated,
    DateTime? LastUpdate);

internal sealed record LegacyCostRow(
    Guid Id,
    decimal Amount,
    int Type,
    string Name,
    string Description,
    decimal DanielPercentage,
    decimal CassiaPercentage,
    Guid? ReserveId,
    DateTime DateCreated,
    DateTime? LastUpdate);

internal sealed record LegacyPaymentRow(
    Guid Id,
    decimal PaidAmount,
    DateTime DatePaid,
    Guid CostId,
    DateTime DateCreated,
    DateTime? LastUpdate);

internal sealed record LegacyIncomeSourceRow(
    Guid Id,
    string Name,
    decimal Amount,
    int Owner,
    string Description,
    bool IsActive,
    DateTime DateCreated,
    DateTime? LastUpdate);

internal sealed record LegacyPurchaseRow(
    Guid Id,
    string Name,
    string ProductUrl,
    int Owner,
    DateTime DateCreated,
    DateTime? LastUpdate);

internal sealed record LegacyInstallmentRow(
    Guid Id,
    Guid PurchaseId,
    decimal Amount,
    int InstallmentNumber,
    bool Paid,
    DateTime DueDate,
    DateTime? PaidDate,
    string PaymentUrl,
    DateTime DateCreated,
    DateTime? LastUpdate);

internal sealed class LegacySnapshot
{
    public List<LegacyUserRow> Users { get; } = [];
    public List<LegacyReserveRow> Reserves { get; } = [];
    public List<LegacyEntryRow> Entries { get; } = [];
    public List<LegacyInvestmentRow> Investments { get; } = [];
    public List<LegacyReserveInvestmentRow> ReserveInvestments { get; } = [];
    public List<LegacyCostRow> Costs { get; } = [];
    public List<LegacyPaymentRow> Payments { get; } = [];
    public List<LegacyIncomeSourceRow> IncomeSources { get; } = [];
    public List<LegacyPurchaseRow> Purchases { get; } = [];
    public List<LegacyInstallmentRow> Installments { get; } = [];
    public List<string> Notes { get; } = [];
}
