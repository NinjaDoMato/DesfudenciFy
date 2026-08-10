using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.Application.Services;

public class DashboardService
{
    private readonly IAppDbContext _db;
    private readonly BalanceService _balance;

    public DashboardService(IAppDbContext db, BalanceService balance)
    {
        _db = db;
        _balance = balance;
    }

    public async Task<DashboardTotalsDto> GetTotalsAsync(CancellationToken cancellationToken = default)
    {
        var freeCurrent = await _balance.GetFreeBalanceAsync(cancellationToken);
        var freeAvailable = await _balance.GetFreeBalanceAvailableAsync(cancellationToken);
        var reserveCurrent = await _db.Entries
            .Where(e => e.Destination == EntryDestination.Reserve)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
        var invested = await _db.ReserveInvestments.SumAsync(ri => (decimal?)ri.Amount, cancellationToken) ?? 0m;
        var income = await _db.IncomeSources.Where(i => i.IsActive).SumAsync(i => (decimal?)i.Amount, cancellationToken) ?? 0m;
        var fixedCosts = await _db.FixedCosts.SumAsync(c => (decimal?)c.Amount, cancellationToken) ?? 0m;
        var monthlyGoal = await _db.Reserves.SumAsync(r => (decimal?)(r.MonthlyGoal ?? 0m), cancellationToken) ?? 0m;
        var propertyRemaining = await _db.Properties
            .SumAsync(p => (decimal?)p.RemainingBalance, cancellationToken) ?? 0m;

        return new DashboardTotalsDto(
            freeCurrent + reserveCurrent,
            freeAvailable,
            invested,
            income,
            fixedCosts,
            monthlyGoal,
            income - (monthlyGoal + fixedCosts),
            propertyRemaining);
    }

    public async Task<IReadOnlyList<MonthlyCapitalDto>> GetMonthlyCapitalAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _db.Entries
            .OrderBy(e => e.OccurredAt)
            .Select(e => new { e.OccurredAt, e.Amount, e.Destination })
            .ToListAsync(cancellationToken);

        var investments = await _db.Investments
            .Where(i => i.Status == InvestmentStatus.Active)
            .Select(i => new { i.StartDate, i.StartAmount })
            .ToListAsync(cancellationToken);

        var start = DateTime.UtcNow.Date.AddMonths(-11);
        start = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = new List<MonthlyCapitalDto>();
        for (var i = 0; i < 12; i++)
        {
            var monthStart = start.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);

            var free = entries
                .Where(e => e.Destination == EntryDestination.FreeBalance && e.OccurredAt < monthEnd)
                .Sum(e => e.Amount);

            var reserve = entries
                .Where(e => e.Destination == EntryDestination.Reserve && e.OccurredAt < monthEnd)
                .Sum(e => e.Amount);

            // Approximate invested capital as sum of active investments started before month end
            var invested = investments.Where(inv => inv.StartDate < monthEnd).Sum(inv => inv.StartAmount);
            var freeCapital = free + Math.Max(0, reserve - invested);

            result.Add(new MonthlyCapitalDto(monthStart.ToString("yyyy-MM"), freeCapital, invested));
        }

        return result;
    }

    public async Task<IReadOnlyList<ReserveDistributionDto>> GetReserveDistributionAsync(CancellationToken cancellationToken = default)
    {
        var reserves = await _db.Reserves.OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var result = new List<ReserveDistributionDto>();
        foreach (var reserve in reserves)
        {
            var current = await _balance.GetReserveCurrentAsync(reserve.Id, cancellationToken);
            result.Add(new ReserveDistributionDto(reserve.Id, reserve.Name, current, reserve.DisplayColor));
        }

        return result;
    }

    public async Task<IReadOnlyList<InvestmentTypeDistributionDto>> GetInvestmentTypeDistributionAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Investments
            .AsNoTracking()
            .Where(i => i.Status == InvestmentStatus.Active)
            .Select(i => new
            {
                i.InvestmentTypeId,
                TypeName = i.InvestmentType.Name,
                i.CurrentAmount,
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => new { x.InvestmentTypeId, x.TypeName })
            .Select(g => new InvestmentTypeDistributionDto(
                g.Key.InvestmentTypeId,
                g.Key.TypeName,
                g.Sum(x => x.CurrentAmount)))
            .OrderByDescending(x => x.Value)
            .ToList();
    }

    public async Task<IReadOnlyList<UpcomingInvestmentDto>> GetUpcomingInvestmentsAsync(CancellationToken cancellationToken = default) =>
        await _db.Investments
            .Where(i => i.Status == InvestmentStatus.Active && i.EndDate != null)
            .OrderBy(i => i.EndDate)
            .Take(10)
            .Select(i => new UpcomingInvestmentDto(i.Id, i.Name, i.EndDate!.Value, i.CurrentAmount))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UpcomingBillDto>> GetUpcomingBillsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.Date;
        var horizon = now.AddMonths(2);

        var pendingCosts = await _db.FixedCosts
            .Where(c => c.DueDate != null && c.DueDate <= horizon)
            .OrderBy(c => c.DueDate)
            .Take(20)
            .Select(c => new UpcomingBillDto("FixedCost", c.Id, c.Name, c.Amount, c.DueDate))
            .ToListAsync(cancellationToken);

        var installments = await _db.Installments
            .Include(i => i.Purchase)
            .Where(i => !i.Paid && i.DueDate <= horizon)
            .OrderBy(i => i.DueDate)
            .Take(20)
            .Select(i => new UpcomingBillDto("Installment", i.Id, i.Purchase.Name, i.Amount, i.DueDate))
            .ToListAsync(cancellationToken);

        return pendingCosts
            .Concat(installments)
            .OrderBy(b => b.DueDate)
            .ThenBy(b => b.Name)
            .Take(20)
            .ToList();
    }
}
