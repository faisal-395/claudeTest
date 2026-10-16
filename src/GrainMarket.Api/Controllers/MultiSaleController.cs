using GrainMarket.Api.Common;
using GrainMarket.Application.MultiSale;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

/// <summary>One vendor/buyer settling with multiple farmers in a single submit — raises one
/// standalone Pakki per row with the shared vendor attached. Shares the Pakki module permission
/// since that's exactly what this creates.</summary>
[Authorize]
[ApiController]
[Route("api/multi-sale")]
[ModulePermission(ModuleName.Pakki)]
public class MultiSaleController : ControllerBase
{
    private readonly IMultiSaleService _service;

    public MultiSaleController(IMultiSaleService service)
    {
        _service = service;
    }

    [ModulePermission(ModuleName.Pakki, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<MultiSaleResultDto>> Create(CreateMultiSaleRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));
}
