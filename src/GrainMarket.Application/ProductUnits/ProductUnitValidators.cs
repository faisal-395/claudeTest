using FluentValidation;

namespace GrainMarket.Application.ProductUnits;

public class CreateProductUnitRequestValidator : AbstractValidator<CreateProductUnitRequest>
{
    public CreateProductUnitRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(10);
        RuleFor(x => x.NameUrdu).MaximumLength(20);
    }
}
