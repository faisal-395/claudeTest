using FluentValidation;

namespace GrainMarket.Application.Common;

/// <summary>An operator-entered override of one deduction line's amount for a single Kachi/Pakki
/// being created — e.g. charging less Commission than the standard Setup > Format rate for one
/// transaction. Applied via DeductionEngine.ApplyOverrides; never changes the underlying
/// DeductionRule.</summary>
public record DeductionOverrideRequest(int DeductionRuleId, decimal Amount);

public class DeductionOverrideRequestValidator : AbstractValidator<DeductionOverrideRequest>
{
    public DeductionOverrideRequestValidator()
    {
        RuleFor(x => x.DeductionRuleId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}
