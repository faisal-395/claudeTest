using FluentValidation;

namespace GrainMarket.Application.ChartOfAccounts;

public class UpsertChartOfAccountRequestValidator : AbstractValidator<UpsertChartOfAccountRequest>
{
    public UpsertChartOfAccountRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameUrdu).MaximumLength(200);
        RuleFor(x => x.AccountType).IsInEnum();
        RuleFor(x => x.AllowedRoleIds)
            .Must((req, roleIds) => !req.IsProtected || roleIds.Count > 0)
            .WithMessage("A protected account must specify at least one allowed role.");
    }
}
