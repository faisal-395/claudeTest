using FluentValidation;

namespace GrainMarket.Application.Common;

/// <summary>An operator-entered override of one deduction rule's rate/value — e.g. charging 1.20%
/// Commission instead of the standard Setup > Format rate of 1.60% — for a single Kachi/Pakki
/// batch being created. Substitutes for DeductionRule.Value inside DeductionEngine.Calculate but
/// still goes through the rule's own CalculationType formula, and never changes the underlying
/// DeductionRule itself.</summary>
public record DeductionOverrideRequest(int DeductionRuleId, decimal Value);

public class DeductionOverrideRequestValidator : AbstractValidator<DeductionOverrideRequest>
{
    public DeductionOverrideRequestValidator()
    {
        RuleFor(x => x.DeductionRuleId).GreaterThan(0);
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
    }
}
