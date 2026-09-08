using GrainMarket.Api.Common;
using GrainMarket.Application.Parties;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/parties")]
[ModulePermission(ModuleName.SetupParty)]
public class PartiesController : ControllerBase
{
    private readonly IPartyService _partyService;

    public PartiesController(IPartyService partyService)
    {
        _partyService = partyService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PartyDto>>> GetAll([FromQuery] PartyType? type, [FromQuery] bool includeInactive, CancellationToken ct)
        => Ok(await _partyService.GetAllAsync(type, includeInactive, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PartyDto>> GetById(int id, CancellationToken ct)
        => Ok(await _partyService.GetByIdAsync(id, ct));

    [ModulePermission(ModuleName.SetupParty, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<PartyDto>> Create(UpsertPartyRequest request, CancellationToken ct)
    {
        var result = await _partyService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.SetupParty, PermissionAction.Edit)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PartyDto>> Update(int id, UpsertPartyRequest request, CancellationToken ct)
        => Ok(await _partyService.UpdateAsync(id, request, ct));

    [ModulePermission(ModuleName.SetupParty, PermissionAction.Delete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _partyService.DeleteAsync(id, ct);
        return NoContent();
    }
}
