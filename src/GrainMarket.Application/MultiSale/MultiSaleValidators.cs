using FluentValidation;

namespace GrainMarket.Application.MultiSale;

public class MultiSaleRowRequestValidator : AbstractValidator<MultiSaleRowRequest>
{
    public MultiSaleRowRequestValidator()
    {
        RuleFor(x => x.FarmerId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.RatePerUnit).NotNull().GreaterThan(0).WithMessage("Rate per Man is required.");
        RuleFor(x => x)
            .Must(x => (x.ManQty ?? 0) + (x.KiloQty ?? 0) + (x.GramQty ?? 0) + (x.BoriQty ?? 0) > 0)
            .WithMessage("At least one of Man/Kilo/Gram/Bori quantity must be greater than zero.")
            .WithName("Weight");
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
