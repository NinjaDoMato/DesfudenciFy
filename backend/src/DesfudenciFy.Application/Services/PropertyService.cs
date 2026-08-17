using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Entities;
using DesfudenciFy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.Application.Services;

public class PropertyService
{
    private readonly IAppDbContext _db;
    private readonly IFileStorage _fileStorage;
    private readonly BalanceService _balance;

    public PropertyService(IAppDbContext db, IFileStorage fileStorage, BalanceService balance)
    {
        _db = db;
        _fileStorage = fileStorage;
        _balance = balance;
    }

    public async Task<IReadOnlyList<PropertyDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var properties = await _db.Properties
            .Include(p => p.Amortizations)
            .Include(p => p.Expenses)
                .ThenInclude(e => e.ExpenseType)
            .Include(p => p.RentPayments)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
        return properties.Select(Map).ToList();
    }

    public async Task<PropertyDto> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await LoadAsync(id, cancellationToken));

    public async Task<PropertyDto> CreateAsync(CreatePropertyRequest request, CancellationToken cancellationToken = default)
    {
        var property = new Property
        {
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            AppraisedValue = request.AppraisedValue,
            RentalAmount = request.RentalAmount,
            InitialFinancingAmount = request.InitialFinancingAmount,
            InstallmentAmount = request.InstallmentAmount,
            RemainingInstallments = request.RemainingInstallments,
            RemainingBalance = request.RemainingBalance,
            IsRented = false
        };
        _db.Add(property);
        await _db.SaveChangesAsync(cancellationToken);

        await SyncInstallmentFixedCostAsync(property, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Map(await LoadAsync(property.Id, cancellationToken));
    }

    public async Task<PropertyDto> UpdateAsync(Guid id, UpdatePropertyRequest request, CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(id, cancellationToken);
        property.Name = request.Name.Trim();
        property.Address = request.Address.Trim();
        property.AppraisedValue = request.AppraisedValue;
        property.RentalAmount = request.RentalAmount;
        property.InitialFinancingAmount = request.InitialFinancingAmount;
        property.InstallmentAmount = request.InstallmentAmount;
        property.RemainingInstallments = request.RemainingInstallments;
        property.RemainingBalance = request.RemainingBalance;
        property.IsRented = request.IsRented;

        await SyncInstallmentFixedCostAsync(property, cancellationToken);
        await SyncRentalIncomeAsync(property, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Map(await LoadAsync(id, cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(property.PhotoPath))
        {
            await _fileStorage.DeleteAsync(property.PhotoPath, cancellationToken);
        }

        var linkedCosts = await _db.FixedCosts.Where(c => c.PropertyId == id).ToListAsync(cancellationToken);
        foreach (var cost in linkedCosts)
        {
            cost.PropertyId = null;
            cost.IsActive = false;
        }

        var linkedIncomes = await _db.IncomeSources.Where(i => i.PropertyId == id).ToListAsync(cancellationToken);
        foreach (var income in linkedIncomes)
        {
            income.PropertyId = null;
            income.IsActive = false;
        }

        _db.Remove(property);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PropertyDto> UploadPhotoAsync(Guid id, Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(property.PhotoPath))
        {
            await _fileStorage.DeleteAsync(property.PhotoPath, cancellationToken);
        }

        property.PhotoPath = await _fileStorage.SavePropertyPhotoAsync(id, content, fileName, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(id, cancellationToken));
    }

    public async Task<(string AbsolutePath, string ContentType)> GetPhotoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(id, cancellationToken);
        if (string.IsNullOrWhiteSpace(property.PhotoPath))
        {
            throw new NotFoundException("Foto do imóvel não encontrada.");
        }

        var absolute = _fileStorage.GetAbsolutePath(property.PhotoPath);
        if (!File.Exists(absolute))
        {
            throw new NotFoundException("Arquivo da foto do imóvel não encontrado.");
        }

        var contentType = Path.GetExtension(absolute).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return (absolute, contentType);
    }

    public async Task<PropertyAmortizationDto> AmortizeAsync(
        Guid propertyId,
        CreateAmortizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(propertyId, cancellationToken);

        var installmentsAmortized = request.InstallmentsAmortized;
        if (installmentsAmortized < 0)
        {
            throw new AppException("A quantidade de parcelas amortizadas não pode ser negativa.");
        }

        var amount = request.Amount;
        if (amount <= 0 && installmentsAmortized > 0 && property.InstallmentAmount > 0)
        {
            amount = installmentsAmortized * property.InstallmentAmount;
        }

        if (amount <= 0)
        {
            throw new AppException("O valor da amortização deve ser maior que zero.");
        }

        if (amount > property.RemainingBalance)
        {
            throw new AppException("O valor da amortização não pode ser maior que o saldo restante.");
        }

        if (installmentsAmortized > property.RemainingInstallments)
        {
            throw new AppException("A quantidade de parcelas amortizadas não pode ser maior que as parcelas restantes.");
        }

        Guid? entryId = null;

        if (request.DebitCash)
        {
            if (request.CashDestination is null)
            {
                throw new AppException("Informe a origem do débito quando a opção Debitar do caixa estiver ativa.");
            }

            await _balance.EnsureAvailableAsync(request.CashDestination.Value, request.ReserveId, amount, cancellationToken);

            var entry = new Entry
            {
                Amount = -amount,
                Observation = request.Observation?.Trim().Length > 0
                    ? request.Observation.Trim()
                    : $"Amortização do imóvel - {property.Name}",
                OccurredAt = request.PaidAt?.ToUniversalTime() ?? DateTime.UtcNow,
                Destination = request.CashDestination.Value,
                ReserveId = request.CashDestination == EntryDestination.Reserve ? request.ReserveId : null
            };
            _db.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
            entryId = entry.Id;
        }

        property.RemainingInstallments = Math.Max(0, property.RemainingInstallments - installmentsAmortized);
        property.RemainingBalance = Math.Max(0, property.RemainingBalance - amount);

        var amortization = new PropertyAmortization
        {
            PropertyId = property.Id,
            Amount = amount,
            InstallmentsAmortized = installmentsAmortized,
            PaidAt = request.PaidAt?.ToUniversalTime() ?? DateTime.UtcNow,
            Observation = request.Observation,
            EntryId = entryId
        };
        _db.Add(amortization);

        await SyncInstallmentFixedCostAsync(property, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new PropertyAmortizationDto(
            amortization.Id,
            amortization.Amount,
            amortization.InstallmentsAmortized,
            amortization.PaidAt,
            amortization.Observation,
            amortization.EntryId);
    }

    public async Task DeleteAmortizationAsync(Guid propertyId, Guid amortizationId, CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(propertyId, cancellationToken);
        var amortization = property.Amortizations.FirstOrDefault(a => a.Id == amortizationId)
                           ?? throw new NotFoundException("Amortização não encontrada.");

        property.RemainingBalance += amortization.Amount;
        property.RemainingInstallments += amortization.InstallmentsAmortized;

        if (amortization.EntryId.HasValue)
        {
            var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Id == amortization.EntryId, cancellationToken);
            if (entry is not null)
            {
                _db.Remove(entry);
            }
        }

        _db.Remove(amortization);
        await SyncInstallmentFixedCostAsync(property, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PropertyExpenseDto> AddExpenseAsync(
        Guid propertyId,
        CreatePropertyExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(propertyId, cancellationToken);
        if (request.Amount <= 0)
        {
            throw new AppException("O valor do gasto deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Observation))
        {
            throw new AppException("Informe a observação do gasto.");
        }

        var expenseType = await _db.PropertyExpenseTypes
            .FirstOrDefaultAsync(t => t.Id == request.ExpenseTypeId && t.IsActive, cancellationToken)
            ?? throw new AppException("Tipo de custo inválido ou inativo.");

        Guid? entryId = null;
        if (request.DebitCash)
        {
            if (request.CashDestination is null)
            {
                throw new AppException("Informe a origem do débito quando a opção Debitar do caixa estiver ativa.");
            }

            await _balance.EnsureAvailableAsync(request.CashDestination.Value, request.ReserveId, request.Amount, cancellationToken);

            var entry = new Entry
            {
                Amount = -request.Amount,
                Observation = request.Observation.Trim(),
                OccurredAt = request.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow,
                Destination = request.CashDestination.Value,
                ReserveId = request.CashDestination == EntryDestination.Reserve ? request.ReserveId : null
            };
            _db.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
            entryId = entry.Id;
        }

        var expense = new PropertyExpense
        {
            PropertyId = property.Id,
            ExpenseTypeId = expenseType.Id,
            Amount = request.Amount,
            Observation = request.Observation.Trim(),
            OccurredAt = request.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow,
            EntryId = entryId
        };
        _db.Add(expense);
        await _db.SaveChangesAsync(cancellationToken);

        return new PropertyExpenseDto(
            expense.Id,
            expense.Amount,
            expenseType.Id,
            expenseType.Name,
            expense.Observation,
            expense.OccurredAt,
            expense.EntryId);
    }

    public async Task DeleteExpenseAsync(Guid propertyId, Guid expenseId, CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(propertyId, cancellationToken);
        var expense = property.Expenses.FirstOrDefault(e => e.Id == expenseId)
                      ?? throw new NotFoundException("Gasto do imóvel não encontrado.");

        if (expense.EntryId.HasValue)
        {
            var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Id == expense.EntryId, cancellationToken);
            if (entry is not null)
            {
                _db.Remove(entry);
            }
        }

        _db.Remove(expense);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PropertyRentPaymentDto> AddRentPaymentAsync(
        Guid propertyId,
        CreatePropertyRentPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(propertyId, cancellationToken);
        if (request.Amount <= 0)
        {
            throw new AppException("O valor do aluguel recebido deve ser maior que zero.");
        }

        var observation = string.IsNullOrWhiteSpace(request.Observation)
            ? $"Aluguel - {property.Name}"
            : request.Observation.Trim();

        var entry = new Entry
        {
            Amount = request.Amount,
            Observation = observation,
            OccurredAt = request.PaidAt?.ToUniversalTime() ?? DateTime.UtcNow,
            Destination = EntryDestination.FreeBalance
        };
        _db.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        var payment = new PropertyRentPayment
        {
            PropertyId = property.Id,
            Amount = request.Amount,
            Observation = string.IsNullOrWhiteSpace(request.Observation) ? null : request.Observation.Trim(),
            PaidAt = request.PaidAt?.ToUniversalTime() ?? DateTime.UtcNow,
            EntryId = entry.Id
        };
        _db.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        return new PropertyRentPaymentDto(payment.Id, payment.Amount, payment.Observation, payment.PaidAt, payment.EntryId);
    }

    public async Task DeleteRentPaymentAsync(Guid propertyId, Guid paymentId, CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(propertyId, cancellationToken);
        var payment = property.RentPayments.FirstOrDefault(p => p.Id == paymentId)
                      ?? throw new NotFoundException("Pagamento de aluguel não encontrado.");

        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Id == payment.EntryId, cancellationToken);
        if (entry is not null)
        {
            _db.Remove(entry);
        }

        _db.Remove(payment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncInstallmentFixedCostAsync(Property property, CancellationToken cancellationToken)
    {
        var linkedCosts = await _db.FixedCosts
            .Where(c => c.PropertyId == property.Id)
            .ToListAsync(cancellationToken);

        var activeCost = linkedCosts.FirstOrDefault(c => c.IsActive) ?? linkedCosts.OrderByDescending(c => c.DateCreated).FirstOrDefault();

        var shouldHaveCost = property.RemainingInstallments > 0 && property.InstallmentAmount > 0;
        if (!shouldHaveCost)
        {
            foreach (var cost in linkedCosts.Where(c => c.IsActive))
            {
                cost.IsActive = false;
            }

            return;
        }

        if (activeCost is null)
        {
            _db.Add(new FixedCost
            {
                Name = $"Parcela - {property.Name}",
                Description = $"Parcela do financiamento do imóvel {property.Name}",
                Amount = property.InstallmentAmount,
                Recurrence = CostRecurrence.Month,
                DueDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
                IsActive = true,
                PropertyId = property.Id
            });
            return;
        }

        activeCost.IsActive = true;
        activeCost.Amount = property.InstallmentAmount;
        activeCost.Name = $"Parcela - {property.Name}";
        activeCost.Description = $"Parcela do financiamento do imóvel {property.Name}";
        activeCost.Recurrence = CostRecurrence.Month;
    }

    private async Task SyncRentalIncomeAsync(Property property, CancellationToken cancellationToken)
    {
        var linkedIncomes = await _db.IncomeSources
            .Where(i => i.PropertyId == property.Id)
            .ToListAsync(cancellationToken);

        var income = linkedIncomes.OrderByDescending(i => i.DateCreated).FirstOrDefault();

        if (!property.IsRented)
        {
            foreach (var item in linkedIncomes.Where(i => i.IsActive))
            {
                item.IsActive = false;
            }

            return;
        }

        if (property.RentalAmount <= 0)
        {
            throw new AppException("Informe o valor do aluguel ao marcar o imóvel como alugado.");
        }

        var rentalType = await _db.IncomeTypes.FirstOrDefaultAsync(
            t => t.Name == "Aluguel" && t.IsActive,
            cancellationToken)
            ?? await _db.IncomeTypes.FirstOrDefaultAsync(t => t.Name == "Aluguel", cancellationToken)
            ?? throw new AppException("Tipo de entrada 'Aluguel' não encontrado. Configure os tipos de entrada.");

        if (income is null)
        {
            _db.Add(new IncomeSource
            {
                Name = $"Aluguel - {property.Name}",
                Amount = property.RentalAmount,
                Description = $"Aluguel do imóvel {property.Name}",
                IsActive = true,
                IncomeTypeId = rentalType.Id,
                PropertyId = property.Id
            });
            return;
        }

        income.IsActive = true;
        income.Name = $"Aluguel - {property.Name}";
        income.Amount = property.RentalAmount;
        income.Description = $"Aluguel do imóvel {property.Name}";
        income.IncomeTypeId = rentalType.Id;
    }

    private async Task<Property> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Properties
            .Include(p => p.Amortizations)
            .Include(p => p.Expenses)
                .ThenInclude(e => e.ExpenseType)
            .Include(p => p.RentPayments)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
        ?? throw new NotFoundException("Imóvel não encontrado.");

    private static PropertyDto Map(Property property)
    {
        var expenseAmounts = property.Expenses.Select(e => e.Amount);
        var rentAmounts = property.RentPayments.Select(r => r.Amount);
        var totalExpenses = expenseAmounts.Sum();
        var totalRentPaid = rentAmounts.Sum();
        var propertyCost = PropertyEconomics.CalculateCost(property.InitialFinancingAmount, expenseAmounts);
        var propertyReturn = PropertyEconomics.CalculateReturn(property.AppraisedValue, propertyCost, rentAmounts);

        return new(
            property.Id,
            property.Name,
            property.Address,
            string.IsNullOrWhiteSpace(property.PhotoPath) ? null : $"/api/v1/properties/{property.Id}/photo",
            property.IsRented,
            property.AppraisedValue,
            property.RentalAmount,
            property.InitialFinancingAmount,
            property.InstallmentAmount,
            property.RemainingInstallments,
            property.RemainingBalance,
            totalExpenses,
            totalRentPaid,
            propertyCost,
            propertyReturn,
            property.Amortizations
                .OrderByDescending(a => a.PaidAt)
                .Select(a => new PropertyAmortizationDto(a.Id, a.Amount, a.InstallmentsAmortized, a.PaidAt, a.Observation, a.EntryId))
                .ToList(),
            property.Expenses
                .OrderByDescending(e => e.OccurredAt)
                .Select(e => new PropertyExpenseDto(
                    e.Id,
                    e.Amount,
                    e.ExpenseTypeId,
                    e.ExpenseType.Name,
                    e.Observation,
                    e.OccurredAt,
                    e.EntryId))
                .ToList(),
            property.RentPayments
                .OrderByDescending(r => r.PaidAt)
                .Select(r => new PropertyRentPaymentDto(r.Id, r.Amount, r.Observation, r.PaidAt, r.EntryId))
                .ToList());
    }
}
