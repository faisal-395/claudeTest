using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Application.Common.Services;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Kachis;

public class KachiService : IKachiService
{
    private readonly IApplicationDbContext _db;
    private readonly ILedgerPostingService _ledger;
    private readonly IInvoiceNumberGenerator _numberGenerator;
    private readonly IDateTimeProvider _clock;

    public KachiService(IApplicationDbContext db, ILedgerPostingService ledger, IInvoiceNumberGenerator numberGenerator, IDateTimeProvider clock)
    {
        _db = db;
        _ledger = ledger;
        _numberGenerator = numberGenerator;
        _clock = clock;
    }

    public async Task<List<KachiDto>> GetAllAsync(int? seasonId = null, CancellationToken ct = default)
    {
        var query = _db.Kachis
            .Include(k => k.Season).Include(k => k.Farmer).Include(k => k.Buyer).Include(k => k.Product).Include(k => k.DeductionLines)
            .Where(k => !k.IsDeleted);
        if (seasonId is not null) query = query.Where(k => k.SeasonId == seasonId);

        var rows = await query.OrderByDescending(k => k.Date).ThenByDescending(k => k.Id).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<KachiDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var kachi = await LoadAsync(id, ct);
        return ToDto(kachi);
    }

    public async Task<KachiDto> CreateAsync(CreateKachiRequest request, CancellationToken ct = default)
    {
        await EnsureReferencesExistAsync(request.SeasonId, request.FarmerId, request.ProductId, ct);
        await EnsureBuyerExistsAsync(request.BuyerId, ct);

        var conversions = await _db.UnitConversions.Where(c => c.IsActive && !c.IsDeleted).ToListAsync(ct);
        var (netWeightKg, boriQty, resolvedDhrnKg) = CalculateWeights(request.TotalWeightKg, request.DhrnKg, request.ProductId, conversions);

        var grossAmount = request.RatePerUnit.HasValue
            ? UnitConversionCalculator.GrossAmountFromRatePerMan(request.RatePerUnit.Value, netWeightKg, request.ProductId, conversions)
            : 0m;

        var rules = await _db.DeductionRules.Where(r => r.IsActive && !r.IsDeleted).ToListAsync(ct);
        var calc = DeductionEngine.Calculate(grossAmount, netWeightKg, DeductionAppliesTo.Kachi, request.ProductId, request.FarmerId, rules);
        var farmerTotal = calc.Lines.Where(l => l.ChargedTo == DeductionChargedTo.Farmer).Sum(l => l.Amount);
        var buyerTotal = calc.Lines.Where(l => l.ChargedTo == DeductionChargedTo.Buyer).Sum(l => l.Amount);

        var kachi = new Kachi
        {
            InvoiceNo = await _numberGenerator.NextAsync("K", ct),
            ReceiptNumber = await _numberGenerator.NextAsync("KR", ct),
            Date = request.Date,
            SeasonId = request.SeasonId,
            FarmerId = request.FarmerId,
            BuyerId = request.BuyerId,
            ProductId = request.ProductId,
            BhartiKgPerBag = request.BhartiKgPerBag,
            TotalWeightKg = request.TotalWeightKg,
            DhrnKg = resolvedDhrnKg,
            BoriQty = boriQty,
            NetWeightKg = netWeightKg,
            RatePerUnit = request.RatePerUnit,
            GrossAmount = grossAmount,
            TotalDeductions = farmerTotal,
            BuyerChargesTotal = buyerTotal,
            Total = grossAmount - farmerTotal,
            Status = InvoiceStatus.Open,
            Notes = request.Notes
        };

        foreach (var line in calc.Lines)
        {
            kachi.DeductionLines.Add(new KachiDeductionLine
            {
                DeductionRuleId = line.DeductionRuleId,
                Name = line.Name,
                NameUrdu = line.NameUrdu,
                Amount = line.Amount,
                VehicleNumber = line.RequiresVehicleNumber ? request.VehicleNumber : null,
                ChargedTo = line.ChargedTo
            });
        }

        _db.Kachis.Add(kachi);
        await _db.SaveChangesAsync(ct);

        await PostLedgerAsync(kachi, calc.Lines, ct);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(kachi.Id, ct);
    }

