using GrainMarket.Api.Common;
using GrainMarket.Application.Purchases;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/purchases")]
[ModulePermission(ModuleName.Purchase)]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _service;

    public PurchasesController(IPurchaseService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<PurchaseDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseDto>> GetById(int id, CancellationToken ct) => Ok(await _service.GetByIdAsync(id, ct));

    [ModulePermission(ModuleName.Purchase, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<PurchaseDto>> Create(CreatePurchaseRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.Purchase, PermissionAction.Delete)]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await _service.CancelAsync(id, ct);
        return NoContent();
    }
}
