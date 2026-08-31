using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesfudenciFy.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly VehicleService _service;

    public VehiclesController(VehicleService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<VehicleDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<VehicleDto> Get(Guid id, CancellationToken cancellationToken) => _service.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<VehicleDto> Create([FromBody] CreateVehicleRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<VehicleDto> Update(Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/photo")]
    [RequestSizeLimit(10_000_000)]
    public async Task<VehicleDto> UploadPhoto(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return await _service.UploadPhotoAsync(id, stream, file.FileName, cancellationToken);
    }

    [HttpGet("{id:guid}/photo")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPhoto(Guid id, CancellationToken cancellationToken)
    {
        var (path, contentType) = await _service.GetPhotoAsync(id, cancellationToken);
        return PhysicalFile(path, contentType);
    }

    [HttpPost("{id:guid}/expenses")]
    public Task<VehicleExpenseDto> AddExpense(Guid id, [FromBody] CreateVehicleExpenseRequest request, CancellationToken cancellationToken) =>
        _service.AddExpenseAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}/expenses/{expenseId:guid}")]
    public async Task<IActionResult> DeleteExpense(Guid id, Guid expenseId, CancellationToken cancellationToken)
    {
        await _service.DeleteExpenseAsync(id, expenseId, cancellationToken);
        return NoContent();
    }
}
