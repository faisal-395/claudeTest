using FluentValidation;
using GrainMarket.Application.Common;

namespace GrainMarket.Application.Pakkis;

public class CreatePakkiFromKachiRequestValidator : AbstractValidator<CreatePakkiFromKachiRequest>
{
    public CreatePakkiFromKachiRequestValidator()
    {
        RuleFor(x => x.KachiId).GreaterThan(0);
        RuleFor(x => x.BuyerId).GreaterThan(0);
        RuleFor(x => x.RatePerUnit).GreaterThan(0).WithMessage("Rate must be greater than zero to finalize a Pakki.");
    }
}

public class CreateStandalonePakkiRequestValidator : AbstractValidator<CreateStandalonePakkiRequest>
{
    public CreateStandalonePakkiRequestValidator()
    {
        RuleFor(x => x.SeasonId).GreaterThan(0);
        RuleFor(x => x.BuyerId).GreaterThan(0);
        RuleFor(x => x.FarmerId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.RatePerUnit).GreaterThan(0).WithMessage("Rate must be greater than zero to finalize a Pakki.");
        RuleFor(x => x.BhartiKgPerBag).NotNull().GreaterThan(0).WithMessage("Bharti (weight per bag) is required.");
        RuleFor(x => x.TotalWeightKg).NotNull().GreaterThan(0).WithMessage("Total weight is required.");
        RuleForEach(x => x.DeductionOverrides).SetValidator(new DeductionOverrideRequestValidator());
    }
}

public class UpdatePakkiRequestValidator : AbstractValidator<UpdatePakkiRequest>
{
    public UpdatePakkiRequestValidator()
    {
        RuleFor(x => x.BuyerId).GreaterThan(0);
        RuleFor(x => x.RatePerUnit).GreaterThan(0).WithMessage("Rate must be greater than zero.");
    }
}
