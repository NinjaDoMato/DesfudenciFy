using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Entities;
using DesfudenciFy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.Application.Services;

public class FixedCostService
{
    private readonly IAppDbContext _db;
    private readonly BalanceService _balance;

    public FixedCostService(IAppDbContext db, BalanceService balance)
    {
        _db = db;
        _balance = balance;
    }

    public async Task<IReadOnlyList<FixedCostDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.FixedCosts
            .Include(c => c.Reserve)
            .Include(c => c.Payments)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<FixedCostDto> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await LoadAsync(id, cancellationToken));

    public async Task<FixedCostDto> CreateAsync(UpsertFixedCostRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CostRecurrence>(request.Recurrence, true, out var recurrence))
        {
            throw new AppException("Recorrência inválida.");
        }

        var entity = new FixedCost
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Amount = request.Amount,
            Recurrence = recurrence,
            DueDate = NormalizeDueDate(request.DueDate),
            ReserveId = request.ReserveId,
            IsActive = true
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(entity.Id, cancellationToken));
    }

    public async Task<FixedCostDto> UpdateAsync(Guid id, UpsertFixedCostRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CostRecurrence>(request.Recurrence, true, out var recurrence))
        {
            throw new AppException("Recorrência inválida.");
        }

        var entity = await LoadAsync(id, cancellationToken);
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim() ?? string.Empty;
        entity.Amount = request.Amount;
        entity.Recurrence = recurrence;
        entity.DueDate = NormalizeDueDate(request.DueDate);
        entity.ReserveId = request.ReserveId;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(id, cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadAsync(id, cancellationToken);
        _db.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CostPaymentDto> PayAsync(Guid id, CreateCostPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var cost = await LoadAsync(id, cancellationToken);
        Guid? entryId = null;

        if (cost.ReserveId.HasValue)
        {
            await _balance.EnsureAvailableAsync(EntryDestination.Reserve, cost.ReserveId, request.PaidAmount, cancellationToken);
            var entry = new Entry
            {
                Amount = -request.PaidAmount,
                Observation = $"Pagamento - {cost.Name}",
                OccurredAt = request.DatePaid?.ToUniversalTime() ?? DateTime.UtcNow,
                Destination = EntryDestination.Reserve,
                ReserveId = cost.ReserveId
            };
            _db.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);
            entryId = entry.Id;
        }

        var payment = new CostPayment
        {
            FixedCostId = cost.Id,
            PaidAmount = request.PaidAmount,
            DatePaid = request.DatePaid?.ToUniversalTime() ?? DateTime.UtcNow,
            EntryId = entryId
        };
        _db.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        AdvanceDueDate(cost);
        await _db.SaveChangesAsync(cancellationToken);

        return new CostPaymentDto(payment.Id, payment.PaidAmount, payment.DatePaid, payment.EntryId);
    }

    public async Task DeletePaymentAsync(Guid costId, Guid paymentId, CancellationToken cancellationToken = default)
    {
        var cost = await LoadAsync(costId, cancellationToken);
        var payment = cost.Payments.FirstOrDefault(p => p.Id == paymentId)
                      ?? throw new NotFoundException("Pagamento não encontrado.");

        if (payment.EntryId.HasValue)
        {
            var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Id == payment.EntryId, cancellationToken);
            if (entry is not null)
            {
                _db.Remove(entry);
            }
        }

        _db.Remove(payment);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<FixedCost> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.FixedCosts.Include(c => c.Reserve).Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
        ?? throw new NotFoundException("Conta fixa não encontrada.");

    private static DateTime? NormalizeDueDate(DateTime? dueDate) =>
        dueDate.HasValue
            ? DateTime.SpecifyKind(dueDate.Value.Date, DateTimeKind.Utc)
            : null;

    private static void AdvanceDueDate(FixedCost cost)
    {
        if (!cost.DueDate.HasValue) return;

        cost.DueDate = RecurrenceCalculator.AdvanceDueDate(cost.DueDate.Value, cost.Recurrence);
    }

    private static FixedCostDto Map(FixedCost cost) =>
        new(
            cost.Id,
            cost.Name,
            cost.Description,
            cost.Amount,
            cost.Recurrence.ToString(),
            cost.DueDate,
            cost.ReserveId,
            cost.Reserve?.Name,
            cost.IsActive,
            cost.PropertyId,
            cost.Payments
                .OrderByDescending(p => p.DatePaid)
                .Select(p => new CostPaymentDto(p.Id, p.PaidAmount, p.DatePaid, p.EntryId))
                .ToList());
}

public class IncomeSourceService
{
    private readonly IAppDbContext _db;

    public IncomeSourceService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<IncomeSourceDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.IncomeSources
            .Include(i => i.IncomeType)
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .Select(i => new IncomeSourceDto(
                i.Id,
                i.Name,
                i.Amount,
                i.Description,
                i.IsActive,
                i.IncomeTypeId,
                i.IncomeType.Name,
                i.PropertyId))
            .ToListAsync(cancellationToken);

