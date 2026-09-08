using GrainMarket.Api.Common;
using GrainMarket.Application.ChartOfAccounts;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/chart-of-accounts")]
[ModulePermission(ModuleName.SetupChartOfAccounts)]
public class ChartOfAccountsController : ControllerBase
{
    private readonly IChartOfAccountService _service;

    public ChartOfAccountsController(IChartOfAccountService service)
    {
        _service = service;
    }

    /// <summary>Protected accounts are already filtered out server-side for roles not on the allow-list.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ChartOfAccountDto>>> GetAll([FromQuery] bool includeInactive, CancellationToken ct)
        => Ok(await _service.GetVisibleAsync(includeInactive, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ChartOfAccountDto>> GetById(int id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [ModulePermission(ModuleName.SetupChartOfAccounts, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<ChartOfAccountDto>> Create(UpsertChartOfAccountRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.SetupChartOfAccounts, PermissionAction.Edit)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ChartOfAccountDto>> Update(int id, UpsertChartOfAccountRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [ModulePermission(ModuleName.SetupChartOfAccounts, PermissionAction.Delete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
