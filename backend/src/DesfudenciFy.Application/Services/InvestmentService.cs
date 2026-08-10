using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Entities;
using DesfudenciFy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.Application.Services;

public class InvestmentService
{
    private readonly IAppDbContext _db;
    private readonly BalanceService _balance;

    public InvestmentService(IAppDbContext db, BalanceService balance)
    {
        _db = db;
        _balance = balance;
    }

    public async Task<IReadOnlyList<InvestmentDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var investments = await _db.Investments
            .Include(i => i.BankAccount)
            .Include(i => i.InvestmentType)
            .Include(i => i.SourceReserves)
            .Where(i => i.Status == InvestmentStatus.Active)
            .OrderBy(i => i.EndDate)
            .ToListAsync(cancellationToken);

        return investments.Select(Map).ToList();
    }

    public async Task<InvestmentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var investment = await LoadAsync(id, cancellationToken);
        return Map(investment);
    }

    public async Task<InvestmentDto> CreateAsync(CreateInvestmentRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSources(request.SourceReserves);

        if (request.EndDate.HasValue && request.EndDate.Value.Date < request.StartDate.Date)
        {
            throw new AppException("A data de fim não pode ser anterior à data de início.");
        }

        _ = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == request.BankAccountId && b.IsActive, cancellationToken)
            ?? throw new NotFoundException("Conta bancária não encontrada.");
        _ = await _db.InvestmentTypes.FirstOrDefaultAsync(t => t.Id == request.InvestmentTypeId && t.IsActive, cancellationToken)
            ?? throw new NotFoundException("Tipo de investimento não encontrado.");

        decimal total = 0;
        foreach (var source in request.SourceReserves)
        {
            if (source.Amount <= 0)
            {
                throw new AppException("O valor alocado deve ser maior que zero.");
            }

            if (source.ReserveId is null)
            {
                await _balance.EnsureAvailableAsync(EntryDestination.FreeBalance, null, source.Amount, cancellationToken);
            }
            else
            {
                await _balance.EnsureAvailableAsync(EntryDestination.Reserve, source.ReserveId, source.Amount, cancellationToken);
            }

            total += source.Amount;
        }

        var investment = new Investment
        {
            Name = request.Name.Trim(),
            Rentability = request.Rentability?.Trim() ?? string.Empty,
            StartAmount = total,
            CurrentAmount = total,
            StartDate = request.StartDate.ToUniversalTime(),
            EndDate = request.EndDate?.ToUniversalTime(),
            BankAccountId = request.BankAccountId,
            InvestmentTypeId = request.InvestmentTypeId,
            Status = InvestmentStatus.Active
        };

        _db.Add(investment);
        foreach (var source in request.SourceReserves)
        {
            _db.Add(new ReserveInvestment
            {
                Investment = investment,
                ReserveId = source.ReserveId,
                Amount = source.Amount
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(investment.Id, cancellationToken));
    }

    public async Task<InvestmentDto> UpdateAsync(Guid id, UpdateInvestmentRequest request, CancellationToken cancellationToken = default)
    {
        var investment = await LoadAsync(id, cancellationToken);
        if (investment.Status != InvestmentStatus.Active)
        {
            throw new AppException("Somente investimentos ativos podem ser atualizados.");
        }

        ValidateSources(request.SourceReserves);

        if (request.EndDate.HasValue && request.EndDate.Value.Date < request.StartDate.Date)
        {
            throw new AppException("A data de fim não pode ser anterior à data de início.");
        }

        _ = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == request.BankAccountId && b.IsActive, cancellationToken)
            ?? throw new NotFoundException("Conta bancária não encontrada.");
        _ = await _db.InvestmentTypes.FirstOrDefaultAsync(t => t.Id == request.InvestmentTypeId && t.IsActive, cancellationToken)
            ?? throw new NotFoundException("Tipo de investimento não encontrado.");

        var existingByKey = investment.SourceReserves.ToDictionary(SourceKey, s => s.Amount);
        decimal total = 0;

        foreach (var source in request.SourceReserves)
        {
            if (source.Amount <= 0)
            {
                throw new AppException("O valor alocado deve ser maior que zero.");
            }

            var key = SourceKey(source);
            var currentLinkAmount = existingByKey.GetValueOrDefault(key);
            decimal maxAllowed;

            if (source.ReserveId is null)
            {
                var available = await _balance.GetFreeBalanceAvailableAsync(cancellationToken);
                maxAllowed = available + currentLinkAmount;
                if (source.Amount > maxAllowed)
                {
                    throw new AppException("Saldo livre insuficiente.");
                }
            }
            else
            {
                var available = await _balance.GetReserveAvailableAsync(source.ReserveId.Value, cancellationToken);
                maxAllowed = available + currentLinkAmount;
                if (source.Amount > maxAllowed)
                {
                    throw new AppException("Saldo disponível insuficiente na reserva.");
                }
            }

            total += source.Amount;
        }

        investment.Name = request.Name.Trim();
        investment.Rentability = request.Rentability?.Trim() ?? string.Empty;
        investment.StartDate = request.StartDate.ToUniversalTime();
        investment.EndDate = request.EndDate?.ToUniversalTime();
        investment.BankAccountId = request.BankAccountId;
        investment.InvestmentTypeId = request.InvestmentTypeId;
        investment.StartAmount = total;
        if (investment.CurrentAmount < total)
        {
            investment.CurrentAmount = total;
        }

        _db.RemoveRange(investment.SourceReserves);
        foreach (var source in request.SourceReserves)
        {
            _db.Add(new ReserveInvestment
            {
                InvestmentId = investment.Id,
                ReserveId = source.ReserveId,
                Amount = source.Amount
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(id, cancellationToken));
    }

    public async Task UpdateCurrentAmountAsync(Guid id, UpdateCurrentAmountRequest request, CancellationToken cancellationToken = default)
    {
        var investment = await LoadAsync(id, cancellationToken);
        if (investment.Status != InvestmentStatus.Active)
        {
            throw new AppException("Somente investimentos ativos podem ser atualizados.");
        }

        investment.CurrentAmount = request.CurrentAmount < investment.StartAmount
            ? investment.StartAmount
            : request.CurrentAmount;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task LiquidateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var investment = await LoadAsync(id, cancellationToken);
        if (investment.Status != InvestmentStatus.Active)
        {
            throw new AppException("O investimento já foi liquidado.");
        }

        var shares = ProfitDistribution.Distribute(
            investment.StartAmount,
            investment.CurrentAmount,
            investment.SourceReserves.Select(link => (link.ReserveId, link.Amount)));

        foreach (var share in shares)
        {
            if (share.ReserveId is null)
            {
                _db.Add(new Entry
                {
                    Amount = share.ProfitShare,
                    Observation = $"Distribuição de lucros do investimento {investment.Name}",
                    OccurredAt = DateTime.UtcNow,
                    Destination = EntryDestination.FreeBalance
                });
            }
            else
            {
                _db.Add(new Entry
                {
                    Amount = share.ProfitShare,
                    Observation = $"Distribuição de lucros do investimento {investment.Name}",
                    OccurredAt = DateTime.UtcNow,
                    Destination = EntryDestination.Reserve,
                    ReserveId = share.ReserveId
                });
            }
        }

        _db.RemoveRange(investment.SourceReserves);
        investment.Status = InvestmentStatus.Liquidated;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Investment> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Investments
            .Include(i => i.BankAccount)
            .Include(i => i.InvestmentType)
            .Include(i => i.SourceReserves)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
        ?? throw new NotFoundException("Investimento não encontrado.");

    private static void ValidateSources(IReadOnlyList<ReserveAllocationDto>? sources)
    {
        if (sources is null || sources.Count == 0)
        {
            throw new AppException("Informe ao menos uma origem com valor (saldo livre ou reserva).");
        }

        var freeCount = sources.Count(s => s.ReserveId is null);
        if (freeCount > 1)
        {
            throw new AppException("Saldo livre só pode aparecer uma vez.");
        }

        var reserveIds = sources.Where(s => s.ReserveId.HasValue).Select(s => s.ReserveId!.Value).ToList();
        if (reserveIds.Count != reserveIds.Distinct().Count())
        {
            throw new AppException("Cada reserva só pode aparecer uma vez.");
        }
    }

    private static string SourceKey(ReserveInvestment source) =>
        source.ReserveId?.ToString() ?? "free";

    private static string SourceKey(ReserveAllocationDto source) =>
        source.ReserveId?.ToString() ?? "free";

    private static InvestmentDto Map(Investment investment) =>
        new(
            investment.Id,
            investment.Name,
            investment.Rentability,
            investment.StartAmount,
            investment.CurrentAmount,
            investment.StartDate,
            investment.EndDate,
            investment.BankAccountId,
            investment.BankAccount.Name,
            investment.InvestmentTypeId,
            investment.InvestmentType.Name,
            investment.Status.ToString(),
            investment.SourceReserves.Select(s => new ReserveAllocationDto(s.ReserveId, s.Amount)).ToList());
}
