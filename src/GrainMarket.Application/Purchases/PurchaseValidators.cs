using FluentValidation;

namespace GrainMarket.Application.Purchases;

public class PurchaseLineRequestValidator : AbstractValidator<PurchaseLineRequest>
{
    public PurchaseLineRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}

public class CreatePurchaseRequestValidator : AbstractValidator<CreatePurchaseRequest>
{
    public CreatePurchaseRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.Lines).SetValidator(new PurchaseLineRequestValidator());
        RuleFor(x => x.PaidCash).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PrintFormat).IsInEnum();
        RuleFor(x => x.PrintLanguage).IsInEnum();
    }
}
