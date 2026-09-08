using GrainMarket.Api.Common;
using GrainMarket.Application.Vouchers;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrainMarket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/vouchers")]
public class VouchersController : ControllerBase
{
    private readonly IVoucherService _service;

    public VouchersController(IVoucherService service)
    {
        _service = service;
    }

    [ModulePermission(ModuleName.Payment)]
    [HttpGet]
    public async Task<ActionResult<List<VoucherDto>>> GetAll([FromQuery] VoucherType? type, [FromQuery] int? seasonId, CancellationToken ct)
        => Ok(await _service.GetAllAsync(type, seasonId, ct));

    [ModulePermission(ModuleName.Payment)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<VoucherDto>> GetById(int id, CancellationToken ct) => Ok(await _service.GetByIdAsync(id, ct));

    [ModulePermission(ModuleName.Payment, PermissionAction.Create)]
    [HttpPost("payment")]
    public async Task<ActionResult<VoucherDto>> CreatePayment(CreatePaymentOrReceiptRequest request, CancellationToken ct)
    {
        var result = await _service.CreatePaymentOrReceiptAsync(request with { VoucherType = VoucherType.Payment }, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.Receipt, PermissionAction.Create)]
    [HttpPost("receipt")]
    public async Task<ActionResult<VoucherDto>> CreateReceipt(CreatePaymentOrReceiptRequest request, CancellationToken ct)
    {
        var result = await _service.CreatePaymentOrReceiptAsync(request with { VoucherType = VoucherType.Receipt }, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.Journal, PermissionAction.Create)]
    [HttpPost("journal")]
    public async Task<ActionResult<VoucherDto>> CreateJournal(CreateJournalRequest request, CancellationToken ct)
    {
        var result = await _service.CreateJournalAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [ModulePermission(ModuleName.Payment, PermissionAction.Delete)]
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await _service.CancelAsync(id, ct);
        return NoContent();
    }
}
