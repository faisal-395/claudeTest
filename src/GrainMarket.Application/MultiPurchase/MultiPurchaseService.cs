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

    public MultiPurchaseService(IApplicationDbContext db, IKachiService kachiService)
    {
        _db = db;
        _kachiService = kachiService;
    }

    public async Task<MultiPurchaseResultDto> CreateAsync(CreateMultiPurchaseRequest request, CancellationToken ct = default)
    {
        var buyer = await _db.Parties.FirstOrDefaultAsync(p => p.Id == request.BuyerId && !p.IsDeleted && p.PartyType == PartyType.Buyer, ct)
            ?? throw new NotFoundException(nameof(Party), request.BuyerId);

        var rows = new List<MultiPurchaseRowResultDto>();
        decimal grandGross = 0, grandDeductions = 0, grandTotal = 0;

        // Each row is its own Kachi — one farmer's lot, with the shared buyer already attached.
        // This raises every row for one buyer in a single submit; it never touches Pakki, which
        // stays a deliberate, separate step per farmer with its own deductions.
        foreach (var row in request.Rows)
        {
            var kachi = await _kachiService.CreateAsync(new CreateKachiRequest(
                request.Date, request.SeasonId, row.FarmerId, request.BuyerId, row.ProductId,
                row.BhartiKgPerBag, row.TotalWeightKg, row.RatePerUnit, row.VehicleNumber, row.Notes), ct);

            rows.Add(new MultiPurchaseRowResultDto(kachi.Id, kachi.InvoiceNo));

            grandGross += kachi.GrossAmount;
            grandDeductions += kachi.TotalDeductions;
            grandTotal += kachi.Total;
        }

        return new MultiPurchaseResultDto(buyer.Id, buyer.Name, request.Date, rows, grandGross, grandDeductions, grandTotal);
    }
}
