using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesfudenciFy.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/fixed-costs")]
public class FixedCostsController : ControllerBase
{
    private readonly FixedCostService _service;

    public FixedCostsController(FixedCostService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<FixedCostDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<FixedCostDto> Get(Guid id, CancellationToken cancellationToken) => _service.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<FixedCostDto> Create([FromBody] UpsertFixedCostRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<FixedCostDto> Update(Guid id, [FromBody] UpsertFixedCostRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/payments")]
    public Task<CostPaymentDto> Pay(Guid id, [FromBody] CreateCostPaymentRequest request, CancellationToken cancellationToken) =>
        _service.PayAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}/payments/{paymentId:guid}")]
    public async Task<IActionResult> DeletePayment(Guid id, Guid paymentId, CancellationToken cancellationToken)
    {
        await _service.DeletePaymentAsync(id, paymentId, cancellationToken);
        return NoContent();
    }
}
