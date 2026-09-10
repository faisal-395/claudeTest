using GrainMarket.Api.Common;
using GrainMarket.Application.MultiPurchase;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

/// <summary>One buyer purchasing from multiple farmers/products in a single submit — shares the
/// Dual Invoice module permission since it's the same underlying action (raise a Kachi and
/// immediately convert it to a Pakki) repeated per row against one shared buyer.</summary>
[Authorize]
[ApiController]
[Route("api/multi-purchase")]
[ModulePermission(ModuleName.DualInvoice)]
public class MultiPurchaseController : ControllerBase
{
    private readonly IMultiPurchaseService _service;

    public MultiPurchaseController(IMultiPurchaseService service)
    {
        _service = service;
    }

    [ModulePermission(ModuleName.DualInvoice, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<MultiPurchaseResultDto>> Create(CreateMultiPurchaseRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));
}
