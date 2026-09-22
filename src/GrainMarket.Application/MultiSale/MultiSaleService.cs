using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Application.Pakkis;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.MultiSale;

public class MultiSaleService : IMultiSaleService
{
    private readonly IApplicationDbContext _db;
    private readonly IPakkiService _pakkiService;
    private readonly IInvoiceNumberGenerator _numberGenerator;

    public MultiSaleService(IApplicationDbContext db, IPakkiService pakkiService, IInvoiceNumberGenerator numberGenerator)
    {
        _db = db;
        _pakkiService = pakkiService;
        _numberGenerator = numberGenerator;
    }

    public async Task<MultiSaleResultDto> CreateAsync(CreateMultiSaleRequest request, CancellationToken ct = default)
    {
        var buyer = await _db.Parties.FirstOrDefaultAsync(p => p.Id == request.BuyerId && !p.IsDeleted && (p.PartyType & PartyType.Vendor) == PartyType.Vendor, ct)
            ?? throw new NotFoundException(nameof(Party), request.BuyerId);

        // One invoice number for the whole batch — not one per row — mirroring
        // MultiPurchaseService's fix: each row is still its own standalone Pakki (one farmer's
        // settlement, its own deductions/ledger postings), just sharing this one InvoiceNo.
        var invoiceNo = await _numberGenerator.NextAsync("P", ct);

        var rows = new List<MultiSaleRowResultDto>();
        decimal grandGross = 0, grandDeductions = 0, grandTotal = 0;

        foreach (var row in request.Rows)
        {
            var pakki = await _pakkiService.CreateStandaloneAsync(new CreateStandalonePakkiRequest(
                request.Date, request.SeasonId, request.BuyerId, row.FarmerId, row.ProductId,
                row.BhartiKgPerBag, row.TotalWeightKg, row.RatePerUnit!.Value, row.VehicleNumber, row.Notes, request.BillNumber,
                row.DeductionOverrides, InvoiceNo: invoiceNo), ct);

            rows.Add(new MultiSaleRowResultDto(pakki.Id, pakki.InvoiceNo));

            grandGross += pakki.GrossAmount;
            grandDeductions += pakki.TotalDeductions;
            grandTotal += pakki.NetPayableToFarmer;
        }

        return new MultiSaleResultDto(buyer.Id, buyer.Name, request.Date, rows, grandGross, grandDeductions, grandTotal);
    }
}
