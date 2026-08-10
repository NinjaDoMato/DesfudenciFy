using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Entities;
using DesfudenciFy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.Application.Services;

public class ReserveService
{
    private readonly IAppDbContext _db;
    private readonly BalanceService _balance;

    public ReserveService(IAppDbContext db, BalanceService balance)
    {
        _db = db;
        _balance = balance;
    }

    public async Task<IReadOnlyList<ReserveDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var reserves = await _db.Reserves.OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var result = new List<ReserveDto>();
        foreach (var reserve in reserves)
        {
            result.Add(await MapAsync(reserve, cancellationToken));
        }

        return result;
    }

    public async Task<ReserveDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reserve = await _db.Reserves.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Reserva não encontrada.");
        return await MapAsync(reserve, cancellationToken);
    }

    public async Task<ReserveDto> CreateAsync(UpsertReserveRequest request, CancellationToken cancellationToken = default)
    {
        var reserve = new Reserve
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Goal = request.Goal,
            DisplayColor = request.DisplayColor,
            MonthlyGoal = request.MonthlyGoal
        };
        _db.Add(reserve);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(reserve, cancellationToken);
    }

    public async Task<ReserveDto> UpdateAsync(Guid id, UpsertReserveRequest request, CancellationToken cancellationToken = default)
    {
        var reserve = await _db.Reserves.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Reserva não encontrada.");
        reserve.Name = request.Name.Trim();
        reserve.Description = request.Description?.Trim() ?? string.Empty;
        reserve.Goal = request.Goal;
        reserve.DisplayColor = request.DisplayColor;
        reserve.MonthlyGoal = request.MonthlyGoal;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(reserve, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reserve = await _db.Reserves.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Reserva não encontrada.");
        if (await _db.ReserveInvestments.AnyAsync(ri => ri.ReserveId == id, cancellationToken))
        {
            throw new AppException("Não é possível excluir uma reserva vinculada a investimentos.");
        }

        var entries = await _db.Entries.Where(e => e.ReserveId == id).ToListAsync(cancellationToken);
        _db.RemoveRange(entries);
        _db.Remove(reserve);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ReserveDto> MapAsync(Reserve reserve, CancellationToken cancellationToken)
    {
        var current = await _balance.GetReserveCurrentAsync(reserve.Id, cancellationToken);
        var invested = await _balance.GetReserveInvestedAsync(reserve.Id, cancellationToken);
        return new ReserveDto(
            reserve.Id,
            reserve.Name,
            reserve.Description,
            reserve.Goal,
            reserve.DisplayColor,
            reserve.MonthlyGoal,
            current,
            invested,
            current - invested);
    }
}

public class EntryService
{
    private readonly IAppDbContext _db;
    private readonly BalanceService _balance;

    public EntryService(IAppDbContext db, BalanceService balance)
    {
        _db = db;
        _balance = balance;
    }

    public async Task<IReadOnlyList<EntryDto>> ListAsync(
        Guid? reserveId,
        EntryDestination? destination,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Entries.AsQueryable();
        if (reserveId.HasValue)
        {
            query = query.Where(e => e.ReserveId == reserveId);
        }

        if (destination.HasValue)
        {
            query = query.Where(e => e.Destination == destination);
        }

        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.DateCreated)
            .Select(e => new EntryDto(
                e.Id,
                e.Amount,
                e.Observation,
                e.OccurredAt,
                e.Destination,
                e.ReserveId,
                e.Reserve != null ? e.Reserve.Name : null))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<EntryDto> CreateAsync(CreateEntryRequest request, CancellationToken cancellationToken = default)
    {
        ValidateDestination(request.Destination, request.ReserveId);

        if (request.Destination == EntryDestination.Reserve)
        {
            _ = await _db.Reserves.FirstOrDefaultAsync(r => r.Id == request.ReserveId, cancellationToken)
                ?? throw new NotFoundException("Reserva não encontrada.");
        }

        var entry = new Entry
        {
            Amount = request.Amount,
            Observation = request.Observation?.Trim() ?? string.Empty,
            OccurredAt = request.OccurredAt?.ToUniversalTime() ?? DateTime.UtcNow,
            Destination = request.Destination,
            ReserveId = request.Destination == EntryDestination.Reserve ? request.ReserveId : null
        };

        _db.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        string? reserveName = null;
        if (entry.ReserveId.HasValue)
        {
            reserveName = await _db.Reserves.Where(r => r.Id == entry.ReserveId).Select(r => r.Name).FirstAsync(cancellationToken);
        }

        return new EntryDto(entry.Id, entry.Amount, entry.Observation, entry.OccurredAt, entry.Destination, entry.ReserveId, reserveName);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _db.Entries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
                    ?? throw new NotFoundException("Lançamento não encontrado.");
        _db.Remove(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TransferAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new AppException("O valor da transferência deve ser maior que zero.");
        }

        ValidateDestination(request.SourceDestination, request.SourceReserveId);
        ValidateDestination(request.TargetDestination, request.TargetReserveId);

        if (request.SourceDestination == request.TargetDestination &&
            request.SourceReserveId == request.TargetReserveId)
        {
            throw new AppException("A origem e o destino devem ser diferentes.");
        }

        await _balance.EnsureAvailableAsync(request.SourceDestination, request.SourceReserveId, request.Amount, cancellationToken);

        var observation = string.IsNullOrWhiteSpace(request.Observation)
            ? "Transferência"
            : request.Observation.Trim();

        _db.Add(new Entry
        {
            Amount = -request.Amount,
            Observation = $"{observation} (saída)",
            OccurredAt = DateTime.UtcNow,
            Destination = request.SourceDestination,
            ReserveId = request.SourceDestination == EntryDestination.Reserve ? request.SourceReserveId : null
        });

        _db.Add(new Entry
        {
            Amount = request.Amount,
            Observation = $"{observation} (entrada)",
            OccurredAt = DateTime.UtcNow,
            Destination = request.TargetDestination,
            ReserveId = request.TargetDestination == EntryDestination.Reserve ? request.TargetReserveId : null
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetFreeBalanceAsync(CancellationToken cancellationToken = default) =>
        await _balance.GetFreeBalanceAvailableAsync(cancellationToken);

    private static void ValidateDestination(EntryDestination destination, Guid? reserveId)
    {
        if (destination == EntryDestination.Reserve && reserveId is null)
        {
            throw new AppException("A reserva é obrigatória quando o destino é uma reserva.");
        }

        if (destination == EntryDestination.FreeBalance && reserveId is not null)
        {
            throw new AppException("A reserva deve ficar vazia quando o destino é o saldo livre.");
        }
    }
}
