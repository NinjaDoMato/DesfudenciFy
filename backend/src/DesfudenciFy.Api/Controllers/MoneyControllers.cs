using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using DesfudenciFy.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesfudenciFy.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reserves")]
public class ReservesController : ControllerBase
{
    private readonly ReserveService _service;

    public ReservesController(ReserveService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<ReserveDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ReserveDto> Get(Guid id, CancellationToken cancellationToken) => _service.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<ReserveDto> Create([FromBody] UpsertReserveRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<ReserveDto> Update(Guid id, [FromBody] UpsertReserveRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/v1/entries")]
public class EntriesController : ControllerBase
{
    private readonly EntryService _service;

    public EntriesController(EntryService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<EntryDto>> List(
        [FromQuery] Guid? reserveId,
        [FromQuery] EntryDestination? destination,
        CancellationToken cancellationToken) =>
        _service.ListAsync(reserveId, destination, cancellationToken);

    [HttpGet("free-balance")]
    public async Task<ActionResult<object>> FreeBalance(CancellationToken cancellationToken) =>
        Ok(new { amount = await _service.GetFreeBalanceAsync(cancellationToken) });

    [HttpPost]
    public Task<EntryDto> Create([FromBody] CreateEntryRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request, CancellationToken cancellationToken)
    {
        await _service.TransferAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/v1/investments")]
public class InvestmentsController : ControllerBase
{
    private readonly InvestmentService _service;

    public InvestmentsController(InvestmentService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<InvestmentDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<InvestmentDto> Get(Guid id, CancellationToken cancellationToken) => _service.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<InvestmentDto> Create([FromBody] CreateInvestmentRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<InvestmentDto> Update(Guid id, [FromBody] UpdateInvestmentRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpPut("{id:guid}/current-amount")]
    public async Task<IActionResult> UpdateCurrentAmount(Guid id, [FromBody] UpdateCurrentAmountRequest request, CancellationToken cancellationToken)
    {
        await _service.UpdateCurrentAmountAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/liquidate")]
    public async Task<IActionResult> Liquidate(Guid id, CancellationToken cancellationToken)
    {
        await _service.LiquidateAsync(id, cancellationToken);
        return NoContent();
    }
}
