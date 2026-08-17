using DesfudenciFy.Domain.Enums;

namespace DesfudenciFy.Application.DTOs;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, Guid UserId, string Email, string FullName, string Role);

public record UserDto(Guid Id, string Email, string FullName, bool IsActive, string Role, DateTime? LastLoginAt);
public record CreateUserRequest(string Email, string Password, string FullName, string Role);
public record UpdateUserRequest(string Email, string FullName, bool IsActive, string Role, string? Password);

public record BankAccountDto(Guid Id, string Name, string? Description, bool IsActive);
public record UpsertBankAccountRequest(string Name, string? Description, bool IsActive);

public record InvestmentTypeDto(Guid Id, string Name, string? Description, bool IsActive);
public record UpsertInvestmentTypeRequest(string Name, string? Description, bool IsActive);

public record ReserveDto(
    Guid Id,
    string Name,
    string Description,
    decimal Goal,
    string? DisplayColor,
    decimal? MonthlyGoal,
    decimal CurrentValue,
    decimal InvestedValue,
    decimal AvailableValue);

public record UpsertReserveRequest(string Name, string Description, decimal Goal, string? DisplayColor, decimal? MonthlyGoal);

public record EntryDto(
    Guid Id,
    decimal Amount,
    string Observation,
    DateTime OccurredAt,
    EntryDestination Destination,
    Guid? ReserveId,
    string? ReserveName);

public record CreateEntryRequest(
    decimal Amount,
    string Observation,
    DateTime? OccurredAt,
    EntryDestination Destination,
    Guid? ReserveId);

public record TransferRequest(
    EntryDestination SourceDestination,
    Guid? SourceReserveId,
    EntryDestination TargetDestination,
    Guid? TargetReserveId,
    decimal Amount,
    string? Observation);

public record ReserveAllocationDto(Guid? ReserveId, decimal Amount);

public record InvestmentDto(
    Guid Id,
    string Name,
    string Rentability,
    decimal StartAmount,
    decimal CurrentAmount,
    DateTime StartDate,
    DateTime? EndDate,
    Guid BankAccountId,
    string BankAccountName,
    Guid InvestmentTypeId,
    string InvestmentTypeName,
    string Status,
    IReadOnlyList<ReserveAllocationDto> SourceReserves);

public record CreateInvestmentRequest(
    string Name,
    string? Rentability,
    DateTime StartDate,
    DateTime? EndDate,
    Guid BankAccountId,
    Guid InvestmentTypeId,
    IReadOnlyList<ReserveAllocationDto> SourceReserves);

public record UpdateInvestmentRequest(
    string Name,
    string? Rentability,
    DateTime StartDate,
    DateTime? EndDate,
    Guid BankAccountId,
    Guid InvestmentTypeId,
    IReadOnlyList<ReserveAllocationDto> SourceReserves);

public record UpdateCurrentAmountRequest(decimal CurrentAmount);

public record PropertyDto(
    Guid Id,
    string Name,
    string Address,
    string? PhotoUrl,
    bool IsRented,
    decimal AppraisedValue,
    decimal RentalAmount,
    decimal InitialFinancingAmount,
    decimal InstallmentAmount,
    int RemainingInstallments,
    decimal RemainingBalance,
    decimal TotalExpenses,
    decimal TotalRentPaid,
    decimal PropertyCost,
    decimal PropertyReturn,
    IReadOnlyList<PropertyAmortizationDto> Amortizations,
    IReadOnlyList<PropertyExpenseDto> Expenses,
    IReadOnlyList<PropertyRentPaymentDto> RentPayments);

public record PropertyAmortizationDto(
    Guid Id,
    decimal Amount,
    int InstallmentsAmortized,
    DateTime PaidAt,
    string? Observation,
    Guid? EntryId);

public record PropertyExpenseDto(
    Guid Id,
    decimal Amount,
    Guid ExpenseTypeId,
    string ExpenseTypeName,
    string Observation,
    DateTime OccurredAt,
    Guid? EntryId);

public record PropertyRentPaymentDto(
    Guid Id,
    decimal Amount,
    string? Observation,
    DateTime PaidAt,
    Guid EntryId);

public record CreatePropertyRequest(
    string Name,
    string Address,
    decimal AppraisedValue,
    decimal RentalAmount,
    decimal InitialFinancingAmount,
    decimal InstallmentAmount,
    int RemainingInstallments,
    decimal RemainingBalance);

