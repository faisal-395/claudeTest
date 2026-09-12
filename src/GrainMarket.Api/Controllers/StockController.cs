using GrainMarket.Api.Common;
using GrainMarket.Application.Stock;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/stock")]
[ModulePermission(ModuleName.SetupProducts)]
public class StockController : ControllerBase
{
    private readonly IStockService _service;

    public StockController(IStockService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<StockDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetStockAsync(ct));
}
