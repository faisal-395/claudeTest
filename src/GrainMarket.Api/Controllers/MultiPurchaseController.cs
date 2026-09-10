using GrainMarket.Api.Common;
using GrainMarket.Application.MultiPurchase;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

/// <summary>One buyer purchasing from multiple farmers/products in a single submit — raises one
/// Kachi per row with the shared buyer attached. Shares the Kachi module permission since that's
/// exactly what this creates; it never touches Pakki.</summary>
[Authorize]
[ApiController]
[Route("api/multi-purchase")]
[ModulePermission(ModuleName.Kachi)]
public class MultiPurchaseController : ControllerBase
{
    private readonly IMultiPurchaseService _service;

    public MultiPurchaseController(IMultiPurchaseService service)
    {
        _service = service;
    }

    [ModulePermission(ModuleName.Kachi, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<MultiPurchaseResultDto>> Create(CreateMultiPurchaseRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));
}
