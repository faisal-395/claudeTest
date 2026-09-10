using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Application.DualInvoice;
using GrainMarket.Application.Pakkis;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.MultiPurchase;

public class MultiPurchaseService : IMultiPurchaseService
{
    private readonly IApplicationDbContext _db;
    private readonly IDualInvoiceService _dualInvoiceService;
    private readonly IPakkiService _pakkiService;

    public MultiPurchaseService(IApplicationDbContext db, IDualInvoiceService dualInvoiceService, IPakkiService pakkiService)
    {
        _db = db;
        _dualInvoiceService = dualInvoiceService;
        _pakkiService = pakkiService;
    }

    public async Task<MultiPurchaseResultDto> CreateAsync(CreateMultiPurchaseRequest request, CancellationToken ct = default)
    {
        var buyer = await _db.Parties.FirstOrDefaultAsync(p => p.Id == request.BuyerId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Party), request.BuyerId);

        var rows = new List<MultiPurchaseRowResultDto>();
        decimal grandGross = 0, grandDeductions = 0, grandNet = 0;

        // Each row is its own Kachi+Pakki — every farmer still gets paid against their own net
        // weight and deductions, exactly as if entered one at a time; this just raises all of
        // them for one buyer in a single submit and hands back everything needed to print them
        // together as one consolidated receipt.
        foreach (var row in request.Rows)
        {
            var dual = await _dualInvoiceService.CreateAsync(new CreateDualInvoiceRequest(
                request.Date, request.SeasonId, row.FarmerId, request.BuyerId, row.ProductId,
                row.ManQty, row.KiloQty, row.GramQty, row.BoriQty, row.RatePerUnit, row.VehicleNumber, row.Notes), ct);

            var pakki = await _pakkiService.GetByIdAsync(dual.PakkiId, ct);
            rows.Add(new MultiPurchaseRowResultDto(pakki.Id, pakki.InvoiceNo));

            grandGross += pakki.GrossAmount;
            grandDeductions += pakki.TotalDeductions;
            grandNet += pakki.NetPayableToFarmer;
        }

        return new MultiPurchaseResultDto(buyer.Id, buyer.Name, request.Date, rows, grandGross, grandDeductions, grandNet);
    }
}
