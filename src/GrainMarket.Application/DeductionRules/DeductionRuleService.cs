using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Interfaces;
using GrainMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrainMarket.Application.DeductionRules;

public class DeductionRuleService : IDeductionRuleService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public DeductionRuleService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<DeductionRuleDto>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.DeductionRules.Where(r => !r.IsDeleted);
        if (!includeInactive) query = query.Where(r => r.IsActive);
        var rows = await query.OrderBy(r => r.SortOrder).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<DeductionRuleDto> CreateAsync(UpsertDeductionRuleRequest request, CancellationToken ct = default)
    {
        var rule = new DeductionRule
        {
            Name = request.Name,
            NameUrdu = request.NameUrdu,
            CalculationType = request.CalculationType,
            Value = request.Value,
            AppliesTo = request.AppliesTo,
            ChargedTo = request.ChargedTo,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            ProductId = request.ProductId,
            PartyId = request.PartyId,
            RequiresVehicleNumber = request.RequiresVehicleNumber,
            IncomeAccountId = request.IncomeAccountId
        };
        _db.DeductionRules.Add(rule);
        await _db.SaveChangesAsync(ct);
        return ToDto(rule);
    }

    public async Task<DeductionRuleDto> UpdateAsync(int id, UpsertDeductionRuleRequest request, CancellationToken ct = default)
    {
        var rule = await _db.DeductionRules.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(DeductionRule), id);

        rule.Name = request.Name;
        rule.NameUrdu = request.NameUrdu;
        rule.CalculationType = request.CalculationType;
        rule.Value = request.Value;
        rule.AppliesTo = request.AppliesTo;
        rule.ChargedTo = request.ChargedTo;
        rule.SortOrder = request.SortOrder;
        rule.IsActive = request.IsActive;
        rule.ProductId = request.ProductId;
        rule.PartyId = request.PartyId;
        rule.RequiresVehicleNumber = request.RequiresVehicleNumber;
        rule.IncomeAccountId = request.IncomeAccountId;
        rule.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(rule);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var rule = await _db.DeductionRules.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(DeductionRule), id);
        rule.IsDeleted = true;
        rule.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    private static DeductionRuleDto ToDto(DeductionRule r) => new(
        r.Id, r.Name, r.NameUrdu, r.CalculationType, r.Value, r.AppliesTo, r.ChargedTo, r.SortOrder, r.IsActive,
        r.ProductId, r.PartyId, r.RequiresVehicleNumber, r.IncomeAccountId);
}
