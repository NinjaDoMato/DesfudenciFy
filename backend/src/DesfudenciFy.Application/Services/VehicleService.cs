using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Entities;
using DesfudenciFy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.Application.Services;

public class VehicleService
{
    private readonly IAppDbContext _db;
    private readonly IFileStorage _fileStorage;
    private readonly BalanceService _balance;

    public VehicleService(IAppDbContext db, IFileStorage fileStorage, BalanceService balance)
    {
        _db = db;
        _fileStorage = fileStorage;
        _balance = balance;
    }

    public async Task<IReadOnlyList<VehicleDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = await _db.Vehicles
            .Include(v => v.Expenses)
                .ThenInclude(e => e.ExpenseType)
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);
        return vehicles.Select(Map).ToList();
    }

    public async Task<VehicleDto> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await LoadAsync(id, cancellationToken));

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateVehicle(request.Name, request.Model, request.Year, request.PaidValue, request.FipeValue);

        var vehicle = new Vehicle
        {
            Name = request.Name.Trim(),
            Model = request.Model.Trim(),
            Year = request.Year,
            PaidValue = request.PaidValue,
            FipeValue = request.FipeValue
        };
        _db.Add(vehicle);
        await _db.SaveChangesAsync(cancellationToken);

        return Map(await LoadAsync(vehicle.Id, cancellationToken));
    }

    public async Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        ValidateVehicle(request.Name, request.Model, request.Year, request.PaidValue, request.FipeValue);

        var vehicle = await LoadAsync(id, cancellationToken);
        vehicle.Name = request.Name.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.Year = request.Year;
        vehicle.PaidValue = request.PaidValue;
        vehicle.FipeValue = request.FipeValue;
        await _db.SaveChangesAsync(cancellationToken);

        return Map(await LoadAsync(id, cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await LoadAsync(id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(vehicle.PhotoPath))
        {
            await _fileStorage.DeleteAsync(vehicle.PhotoPath, cancellationToken);
        }

        _db.Remove(vehicle);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<VehicleDto> UploadPhotoAsync(Guid id, Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var vehicle = await LoadAsync(id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(vehicle.PhotoPath))
        {
            await _fileStorage.DeleteAsync(vehicle.PhotoPath, cancellationToken);
        }

        vehicle.PhotoPath = await _fileStorage.SaveVehiclePhotoAsync(id, content, fileName, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(id, cancellationToken));
    }

    public async Task<(string AbsolutePath, string ContentType)> GetPhotoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vehicle = await LoadAsync(id, cancellationToken);
        if (string.IsNullOrWhiteSpace(vehicle.PhotoPath))
        {
            throw new NotFoundException("Foto do veículo não encontrada.");
        }

        var absolute = _fileStorage.GetAbsolutePath(vehicle.PhotoPath);
        if (!File.Exists(absolute))
        {
            throw new NotFoundException("Arquivo da foto do veículo não encontrado.");
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

    public async Task<VehicleExpenseDto> AddExpenseAsync(
        Guid vehicleId,
        CreateVehicleExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await LoadAsync(vehicleId, cancellationToken);
        if (request.Amount <= 0)
        {
            throw new AppException("O valor do gasto deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Observation))
        {
            throw new AppException("Informe a observação do gasto.");
        }

        var expenseType = await _db.VehicleExpenseTypes
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

        var expense = new VehicleExpense
        {
            VehicleId = vehicle.Id,
            ExpenseTypeId = expenseType.Id,
            Amount = request.Amount,
            Observation = request.Observation.Trim(),
            OccurredAt = request.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow,
            EntryId = entryId
        };
        _db.Add(expense);
        await _db.SaveChangesAsync(cancellationToken);

        return new VehicleExpenseDto(
            expense.Id,
            expense.Amount,
            expenseType.Id,
            expenseType.Name,
            expense.Observation,
            expense.OccurredAt,
            expense.EntryId);
    }

    public async Task DeleteExpenseAsync(Guid vehicleId, Guid expenseId, CancellationToken cancellationToken = default)
    {
        var vehicle = await LoadAsync(vehicleId, cancellationToken);
        var expense = vehicle.Expenses.FirstOrDefault(e => e.Id == expenseId)
                      ?? throw new NotFoundException("Gasto do veículo não encontrado.");

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

    private async Task<Vehicle> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Vehicles
                   .Include(v => v.Expenses)
                       .ThenInclude(e => e.ExpenseType)
                   .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
               ?? throw new NotFoundException("Veículo não encontrado.");
    }

    private static void ValidateVehicle(string name, string model, int year, decimal paidValue, decimal fipeValue)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AppException("Informe o nome do veículo.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new AppException("Informe o modelo do veículo.");
        }

        if (year < 1900 || year > DateTime.UtcNow.Year + 1)
        {
            throw new AppException("Informe um ano válido para o veículo.");
        }

        if (paidValue < 0)
        {
            throw new AppException("O valor pago não pode ser negativo.");
        }

        if (fipeValue < 0)
        {
            throw new AppException("O valor FIPE não pode ser negativo.");
        }
    }

    private static VehicleDto Map(Vehicle vehicle)
    {
        var expenseAmounts = vehicle.Expenses.Select(e => e.Amount);
        var totalExpenses = VehicleEconomics.CalculateTotalExpenses(expenseAmounts);
        var fipeVariance = VehicleEconomics.CalculateFipeVariance(vehicle.PaidValue, totalExpenses, vehicle.FipeValue);

        return new(
            vehicle.Id,
            vehicle.Name,
            vehicle.Model,
            vehicle.Year,
            string.IsNullOrWhiteSpace(vehicle.PhotoPath) ? null : $"/api/v1/vehicles/{vehicle.Id}/photo",
            vehicle.PaidValue,
            vehicle.FipeValue,
            totalExpenses,
            fipeVariance,
            vehicle.Expenses
                .OrderByDescending(e => e.OccurredAt)
                .Select(e => new VehicleExpenseDto(
                    e.Id,
                    e.Amount,
                    e.ExpenseTypeId,
                    e.ExpenseType.Name,
                    e.Observation,
                    e.OccurredAt,
                    e.EntryId))
                .ToList());
    }
}
