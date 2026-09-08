namespace GrainMarket.Application.DeductionRules;

public interface IDeductionRuleService
{
    Task<List<DeductionRuleDto>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<DeductionRuleDto> CreateAsync(UpsertDeductionRuleRequest request, CancellationToken ct = default);
    Task<DeductionRuleDto> UpdateAsync(int id, UpsertDeductionRuleRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
