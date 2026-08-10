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

    public LookupsController(BankAccountService bankAccounts, InvestmentTypeService investmentTypes)
    {
        _bankAccounts = bankAccounts;
        _investmentTypes = investmentTypes;
    }

    [HttpGet("bank-accounts")]
    public Task<IReadOnlyList<BankAccountDto>> BankAccounts(CancellationToken cancellationToken) =>
        _bankAccounts.ListAsync(cancellationToken);

    [HttpGet("investment-types")]
    public Task<IReadOnlyList<InvestmentTypeDto>> InvestmentTypes(CancellationToken cancellationToken) =>
        _investmentTypes.ListAsync(cancellationToken);
}
