using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Application.Common;
using DesfudenciFy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.Application.Services;

public class BalanceService
{
    private readonly IAppDbContext _db;

    public BalanceService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetFreeBalanceAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Entries
            .Where(e => e.Destination == EntryDestination.FreeBalance)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
    }

    public async Task<decimal> GetFreeBalanceInvestedAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ReserveInvestments
            .Where(ri => ri.ReserveId == null)
            .SumAsync(ri => (decimal?)ri.Amount, cancellationToken) ?? 0m;
    }

    public async Task<decimal> GetFreeBalanceAvailableAsync(CancellationToken cancellationToken = default)
    {
        var current = await GetFreeBalanceAsync(cancellationToken);
        var invested = await GetFreeBalanceInvestedAsync(cancellationToken);
        return current - invested;
    }

    public async Task<decimal> GetReserveCurrentAsync(Guid reserveId, CancellationToken cancellationToken = default)
    {
        return await _db.Entries
            .Where(e => e.Destination == EntryDestination.Reserve && e.ReserveId == reserveId)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
    }

    public async Task<decimal> GetReserveInvestedAsync(Guid reserveId, CancellationToken cancellationToken = default)
    {
        return await _db.ReserveInvestments
            .Where(ri => ri.ReserveId == reserveId)
            .SumAsync(ri => (decimal?)ri.Amount, cancellationToken) ?? 0m;
    }

    public async Task<decimal> GetReserveAvailableAsync(Guid reserveId, CancellationToken cancellationToken = default)
    {
        var current = await GetReserveCurrentAsync(reserveId, cancellationToken);
        var invested = await GetReserveInvestedAsync(reserveId, cancellationToken);
        return current - invested;
    }

    public async Task EnsureAvailableAsync(
        EntryDestination destination,
        Guid? reserveId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new AppException("O valor deve ser maior que zero.");
        }

        if (destination == EntryDestination.FreeBalance)
        {
            var free = await GetFreeBalanceAvailableAsync(cancellationToken);
            if (free < amount)
            {
                throw new AppException("Saldo livre insuficiente.");
            }

            return;
        }

        if (reserveId is null)
        {
            throw new AppException("A reserva é obrigatória para destino em reserva.");
        }

        var available = await GetReserveAvailableAsync(reserveId.Value, cancellationToken);
        if (available < amount)
        {
            throw new AppException("Saldo disponível insuficiente na reserva.");
        }
    }
}
