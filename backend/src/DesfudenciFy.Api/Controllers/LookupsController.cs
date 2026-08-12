using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesfudenciFy.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/lookups")]
public class LookupsController : ControllerBase
{
    private readonly BankAccountService _bankAccounts;
    private readonly InvestmentTypeService _investmentTypes;
    private readonly IncomeTypeService _incomeTypes;

    public LookupsController(
        BankAccountService bankAccounts,
        InvestmentTypeService investmentTypes,
        IncomeTypeService incomeTypes)
    {
        _bankAccounts = bankAccounts;
        _investmentTypes = investmentTypes;
        _incomeTypes = incomeTypes;
    }

    [HttpGet("bank-accounts")]
    public Task<IReadOnlyList<BankAccountDto>> BankAccounts(CancellationToken cancellationToken) =>
        _bankAccounts.ListAsync(cancellationToken);

    [HttpGet("investment-types")]
    public Task<IReadOnlyList<InvestmentTypeDto>> InvestmentTypes(CancellationToken cancellationToken) =>
        _investmentTypes.ListAsync(cancellationToken);

    [HttpGet("income-types")]
    public async Task<IReadOnlyList<IncomeTypeDto>> IncomeTypes(CancellationToken cancellationToken)
    {
        var types = await _incomeTypes.ListAsync(cancellationToken);
        return types.Where(t => t.IsActive).ToList();
    }
}
