using GrainMarket.Api.Common;
using GrainMarket.Application.Kachis;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/kachis")]
[ModulePermission(ModuleName.Kachi)]
public class KachisController : ControllerBase
{
    private readonly IKachiService _service;

    public KachisController(IKachiService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<KachiDto>>> GetAll([FromQuery] int? seasonId, CancellationToken ct)
        => Ok(await _service.GetAllAsync(seasonId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<KachiDto>> GetById(int id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    [ModulePermission(ModuleName.Kachi, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<KachiDto>> Create(CreateKachiRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.Kachi, PermissionAction.Edit)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<KachiDto>> Update(int id, UpdateKachiRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [ModulePermission(ModuleName.Kachi, PermissionAction.Delete)]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await _service.CancelAsync(id, ct);
        return NoContent();
    }
}
