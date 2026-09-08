using GrainMarket.Api.Common;
using GrainMarket.Application.Expenses;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/expenses")]
[ModulePermission(ModuleName.Expense)]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _service;

    public ExpensesController(IExpenseService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExpenseDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [ModulePermission(ModuleName.Expense, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create(CreateExpenseRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));
}
