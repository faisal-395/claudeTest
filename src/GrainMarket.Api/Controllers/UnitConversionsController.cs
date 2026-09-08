using GrainMarket.Api.Common;
using GrainMarket.Application.UnitConversions;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/unit-conversions")]
[ModulePermission(ModuleName.SetupUnitConversions)]
public class UnitConversionsController : ControllerBase
{
    private readonly IUnitConversionService _service;

    public UnitConversionsController(IUnitConversionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<UnitConversionDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [ModulePermission(ModuleName.SetupUnitConversions, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<UnitConversionDto>> Create(UpsertUnitConversionRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));

    [ModulePermission(ModuleName.SetupUnitConversions, PermissionAction.Edit)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UnitConversionDto>> Update(int id, UpsertUnitConversionRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [ModulePermission(ModuleName.SetupUnitConversions, PermissionAction.Delete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
