using GrainMarket.Application.Common;
using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Application.Common.Services;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.Pakkis;

public class PakkiService : IPakkiService
{
    private readonly IApplicationDbContext _db;
    private readonly ILedgerPostingService _ledger;
    private readonly Kachis.IKachiService _kachiService;
    private readonly IInvoiceNumberGenerator _numberGenerator;
    private readonly IDateTimeProvider _clock;

    public PakkiService(IApplicationDbContext db, ILedgerPostingService ledger, Kachis.IKachiService kachiService, IInvoiceNumberGenerator numberGenerator, IDateTimeProvider clock)
    {
        _db = db;
        _ledger = ledger;
        _kachiService = kachiService;
        _numberGenerator = numberGenerator;
        _clock = clock;
    }

    public async Task<List<PakkiDto>> GetAllAsync(int? seasonId = null, CancellationToken ct = default)
    {
        var query = _db.Pakkis
            .Include(p => p.Season).Include(p => p.Buyer).Include(p => p.Farmer).Include(p => p.Product)
            .Include(p => p.Kachi).Include(p => p.DeductionLines)
            .Where(p => !p.IsDeleted);
        if (seasonId is not null) query = query.Where(p => p.SeasonId == seasonId);

        var rows = await query.OrderByDescending(p => p.Date).ThenByDescending(p => p.Id).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<PakkiDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var pakki = await LoadAsync(id, ct);
        return ToDto(pakki);
    }

    public async Task<PakkiDto> CreateFromKachiAsync(CreatePakkiFromKachiRequest request, CancellationToken ct = default)
    {
        var kachi = await _db.Kachis
            .Include(k => k.Season)
            .FirstOrDefaultAsync(k => k.Id == request.KachiId && !k.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Kachi), request.KachiId);

        if (kachi.Status != InvoiceStatus.Open)
        {
            throw new InvalidCalculationException("Only an open Kachi can be converted to a Pakki.");
        }

        if (!await _db.Parties.AnyAsync(p => p.Id == request.BuyerId && !p.IsDeleted && (p.PartyType & PartyType.Vendor) == PartyType.Vendor, ct))
        {
            throw new NotFoundException(nameof(Party), request.BuyerId);
        }

        // The Kachi already posted its own farmer/buyer ledger entries when it was created; this
        // Pakki now becomes the authoritative record for the same underlying transaction, so
        // reverse the Kachi's postings first or both would be counted.
        await _kachiService.ReverseLedgerForConversionAsync(kachi.Id, ct);

        var conversions = await _db.UnitConversions.Where(c => c.IsActive && !c.IsDeleted).ToListAsync(ct);
        var grossAmount = UnitConversionCalculator.GrossAmountFromRatePerMan(request.RatePerUnit, kachi.NetWeightKg, kachi.ProductId, conversions);
        var rules = await _db.DeductionRules.Where(r => r.IsActive && !r.IsDeleted).ToListAsync(ct);
        var calc = DeductionEngine.Calculate(grossAmount, kachi.NetWeightKg, DeductionAppliesTo.Pakki, kachi.ProductId, kachi.FarmerId, rules);
        var farmerTotal = calc.Lines.Where(l => l.ChargedTo == DeductionChargedTo.Seller).Sum(l => l.Amount);
        var buyerTotal = calc.Lines.Where(l => l.ChargedTo == DeductionChargedTo.Buyer).Sum(l => l.Amount);

        var pakki = new Pakki
        {
            InvoiceNo = await _numberGenerator.NextAsync("P", ct),
            Date = request.Date,
            SeasonId = kachi.SeasonId,
            KachiId = kachi.Id,
            BuyerId = request.BuyerId,
            FarmerId = kachi.FarmerId,
            ProductId = kachi.ProductId,
            BhartiKgPerBag = kachi.BhartiKgPerBag,
            TotalWeightKg = kachi.TotalWeightKg,
            BoriQty = kachi.BoriQty,
            NetWeightKg = kachi.NetWeightKg,
            RatePerUnit = request.RatePerUnit,
            GrossAmount = grossAmount,
            TotalDeductions = farmerTotal,
            BuyerChargesTotal = buyerTotal,
            NetPayableToFarmer = grossAmount - farmerTotal,
            VehicleNumber = request.VehicleNumber,
            Status = InvoiceStatus.Open,
            Notes = request.Notes
        };

