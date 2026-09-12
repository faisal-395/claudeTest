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

    public SaleInvoiceService(IApplicationDbContext db, ILedgerPostingService ledger, IInvoiceNumberGenerator numberGenerator, IDateTimeProvider clock, IStockService stock)
    {
        _db = db;
        _ledger = ledger;
        _numberGenerator = numberGenerator;
        _clock = clock;
        _stock = stock;
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

        var productNames = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        foreach (var group in request.Lines.GroupBy(l => l.ProductId))
        {
            var requestedQty = group.Sum(l => l.Quantity);
            var onHandQty = await _stock.GetOnHandQtyAsync(group.Key, ct);
            if (requestedQty > onHandQty)
                throw new InvalidCalculationException($"Not enough stock for {productNames[group.Key]}: only {onHandQty:N2} available, {requestedQty:N2} requested.");
        }

        var sale = new SaleInvoice
        {
            InvoiceNo = await _numberGenerator.NextAsync("S", ct),
            BillNo = request.BillNo,
            Date = request.Date,
            CustomerId = request.CustomerId,
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

            sale.Lines.Add(new SaleInvoiceLine
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

        sale.TotalBill = totalBill;
        sale.TotalDiscount = totalDiscount;
        sale.NetBill = netBill;
        sale.ReceivedCash = request.ReceivedCash;
        sale.PayCash = request.ReceivedCash > netBill ? request.ReceivedCash - netBill : 0;

        _db.SaleInvoices.Add(sale);
        await _db.SaveChangesAsync(ct);

        var salesIncomeAccountId = await GetAccountIdAsync(DomainConstants.SalesIncomeAccountCode, ct);
        var cashAccountId = await GetAccountIdAsync(DomainConstants.CashAccountCode, ct);

        await _ledger.PostPartyEntryAsync(sale.CustomerId, sale.Date, netBill, 0, LedgerSourceType.Sale, sale.Id, $"Sale {sale.InvoiceNo}", ct);
        await _ledger.PostAccountEntryAsync(salesIncomeAccountId, sale.Date, 0, netBill, LedgerSourceType.Sale, sale.Id, $"Sale {sale.InvoiceNo}", ct);

        var cashApplied = Math.Min(request.ReceivedCash, netBill);
        if (cashApplied > 0)
        {
            await _ledger.PostAccountEntryAsync(cashAccountId, sale.Date, cashApplied, 0, LedgerSourceType.Sale, sale.Id, $"Cash received: {sale.InvoiceNo}", ct);
            await _ledger.PostPartyEntryAsync(sale.CustomerId, sale.Date, 0, cashApplied, LedgerSourceType.Sale, sale.Id, $"Cash received: {sale.InvoiceNo}", ct);
        }

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(sale.Id, ct);
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        var sale = await LoadAsync(id, ct);
        if (sale.IsCancelled) return;

        sale.IsCancelled = true;
        sale.UpdatedAtUtc = _clock.UtcNow;

        var salesIncomeAccountId = await GetAccountIdAsync(DomainConstants.SalesIncomeAccountCode, ct);
        var cashAccountId = await GetAccountIdAsync(DomainConstants.CashAccountCode, ct);
        var reason = $"Reversal: {sale.InvoiceNo} cancelled";

        await _ledger.PostPartyEntryAsync(sale.CustomerId, _clock.UtcNow, 0, sale.NetBill, LedgerSourceType.Sale, sale.Id, reason, ct);
        await _ledger.PostAccountEntryAsync(salesIncomeAccountId, _clock.UtcNow, sale.NetBill, 0, LedgerSourceType.Sale, sale.Id, reason, ct);

        var cashApplied = Math.Min(sale.ReceivedCash, sale.NetBill);
        if (cashApplied > 0)
        {
            await _ledger.PostAccountEntryAsync(cashAccountId, _clock.UtcNow, 0, cashApplied, LedgerSourceType.Sale, sale.Id, reason, ct);
            await _ledger.PostPartyEntryAsync(sale.CustomerId, _clock.UtcNow, cashApplied, 0, LedgerSourceType.Sale, sale.Id, reason, ct);
        }

        await _db.SaveChangesAsync(ct);
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
        s.Id, s.InvoiceNo, s.BillNo, s.Date, s.CustomerId, s.Customer.Name,
        s.TotalBill, s.TotalDiscount, s.NetBill, s.ReceivedCash, s.PayCash, s.PrintFormat, s.PrintLanguage, s.IsCancelled,
        s.Lines.Select(l => new SaleInvoiceLineDto(l.ProductId, l.Product.Name, l.Quantity, l.Price, l.DiscountPercent, l.NetPrice)).ToList());
}
