using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesfudenciFy.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/income-sources")]
public class IncomeSourcesController : ControllerBase
{
    private readonly IncomeSourceService _service;

    public IncomeSourcesController(IncomeSourceService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<IncomeSourceDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpPost]
    public Task<IncomeSourceDto> Create([FromBody] UpsertIncomeSourceRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<IncomeSourceDto> Update(Guid id, [FromBody] UpsertIncomeSourceRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
