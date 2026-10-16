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
    private readonly ICurrentUser _currentUser;

    public PurchaseService(IApplicationDbContext db, ILedgerPostingService ledger, IInvoiceNumberGenerator numberGenerator, IDateTimeProvider clock, ICurrentUser currentUser)
    {
        _db = db;
        _ledger = ledger;
        _numberGenerator = numberGenerator;
        _clock = clock;
        _currentUser = currentUser;
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

    public async Task<NextPurchaseInvoiceNoDto> ReserveNextInvoiceNoAsync(CancellationToken ct = default) => new(await _numberGenerator.NextAsync("PU", ct));

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
            InvoiceNo = request.InvoiceNo ?? await _numberGenerator.NextAsync("PU", ct),
            BillNo = request.BillNo,
            Date = request.Date,
            Description = request.Description,
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
                NetPrice = net,
                RemainingQuantity = line.Quantity,
                ExpiryDate = line.ExpiryDate
            });
        }

        var netBill = totalBill - totalDiscount;
        if (netBill < 0) throw new InvalidCalculationException("Net bill cannot be negative.");

        purchase.TotalBill = totalBill;
        purchase.TotalDiscount = totalDiscount;
        purchase.NetBill = netBill;
        purchase.PaidCash = request.PaidCash;
        purchase.ApprovalStatus = await DetermineApprovalStatusAsync(ct);
        purchase.SubmittedByUserId = _currentUser.UserId;

        _db.Purchases.Add(purchase);
        await _db.SaveChangesAsync(ct);

        if (purchase.ApprovalStatus == ApprovalStatus.Approved)
        {
            await PostLedgerAsync(purchase, ct);
            await _db.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(purchase.Id, ct);
    }

    /// <summary>Posts a Purchase's expense/supplier/cash entries straight from its own stored
    /// fields — used both right after Create (when it doesn't need approval) and by ApproveAsync.</summary>
    private async Task PostLedgerAsync(Purchase purchase, CancellationToken ct)
    {
        var purchaseExpenseAccountId = await GetAccountIdAsync(DomainConstants.PurchaseExpenseAccountCode, ct);
        var cashAccountId = await GetAccountIdAsync(DomainConstants.CashAccountCode, ct);

        await _ledger.PostAccountEntryAsync(purchaseExpenseAccountId, purchase.Date, purchase.NetBill, 0, LedgerSourceType.Purchase, purchase.Id, $"Purchase {purchase.InvoiceNo}", ct);
        await _ledger.PostPartyEntryAsync(purchase.SupplierId, purchase.Date, 0, purchase.NetBill, LedgerSourceType.Purchase, purchase.Id, $"Purchase {purchase.InvoiceNo}", ct);

        var cashApplied = Math.Min(purchase.PaidCash, purchase.NetBill);
        if (cashApplied > 0)
        {
            await _ledger.PostPartyEntryAsync(purchase.SupplierId, purchase.Date, cashApplied, 0, LedgerSourceType.Purchase, purchase.Id, $"Cash paid: {purchase.InvoiceNo}", ct);
            await _ledger.PostAccountEntryAsync(cashAccountId, purchase.Date, 0, cashApplied, LedgerSourceType.Purchase, purchase.Id, $"Cash paid: {purchase.InvoiceNo}", ct);
        }
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        var purchase = await LoadAsync(id, ct);
        if (purchase.IsCancelled) return;

        // A cancelled purchase's lots drop out of stock entirely — if a Sale Invoice has already
        // drawn FIFO stock from one of them, cancelling now would silently invalidate that sale's
        // cost basis and on-hand math.
        if (purchase.Lines.Any(l => l.RemainingQuantity < l.Quantity))
            throw new InvalidCalculationException("This purchase can't be cancelled: some of its stock has already been sold.");

        purchase.IsCancelled = true;
        purchase.UpdatedAtUtc = _clock.UtcNow;

        // Nothing was ever posted for a Purchase still awaiting (or denied) review, so there is
        // nothing to reverse — only an Approved Purchase has live ledger entries.
        if (purchase.ApprovalStatus == ApprovalStatus.Approved)
        {
            var purchaseExpenseAccountId = await GetAccountIdAsync(DomainConstants.PurchaseExpenseAccountCode, ct);
            var cashAccountId = await GetAccountIdAsync(DomainConstants.CashAccountCode, ct);
            var reason = $"Reversal: {purchase.InvoiceNo} cancelled";

            // Dated with the purchase's own Date, not _clock.UtcNow: LedgerQueryService and
            // LedgerPostingService's "find the latest balance" lookup both sort primarily by Date, so
            // a reversal dated with the real wall-clock time (which carries a time-of-day, unlike the
            // midnight-only business Date on every normal entry) would sort as "later" than same-day
            // entries actually posted after it — scrambling both the displayed order and the running
            // balance chain on any same-day cancel.
            await _ledger.PostAccountEntryAsync(purchaseExpenseAccountId, purchase.Date, 0, purchase.NetBill, LedgerSourceType.Purchase, purchase.Id, reason, ct);
            await _ledger.PostPartyEntryAsync(purchase.SupplierId, purchase.Date, purchase.NetBill, 0, LedgerSourceType.Purchase, purchase.Id, reason, ct);

            var cashApplied = Math.Min(purchase.PaidCash, purchase.NetBill);
            if (cashApplied > 0)
            {
                await _ledger.PostPartyEntryAsync(purchase.SupplierId, purchase.Date, 0, cashApplied, LedgerSourceType.Purchase, purchase.Id, reason, ct);
                await _ledger.PostAccountEntryAsync(cashAccountId, purchase.Date, cashApplied, 0, LedgerSourceType.Purchase, purchase.Id, reason, ct);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Approves a Pending Purchase, posting the ledger entries that were deferred at creation.</summary>
    public async Task<PurchaseDto> ApproveAsync(int id, CancellationToken ct = default)
    {
        var purchase = await LoadAsync(id, ct);
        if (purchase.ApprovalStatus != ApprovalStatus.Pending)
        {
            throw new InvalidCalculationException("Only a Purchase awaiting review can be approved.");
        }

        purchase.ApprovalStatus = ApprovalStatus.Approved;
        purchase.ReviewedByUserId = _currentUser.UserId;
        purchase.ReviewedAtUtc = _clock.UtcNow;
        purchase.UpdatedAtUtc = _clock.UtcNow;

        await PostLedgerAsync(purchase, ct);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    /// <summary>Rejects a Pending Purchase — nothing was ever posted, so there is nothing to
    /// reverse. There is no edit for a Purchase today, so the submitter must Cancel and re-enter it.</summary>
    public async Task<PurchaseDto> RejectAsync(int id, string? reason, CancellationToken ct = default)
    {
        var purchase = await LoadAsync(id, ct);
        if (purchase.ApprovalStatus != ApprovalStatus.Pending)
        {
            throw new InvalidCalculationException("Only a Purchase awaiting review can be rejected.");
        }

        purchase.ApprovalStatus = ApprovalStatus.Rejected;
        purchase.ReviewedByUserId = _currentUser.UserId;
        purchase.ReviewedAtUtc = _clock.UtcNow;
        purchase.RejectionReason = reason;
        purchase.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    private async Task<ApprovalStatus> DetermineApprovalStatusAsync(CancellationToken ct)
    {
        if (_currentUser.RoleId is null) return ApprovalStatus.Approved;
        var requiresApproval = await _db.Roles.Where(r => r.Id == _currentUser.RoleId).Select(r => r.RequiresApproval).FirstOrDefaultAsync(ct);
        return requiresApproval ? ApprovalStatus.Pending : ApprovalStatus.Approved;
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
        p.Id, p.InvoiceNo, p.BillNo, p.Date, p.Description, p.SupplierId, p.Supplier.Name,
        p.TotalBill, p.TotalDiscount, p.NetBill, p.PaidCash, p.PrintFormat, p.PrintLanguage, p.IsCancelled,
        p.ApprovalStatus, p.SubmittedByUserId, p.RejectionReason,
        p.Lines.Select(l => new PurchaseLineDto(l.ProductId, l.Product.Name, l.Quantity, l.Price, l.DiscountPercent, l.NetPrice, l.ExpiryDate)).ToList());
}
