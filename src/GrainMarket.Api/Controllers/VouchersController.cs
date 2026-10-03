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

    // Deliberately not gated by ModulePermission: Payment and Receipt's own "Today's Vouchers"
    // grid calls this filtered by its own type, so a Receipt-only (no Payment) role still needs to
    // see its own receipts — matches the Seasons/Parties/ChartOfAccounts "open read, gated write"
    // convention used elsewhere for reference/record data.
    [HttpGet]
    public async Task<ActionResult<List<VoucherDto>>> GetAll([FromQuery] VoucherType? type, [FromQuery] int? seasonId, CancellationToken ct)
        => Ok(await _service.GetAllAsync(type, seasonId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VoucherDto>> GetById(int id, CancellationToken ct) => Ok(await _service.GetByIdAsync(id, ct));

    // Reserves (consumes) the next PV number immediately, so the Payment screen can show it before
    // Save — matches Purchase/Sale Invoice's reserve-ahead pattern. Left unsaved, that number is
    // simply skipped.
    [ModulePermission(ModuleName.Payment, PermissionAction.Create)]
    [HttpGet("payment/next-voucher-no")]
    public async Task<ActionResult<NextVoucherNoDto>> ReserveNextPaymentNo(CancellationToken ct)
        => Ok(await _service.ReserveNextVoucherNoAsync(VoucherType.Payment, ct));

    [ModulePermission(ModuleName.Receipt, PermissionAction.Create)]
    [HttpGet("receipt/next-voucher-no")]
    public async Task<ActionResult<NextVoucherNoDto>> ReserveNextReceiptNo(CancellationToken ct)
        => Ok(await _service.ReserveNextVoucherNoAsync(VoucherType.Receipt, ct));

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

    // Gated the same way Cancel below already is: Payment's own Edit permission stands in for
    // "can edit a voucher", regardless of whether this particular one is a Payment or a Receipt.
    [ModulePermission(ModuleName.Payment, PermissionAction.Edit)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<VoucherDto>> Update(int id, UpdatePaymentOrReceiptRequest request, CancellationToken ct)
        => Ok(await _service.UpdatePaymentOrReceiptAsync(id, request, ct));

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
