using FluentValidation;
using GrainMarket.Application.Common;

namespace GrainMarket.Application.MultiSale;

public class MultiSaleRowRequestValidator : AbstractValidator<MultiSaleRowRequest>
{
    public MultiSaleRowRequestValidator()
    {
        RuleFor(x => x.FarmerId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.RatePerUnit).NotNull().GreaterThan(0).WithMessage("Rate per Man is required.");
        RuleFor(x => x.BhartiKgPerBag).NotNull().GreaterThan(0).WithMessage("Bharti (weight per bag) is required.");
        RuleFor(x => x.TotalWeightKg).NotNull().GreaterThan(0).WithMessage("Total weight is required.");
        RuleForEach(x => x.DeductionOverrides).SetValidator(new DeductionOverrideRequestValidator());
    }
}

public class CreateMultiSaleRequestValidator : AbstractValidator<CreateMultiSaleRequest>
{
    public CreateMultiSaleRequestValidator()
    {
        RuleFor(x => x.SeasonId).GreaterThan(0);
        RuleFor(x => x.BuyerId).GreaterThan(0);
        RuleFor(x => x.Rows).NotEmpty().WithMessage("Add at least one farmer/item row.");
        RuleForEach(x => x.Rows).SetValidator(new MultiSaleRowRequestValidator());
    }
}
