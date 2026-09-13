using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Stock;

/// <summary>Stock is tracked lot-by-lot: every PurchaseLine is its own FIFO lot carrying a
/// RemainingQuantity that Sale Invoice draws down oldest-first (AllocateFifoAsync), so on-hand
/// quantity, cost, and expiry all stay tied to the specific goods actually in stock rather than a
/// single aggregate balance.</summary>
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

    public async Task<SuggestedSalePriceDto> GetSuggestedSalePriceAsync(int productId, decimal quantity, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Product), productId);

        var lines = await OrderedAvailableLinesAsync(productId, ct);
        var availableQty = lines.Sum(l => l.RemainingQuantity);

        var remaining = quantity;
        decimal costTotal = 0, costedQty = 0;
        foreach (var line in lines)
        {
            if (remaining <= 0) break;
            var take = Math.Min(remaining, line.RemainingQuantity);
            costTotal += take * line.Price;
            costedQty += take;
            remaining -= take;
        }
        // Requested quantity exceeds on-hand stock — price the shortfall at the newest lot's cost
        // (or the product's default rate if there's no purchase history at all) so the preview still
        // returns a sensible number instead of understating it; Sale Invoice itself still blocks the
        // sale server-side if stock is actually insufficient at save time.
        if (remaining > 0)
        {
            var fallbackCost = lines.Count > 0 ? lines[^1].Price : product.DefaultRate;
            costTotal += remaining * fallbackCost;
            costedQty += remaining;
        }

        var averageCost = costedQty > 0 ? costTotal / costedQty : 0;
        var suggestedPrice = product.SalePrice
            ?? (product.SaleMarkupPercent.HasValue ? Math.Round(averageCost * (1 + product.SaleMarkupPercent.Value / 100m), 2) : averageCost);

        return new SuggestedSalePriceDto(availableQty, averageCost, suggestedPrice);
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

    private IQueryable<PurchaseLine> ActiveLines() =>
        _db.PurchaseLines.Where(l => !l.IsDeleted && !l.Purchase.IsCancelled && !l.Purchase.IsDeleted);

    private async Task<List<PurchaseLine>> OrderedAvailableLinesAsync(int productId, CancellationToken ct, bool tracked = false)
    {
        var query = ActiveLines().Include(l => l.Purchase).Where(l => l.ProductId == productId && l.RemainingQuantity > 0);
        if (!tracked) query = query.AsNoTracking();
        return await query.OrderBy(l => l.Purchase.Date).ThenBy(l => l.Id).ToListAsync(ct);
    }
}
