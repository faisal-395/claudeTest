using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.DeductionRules;

public record DeductionRuleDto(
    int Id, string Name, string NameUrdu, DeductionCalculationType CalculationType, decimal Value,
    DeductionAppliesTo AppliesTo, DeductionChargedTo ChargedTo, int SortOrder, bool IsActive, int? ProductId, int? PartyId, bool RequiresVehicleNumber,
    int? IncomeAccountId);

public record UpsertDeductionRuleRequest(
    string Name, string NameUrdu, DeductionCalculationType CalculationType, decimal Value,
    DeductionAppliesTo AppliesTo, DeductionChargedTo ChargedTo, int SortOrder, bool IsActive, int? ProductId, int? PartyId, bool RequiresVehicleNumber,
    int? IncomeAccountId);
