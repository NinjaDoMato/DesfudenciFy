using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesfudenciFy.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _service;

    public DashboardController(DashboardService service) => _service = service;

    [HttpGet("totals")]
    public Task<DashboardTotalsDto> Totals(CancellationToken cancellationToken) => _service.GetTotalsAsync(cancellationToken);

    [HttpGet("monthly-capital")]
    public Task<IReadOnlyList<MonthlyCapitalDto>> MonthlyCapital(CancellationToken cancellationToken) =>
        _service.GetMonthlyCapitalAsync(cancellationToken);

    [HttpGet("reserve-distribution")]
    public Task<IReadOnlyList<ReserveDistributionDto>> ReserveDistribution(CancellationToken cancellationToken) =>
        _service.GetReserveDistributionAsync(cancellationToken);

    [HttpGet("investment-type-distribution")]
    public Task<IReadOnlyList<InvestmentTypeDistributionDto>> InvestmentTypeDistribution(CancellationToken cancellationToken) =>
        _service.GetInvestmentTypeDistributionAsync(cancellationToken);

    [HttpGet("upcoming-investments")]
    public Task<IReadOnlyList<UpcomingInvestmentDto>> UpcomingInvestments(CancellationToken cancellationToken) =>
        _service.GetUpcomingInvestmentsAsync(cancellationToken);

    [HttpGet("upcoming-bills")]
    public Task<IReadOnlyList<UpcomingBillDto>> UpcomingBills(CancellationToken cancellationToken) =>
        _service.GetUpcomingBillsAsync(cancellationToken);
}
