using FluentValidation;

namespace GrainMarket.Application.Roles;

public class UpsertRoleRequestValidator : AbstractValidator<UpsertRoleRequest>
{
    public UpsertRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
