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
    private readonly IInvoiceNumberGenerator _numberGenerator;
    private readonly IDateTimeProvider _clock;

    public KachiService(IApplicationDbContext db, IInvoiceNumberGenerator numberGenerator, IDateTimeProvider clock)
    {
        _db = db;
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
        var netWeightKg = UnitConversionCalculator.ToBaseKg(request.ManQty, request.KiloQty, request.GramQty, request.BoriQty, request.ProductId, conversions);

        var grossAmount = request.RatePerUnit.HasValue
            ? UnitConversionCalculator.GrossAmountFromRatePerMan(request.RatePerUnit.Value, netWeightKg, request.ProductId, conversions)
            : 0m;

        var rules = await _db.DeductionRules.Where(r => r.IsActive && !r.IsDeleted).ToListAsync(ct);
        var calc = DeductionEngine.Calculate(grossAmount, netWeightKg, DeductionAppliesTo.Kachi, request.ProductId, request.FarmerId, rules);

        var kachi = new Kachi
        {
            InvoiceNo = await _numberGenerator.NextAsync("K", ct),
            ReceiptNumber = await _numberGenerator.NextAsync("KR", ct),
            Date = request.Date,
            SeasonId = request.SeasonId,
            FarmerId = request.FarmerId,
            BuyerId = request.BuyerId,
            ProductId = request.ProductId,
            ManQty = request.ManQty,
            KiloQty = request.KiloQty,
            GramQty = request.GramQty,
            BoriQty = request.BoriQty,
            NetWeightKg = netWeightKg,
            RatePerUnit = request.RatePerUnit,
            GrossAmount = grossAmount,
            TotalDeductions = calc.TotalDeductions,
            Total = calc.NetAmount,
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
                VehicleNumber = line.RequiresVehicleNumber ? request.VehicleNumber : null
            });
        }

        _db.Kachis.Add(kachi);
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
        var netWeightKg = UnitConversionCalculator.ToBaseKg(request.ManQty, request.KiloQty, request.GramQty, request.BoriQty, request.ProductId, conversions);
        var grossAmount = request.RatePerUnit.HasValue
            ? UnitConversionCalculator.GrossAmountFromRatePerMan(request.RatePerUnit.Value, netWeightKg, request.ProductId, conversions)
            : 0m;

        var rules = await _db.DeductionRules.Where(r => r.IsActive && !r.IsDeleted).ToListAsync(ct);
        var calc = DeductionEngine.Calculate(grossAmount, netWeightKg, DeductionAppliesTo.Kachi, request.ProductId, request.FarmerId, rules);

        kachi.Date = request.Date;
        kachi.SeasonId = request.SeasonId;
        kachi.FarmerId = request.FarmerId;
        kachi.BuyerId = request.BuyerId;
        kachi.ProductId = request.ProductId;
        kachi.ManQty = request.ManQty;
        kachi.KiloQty = request.KiloQty;
        kachi.GramQty = request.GramQty;
        kachi.BoriQty = request.BoriQty;
        kachi.NetWeightKg = netWeightKg;
        kachi.RatePerUnit = request.RatePerUnit;
        kachi.GrossAmount = grossAmount;
        kachi.TotalDeductions = calc.TotalDeductions;
        kachi.Total = calc.NetAmount;
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
                VehicleNumber = line.RequiresVehicleNumber ? request.VehicleNumber : null
            });
        }

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

    private static KachiDto ToDto(Kachi k) => new(
        k.Id, k.InvoiceNo, k.ReceiptNumber, k.Date, k.SeasonId, k.Season.Name, k.FarmerId, k.Farmer.Name, k.BuyerId, k.Buyer?.Name,
        k.ProductId, k.Product.Name, k.ManQty, k.KiloQty, k.GramQty, k.BoriQty, k.NetWeightKg,
        k.RatePerUnit, k.GrossAmount, k.TotalDeductions, k.Total, k.Status, k.ConvertedToPakkiId, k.Notes,
        k.DeductionLines.Select(l => new KachiDeductionLineDto(l.DeductionRuleId, l.Name, l.NameUrdu, l.Amount, l.VehicleNumber)).ToList());
}
