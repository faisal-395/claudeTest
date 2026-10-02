using GrainMarket.Api.Common;
using GrainMarket.Application.DeductionRules;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/deduction-rules")]
public class DeductionRulesController : ControllerBase
{
    private readonly IDeductionRuleService _service;

    public DeductionRulesController(IDeductionRuleService service)
    {
        _service = service;
    }

    // Deliberately not gated by ModulePermission: Kachi/Pakki entry needs the active rules to
    // compute and preview deductions live, regardless of the caller's Setup access.
    [HttpGet]
    public async Task<ActionResult<List<DeductionRuleDto>>> GetAll([FromQuery] bool includeInactive, CancellationToken ct)
        => Ok(await _service.GetAllAsync(includeInactive, ct));

    [ModulePermission(ModuleName.SetupDeductionRules, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<DeductionRuleDto>> Create(UpsertDeductionRuleRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));

    [ModulePermission(ModuleName.SetupDeductionRules, PermissionAction.Edit)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DeductionRuleDto>> Update(int id, UpsertDeductionRuleRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [ModulePermission(ModuleName.SetupDeductionRules, PermissionAction.Delete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
