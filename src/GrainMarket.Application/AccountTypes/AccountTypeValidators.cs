using FluentValidation;

namespace GrainMarket.Application.AccountTypes;

public class CreateAccountTypeRequestValidator : AbstractValidator<CreateAccountTypeRequest>
{
    public CreateAccountTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NameUrdu).MaximumLength(100);
    }
}