    public async Task<IncomeSourceDto> CreateAsync(UpsertIncomeSourceRequest request, CancellationToken cancellationToken = default)
    {
        var type = await RequireActiveIncomeTypeAsync(request.IncomeTypeId, cancellationToken);
        var entity = new IncomeSource
        {
            Name = request.Name.Trim(),
            Amount = request.Amount,
            Description = request.Description?.Trim() ?? string.Empty,
            IsActive = request.IsActive,
            IncomeTypeId = type.Id
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new IncomeSourceDto(entity.Id, entity.Name, entity.Amount, entity.Description, entity.IsActive, type.Id, type.Name, entity.PropertyId);
    }

    public async Task<IncomeSourceDto> UpdateAsync(Guid id, UpsertIncomeSourceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.IncomeSources.Include(i => i.IncomeType).FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Entrada não encontrada.");
        var type = await RequireActiveIncomeTypeAsync(request.IncomeTypeId, cancellationToken);
        entity.Name = request.Name.Trim();
        entity.Amount = request.Amount;
        entity.Description = request.Description?.Trim() ?? string.Empty;
        entity.IsActive = request.IsActive;
        entity.IncomeTypeId = type.Id;
        await _db.SaveChangesAsync(cancellationToken);
        return new IncomeSourceDto(entity.Id, entity.Name, entity.Amount, entity.Description, entity.IsActive, type.Id, type.Name, entity.PropertyId);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.IncomeSources.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Entrada não encontrada.");
        entity.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IncomeType> RequireActiveIncomeTypeAsync(Guid incomeTypeId, CancellationToken cancellationToken)
    {
        return await _db.IncomeTypes.FirstOrDefaultAsync(t => t.Id == incomeTypeId && t.IsActive, cancellationToken)
               ?? throw new AppException("Tipo de entrada inválido ou inativo.");
    }
}

public class PurchaseService
{
    private readonly IAppDbContext _db;
    private readonly BalanceService _balance;

    public PurchaseService(IAppDbContext db, BalanceService balance)
    {
        _db = db;
        _balance = balance;
    }

    public async Task<IReadOnlyList<PurchaseDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var purchases = await _db.Purchases
            .Include(p => p.Installments)
            .Include(p => p.Reserve)
            .OrderByDescending(p => p.DateCreated)
            .ToListAsync(cancellationToken);
        return purchases.Select(Map).ToList();
    }

    public async Task<PurchaseDto> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await LoadAsync(id, cancellationToken));

