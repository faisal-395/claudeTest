using GrainMarket.Api.Common;
using GrainMarket.Application.Pakkis;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/pakkis")]
[ModulePermission(ModuleName.Pakki)]
public class PakkisController : ControllerBase
{
    private readonly IPakkiService _service;

    public PakkisController(IPakkiService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<PakkiDto>>> GetAll([FromQuery] int? seasonId, CancellationToken ct)
        => Ok(await _service.GetAllAsync(seasonId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PakkiDto>> GetById(int id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [ModulePermission(ModuleName.Pakki, PermissionAction.Create)]
    [HttpPost("from-kachi")]
    public async Task<ActionResult<PakkiDto>> CreateFromKachi(CreatePakkiFromKachiRequest request, CancellationToken ct)
    {
        var result = await _service.CreateFromKachiAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.Pakki, PermissionAction.Create)]
    [HttpPost("standalone")]
    public async Task<ActionResult<PakkiDto>> CreateStandalone(CreateStandalonePakkiRequest request, CancellationToken ct)
    {
        var result = await _service.CreateStandaloneAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.Pakki, PermissionAction.Edit)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PakkiDto>> Update(int id, UpdatePakkiRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [ModulePermission(ModuleName.Pakki, PermissionAction.Delete)]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await _service.CancelAsync(id, ct);
        return NoContent();
    }
}
