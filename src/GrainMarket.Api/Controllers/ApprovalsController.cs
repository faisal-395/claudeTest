using GrainMarket.Api.Common;
using GrainMarket.Application.Approvals;
using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Kachis;
using GrainMarket.Application.Pakkis;
using GrainMarket.Application.Purchases;
using GrainMarket.Application.SaleInvoices;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

/// <summary>Setup &gt; Approvals: a single worklist across Kachi/Pakki/Purchase/Sale Invoice for
/// whichever roles have Role.RequiresApproval set (configurable under Setup &gt; Users &amp; Roles).
/// Approve/Reject on a given row always delegates to that type's own service, since only it knows
/// how to post or reverse that type's ledger entries — this controller is just the shared
/// entityType-keyed front door.</summary>
[Authorize]
[ApiController]
[Route("api/approvals")]
[ModulePermission(ModuleName.Approvals)]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalsService _approvals;
    private readonly IKachiService _kachis;
    private readonly IPakkiService _pakkis;
    private readonly IPurchaseService _purchases;
    private readonly ISaleInvoiceService _saleInvoices;

    public ApprovalsController(IApprovalsService approvals, IKachiService kachis, IPakkiService pakkis, IPurchaseService purchases, ISaleInvoiceService saleInvoices)
    {
        _approvals = approvals;
        _kachis = kachis;
        _pakkis = pakkis;
        _purchases = purchases;
        _saleInvoices = saleInvoices;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<PendingApprovalDto>>> GetPending(CancellationToken ct) => Ok(await _approvals.GetPendingAsync(ct));

    [ModulePermission(ModuleName.Approvals, PermissionAction.Edit)]
    [HttpPost("{entityType}/{id:int}/approve")]
    public async Task<IActionResult> Approve(string entityType, int id, CancellationToken ct)
    {
        switch (entityType)
        {
            case "Kachi": await _kachis.ApproveAsync(id, ct); break;
            case "Pakki": await _pakkis.ApproveAsync(id, ct); break;
            case "Purchase": await _purchases.ApproveAsync(id, ct); break;
            case "SaleInvoice": await _saleInvoices.ApproveAsync(id, ct); break;
            default: throw new InvalidCalculationException($"Unknown approval entity type \"{entityType}\".");
        }
        return NoContent();
    }

    [ModulePermission(ModuleName.Approvals, PermissionAction.Edit)]
    [HttpPost("{entityType}/{id:int}/reject")]
    public async Task<IActionResult> Reject(string entityType, int id, [FromBody] RejectRequest request, CancellationToken ct)
    {
        switch (entityType)
        {
            case "Kachi": await _kachis.RejectAsync(id, request.Reason, ct); break;
            case "Pakki": await _pakkis.RejectAsync(id, request.Reason, ct); break;
            case "Purchase": await _purchases.RejectAsync(id, request.Reason, ct); break;
            case "SaleInvoice": await _saleInvoices.RejectAsync(id, request.Reason, ct); break;
            default: throw new InvalidCalculationException($"Unknown approval entity type \"{entityType}\".");
        }
        return NoContent();
    }
}
