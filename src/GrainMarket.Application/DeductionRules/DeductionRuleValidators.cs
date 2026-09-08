using FluentValidation;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.DeductionRules;

public class UpsertDeductionRuleRequestValidator : AbstractValidator<UpsertDeductionRuleRequest>
{
    public UpsertDeductionRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameUrdu).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CalculationType).IsInEnum();
        RuleFor(x => x.AppliesTo).IsInEnum();
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Value).LessThanOrEqualTo(100)
            .When(x => x.CalculationType == DeductionCalculationType.PercentOfGross)
            .WithMessage("A percentage deduction cannot exceed 100%.");
    }
}
