using FluentValidation;

namespace GrainMarket.Application.Products;

public class UpsertProductRequestValidator : AbstractValidator<UpsertProductRequest>
{
    public UpsertProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameUrdu).MaximumLength(200);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.BaseUnit).NotEmpty().MaximumLength(10);
        RuleFor(x => x.DefaultRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0).When(x => x.SalePrice.HasValue);
        RuleFor(x => x.SaleMarkupPercent).GreaterThanOrEqualTo(0).When(x => x.SaleMarkupPercent.HasValue);
    }
}
