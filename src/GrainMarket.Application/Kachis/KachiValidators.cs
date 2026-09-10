using FluentValidation;

namespace GrainMarket.Application.Kachis;

public class CreateKachiRequestValidator : AbstractValidator<CreateKachiRequest>
{
    public CreateKachiRequestValidator()
    {
        RuleFor(x => x.SeasonId).GreaterThan(0);
        RuleFor(x => x.FarmerId).GreaterThan(0);
        RuleFor(x => x.BuyerId).NotNull().GreaterThan(0).WithMessage("Buyer is required.");
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.RatePerUnit).NotNull().GreaterThan(0).WithMessage("Rate per Man is required.");
        RuleFor(x => x.BhartiKgPerBag).NotNull().GreaterThan(0).WithMessage("Bharti (weight per bag) is required.");
        RuleFor(x => x.TotalWeightKg).NotNull().GreaterThan(0).WithMessage("Total weight is required.");
        RuleFor(x => x.DhrnKg).GreaterThanOrEqualTo(0).When(x => x.DhrnKg.HasValue).WithMessage("Dhrn cannot be negative.");
        RuleFor(x => x)
            .Must(x => (x.DhrnKg ?? 0) < (x.TotalWeightKg ?? 0))
            .When(x => x.TotalWeightKg.HasValue)
            .WithMessage("Dhrn cannot be greater than or equal to total weight.")
            .WithName("Dhrn");
    }
}

public class UpdateKachiRequestValidator : AbstractValidator<UpdateKachiRequest>
{
    public UpdateKachiRequestValidator()
    {
        RuleFor(x => x.SeasonId).GreaterThan(0);
        RuleFor(x => x.FarmerId).GreaterThan(0);
        RuleFor(x => x.BuyerId).NotNull().GreaterThan(0).WithMessage("Buyer is required.");
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.RatePerUnit).NotNull().GreaterThan(0).WithMessage("Rate per Man is required.");
        RuleFor(x => x.BhartiKgPerBag).NotNull().GreaterThan(0).WithMessage("Bharti (weight per bag) is required.");
        RuleFor(x => x.TotalWeightKg).NotNull().GreaterThan(0).WithMessage("Total weight is required.");
        RuleFor(x => x.DhrnKg).GreaterThanOrEqualTo(0).When(x => x.DhrnKg.HasValue).WithMessage("Dhrn cannot be negative.");
        RuleFor(x => x)
            .Must(x => (x.DhrnKg ?? 0) < (x.TotalWeightKg ?? 0))
            .When(x => x.TotalWeightKg.HasValue)
            .WithMessage("Dhrn cannot be greater than or equal to total weight.")
            .WithName("Dhrn");
    }
}
