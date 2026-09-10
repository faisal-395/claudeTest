using FluentValidation;

namespace GrainMarket.Application.Kachis;

public class CreateKachiRequestValidator : AbstractValidator<CreateKachiRequest>
{
    public CreateKachiRequestValidator()
    {
        RuleFor(x => x.SeasonId).GreaterThan(0);
        RuleFor(x => x.FarmerId).GreaterThan(0);
        RuleFor(x => x.BuyerId).GreaterThan(0).When(x => x.BuyerId.HasValue);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.RatePerUnit).GreaterThanOrEqualTo(0).When(x => x.RatePerUnit.HasValue);
        RuleFor(x => x)
            .Must(x => (x.ManQty ?? 0) + (x.KiloQty ?? 0) + (x.GramQty ?? 0) + (x.BoriQty ?? 0) > 0)
            .WithMessage("At least one of Man/Kilo/Gram/Bori quantity must be greater than zero.")
            .WithName("Weight");
    }
}

public class UpdateKachiRequestValidator : AbstractValidator<UpdateKachiRequest>
{
    public UpdateKachiRequestValidator()
    {
        RuleFor(x => x.SeasonId).GreaterThan(0);
        RuleFor(x => x.FarmerId).GreaterThan(0);
        RuleFor(x => x.BuyerId).GreaterThan(0).When(x => x.BuyerId.HasValue);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.RatePerUnit).GreaterThanOrEqualTo(0).When(x => x.RatePerUnit.HasValue);
        RuleFor(x => x)
            .Must(x => (x.ManQty ?? 0) + (x.KiloQty ?? 0) + (x.GramQty ?? 0) + (x.BoriQty ?? 0) > 0)
            .WithMessage("At least one of Man/Kilo/Gram/Bori quantity must be greater than zero.")
            .WithName("Weight");
    }
}
