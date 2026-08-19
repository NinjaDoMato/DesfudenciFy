using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesfudenciFy.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public Task<LoginResponse> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        _authService.LoginAsync(request, cancellationToken);

    [HttpPost("refresh")]
    [AllowAnonymous]
    public Task<RefreshTokenResponse> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken) =>
        _authService.RefreshAsync(request, cancellationToken);

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout() => Ok(new { message = "Logged out" });
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _service;

    public UsersController(UserService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<UserDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpPost]
    public Task<UserDto> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<UserDto> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/bank-accounts")]
public class BankAccountsController : ControllerBase
{
    private readonly BankAccountService _service;

    public BankAccountsController(BankAccountService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<BankAccountDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpPost]
    public Task<BankAccountDto> Create([FromBody] UpsertBankAccountRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<BankAccountDto> Update(Guid id, [FromBody] UpsertBankAccountRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/investment-types")]
public class InvestmentTypesController : ControllerBase
{
    private readonly InvestmentTypeService _service;

    public InvestmentTypesController(InvestmentTypeService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<InvestmentTypeDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpPost]
    public Task<InvestmentTypeDto> Create([FromBody] UpsertInvestmentTypeRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<InvestmentTypeDto> Update(Guid id, [FromBody] UpsertInvestmentTypeRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/income-types")]
public class IncomeTypesController : ControllerBase
{
    private readonly IncomeTypeService _service;

    public IncomeTypesController(IncomeTypeService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<IncomeTypeDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpPost]
    public Task<IncomeTypeDto> Create([FromBody] UpsertIncomeTypeRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<IncomeTypeDto> Update(Guid id, [FromBody] UpsertIncomeTypeRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/property-expense-types")]
public class PropertyExpenseTypesController : ControllerBase
{
    private readonly PropertyExpenseTypeService _service;

    public PropertyExpenseTypesController(PropertyExpenseTypeService service) => _service = service;

    [HttpGet]
    public Task<IReadOnlyList<PropertyExpenseTypeDto>> List(CancellationToken cancellationToken) => _service.ListAsync(cancellationToken);

    [HttpPost]
    public Task<PropertyExpenseTypeDto> Create([FromBody] UpsertPropertyExpenseTypeRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<PropertyExpenseTypeDto> Update(Guid id, [FromBody] UpsertPropertyExpenseTypeRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
