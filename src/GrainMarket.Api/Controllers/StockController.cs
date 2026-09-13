using GrainMarket.Api.Common;
using GrainMarket.Application.Stock;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/stock")]
public class StockController : ControllerBase
{
    private readonly IStockService _service;

    public StockController(IStockService service)
    {
        _service = service;
    }

    [ModulePermission(ModuleName.SetupProducts)]
    [HttpGet]
    public async Task<ActionResult<List<StockDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetStockAsync(ct));

    // Gated by SaleInvoice (not SetupProducts) — this is called from the Sale Invoice screen itself
    // to auto-fill a line's price, so anyone who can use Sale Invoice needs to be able to call it.
    [ModulePermission(ModuleName.SaleInvoice)]
    [HttpGet("suggested-price")]
    public async Task<ActionResult<SuggestedSalePriceDto>> GetSuggestedPrice([FromQuery] int productId, [FromQuery] decimal quantity, CancellationToken ct)
        => Ok(await _service.GetSuggestedSalePriceAsync(productId, quantity, ct));
}
