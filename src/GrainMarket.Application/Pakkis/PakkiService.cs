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
    private readonly IInvoiceNumberGenerator _numberGenerator;
    private readonly IDateTimeProvider _clock;

    public PakkiService(IApplicationDbContext db, ILedgerPostingService ledger, IInvoiceNumberGenerator numberGenerator, IDateTimeProvider clock)
    {
        _db = db;
        _ledger = ledger;
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

        if (!await _db.Parties.AnyAsync(p => p.Id == request.BuyerId && !p.IsDeleted && p.PartyType == PartyType.Buyer, ct))
        {
            throw new NotFoundException(nameof(Party), request.BuyerId);
        }

        var conversions = await _db.UnitConversions.Where(c => c.IsActive && !c.IsDeleted).ToListAsync(ct);
        var grossAmount = UnitConversionCalculator.GrossAmountFromRatePerMan(request.RatePerUnit, kachi.NetWeightKg, kachi.ProductId, conversions);
        var rules = await _db.DeductionRules.Where(r => r.IsActive && !r.IsDeleted).ToListAsync(ct);
        var calc = DeductionEngine.Calculate(grossAmount, kachi.NetWeightKg, DeductionAppliesTo.Pakki, kachi.ProductId, kachi.FarmerId, rules);

        var pakki = new Pakki
        {
            InvoiceNo = await _numberGenerator.NextAsync("P", ct),
            Date = request.Date,
            SeasonId = kachi.SeasonId,
            KachiId = kachi.Id,
            BuyerId = request.BuyerId,
            FarmerId = kachi.FarmerId,
            ProductId = kachi.ProductId,
            ManQty = kachi.ManQty,
            KiloQty = kachi.KiloQty,
            GramQty = kachi.GramQty,
            BoriQty = kachi.BoriQty,
            NetWeightKg = kachi.NetWeightKg,
            RatePerUnit = request.RatePerUnit,
            GrossAmount = grossAmount,
            TotalDeductions = calc.TotalDeductions,
            NetPayableToFarmer = calc.NetAmount,
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
                VehicleNumber = line.RequiresVehicleNumber ? request.VehicleNumber : null
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
        if (!await _db.Parties.AnyAsync(p => p.Id == request.BuyerId && !p.IsDeleted && p.PartyType == PartyType.Buyer, ct))
            throw new NotFoundException(nameof(Party), request.BuyerId);
        if (!await _db.Parties.AnyAsync(p => p.Id == request.FarmerId && !p.IsDeleted && p.PartyType == PartyType.Farmer, ct))
            throw new NotFoundException(nameof(Party), request.FarmerId);
        if (!await _db.Products.AnyAsync(p => p.Id == request.ProductId && !p.IsDeleted, ct))
            throw new NotFoundException(nameof(Product), request.ProductId);

        var conversions = await _db.UnitConversions.Where(c => c.IsActive && !c.IsDeleted).ToListAsync(ct);
        var netWeightKg = UnitConversionCalculator.ToBaseKg(request.ManQty, request.KiloQty, request.GramQty, request.BoriQty, request.ProductId, conversions);
        var grossAmount = UnitConversionCalculator.GrossAmountFromRatePerMan(request.RatePerUnit, netWeightKg, request.ProductId, conversions);

        var rules = await _db.DeductionRules.Where(r => r.IsActive && !r.IsDeleted).ToListAsync(ct);
        var calc = DeductionEngine.Calculate(grossAmount, netWeightKg, DeductionAppliesTo.Pakki, request.ProductId, request.FarmerId, rules);

        var pakki = new Pakki
        {
            InvoiceNo = await _numberGenerator.NextAsync("P", ct),
            Date = request.Date,
            SeasonId = request.SeasonId,
            KachiId = null,
            BuyerId = request.BuyerId,
            FarmerId = request.FarmerId,
            ProductId = request.ProductId,
            ManQty = request.ManQty,
            KiloQty = request.KiloQty,
            GramQty = request.GramQty,
            BoriQty = request.BoriQty,
            NetWeightKg = netWeightKg,
            RatePerUnit = request.RatePerUnit,
            GrossAmount = grossAmount,
            TotalDeductions = calc.TotalDeductions,
            NetPayableToFarmer = calc.NetAmount,
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
                VehicleNumber = line.RequiresVehicleNumber ? request.VehicleNumber : null
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
        if (!await _db.Parties.AnyAsync(p => p.Id == request.BuyerId && !p.IsDeleted && p.PartyType == PartyType.Buyer, ct))
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

        pakki.BuyerId = request.BuyerId;
        pakki.RatePerUnit = request.RatePerUnit;
        pakki.GrossAmount = grossAmount;
        pakki.TotalDeductions = calc.TotalDeductions;
        pakki.NetPayableToFarmer = calc.NetAmount;
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
                VehicleNumber = line.RequiresVehicleNumber ? request.VehicleNumber : null
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

        if (pakki.KachiId is not null)
        {
            var kachi = await _db.Kachis.FirstOrDefaultAsync(k => k.Id == pakki.KachiId, ct);
            if (kachi is not null)
            {
                kachi.Status = InvoiceStatus.Open;
                kachi.ConvertedToPakkiId = null;
            }
        }

        await ReverseLedgerAsync(pakki, $"Reversal: {pakki.InvoiceNo} cancelled", ct);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Posts the mirror image of a Pakki's ledger postings, so the running balance is
    /// corrected without ever deleting or mutating a previously posted row. Used by both
    /// CancelAsync and UpdateAsync (which reverses, then posts fresh entries for the new terms).</summary>
    private async Task ReverseLedgerAsync(Pakki pakki, string reason, CancellationToken ct)
    {
        await _ledger.PostPartyEntryAsync(pakki.BuyerId, _clock.UtcNow, 0, pakki.GrossAmount, LedgerSourceType.Pakki, pakki.Id, reason, ct);
        await _ledger.PostPartyEntryAsync(pakki.FarmerId, _clock.UtcNow, pakki.NetPayableToFarmer, 0, LedgerSourceType.Pakki, pakki.Id, reason, ct);
        foreach (var line in pakki.DeductionLines)
        {
            var accountId = await ResolveIncomeAccountIdAsync(await _db.DeductionRules.Where(r => r.Id == line.DeductionRuleId).Select(r => r.IncomeAccountId).FirstOrDefaultAsync(ct), ct);
            await _ledger.PostAccountEntryAsync(accountId, _clock.UtcNow, line.Amount, 0, LedgerSourceType.Pakki, pakki.Id, $"{reason} ({line.Name})", ct);
        }
    }

    private async Task PostLedgerAsync(Pakki pakki, DeductionCalculationResult calc, CancellationToken ct)
    {
        await _ledger.PostPartyEntryAsync(pakki.BuyerId, pakki.Date, pakki.GrossAmount, 0, LedgerSourceType.Pakki, pakki.Id, $"Pakki {pakki.InvoiceNo}", ct);
        await _ledger.PostPartyEntryAsync(pakki.FarmerId, pakki.Date, 0, pakki.NetPayableToFarmer, LedgerSourceType.Pakki, pakki.Id, $"Pakki {pakki.InvoiceNo}", ct);

        foreach (var line in calc.Lines)
        {
            var accountId = await ResolveIncomeAccountIdAsync(line.IncomeAccountId, ct);
            await _ledger.PostAccountEntryAsync(accountId, pakki.Date, 0, line.Amount, LedgerSourceType.Pakki, pakki.Id, $"Pakki {pakki.InvoiceNo}: {line.Name}", ct);
        }
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
        p.Id, p.InvoiceNo, p.Date, p.SeasonId, p.Season.Name, p.KachiId, p.Kachi?.InvoiceNo,
        p.BuyerId, p.Buyer.Name, p.FarmerId, p.Farmer.Name, p.ProductId, p.Product.Name,
        p.ManQty, p.KiloQty, p.GramQty, p.BoriQty, p.NetWeightKg, p.RatePerUnit, p.GrossAmount,
        p.TotalDeductions, p.NetPayableToFarmer, p.VehicleNumber, p.Status, p.Notes,
        p.DeductionLines.Select(l => new PakkiDeductionLineDto(l.DeductionRuleId, l.Name, l.NameUrdu, l.Amount, l.VehicleNumber)).ToList());
}