    public async Task<PurchaseDto> CreateAsync(CreatePurchaseRequest request, CancellationToken cancellationToken = default)
    {
        var amounts = InstallmentCalculator.SplitTotal(request.TotalAmount, request.InstallmentCount);
        var firstDue = (request.FirstDueDate ?? DateTime.UtcNow).ToUniversalTime();
        var purchase = new Purchase
        {
            Name = request.Name.Trim(),
            ProductUrl = request.ProductUrl
        };
        await ApplyDebitSourceAsync(purchase, request.DebitSource, request.ReserveId, cancellationToken);

        for (var i = 0; i < amounts.Count; i++)
        {
            purchase.Installments.Add(new Installment
            {
                Amount = amounts[i],
                InstallmentNumber = i + 1,
                DueDate = firstDue.AddMonths(i),
                Paid = false
            });
        }

        _db.Add(purchase);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(purchase.Id, cancellationToken));
    }

    public async Task<PurchaseDto> UpdateAsync(Guid id, UpdatePurchaseRequest request, CancellationToken cancellationToken = default)
    {
        var purchase = await LoadAsync(id, cancellationToken);
        await ApplyDebitSourceAsync(purchase, request.DebitSource, request.ReserveId, cancellationToken);
        purchase.Name = request.Name.Trim();
        purchase.ProductUrl = string.IsNullOrWhiteSpace(request.ProductUrl) ? null : request.ProductUrl.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(id, cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var purchase = await LoadAsync(id, cancellationToken);
        await RemoveLinkedEntriesAsync(purchase.Installments, cancellationToken);
        _db.Remove(purchase);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<InstallmentDto> PayInstallmentAsync(Guid purchaseId, Guid installmentId, CancellationToken cancellationToken = default)
    {
        var installment = await LoadInstallmentAsync(purchaseId, installmentId, cancellationToken);
        if (installment.Paid)
        {
            throw new AppException("Parcela já está paga.");
        }

        var debit = ResolveDebit(installment.Purchase);
        if (debit is not null)
        {
            await _balance.EnsureAvailableAsync(
                debit.Value.Destination,
                debit.Value.ReserveId,
                installment.Amount,
                cancellationToken);

            var entry = new Entry
            {
                Amount = -installment.Amount,
                Observation = $"Pagamento parcela {installment.InstallmentNumber} - {installment.Purchase.Name}",
                OccurredAt = DateTime.UtcNow,
                Destination = debit.Value.Destination,
                ReserveId = debit.Value.ReserveId
            };
            _db.Add(entry);
            installment.Entry = entry;
        }

        installment.Paid = true;
        installment.PaidDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return MapInstallment(installment);
    }

    public async Task<InstallmentDto> UnpayInstallmentAsync(Guid purchaseId, Guid installmentId, CancellationToken cancellationToken = default)
    {
        var installment = await LoadInstallmentAsync(purchaseId, installmentId, cancellationToken);
        if (!installment.Paid)
        {
            throw new AppException("Parcela não está paga.");
        }

        await RemoveLinkedEntriesAsync([installment], cancellationToken);
        installment.Paid = false;
        installment.PaidDate = null;
        await _db.SaveChangesAsync(cancellationToken);
        return MapInstallment(installment);
    }

    private async Task<Purchase> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Purchases
            .Include(p => p.Installments)
            .Include(p => p.Reserve)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
        ?? throw new NotFoundException("Parcelamento não encontrado.");

    private async Task<Installment> LoadInstallmentAsync(Guid purchaseId, Guid installmentId, CancellationToken cancellationToken) =>
        await _db.Installments
            .Include(i => i.Purchase)
            .FirstOrDefaultAsync(i => i.Id == installmentId && i.PurchaseId == purchaseId, cancellationToken)
        ?? throw new NotFoundException("Parcela não encontrada.");

    private async Task ApplyDebitSourceAsync(
        Purchase purchase,
        string? debitSource,
        Guid? reserveId,
        CancellationToken cancellationToken)
    {
        var parsed = ParseDebitSource(debitSource, reserveId);
        if (parsed == PurchaseDebitSource.Reserve)
        {
            if (!reserveId.HasValue)
            {
                throw new AppException("Informe a reserva para debitar.");
            }

            _ = await _db.Reserves.FirstOrDefaultAsync(r => r.Id == reserveId.Value, cancellationToken)
                ?? throw new AppException("Reserva inválida.");
            purchase.DebitSource = PurchaseDebitSource.Reserve;
            purchase.ReserveId = reserveId;
            return;
        }

        purchase.DebitSource = parsed;
        purchase.ReserveId = null;
    }

    private static PurchaseDebitSource ParseDebitSource(string? debitSource, Guid? reserveId)
    {
        if (reserveId.HasValue
            && (string.IsNullOrWhiteSpace(debitSource)
                || debitSource.Equals(nameof(PurchaseDebitSource.None), StringComparison.OrdinalIgnoreCase)))
        {
            return PurchaseDebitSource.Reserve;
        }

        if (string.IsNullOrWhiteSpace(debitSource))
        {
            return PurchaseDebitSource.None;
        }

        if (!Enum.TryParse<PurchaseDebitSource>(debitSource, true, out var parsed))
        {
            throw new AppException("Origem de débito inválida.");
        }

        return parsed;
    }

    private static (EntryDestination Destination, Guid? ReserveId)? ResolveDebit(Purchase purchase)
    {
        if (purchase.DebitSource == PurchaseDebitSource.FreeBalance)
        {
            return (EntryDestination.FreeBalance, null);
        }

        if (purchase.DebitSource == PurchaseDebitSource.Reserve || purchase.ReserveId.HasValue)
        {
            return (EntryDestination.Reserve, purchase.ReserveId);
        }

        return null;
    }

    private async Task RemoveLinkedEntriesAsync(IEnumerable<Installment> installments, CancellationToken cancellationToken)
    {
        var entryIds = installments
            .Where(i => i.EntryId.HasValue)
            .Select(i => i.EntryId!.Value)
            .Distinct()
            .ToList();
        if (entryIds.Count == 0) return;

        var entries = await _db.Entries.Where(e => entryIds.Contains(e.Id)).ToListAsync(cancellationToken);
        foreach (var installment in installments.Where(i => i.EntryId.HasValue))
        {
            installment.EntryId = null;
            installment.Entry = null;
        }

        _db.RemoveRange(entries);
    }

    private static PurchaseDto Map(Purchase purchase) =>
        new(
            purchase.Id,
            purchase.Name,
            purchase.ProductUrl,
            purchase.DebitSource.ToString(),
            purchase.ReserveId,
            purchase.Reserve?.Name,
            purchase.Installments
                .OrderBy(i => i.InstallmentNumber)
                .Select(MapInstallment)
                .ToList());

    private static InstallmentDto MapInstallment(Installment installment) =>
        new(
            installment.Id,
            installment.Amount,
            installment.InstallmentNumber,
            installment.Paid,
            installment.DueDate,
            installment.PaidDate,
            installment.PaymentUrl,
            installment.EntryId);
}
