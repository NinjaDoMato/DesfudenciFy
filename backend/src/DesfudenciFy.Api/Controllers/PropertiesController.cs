using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesfudenciFy.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/properties")]
public class PropertiesController : ControllerBase
{
    private readonly PropertyService _service;

    public PropertiesController(PropertyService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<PropertyDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<PropertyDto> Get(Guid id, CancellationToken cancellationToken) => _service.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<PropertyDto> Create([FromBody] CreatePropertyRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<PropertyDto> Update(Guid id, [FromBody] UpdatePropertyRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/photo")]
    [RequestSizeLimit(10_000_000)]
    public async Task<PropertyDto> UploadPhoto(Guid id, IFormFile file, CancellationToken cancellationToken)
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

    [HttpPost("{id:guid}/amortizations")]
    public Task<PropertyAmortizationDto> Amortize(Guid id, [FromBody] CreateAmortizationRequest request, CancellationToken cancellationToken) =>
        _service.AmortizeAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}/amortizations/{amortizationId:guid}")]
    public async Task<IActionResult> DeleteAmortization(Guid id, Guid amortizationId, CancellationToken cancellationToken)
    {
        await _service.DeleteAmortizationAsync(id, amortizationId, cancellationToken);
        return NoContent();
    }
}
