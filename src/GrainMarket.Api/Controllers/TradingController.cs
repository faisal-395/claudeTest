using GrainMarket.Api.Common;
using GrainMarket.Application.Trading;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/trading")]
[ModulePermission(ModuleName.Trading)]
public class TradingController : ControllerBase
{
    private readonly ITradingService _service;

    public TradingController(ITradingService service)
    {
        _service = service;
    }

    [HttpGet("stock-position")]
    public async Task<ActionResult<List<TradingProductPositionDto>>> GetStockPosition([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _service.GetStockPositionAsync(from, to, ct));
}
