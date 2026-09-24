using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Application.Kachis;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.MultiPurchase;

public class MultiPurchaseService : IMultiPurchaseService
{
    private readonly IApplicationDbContext _db;
    private readonly IKachiService _kachiService;
    private readonly IInvoiceNumberGenerator _numberGenerator;

    public MultiPurchaseService(IApplicationDbContext db, IKachiService kachiService, IInvoiceNumberGenerator numberGenerator)
    {
        _db = db;
        _kachiService = kachiService;
        _numberGenerator = numberGenerator;
    }

    public async Task<MultiPurchaseResultDto> CreateAsync(CreateMultiPurchaseRequest request, CancellationToken ct = default)
    {
        var buyer = await _db.Parties.FirstOrDefaultAsync(p => p.Id == request.BuyerId && !p.IsDeleted && (p.PartyType & PartyType.Vendor) == PartyType.Vendor, ct)
            ?? throw new NotFoundException(nameof(Party), request.BuyerId);

        // One invoice number for the whole batch (one buyer, several farmer rows) — not one per
        // row — so the buyer sees a single invoice covering everyone bought from in this sitting.
        // Each row is still its own Kachi underneath (one farmer's lot, its own weight/deductions/
        // ledger postings — farmers have separate ledger accounts, so that can't be merged), just
        // sharing this one InvoiceNo; it never touches Pakki, which stays a deliberate, separate
        // step per farmer with its own deductions.
        var invoiceNo = await _numberGenerator.NextAsync("K", ct);

        var rows = new List<MultiPurchaseRowResultDto>();
        decimal grandGross = 0, grandDeductions = 0, grandTotal = 0;

        foreach (var row in request.Rows)
        {
            var kachi = await _kachiService.CreateAsync(new CreateKachiRequest(
                request.Date, request.SeasonId, row.FarmerId, request.BuyerId, row.ProductId,
                row.BhartiKgPerBag, row.TotalWeightKg, row.RatePerUnit, row.VehicleNumber, row.Notes, request.BillNumber,
                row.DeductionOverrides, InvoiceNo: invoiceNo), ct);

            rows.Add(new MultiPurchaseRowResultDto(kachi.Id, kachi.InvoiceNo));

            grandGross += kachi.GrossAmount;
            grandDeductions += kachi.TotalDeductions;
            grandTotal += kachi.Total;
        }

        return new MultiPurchaseResultDto(buyer.Id, buyer.Name, request.Date, rows, grandGross, grandDeductions, grandTotal);
    }
}
