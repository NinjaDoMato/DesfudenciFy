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
    private readonly PropertyExpenseTypeService _expenseTypes;
    private readonly VehicleExpenseTypeService _vehicleExpenseTypes;

    public LookupsController(
        BankAccountService bankAccounts,
        InvestmentTypeService investmentTypes,
        IncomeTypeService incomeTypes,
        PropertyExpenseTypeService expenseTypes,
        VehicleExpenseTypeService vehicleExpenseTypes)
    {
        _bankAccounts = bankAccounts;
        _investmentTypes = investmentTypes;
        _incomeTypes = incomeTypes;
        _expenseTypes = expenseTypes;
        _vehicleExpenseTypes = vehicleExpenseTypes;
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

    [HttpGet("property-expense-types")]
    public async Task<IReadOnlyList<PropertyExpenseTypeDto>> PropertyExpenseTypes(CancellationToken cancellationToken)
    {
        var types = await _expenseTypes.ListAsync(cancellationToken);
        return types.Where(t => t.IsActive).ToList();
    }

    [HttpGet("vehicle-expense-types")]
    public async Task<IReadOnlyList<VehicleExpenseTypeDto>> VehicleExpenseTypes(CancellationToken cancellationToken)
    {
        var types = await _vehicleExpenseTypes.ListAsync(cancellationToken);
        return types.Where(t => t.IsActive).ToList();
    }
}