public record UpdatePropertyRequest(
    string Name,
    string Address,
    bool IsRented,
    decimal AppraisedValue,
    decimal RentalAmount,
    decimal InitialFinancingAmount,
    decimal InstallmentAmount,
    int RemainingInstallments,
    decimal RemainingBalance);

public record CreateAmortizationRequest(
    decimal Amount,
    int InstallmentsAmortized,
    DateTime? PaidAt,
    string? Observation,
    bool DebitCash,
    EntryDestination? CashDestination,
    Guid? ReserveId);

public record CreatePropertyExpenseRequest(
    decimal Amount,
    Guid ExpenseTypeId,
    string Observation,
    DateTime? OccurredAt,
    bool DebitCash,
    EntryDestination? CashDestination,
    Guid? ReserveId);

public record CreatePropertyRentPaymentRequest(
    decimal Amount,
    string? Observation,
    DateTime? PaidAt);

public record FixedCostDto(
    Guid Id,
    string Name,
    string Description,
    decimal Amount,
    string Recurrence,
    DateTime? DueDate,
    Guid? ReserveId,
    string? ReserveName,
    bool IsActive,
    Guid? PropertyId,
    IReadOnlyList<CostPaymentDto> Payments);

public record CostPaymentDto(Guid Id, decimal PaidAmount, DateTime DatePaid, Guid? EntryId);
public record UpsertFixedCostRequest(string Name, string Description, decimal Amount, string Recurrence, DateTime? DueDate, Guid? ReserveId);
public record CreateCostPaymentRequest(decimal PaidAmount, DateTime? DatePaid);

public record IncomeTypeDto(Guid Id, string Name, string? Description, bool IsActive);
public record UpsertIncomeTypeRequest(string Name, string? Description, bool IsActive);

public record PropertyExpenseTypeDto(Guid Id, string Name, string? Description, bool IsActive);
public record UpsertPropertyExpenseTypeRequest(string Name, string? Description, bool IsActive);

public record IncomeSourceDto(
    Guid Id,
    string Name,
    decimal Amount,
    string Description,
    bool IsActive,
    Guid IncomeTypeId,
    string IncomeTypeName,
    Guid? PropertyId);

public record UpsertIncomeSourceRequest(
    string Name,
    decimal Amount,
    string Description,
    bool IsActive,
    Guid IncomeTypeId);

public record PurchaseDto(
    Guid Id,
    string Name,
    string? ProductUrl,
    string DebitSource,
    Guid? ReserveId,
    string? ReserveName,
    IReadOnlyList<InstallmentDto> Installments);
public record InstallmentDto(
    Guid Id,
    decimal Amount,
    int InstallmentNumber,
    bool Paid,
    DateTime DueDate,
    DateTime? PaidDate,
    string? PaymentUrl,
    Guid? EntryId);
public record CreatePurchaseRequest(
    string Name,
    string? ProductUrl,
    decimal TotalAmount,
    int InstallmentCount,
    DateTime? FirstDueDate,
    Guid? ReserveId = null,
    string DebitSource = "None");
public record UpdatePurchaseRequest(string Name, string? ProductUrl, Guid? ReserveId = null, string DebitSource = "None");

public record DashboardTotalsDto(
    decimal TotalAccumulated,
    decimal TotalFreeBalance,
    decimal TotalInvested,
    decimal TotalIncome,
    decimal TotalFixedCosts,
    decimal MonthlyInvestmentGoal,
    decimal MonthlyBalance,
    decimal TotalPropertyRemainingBalance,
    decimal TotalFinancialCapital,
    decimal TotalPropertyAppraisedValue);

public record MonthlyCapitalDto(string Month, decimal FreeCapital, decimal InvestedCapital);
public record ReserveDistributionDto(Guid ReserveId, string Name, decimal Value, string? Color);
public record InvestmentTypeDistributionDto(Guid InvestmentTypeId, string Name, decimal Value);
public record UpcomingInvestmentDto(Guid Id, string Name, DateTime EndDate, decimal CurrentAmount);
public record UpcomingBillDto(string Kind, Guid Id, string Name, decimal Amount, DateTime? DueDate, Guid? TargetId = null);
