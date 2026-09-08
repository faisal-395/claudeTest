using GrainMarket.Api.Common;
using GrainMarket.Application.Recovery;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/recovery")]
[ModulePermission(ModuleName.Recovery)]
public class RecoveryController : ControllerBase
{
    private readonly IRecoveryService _service;

    public RecoveryController(IRecoveryService service)
    {
        _service = service;
    }

    [HttpGet("outstanding")]
    public async Task<ActionResult<List<OutstandingPartyDto>>> GetOutstanding(CancellationToken ct) => Ok(await _service.GetOutstandingAsync(ct));

    [HttpGet("notes/{partyId:int}")]
    public async Task<ActionResult<List<RecoveryNoteDto>>> GetNotes(int partyId, CancellationToken ct) => Ok(await _service.GetNotesAsync(partyId, ct));

    [ModulePermission(ModuleName.Recovery, PermissionAction.Create)]
    [HttpPost("notes")]
    public async Task<ActionResult<RecoveryNoteDto>> AddNote(CreateRecoveryNoteRequest request, CancellationToken ct)
        => Ok(await _service.AddNoteAsync(request, ct));
}
