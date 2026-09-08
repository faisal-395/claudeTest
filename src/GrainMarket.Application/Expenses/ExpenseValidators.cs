using FluentValidation;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Expenses;

public class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.ExpenseAccountId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaidFrom).Must(t => t is LedgerPartyRefType.Cash or LedgerPartyRefType.Bank or LedgerPartyRefType.Account);
        RuleFor(x => x.PaidFromAccountId).NotNull().When(x => x.PaidFrom == LedgerPartyRefType.Account).WithMessage("Paid-from account is required.");
    }
}
