using GrainMarket.Api.Common;
using GrainMarket.Application.SaleInvoices;
using GrainMarket.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/sale-invoices")]
[ModulePermission(ModuleName.SaleInvoice)]
public class SaleInvoicesController : ControllerBase
{
    private readonly ISaleInvoiceService _service;

    public SaleInvoicesController(ISaleInvoiceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<SaleInvoiceDto>>> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaleInvoiceDto>> GetById(int id, CancellationToken ct) => Ok(await _service.GetByIdAsync(id, ct));

    [ModulePermission(ModuleName.SaleInvoice, PermissionAction.Create)]
    [HttpPost]
    public async Task<ActionResult<SaleInvoiceDto>> Create(CreateSaleInvoiceRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.SaleInvoice, PermissionAction.Delete)]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await _service.CancelAsync(id, ct);
        return NoContent();
    }
}
