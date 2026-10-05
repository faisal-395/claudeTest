using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Application.Stock;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.SaleInvoices;

public class SaleInvoiceService : ISaleInvoiceService
{
    private readonly IApplicationDbContext _db;
    private readonly ILedgerPostingService _ledger;
    private readonly IInvoiceNumberGenerator _numberGenerator;
    private readonly IDateTimeProvider _clock;
    private readonly IStockService _stock;
    private readonly ICurrentUser _currentUser;

    public SaleInvoiceService(IApplicationDbContext db, ILedgerPostingService ledger, IInvoiceNumberGenerator numberGenerator, IDateTimeProvider clock, IStockService stock, ICurrentUser currentUser)
    {
        _db = db;
        _ledger = ledger;
        _numberGenerator = numberGenerator;
        _clock = clock;
        _stock = stock;
        _currentUser = currentUser;
    }

    public async Task<List<SaleInvoiceDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _db.SaleInvoices.Include(s => s.Customer).Include(s => s.Lines).ThenInclude(l => l.Product)
            .Where(s => !s.IsDeleted).OrderByDescending(s => s.Date).ThenByDescending(s => s.Id).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<SaleInvoiceDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var row = await LoadAsync(id, ct);
        return ToDto(row);
    }

    public async Task<NextSaleInvoiceNoDto> ReserveNextInvoiceNoAsync(CancellationToken ct = default) => new(await _numberGenerator.NextAsync("S", ct));

    public async Task<SaleInvoiceDto> CreateAsync(CreateSaleInvoiceRequest request, CancellationToken ct = default)
    {
        if (!await _db.Parties.AnyAsync(p => p.Id == request.CustomerId && !p.IsDeleted && (p.PartyType & PartyType.Farmer) == PartyType.Farmer, ct))
            throw new NotFoundException(nameof(Party), request.CustomerId);

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var productCount = await _db.Products.CountAsync(p => productIds.Contains(p.Id) && !p.IsDeleted, ct);
        if (productCount != productIds.Count)
            throw new InvalidCalculationException("One or more products on the invoice do not exist.");

        // Sale Invoice is for farm inputs (pesticides, seeds, fertilizer) sold to a farmer — not the
        // grain catalog Kachi/Pakki trade in.
        var nonInputCount = await _db.Products.CountAsync(p => productIds.Contains(p.Id) && p.Category != DomainConstants.ProductCategoryInput, ct);
        if (nonInputCount > 0)
            throw new InvalidCalculationException("Sale Invoice can only include input products (pesticides, seeds, fertilizer), not grain products.");

        var sale = new SaleInvoice
        {
            InvoiceNo = request.InvoiceNo ?? await _numberGenerator.NextAsync("S", ct),
            BillNo = request.BillNo,
            Date = request.Date,
            Description = request.Description,
            CustomerId = request.CustomerId,
            PrintFormat = request.PrintFormat,
            PrintLanguage = request.PrintLanguage
        };

        // Each line draws its quantity from the product's oldest available purchase lot(s) first
        // (FIFO) — AllocateFifoAsync itself throws if stock is insufficient, which doubles as this
        // request's stock-availability check; nothing is persisted until SaveChangesAsync below, so
        // a failure partway through leaves no partial allocation behind.
        decimal totalBill = 0, totalDiscount = 0;
        var allocationsToAdd = new List<SaleInvoiceLineAllocation>();
        foreach (var line in request.Lines)
        {
            var gross = Math.Round(line.Quantity * line.Price, 2);
            var discount = Math.Round(gross * (line.DiscountPercent / 100m), 2);
            var net = gross - discount;
            totalBill += gross;
            totalDiscount += discount;

            var saleLine = new SaleInvoiceLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                Price = line.Price,
                DiscountPercent = line.DiscountPercent,
                NetPrice = net
            };
            sale.Lines.Add(saleLine);

            var fifoAllocations = await _stock.AllocateFifoAsync(line.ProductId, line.Quantity, ct);
            allocationsToAdd.AddRange(fifoAllocations.Select(a => new SaleInvoiceLineAllocation
            {
                SaleInvoiceLine = saleLine,
                PurchaseLineId = a.PurchaseLine.Id,
                Quantity = a.Quantity,
                UnitCost = a.UnitCost
            }));
        }

        var netBill = totalBill - totalDiscount;
        if (netBill < 0) throw new InvalidCalculationException("Net bill cannot be negative.");

        sale.TotalBill = totalBill;
        sale.TotalDiscount = totalDiscount;
        sale.NetBill = netBill;
        sale.ReceivedCash = request.ReceivedCash;
        sale.PayCash = request.ReceivedCash > netBill ? request.ReceivedCash - netBill : 0;
        sale.ApprovalStatus = await DetermineApprovalStatusAsync(ct);
        sale.SubmittedByUserId = _currentUser.UserId;

        _db.SaleInvoices.Add(sale);
        _db.SaleInvoiceLineAllocations.AddRange(allocationsToAdd);
        await _db.SaveChangesAsync(ct);

        if (sale.ApprovalStatus == ApprovalStatus.Approved)
        {
            await PostLedgerAsync(sale, ct);
            await _db.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(sale.Id, ct);
    }

    /// <summary>Posts a Sale Invoice's customer/income/cash entries straight from its own stored
    /// fields — used both right after Create (when it doesn't need approval) and by ApproveAsync.</summary>
    private async Task PostLedgerAsync(SaleInvoice sale, CancellationToken ct)
    {
        var salesIncomeAccountId = await GetAccountIdAsync(DomainConstants.SalesIncomeAccountCode, ct);
        var cashAccountId = await GetAccountIdAsync(DomainConstants.CashAccountCode, ct);

        await _ledger.PostPartyEntryAsync(sale.CustomerId, sale.Date, sale.NetBill, 0, LedgerSourceType.Sale, sale.Id, $"Sale {sale.InvoiceNo}", ct);
        await _ledger.PostAccountEntryAsync(salesIncomeAccountId, sale.Date, 0, sale.NetBill, LedgerSourceType.Sale, sale.Id, $"Sale {sale.InvoiceNo}", ct);

        var cashApplied = Math.Min(sale.ReceivedCash, sale.NetBill);
        if (cashApplied > 0)
        {
            await _ledger.PostAccountEntryAsync(cashAccountId, sale.Date, cashApplied, 0, LedgerSourceType.Sale, sale.Id, $"Cash received: {sale.InvoiceNo}", ct);
            await _ledger.PostPartyEntryAsync(sale.CustomerId, sale.Date, 0, cashApplied, LedgerSourceType.Sale, sale.Id, $"Cash received: {sale.InvoiceNo}", ct);
        }
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        var sale = await LoadAsync(id, ct);
        if (sale.IsCancelled) return;

        sale.IsCancelled = true;
        sale.UpdatedAtUtc = _clock.UtcNow;

        // Nothing was ever posted for a Sale Invoice still awaiting (or denied) review, so there is
        // nothing to reverse — only an Approved Sale Invoice has live ledger entries.
        if (sale.ApprovalStatus == ApprovalStatus.Approved)
        {
            var salesIncomeAccountId = await GetAccountIdAsync(DomainConstants.SalesIncomeAccountCode, ct);
            var cashAccountId = await GetAccountIdAsync(DomainConstants.CashAccountCode, ct);
            var reason = $"Reversal: {sale.InvoiceNo} cancelled";

            // Dated with the sale's own Date, not _clock.UtcNow: LedgerQueryService and
            // LedgerPostingService's "find the latest balance" lookup both sort primarily by Date, so
            // a reversal dated with the real wall-clock time (which carries a time-of-day, unlike the
            // midnight-only business Date on every normal entry) would sort as "later" than same-day
            // entries actually posted after it — scrambling both the displayed order and the running
            // balance chain on any same-day cancel.
            await _ledger.PostPartyEntryAsync(sale.CustomerId, sale.Date, 0, sale.NetBill, LedgerSourceType.Sale, sale.Id, reason, ct);
            await _ledger.PostAccountEntryAsync(salesIncomeAccountId, sale.Date, sale.NetBill, 0, LedgerSourceType.Sale, sale.Id, reason, ct);

            var cashApplied = Math.Min(sale.ReceivedCash, sale.NetBill);
            if (cashApplied > 0)
            {
                await _ledger.PostAccountEntryAsync(cashAccountId, sale.Date, 0, cashApplied, LedgerSourceType.Sale, sale.Id, reason, ct);
                await _ledger.PostPartyEntryAsync(sale.CustomerId, sale.Date, cashApplied, 0, LedgerSourceType.Sale, sale.Id, reason, ct);
            }
        }

        // Return this invoice's FIFO allocations to their source lots so cancelling frees the stock
        // back up for future sales.
        var lineIds = sale.Lines.Select(l => l.Id).ToList();
        var allocations = await _db.SaleInvoiceLineAllocations.Include(a => a.PurchaseLine)
            .Where(a => lineIds.Contains(a.SaleInvoiceLineId) && !a.IsDeleted).ToListAsync(ct);
        foreach (var allocation in allocations)
        {
            allocation.PurchaseLine.RemainingQuantity += allocation.Quantity;
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Approves a Pending Sale Invoice, posting the ledger entries that were deferred at creation.</summary>
    public async Task<SaleInvoiceDto> ApproveAsync(int id, CancellationToken ct = default)
    {
        var sale = await LoadAsync(id, ct);
        if (sale.ApprovalStatus != ApprovalStatus.Pending)
        {
            throw new InvalidCalculationException("Only a Sale Invoice awaiting review can be approved.");
        }

        sale.ApprovalStatus = ApprovalStatus.Approved;
        sale.ReviewedByUserId = _currentUser.UserId;
        sale.ReviewedAtUtc = _clock.UtcNow;
        sale.UpdatedAtUtc = _clock.UtcNow;

        await PostLedgerAsync(sale, ct);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    /// <summary>Rejects a Pending Sale Invoice — nothing was ever posted, so there is nothing to
    /// reverse. There is no edit for a Sale Invoice today, so the submitter must Cancel and
    /// re-enter it.</summary>
    public async Task<SaleInvoiceDto> RejectAsync(int id, string? reason, CancellationToken ct = default)
    {
        var sale = await LoadAsync(id, ct);
        if (sale.ApprovalStatus != ApprovalStatus.Pending)
        {
            throw new InvalidCalculationException("Only a Sale Invoice awaiting review can be rejected.");
        }

        sale.ApprovalStatus = ApprovalStatus.Rejected;
        sale.ReviewedByUserId = _currentUser.UserId;
        sale.ReviewedAtUtc = _clock.UtcNow;
        sale.RejectionReason = reason;
        sale.UpdatedAtUtc = _clock.UtcNow;

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

    private async Task<SaleInvoice> LoadAsync(int id, CancellationToken ct)
    {
        return await _db.SaleInvoices.Include(s => s.Customer).Include(s => s.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(SaleInvoice), id);
    }

    private static SaleInvoiceDto ToDto(SaleInvoice s) => new(
        s.Id, s.InvoiceNo, s.BillNo, s.Date, s.Description, s.CustomerId, s.Customer.Name,
        s.TotalBill, s.TotalDiscount, s.NetBill, s.ReceivedCash, s.PayCash, s.PrintFormat, s.PrintLanguage, s.IsCancelled,
        s.ApprovalStatus, s.SubmittedByUserId, s.RejectionReason,
        s.Lines.Select(l => new SaleInvoiceLineDto(l.ProductId, l.Product.Name, l.Quantity, l.Price, l.DiscountPercent, l.NetPrice)).ToList());
}
