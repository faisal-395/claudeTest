using GrainMarket.Api.Common;
using GrainMarket.Application.AccountTypes;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/account-types")]
public class AccountTypesController : ControllerBase
{
    private readonly IAccountTypeService _service;

    public AccountTypesController(IAccountTypeService service)
    {
        _service = service;
    }

    // Deliberately not gated by ModulePermission: the Chart of Accounts form needs this list to
    // populate its Account Type dropdown regardless of the caller's Setup access.
    [HttpGet]
    public async Task<ActionResult<List<AccountTypeDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [ModulePermission(ModuleName.SetupChartOfAccounts, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<AccountTypeDto>> Create(CreateAccountTypeRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));
}
