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
            InitialFinancingAmount = request.InitialFinancingAmount,
            InstallmentAmount = request.InstallmentAmount,
            RemainingInstallments = request.RemainingInstallments,
            RemainingBalance = request.RemainingBalance,
            IsRented = false
        };
        _db.Add(property);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(property);
    }

    public async Task<PropertyDto> UpdateAsync(Guid id, UpdatePropertyRequest request, CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(id, cancellationToken);
        property.Name = request.Name.Trim();
        property.Address = request.Address.Trim();
        property.IsRented = request.IsRented;
        property.InitialFinancingAmount = request.InitialFinancingAmount;
        property.InstallmentAmount = request.InstallmentAmount;
        property.RemainingInstallments = request.RemainingInstallments;
        property.RemainingBalance = request.RemainingBalance;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(property);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await LoadAsync(id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(property.PhotoPath))
        {
            await _fileStorage.DeleteAsync(property.PhotoPath, cancellationToken);
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
        return Map(property);
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
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Property> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Properties.Include(p => p.Amortizations).FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
        ?? throw new NotFoundException("Imóvel não encontrado.");

    private static PropertyDto Map(Property property) =>
        new(
            property.Id,
            property.Name,
            property.Address,
            string.IsNullOrWhiteSpace(property.PhotoPath) ? null : $"/api/v1/properties/{property.Id}/photo",
            property.IsRented,
            property.InitialFinancingAmount,
            property.InstallmentAmount,
            property.RemainingInstallments,
            property.RemainingBalance,
            property.Amortizations
                .OrderByDescending(a => a.PaidAt)
                .Select(a => new PropertyAmortizationDto(a.Id, a.Amount, a.InstallmentsAmortized, a.PaidAt, a.Observation, a.EntryId))
                .ToList());
}
