using FluentValidation;

namespace GrainMarket.Application.UnitConversions;

public class UpsertUnitConversionRequestValidator : AbstractValidator<UpsertUnitConversionRequest>
{
    public UpsertUnitConversionRequestValidator()
    {
        RuleFor(x => x.Unit).IsInEnum();
        RuleFor(x => x.FactorToKg).GreaterThan(0).WithMessage("Conversion factor must be greater than zero.");
    }
}
