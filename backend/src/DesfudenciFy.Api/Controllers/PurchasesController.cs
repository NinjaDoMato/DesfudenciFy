using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesfudenciFy.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/purchases")]
public class PurchasesController : ControllerBase
{
    private readonly PurchaseService _service;

    public PurchasesController(PurchaseService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<PurchaseDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<PurchaseDto> Get(Guid id, CancellationToken cancellationToken) => _service.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<PurchaseDto> Create([FromBody] CreatePurchaseRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<PurchaseDto> Update(Guid id, [FromBody] UpdatePurchaseRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/installments/{installmentId:guid}/pay")]
    public Task<InstallmentDto> PayInstallment(Guid id, Guid installmentId, CancellationToken cancellationToken) =>
        _service.PayInstallmentAsync(id, installmentId, cancellationToken);

    [HttpPost("{id:guid}/installments/{installmentId:guid}/unpay")]
    public Task<InstallmentDto> UnpayInstallment(Guid id, Guid installmentId, CancellationToken cancellationToken) =>
        _service.UnpayInstallmentAsync(id, installmentId, cancellationToken);
}
