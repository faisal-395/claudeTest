using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Stock;

/// <summary>Stock is tracked lot-by-lot: every PurchaseLine is its own lot carrying a
/// RemainingQuantity, so on-hand quantity and expiry stay tied to the specific goods actually in
/// stock rather than a single aggregate balance. Physical stock movement (AllocateFifoAsync) is
/// FIFO — the oldest lot is drawn down first, which is what actually leaves the shelf and what
/// expiry tracking assumes. The sale rate preview (GetSuggestedSalePriceAsync) is priced on LIFO
/// instead: always the single most recent purchase price, not a blend with older stock — so the
/// moment a new purchase lands at a different price, that immediately becomes the pricing basis,
/// even while older (cheaper or costlier) stock is what physically ships first.</summary>
public class StockService : IStockService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public StockService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<StockDto>> GetStockAsync(CancellationToken ct = default)
    {
        var products = await _db.Products
            .Where(p => !p.IsDeleted && p.IsActive && p.Category == DomainConstants.ProductCategoryInput)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var onHand = await ActiveLines()
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(l => l.RemainingQuantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, ct);

        var purchased = await ActiveLines()
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(l => l.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty, ct);

        return products.Select(p =>
        {
            var onHandQty = onHand.GetValueOrDefault(p.Id);
            var purchasedQty = purchased.GetValueOrDefault(p.Id);
            return new StockDto(p.Id, p.Name, p.NameUrdu, p.BaseUnit, purchasedQty, purchasedQty - onHandQty, onHandQty);
        }).ToList();
    }

    public async Task<SuggestedSalePriceDto> GetSuggestedSalePriceAsync(int productId, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Product), productId);

        // The single most recent purchase (by Purchase date, then line Id as tie-break) — not
        // filtered by RemainingQuantity, so a fully-depleted-but-most-recent lot still counts as the
        // latest known cost rather than falling back to an older, still-in-stock lot's price.
        var latestLine = await ActiveLines().Include(l => l.Purchase)
            .Where(l => l.ProductId == productId)
            .OrderByDescending(l => l.Purchase.Date).ThenByDescending(l => l.Id)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        var latestCost = latestLine?.Price ?? product.DefaultRate;
        var availableQty = await ActiveLines().Where(l => l.ProductId == productId)
            .SumAsync(l => (decimal?)l.RemainingQuantity, ct) ?? 0;

        var suggestedPrice = product.SalePrice
            ?? (product.SaleMarkupPercent.HasValue ? Math.Round(latestCost * (1 + product.SaleMarkupPercent.Value / 100m), 2) : latestCost);

        return new SuggestedSalePriceDto(availableQty, latestCost, suggestedPrice);
    }

    public async Task<List<FifoAllocation>> AllocateFifoAsync(int productId, decimal quantity, CancellationToken ct = default)
    {
        var lines = await OrderedAvailableLinesAsync(productId, ct, tracked: true);
        var availableQty = lines.Sum(l => l.RemainingQuantity);
        if (quantity > availableQty)
            throw new InvalidCalculationException($"Not enough stock: only {availableQty:N2} available, {quantity:N2} requested.");

        var allocations = new List<FifoAllocation>();
        var remaining = quantity;
        foreach (var line in lines)
        {
            if (remaining <= 0) break;
            // A prior line in the same request may already have depleted this lot in memory — the
            // query above still returns it because its filter runs against the (unchanged-until-
            // SaveChanges) database value, but EF's identity resolution hands back the same tracked
            // instance with today's real in-memory RemainingQuantity, so skip it here instead.
            if (line.RemainingQuantity <= 0) continue;
            var take = Math.Min(remaining, line.RemainingQuantity);
            line.RemainingQuantity -= take;
            allocations.Add(new FifoAllocation(line, take, line.Price));
            remaining -= take;
        }

        return allocations;
    }

    public async Task<List<ExpiringLotDto>> GetExpiringLotsAsync(int withinDays, CancellationToken ct = default)
    {
        var today = _clock.UtcNow.Date;
        var cutoff = today.AddDays(withinDays);

        var lines = await _db.PurchaseLines
            .Include(l => l.Product)
            .Where(l => !l.IsDeleted && !l.Purchase.IsCancelled && !l.Purchase.IsDeleted
                && l.ExpiryDate != null && l.ExpiryDate <= cutoff && l.RemainingQuantity > 0)
            .OrderBy(l => l.ExpiryDate)
            .ToListAsync(ct);

        return lines.Select(l => new ExpiringLotDto(
            l.ProductId, l.Product.Name, l.Product.NameUrdu, l.Product.BaseUnit,
            l.Id, l.ExpiryDate!.Value, l.RemainingQuantity, (l.ExpiryDate.Value.Date - today).Days)).ToList();
    }

    // Deliberately no .Include(Purchase) here — this is shared by plain aggregate (GroupBy/Sum)
    // queries too, where an Include would be a no-op EF Core warns about. The `!l.Purchase.IsCancelled`
    // filter still translates to a join either way; call sites that need Purchase.Date materialized
    // (ordering by it below, or projecting it) add their own .Include(l => l.Purchase).
    private IQueryable<PurchaseLine> ActiveLines() =>
        _db.PurchaseLines.Where(l => !l.IsDeleted && !l.Purchase.IsCancelled && !l.Purchase.IsDeleted);

    /// <summary>FIFO — oldest lot (by Purchase date, then line Id) first, matching physical
    /// stock draw-down.</summary>
    private async Task<List<PurchaseLine>> OrderedAvailableLinesAsync(int productId, CancellationToken ct, bool tracked = false)
    {
        var query = ActiveLines().Include(l => l.Purchase).Where(l => l.ProductId == productId && l.RemainingQuantity > 0);
        if (!tracked) query = query.AsNoTracking();
        return await query.OrderBy(l => l.Purchase.Date).ThenBy(l => l.Id).ToListAsync(ct);
    }
}
