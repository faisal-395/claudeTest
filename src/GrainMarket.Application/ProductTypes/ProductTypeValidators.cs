using FluentValidation;

namespace GrainMarket.Application.ProductTypes;

public class CreateProductTypeRequestValidator : AbstractValidator<CreateProductTypeRequest>
{
    public CreateProductTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameUrdu).MaximumLength(100);
    }
}
