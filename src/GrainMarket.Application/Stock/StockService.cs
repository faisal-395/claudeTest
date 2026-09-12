using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Stock;

/// <summary>Stock is always derived on-the-fly from non-cancelled Purchase/SaleInvoice lines rather
/// than a stored running-balance column, so it can never drift from the transaction history and stays
/// consistent even when a purchase or sale is later cancelled — the same principle the Ledger uses.</summary>
public class StockService : IStockService
{
    private readonly IApplicationDbContext _db;

    public StockService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<StockDto>> GetStockAsync(CancellationToken ct = default)
    {
        var products = await _db.Products
            .Where(p => !p.IsDeleted && p.IsActive && p.Category == DomainConstants.ProductCategoryInput)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var purchased = await _db.Purchases.Where(p => !p.IsDeleted && !p.IsCancelled)
            .SelectMany(p => p.Lines)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(l => l.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, ct);

        var sold = await _db.SaleInvoices.Where(s => !s.IsDeleted && !s.IsCancelled)
            .SelectMany(s => s.Lines)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(l => l.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, ct);

        return products.Select(p =>
        {
            var purchasedQty = purchased.GetValueOrDefault(p.Id);
            var soldQty = sold.GetValueOrDefault(p.Id);
            return new StockDto(p.Id, p.Name, p.NameUrdu, p.BaseUnit, purchasedQty, soldQty, purchasedQty - soldQty);
        }).ToList();
    }

    public async Task<decimal> GetOnHandQtyAsync(int productId, CancellationToken ct = default)
    {
        var purchasedQty = await _db.Purchases.Where(p => !p.IsDeleted && !p.IsCancelled)
            .SelectMany(p => p.Lines).Where(l => l.ProductId == productId)
            .SumAsync(l => (decimal?)l.Quantity, ct) ?? 0;

        var soldQty = await _db.SaleInvoices.Where(s => !s.IsDeleted && !s.IsCancelled)
            .SelectMany(s => s.Lines).Where(l => l.ProductId == productId)
            .SumAsync(l => (decimal?)l.Quantity, ct) ?? 0;

        return purchasedQty - soldQty;
    }
}
