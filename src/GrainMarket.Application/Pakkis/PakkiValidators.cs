using FluentValidation;

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
        RuleFor(x => x)
            .Must(x => (x.ManQty ?? 0) + (x.KiloQty ?? 0) + (x.GramQty ?? 0) + (x.BoriQty ?? 0) > 0)
            .WithMessage("At least one of Man/Kilo/Gram/Bori quantity must be greater than zero.")
            .WithName("Weight");
    }
}
