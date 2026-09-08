using GrainMarket.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Trading;

public class TradingService : ITradingService
{
    private readonly IApplicationDbContext _db;

    public TradingService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<TradingProductPositionDto>> GetStockPositionAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var purchaseLines = _db.PurchaseLines.Include(l => l.Product).Include(l => l.Purchase)
            .Where(l => !l.IsDeleted && !l.Purchase.IsDeleted);
        var saleLines = _db.SaleInvoiceLines.Include(l => l.Product).Include(l => l.SaleInvoice)
            .Where(l => !l.IsDeleted && !l.SaleInvoice.IsDeleted);

        if (from is not null)
        {
            purchaseLines = purchaseLines.Where(l => l.Purchase.Date >= from);
            saleLines = saleLines.Where(l => l.SaleInvoice.Date >= from);
        }
        if (to is not null)
        {
            purchaseLines = purchaseLines.Where(l => l.Purchase.Date <= to);
            saleLines = saleLines.Where(l => l.SaleInvoice.Date <= to);
        }

        var purchased = await purchaseLines
            .GroupBy(l => new { l.ProductId, l.Product.Name })
            .Select(g => new { g.Key.ProductId, g.Key.Name, Qty = g.Sum(x => x.Quantity), Value = g.Sum(x => x.NetPrice) })
            .ToListAsync(ct);

        var sold = await saleLines
            .GroupBy(l => new { l.ProductId, l.Product.Name })
            .Select(g => new { g.Key.ProductId, g.Key.Name, Qty = g.Sum(x => x.Quantity), Value = g.Sum(x => x.NetPrice) })
            .ToListAsync(ct);

        var productIds = purchased.Select(p => p.ProductId).Union(sold.Select(s => s.ProductId)).Distinct();

        return productIds.Select(id =>
        {
            var p = purchased.FirstOrDefault(x => x.ProductId == id);
            var s = sold.FirstOrDefault(x => x.ProductId == id);
            var name = p?.Name ?? s?.Name ?? string.Empty;
            var purchasedQty = p?.Qty ?? 0m;
            var soldQty = s?.Qty ?? 0m;
            return new TradingProductPositionDto(id, name, purchasedQty, soldQty, purchasedQty - soldQty, p?.Value ?? 0m, s?.Value ?? 0m);
        })
        .OrderBy(x => x.ProductName)
        .ToList();
    }
}
