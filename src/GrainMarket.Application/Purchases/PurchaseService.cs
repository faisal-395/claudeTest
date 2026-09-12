using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Purchases;

public class PurchaseService : IPurchaseService
{
    private readonly IApplicationDbContext _db;
    private readonly ILedgerPostingService _ledger;
    private readonly IInvoiceNumberGenerator _numberGenerator;
    private readonly IDateTimeProvider _clock;

    public PurchaseService(IApplicationDbContext db, ILedgerPostingService ledger, IInvoiceNumberGenerator numberGenerator, IDateTimeProvider clock)
    {
        _db = db;
        _ledger = ledger;
        _numberGenerator = numberGenerator;
        _clock = clock;
    }

    public async Task<List<PurchaseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _db.Purchases.Include(p => p.Supplier).Include(p => p.Lines).ThenInclude(l => l.Product)
            .Where(p => !p.IsDeleted).OrderByDescending(p => p.Date).ThenByDescending(p => p.Id).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<PurchaseDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var row = await LoadAsync(id, ct);
        return ToDto(row);
    }

    public async Task<PurchaseDto> CreateAsync(CreatePurchaseRequest request, CancellationToken ct = default)
    {
        if (!await _db.Parties.AnyAsync(p => p.Id == request.SupplierId && !p.IsDeleted && (p.PartyType & PartyType.Supplier) == PartyType.Supplier, ct))
            throw new NotFoundException(nameof(Party), request.SupplierId);

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var productCount = await _db.Products.CountAsync(p => productIds.Contains(p.Id) && !p.IsDeleted, ct);
        if (productCount != productIds.Count)
            throw new InvalidCalculationException("One or more products on the purchase do not exist.");

        // Purchase is for farm inputs (pesticides, seeds, fertilizer) bought from a supplier — not
        // the grain catalog Kachi/Pakki trade in.
        var nonInputCount = await _db.Products.CountAsync(p => productIds.Contains(p.Id) && p.Category != DomainConstants.ProductCategoryInput, ct);
        if (nonInputCount > 0)
            throw new InvalidCalculationException("Purchase can only include input products (pesticides, seeds, fertilizer), not grain products.");

        var purchase = new Purchase
        {
            InvoiceNo = await _numberGenerator.NextAsync("PU", ct),
            BillNo = request.BillNo,
            Date = request.Date,
            SupplierId = request.SupplierId,
            PrintFormat = request.PrintFormat,
            PrintLanguage = request.PrintLanguage
        };

        decimal totalBill = 0, totalDiscount = 0;
        foreach (var line in request.Lines)
        {
            var gross = Math.Round(line.Quantity * line.Price, 2);
            var discount = Math.Round(gross * (line.DiscountPercent / 100m), 2);
            var net = gross - discount;
            totalBill += gross;
            totalDiscount += discount;

            purchase.Lines.Add(new PurchaseLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                Price = line.Price,
                DiscountPercent = line.DiscountPercent,
                NetPrice = net
            });
        }

        var netBill = totalBill - totalDiscount;
        if (netBill < 0) throw new InvalidCalculationException("Net bill cannot be negative.");

        purchase.TotalBill = totalBill;
        purchase.TotalDiscount = totalDiscount;
        purchase.NetBill = netBill;
        purchase.PaidCash = request.PaidCash;

        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync(ct);

        var purchaseExpenseAccountId = await GetAccountIdAsync(DomainConstants.PurchaseExpenseAccountCode, ct);
        var cashAccountId = await GetAccountIdAsync(DomainConstants.CashAccountCode, ct);

        await _ledger.PostAccountEntryAsync(purchaseExpenseAccountId, purchase.Date, netBill, 0, LedgerSourceType.Purchase, purchase.Id, $"Purchase {purchase.InvoiceNo}", ct);
        await _ledger.PostPartyEntryAsync(purchase.SupplierId, purchase.Date, 0, netBill, LedgerSourceType.Purchase, purchase.Id, $"Purchase {purchase.InvoiceNo}", ct);

        var cashApplied = Math.Min(request.PaidCash, netBill);
        if (cashApplied > 0)
        {
            await _ledger.PostPartyEntryAsync(purchase.SupplierId, purchase.Date, cashApplied, 0, LedgerSourceType.Purchase, purchase.Id, $"Cash paid: {purchase.InvoiceNo}", ct);
            await _ledger.PostAccountEntryAsync(cashAccountId, purchase.Date, 0, cashApplied, LedgerSourceType.Purchase, purchase.Id, $"Cash paid: {purchase.InvoiceNo}", ct);
        }

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(purchase.Id, ct);
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        var purchase = await LoadAsync(id, ct);
        if (purchase.IsCancelled) return;

        purchase.IsCancelled = true;
        purchase.UpdatedAtUtc = _clock.UtcNow;

        var purchaseExpenseAccountId = await GetAccountIdAsync(DomainConstants.PurchaseExpenseAccountCode, ct);
        var cashAccountId = await GetAccountIdAsync(DomainConstants.CashAccountCode, ct);
        var reason = $"Reversal: {purchase.InvoiceNo} cancelled";

        await _ledger.PostAccountEntryAsync(purchaseExpenseAccountId, _clock.UtcNow, 0, purchase.NetBill, LedgerSourceType.Purchase, purchase.Id, reason, ct);
        await _ledger.PostPartyEntryAsync(purchase.SupplierId, _clock.UtcNow, purchase.NetBill, 0, LedgerSourceType.Purchase, purchase.Id, reason, ct);

        var cashApplied = Math.Min(purchase.PaidCash, purchase.NetBill);
        if (cashApplied > 0)
        {
            await _ledger.PostPartyEntryAsync(purchase.SupplierId, _clock.UtcNow, 0, cashApplied, LedgerSourceType.Purchase, purchase.Id, reason, ct);
            await _ledger.PostAccountEntryAsync(cashAccountId, _clock.UtcNow, cashApplied, 0, LedgerSourceType.Purchase, purchase.Id, reason, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> GetAccountIdAsync(string code, CancellationToken ct)
    {
        var account = await _db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Code == code, ct)
            ?? throw new InvalidCalculationException($"Required chart-of-accounts row with code {code} is missing. Re-run seed data.");
        return account.Id;
    }

    private async Task<Purchase> LoadAsync(int id, CancellationToken ct)
    {
        return await _db.Purchases.Include(p => p.Supplier).Include(p => p.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Purchase), id);
    }

    private static PurchaseDto ToDto(Purchase p) => new(
        p.Id, p.InvoiceNo, p.BillNo, p.Date, p.SupplierId, p.Supplier.Name,
        p.TotalBill, p.TotalDiscount, p.NetBill, p.PaidCash, p.PrintFormat, p.PrintLanguage, p.IsCancelled,
        p.Lines.Select(l => new PurchaseLineDto(l.ProductId, l.Product.Name, l.Quantity, l.Price, l.DiscountPercent, l.NetPrice)).ToList());
}