        foreach (var line in calc.Lines)
        {
            pakki.DeductionLines.Add(new PakkiDeductionLine
            {
                DeductionRuleId = line.DeductionRuleId,
                Name = line.Name,
                NameUrdu = line.NameUrdu,
                Amount = line.Amount,
                VehicleNumber = line.RequiresVehicleNumber ? request.VehicleNumber : null,
                ChargedTo = line.ChargedTo
            });
        }

        _db.Pakkis.Add(pakki);
        await _db.SaveChangesAsync(ct);

        kachi.Status = InvoiceStatus.ConvertedToPakki;
        kachi.ConvertedToPakkiId = pakki.Id;
        kachi.UpdatedAtUtc = _clock.UtcNow;

        await PostLedgerAsync(pakki, calc, ct);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(pakki.Id, ct);
    }

    public async Task<PakkiDto> CreateStandaloneAsync(CreateStandalonePakkiRequest request, CancellationToken ct = default)
    {
        if (!await _db.Seasons.AnyAsync(s => s.Id == request.SeasonId && !s.IsDeleted, ct))
            throw new NotFoundException(nameof(Season), request.SeasonId);
        if (!await _db.Parties.AnyAsync(p => p.Id == request.BuyerId && !p.IsDeleted && (p.PartyType & PartyType.Vendor) == PartyType.Vendor, ct))
            throw new NotFoundException(nameof(Party), request.BuyerId);
        // Pakki is vendor-to-vendor: the payee (request.FarmerId) must also be a Vendor-flagged
        // party, not necessarily a Farmer — unlike Kachi, where the payee is always a Farmer.
        if (!await _db.Parties.AnyAsync(p => p.Id == request.FarmerId && !p.IsDeleted && (p.PartyType & PartyType.Vendor) == PartyType.Vendor, ct))
            throw new NotFoundException(nameof(Party), request.FarmerId);
        if (!await _db.Products.AnyAsync(p => p.Id == request.ProductId && !p.IsDeleted, ct))
            throw new NotFoundException(nameof(Product), request.ProductId);

        var conversions = await _db.UnitConversions.Where(c => c.IsActive && !c.IsDeleted).ToListAsync(ct);
        var (netWeightKg, boriQty) = CalculateWeights(request.TotalWeightKg, request.ProductId, conversions);
        var grossAmount = UnitConversionCalculator.GrossAmountFromRatePerMan(request.RatePerUnit, netWeightKg, request.ProductId, conversions);

        var rules = await _db.DeductionRules.Where(r => r.IsActive && !r.IsDeleted).ToListAsync(ct);
        var rateOverrides = request.DeductionOverrides?.ToDictionary(o => o.DeductionRuleId, o => o.Value);
        var calc = DeductionEngine.Calculate(grossAmount, netWeightKg, DeductionAppliesTo.Pakki, request.ProductId, request.FarmerId, rules, rateOverrides);
        var farmerTotal = calc.Lines.Where(l => l.ChargedTo == DeductionChargedTo.Seller).Sum(l => l.Amount);
        var buyerTotal = calc.Lines.Where(l => l.ChargedTo == DeductionChargedTo.Buyer).Sum(l => l.Amount);

        var pakki = new Pakki
        {
            InvoiceNo = await _numberGenerator.NextAsync("P", ct),
            BillNumber = request.BillNumber,
            Date = request.Date,
            SeasonId = request.SeasonId,
            KachiId = null,
            BuyerId = request.BuyerId,
            FarmerId = request.FarmerId,
            ProductId = request.ProductId,
            BhartiKgPerBag = request.BhartiKgPerBag,
            TotalWeightKg = request.TotalWeightKg,
            BoriQty = boriQty,
            NetWeightKg = netWeightKg,
            RatePerUnit = request.RatePerUnit,
            GrossAmount = grossAmount,
            TotalDeductions = farmerTotal,
            BuyerChargesTotal = buyerTotal,
            NetPayableToFarmer = grossAmount - farmerTotal,
            VehicleNumber = request.VehicleNumber,
            Status = InvoiceStatus.Open,
            Notes = request.Notes
        };

        foreach (var line in calc.Lines)
        {
            pakki.DeductionLines.Add(new PakkiDeductionLine
            {
                DeductionRuleId = line.DeductionRuleId,
                Name = line.Name,
                NameUrdu = line.NameUrdu,
                Amount = line.Amount,
                VehicleNumber = line.RequiresVehicleNumber ? request.VehicleNumber : null,
                ChargedTo = line.ChargedTo
            });
        }

        _db.Pakkis.Add(pakki);
        await _db.SaveChangesAsync(ct);

        await PostLedgerAsync(pakki, calc, ct);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(pakki.Id, ct);
    }

    public async Task<PakkiDto> UpdateAsync(int id, UpdatePakkiRequest request, CancellationToken ct = default)
    {
        var pakki = await LoadAsync(id, ct);
        if (pakki.Status != InvoiceStatus.Open)
        {
            throw new InvalidCalculationException("Only an open Pakki (not yet cancelled) can be edited.");
        }
        if (!await _db.Parties.AnyAsync(p => p.Id == request.BuyerId && !p.IsDeleted && (p.PartyType & PartyType.Vendor) == PartyType.Vendor, ct))
        {
            throw new NotFoundException(nameof(Party), request.BuyerId);
        }

        // A Pakki is a posted financial document — editing it means reversing exactly what was
        // posted (mirroring CancelAsync's approach), then posting fresh entries for the new terms.
        // Weight/product/farmer/season never change here; only buyer, rate, vehicle and notes do.
        await ReverseLedgerAsync(pakki, $"Reversal: {pakki.InvoiceNo} edited", ct);

        var conversions = await _db.UnitConversions.Where(c => c.IsActive && !c.IsDeleted).ToListAsync(ct);
        var grossAmount = UnitConversionCalculator.GrossAmountFromRatePerMan(request.RatePerUnit, pakki.NetWeightKg, pakki.ProductId, conversions);

        var rules = await _db.DeductionRules.Where(r => r.IsActive && !r.IsDeleted).ToListAsync(ct);
        var calc = DeductionEngine.Calculate(grossAmount, pakki.NetWeightKg, DeductionAppliesTo.Pakki, pakki.ProductId, pakki.FarmerId, rules);
        var farmerTotal = calc.Lines.Where(l => l.ChargedTo == DeductionChargedTo.Seller).Sum(l => l.Amount);
        var buyerTotal = calc.Lines.Where(l => l.ChargedTo == DeductionChargedTo.Buyer).Sum(l => l.Amount);

        pakki.BuyerId = request.BuyerId;
        pakki.RatePerUnit = request.RatePerUnit;
        pakki.GrossAmount = grossAmount;
        pakki.TotalDeductions = farmerTotal;
        pakki.BuyerChargesTotal = buyerTotal;
        pakki.NetPayableToFarmer = grossAmount - farmerTotal;
        pakki.VehicleNumber = request.VehicleNumber;
        pakki.Notes = request.Notes;
        pakki.UpdatedAtUtc = _clock.UtcNow;

        foreach (var existing in pakki.DeductionLines.ToList())
        {
            _db.PakkiDeductionLines.Remove(existing);
        }
        pakki.DeductionLines.Clear();
        foreach (var line in calc.Lines)
        {
            pakki.DeductionLines.Add(new PakkiDeductionLine
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
        await PostLedgerAsync(pakki, calc, ct);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        var pakki = await LoadAsync(id, ct);
        pakki.Status = InvoiceStatus.Cancelled;
        pakki.UpdatedAtUtc = _clock.UtcNow;

        int? reopenedKachiId = null;
        if (pakki.KachiId is not null)
        {
            var kachi = await _db.Kachis.FirstOrDefaultAsync(k => k.Id == pakki.KachiId, ct);
            if (kachi is not null)
            {
                kachi.Status = InvoiceStatus.Open;
                kachi.ConvertedToPakkiId = null;
                reopenedKachiId = kachi.Id;
            }
        }

        await ReverseLedgerAsync(pakki, $"Reversal: {pakki.InvoiceNo} cancelled", ct);
        await _db.SaveChangesAsync(ct);

        // The Kachi's own ledger postings were reversed when it was converted to this Pakki
        // (see ReverseLedgerForConversionAsync); now that it's Open again, re-post them so it isn't
        // left with no ledger presence at all.
        if (reopenedKachiId is not null)
        {
            await _kachiService.RepostLedgerAfterPakkiCancellationAsync(reopenedKachiId.Value, ct);
        }
    }

    /// <summary>Posts the mirror image of a Pakki's ledger postings, so the running balance is
    /// corrected without ever deleting or mutating a previously posted row. Used by both
    /// CancelAsync and UpdateAsync (which reverses, then posts fresh entries for the new terms).</summary>
    private async Task ReverseLedgerAsync(Pakki pakki, string reason, CancellationToken ct)
    {
        await _ledger.PostPartyEntryAsync(pakki.BuyerId, _clock.UtcNow, 0, pakki.GrossAmount + pakki.BuyerChargesTotal, LedgerSourceType.Pakki, pakki.Id, reason, ct);
        await _ledger.PostPartyEntryAsync(pakki.FarmerId, _clock.UtcNow, pakki.NetPayableToFarmer, 0, LedgerSourceType.Pakki, pakki.Id, reason, ct);
        foreach (var line in pakki.DeductionLines)
        {
            var accountId = await ResolveIncomeAccountIdAsync(await _db.DeductionRules.Where(r => r.Id == line.DeductionRuleId).Select(r => r.IncomeAccountId).FirstOrDefaultAsync(ct), ct);
            await _ledger.PostAccountEntryAsync(accountId, _clock.UtcNow, line.Amount, 0, LedgerSourceType.Pakki, pakki.Id, $"{reason} ({line.Name})", ct);
        }
    }

    private async Task PostLedgerAsync(Pakki pakki, DeductionCalculationResult calc, CancellationToken ct)
    {
        // Buyer pays the price plus whatever's charged to them (GrossAmount + BuyerChargesTotal) —
        // mirrors KachiService's exact treatment, and is what keeps this posting balanced against
        // the farmer's payout (only reduced by seller-charged lines) plus every line's own credit.
        await _ledger.PostPartyEntryAsync(pakki.BuyerId, pakki.Date, pakki.GrossAmount + pakki.BuyerChargesTotal, 0, LedgerSourceType.Pakki, pakki.Id, $"Pakki {pakki.InvoiceNo}", ct);
        await _ledger.PostPartyEntryAsync(pakki.FarmerId, pakki.Date, 0, pakki.NetPayableToFarmer, LedgerSourceType.Pakki, pakki.Id, $"Pakki {pakki.InvoiceNo}", ct);

        foreach (var line in calc.Lines)
        {
            var accountId = await ResolveIncomeAccountIdAsync(line.IncomeAccountId, ct);
            await _ledger.PostAccountEntryAsync(accountId, pakki.Date, 0, line.Amount, LedgerSourceType.Pakki, pakki.Id, $"Pakki {pakki.InvoiceNo}: {line.Name}", ct);
        }
    }

    /// <summary>Net weight ("Safi Wazan") is simply TotalWeightKg — mirrors
    /// KachiService.CalculateWeights exactly. Bori is the same net weight re-expressed in the
    /// market's Bori unit, purely for display — computed, never entered.</summary>
    private static (decimal NetWeightKg, decimal? BoriQty) CalculateWeights(
        decimal? totalWeightKg, int? productId, IReadOnlyCollection<UnitConversion> conversions)
    {
        if (totalWeightKg is not > 0) return (0m, null);

        var netWeightKg = totalWeightKg.Value;
        var boriFactor = UnitConversionCalculator.FactorFor(WeightUnit.Bori, productId, conversions);
        var boriQty = netWeightKg / boriFactor;

        return (netWeightKg, boriQty);
    }

    private async Task<int> ResolveIncomeAccountIdAsync(int? explicitAccountId, CancellationToken ct)
    {
        if (explicitAccountId.HasValue) return explicitAccountId.Value;

        var suspense = await _db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Code == DomainConstants.UnallocatedDeductionsAccountCode, ct)
            ?? throw new InvalidCalculationException($"The Unallocated Deductions account (code {DomainConstants.UnallocatedDeductionsAccountCode}) is missing. Re-run seed data or set an income account on every deduction rule.");
        return suspense.Id;
    }

    private async Task<Pakki> LoadAsync(int id, CancellationToken ct)
    {
        return await _db.Pakkis
            .Include(p => p.Season).Include(p => p.Buyer).Include(p => p.Farmer).Include(p => p.Product)
            .Include(p => p.Kachi).Include(p => p.DeductionLines)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Pakki), id);
    }

    private static PakkiDto ToDto(Pakki p) => new(
        p.Id, p.InvoiceNo, p.BillNumber, p.Date, p.SeasonId, p.Season.Name, p.KachiId, p.Kachi?.InvoiceNo,
        p.BuyerId, p.Buyer.Name, p.FarmerId, p.Farmer.Name, p.ProductId, p.Product.Name,
        p.BhartiKgPerBag, p.TotalWeightKg, p.BoriQty, p.NetWeightKg, p.RatePerUnit, p.GrossAmount,
        p.TotalDeductions, p.BuyerChargesTotal, p.NetPayableToFarmer, p.VehicleNumber, p.Status, p.Notes,
        p.DeductionLines.Select(l => new PakkiDeductionLineDto(l.DeductionRuleId, l.Name, l.NameUrdu, l.Amount, l.VehicleNumber, l.ChargedTo)).ToList());
}