    public async Task<KachiDto> UpdateAsync(int id, UpdateKachiRequest request, CancellationToken ct = default)
    {
        var kachi = await LoadAsync(id, ct);
        if (kachi.Status != InvoiceStatus.Open)
        {
            throw new InvalidCalculationException("Only an open Kachi (not yet converted to Pakki or cancelled) can be edited.");
        }

        await EnsureReferencesExistAsync(request.SeasonId, request.FarmerId, request.ProductId, ct);
        await EnsureBuyerExistsAsync(request.BuyerId, ct);

        var conversions = await _db.UnitConversions.Where(c => c.IsActive && !c.IsDeleted).ToListAsync(ct);
        var (netWeightKg, boriQty, resolvedDhrnKg) = CalculateWeights(request.TotalWeightKg, request.DhrnKg, request.ProductId, conversions);
        var grossAmount = request.RatePerUnit.HasValue
            ? UnitConversionCalculator.GrossAmountFromRatePerMan(request.RatePerUnit.Value, netWeightKg, request.ProductId, conversions)
            : 0m;

        var rules = await _db.DeductionRules.Where(r => r.IsActive && !r.IsDeleted).ToListAsync(ct);
        var calc = DeductionEngine.Calculate(grossAmount, netWeightKg, DeductionAppliesTo.Kachi, request.ProductId, request.FarmerId, rules);
        var farmerTotal = calc.Lines.Where(l => l.ChargedTo == DeductionChargedTo.Farmer).Sum(l => l.Amount);
        var buyerTotal = calc.Lines.Where(l => l.ChargedTo == DeductionChargedTo.Buyer).Sum(l => l.Amount);

        // Reverse against the pre-update buyer/amounts before anything is mutated — mirrors
        // PakkiService.UpdateAsync (reverse what was posted, then post fresh entries below).
        await ReverseLedgerAsync(kachi, $"Reversal: {kachi.InvoiceNo} edited", ct);

        kachi.Date = request.Date;
        kachi.SeasonId = request.SeasonId;
        kachi.FarmerId = request.FarmerId;
        kachi.BuyerId = request.BuyerId;
        kachi.ProductId = request.ProductId;
        kachi.BhartiKgPerBag = request.BhartiKgPerBag;
        kachi.TotalWeightKg = request.TotalWeightKg;
        kachi.DhrnKg = resolvedDhrnKg;
        kachi.BoriQty = boriQty;
        kachi.NetWeightKg = netWeightKg;
        kachi.RatePerUnit = request.RatePerUnit;
        kachi.GrossAmount = grossAmount;
        kachi.TotalDeductions = farmerTotal;
        kachi.BuyerChargesTotal = buyerTotal;
        kachi.Total = grossAmount - farmerTotal;
        kachi.Notes = request.Notes;
        kachi.UpdatedAtUtc = _clock.UtcNow;

        foreach (var existing in kachi.DeductionLines.ToList())
        {
            _db.KachiDeductionLines.Remove(existing);
        }
        kachi.DeductionLines.Clear();
        foreach (var line in calc.Lines)
        {
            kachi.DeductionLines.Add(new KachiDeductionLine
            {
                DeductionRuleId = line.DeductionRuleId,
                Name = line.Name,
                NameUrdu = line.NameUrdu,
                Amount = line.Amount,
                VehicleNumber = line.RequiresVehicleNumber ? request.VehicleNumber : null,
                ChargedTo = line.ChargedTo
            });
        }

        await _db.SaveChangesAsync(ct);

        await PostLedgerAsync(kachi, calc.Lines, ct);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        var kachi = await LoadAsync(id, ct);
        if (kachi.Status == InvoiceStatus.ConvertedToPakki)
        {
            throw new InvalidCalculationException("A Kachi already converted to a Pakki cannot be cancelled.");
        }
        kachi.Status = InvoiceStatus.Cancelled;
        kachi.UpdatedAtUtc = _clock.UtcNow;

        await ReverseLedgerAsync(kachi, $"Reversal: {kachi.InvoiceNo} cancelled", ct);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Reverses this Kachi's own ledger postings without cancelling it — called by
    /// PakkiService right before it converts this Kachi to a Pakki, since the Pakki then posts the
    /// authoritative farmer/buyer entries for the same underlying transaction and posting both
    /// would double-count it.</summary>
    public async Task ReverseLedgerForConversionAsync(int kachiId, CancellationToken ct = default)
    {
        var kachi = await LoadAsync(kachiId, ct);
        await ReverseLedgerAsync(kachi, $"Reversal: {kachi.InvoiceNo} converted to Pakki", ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RepostLedgerAfterPakkiCancellationAsync(int kachiId, CancellationToken ct = default)
    {
        var kachi = await LoadAsync(kachiId, ct);
        await PostLedgerFromStoredLinesAsync(kachi, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Kachi> LoadAsync(int id, CancellationToken ct)
    {
        return await _db.Kachis
            .Include(k => k.Season).Include(k => k.Farmer).Include(k => k.Buyer).Include(k => k.Product).Include(k => k.DeductionLines)
            .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Kachi), id);
    }

    private async Task EnsureReferencesExistAsync(int seasonId, int farmerId, int productId, CancellationToken ct)
    {
        if (!await _db.Seasons.AnyAsync(s => s.Id == seasonId && !s.IsDeleted, ct))
            throw new NotFoundException(nameof(Season), seasonId);
        if (!await _db.Parties.AnyAsync(p => p.Id == farmerId && !p.IsDeleted && p.PartyType == PartyType.Farmer, ct))
            throw new NotFoundException(nameof(Party), farmerId);
        if (!await _db.Products.AnyAsync(p => p.Id == productId && !p.IsDeleted, ct))
            throw new NotFoundException(nameof(Product), productId);
    }

    private async Task EnsureBuyerExistsAsync(int? buyerId, CancellationToken ct)
    {
        if (buyerId is null) return;
        if (!await _db.Parties.AnyAsync(p => p.Id == buyerId && !p.IsDeleted && p.PartyType == PartyType.Buyer, ct))
            throw new NotFoundException(nameof(Party), buyerId.Value);
    }

    /// <summary>Net weight ("Safi Wazan") is TotalWeightKg minus Dhrn (a tare/wastage allowance).
    /// Dhrn defaults to DomainConstants.DefaultDhrnKg (5kg, the market's current standard) when not
    /// entered — an explicit 0 (or any other value) always overrides it. The resolved Dhrn is
    /// returned too, so it's what actually gets stored on the Kachi, not the null that came in.
    /// Bori is the same net weight re-expressed in the market's Bori unit (kg-per-Bori from
    /// Setup &gt; Unit Conversions), purely for display — it's calculated, never entered, unlike
    /// BhartiKgPerBag (the weight-per-bag for this crop, which is required at entry but only used
    /// for GrossAmountFromRatePerMan indirectly via NetWeightKg — it doesn't feed this calculation
    /// directly).</summary>
    private static (decimal NetWeightKg, decimal? BoriQty, decimal DhrnKg) CalculateWeights(
        decimal? totalWeightKg, decimal? dhrnKg, int? productId, IReadOnlyCollection<UnitConversion> conversions)
    {
        if (totalWeightKg is not > 0) return (0m, null, 0m);

        var resolvedDhrnKg = dhrnKg ?? DomainConstants.DefaultDhrnKg;
        var netWeightKg = totalWeightKg.Value - resolvedDhrnKg;
        if (netWeightKg <= 0)
        {
            throw new InvalidCalculationException("Dhrn cannot be greater than or equal to total weight.");
        }

        var boriFactor = UnitConversionCalculator.FactorFor(WeightUnit.Bori, productId, conversions);
        var boriQty = netWeightKg / boriFactor;

        return (netWeightKg, boriQty, resolvedDhrnKg);
    }

    /// <summary>Posts the Kachi's farmer/buyer double-entry: the buyer is debited for what they owe
    /// (GrossAmount + BuyerChargesTotal — the price plus whatever's charged to them), the farmer is
    /// credited for their net payable (Total = GrossAmount minus farmer-charged deductions only),
    /// and every deduction line (farmer- or buyer-charged alike) credits its income account — both
    /// groups are business income, just sourced from a different party. The two sides always
    /// balance: buyer debit (Gross + BuyerCharges) == farmer credit (Gross - FarmerDeductions) +
    /// sum(all lines) (FarmerDeductions + BuyerCharges).</summary>
    private async Task PostLedgerAsync(Kachi kachi, IReadOnlyList<DeductionLineResult> lines, CancellationToken ct)
    {
        if (kachi.BuyerId is null) return; // historical rows only — Buyer is required going forward

        await _ledger.PostPartyEntryAsync(kachi.BuyerId.Value, kachi.Date, kachi.GrossAmount + kachi.BuyerChargesTotal, 0, LedgerSourceType.Kachi, kachi.Id, $"Kachi {kachi.InvoiceNo}", ct);
        await _ledger.PostPartyEntryAsync(kachi.FarmerId, kachi.Date, 0, kachi.Total, LedgerSourceType.Kachi, kachi.Id, $"Kachi {kachi.InvoiceNo}", ct);

        foreach (var line in lines)
        {
            var accountId = await ResolveIncomeAccountIdAsync(line.IncomeAccountId, ct);
            await _ledger.PostAccountEntryAsync(accountId, kachi.Date, 0, line.Amount, LedgerSourceType.Kachi, kachi.Id, $"Kachi {kachi.InvoiceNo}: {line.Name}", ct);
        }
    }

    /// <summary>Same posting as PostLedgerAsync, but reading the deduction lines back from the
    /// Kachi's own stored DeductionLines (resolving each rule's income account by lookup) instead
    /// of a freshly computed DeductionCalculationResult — used only by
    /// RepostLedgerAfterPakkiCancellationAsync, where there's no fresh calculation to hand it.</summary>
    private async Task PostLedgerFromStoredLinesAsync(Kachi kachi, CancellationToken ct)
    {
        if (kachi.BuyerId is null) return;

        await _ledger.PostPartyEntryAsync(kachi.BuyerId.Value, kachi.Date, kachi.GrossAmount + kachi.BuyerChargesTotal, 0, LedgerSourceType.Kachi, kachi.Id, $"Kachi {kachi.InvoiceNo}", ct);
        await _ledger.PostPartyEntryAsync(kachi.FarmerId, kachi.Date, 0, kachi.Total, LedgerSourceType.Kachi, kachi.Id, $"Kachi {kachi.InvoiceNo}", ct);

        foreach (var line in kachi.DeductionLines)
        {
            var ruleAccountId = await _db.DeductionRules.Where(r => r.Id == line.DeductionRuleId).Select(r => r.IncomeAccountId).FirstOrDefaultAsync(ct);
            var accountId = await ResolveIncomeAccountIdAsync(ruleAccountId, ct);
            await _ledger.PostAccountEntryAsync(accountId, kachi.Date, 0, line.Amount, LedgerSourceType.Kachi, kachi.Id, $"Kachi {kachi.InvoiceNo}: {line.Name}", ct);
        }
    }

    /// <summary>Posts the mirror image of PostLedgerAsync, so the running balance is corrected
    /// without ever deleting or mutating a previously posted row. Used by CancelAsync,
    /// UpdateAsync (reverse, then post fresh entries for the new terms) and
    /// ReverseLedgerForConversionAsync (converting to a Pakki).</summary>
    private async Task ReverseLedgerAsync(Kachi kachi, string reason, CancellationToken ct)
    {
        if (kachi.BuyerId is null) return;

        await _ledger.PostPartyEntryAsync(kachi.BuyerId.Value, _clock.UtcNow, 0, kachi.GrossAmount + kachi.BuyerChargesTotal, LedgerSourceType.Kachi, kachi.Id, reason, ct);
        await _ledger.PostPartyEntryAsync(kachi.FarmerId, _clock.UtcNow, kachi.Total, 0, LedgerSourceType.Kachi, kachi.Id, reason, ct);

        foreach (var line in kachi.DeductionLines)
        {
            var ruleAccountId = await _db.DeductionRules.Where(r => r.Id == line.DeductionRuleId).Select(r => r.IncomeAccountId).FirstOrDefaultAsync(ct);
            var accountId = await ResolveIncomeAccountIdAsync(ruleAccountId, ct);
            await _ledger.PostAccountEntryAsync(accountId, _clock.UtcNow, line.Amount, 0, LedgerSourceType.Kachi, kachi.Id, $"{reason} ({line.Name})", ct);
        }
    }

    private async Task<int> ResolveIncomeAccountIdAsync(int? explicitAccountId, CancellationToken ct)
    {
        if (explicitAccountId.HasValue) return explicitAccountId.Value;

        var suspense = await _db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Code == DomainConstants.UnallocatedDeductionsAccountCode, ct)
            ?? throw new InvalidCalculationException($"The Unallocated Deductions account (code {DomainConstants.UnallocatedDeductionsAccountCode}) is missing. Re-run seed data or set an income account on every deduction rule.");
        return suspense.Id;
    }

    private static KachiDto ToDto(Kachi k) => new(
        k.Id, k.InvoiceNo, k.ReceiptNumber, k.Date, k.SeasonId, k.Season.Name, k.FarmerId, k.Farmer.Name, k.BuyerId, k.Buyer?.Name,
        k.ProductId, k.Product.Name, k.BhartiKgPerBag, k.TotalWeightKg, k.DhrnKg, k.BoriQty, k.NetWeightKg,
        k.RatePerUnit, k.GrossAmount, k.TotalDeductions, k.BuyerChargesTotal, k.Total, k.Status, k.ConvertedToPakkiId, k.Notes,
        k.DeductionLines.Select(l => new KachiDeductionLineDto(l.DeductionRuleId, l.Name, l.NameUrdu, l.Amount, l.VehicleNumber, l.ChargedTo)).ToList());
}
