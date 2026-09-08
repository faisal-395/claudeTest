using GrainMarket.Api.Common;
using GrainMarket.Application.Ledger;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/ledger")]
[ModulePermission(ModuleName.Ledger)]
public class LedgerController : ControllerBase
{
    private readonly ILedgerQueryService _service;

    public LedgerController(ILedgerQueryService service)
    {
        _service = service;
    }

    [HttpGet("party/{partyId:int}")]
    public async Task<ActionResult<PartyLedgerDto>> GetPartyLedger(int partyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _service.GetPartyLedgerAsync(partyId, from, to, ct));

    /// <summary>Throws 403 server-side (via ForbiddenAccessException) if the account is protected and the caller's role isn't allowed — never just hidden client-side.</summary>
    [HttpGet("account/{accountId:int}")]
    public async Task<ActionResult<AccountLedgerDto>> GetAccountLedger(int accountId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _service.GetAccountLedgerAsync(accountId, from, to, ct));
}
