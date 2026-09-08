using FluentValidation;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Vouchers;

public class CreatePaymentOrReceiptRequestValidator : AbstractValidator<CreatePaymentOrReceiptRequest>
{
    public CreatePaymentOrReceiptRequestValidator()
    {
        RuleFor(x => x.VoucherType).Must(t => t is VoucherType.Payment or VoucherType.Receipt);
        RuleFor(x => x.SeasonId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);

        RuleFor(x => x.FromPartyId).NotNull().When(x => x.FromType == LedgerPartyRefType.Party).WithMessage("From party is required.");
        RuleFor(x => x.FromAccountId).NotNull().When(x => x.FromType == LedgerPartyRefType.Account).WithMessage("From account is required.");
        RuleFor(x => x.ToPartyId).NotNull().When(x => x.ToType == LedgerPartyRefType.Party).WithMessage("To party is required.");
        RuleFor(x => x.ToAccountId).NotNull().When(x => x.ToType == LedgerPartyRefType.Account).WithMessage("To account is required.");

        RuleFor(x => x)
            .Must(x => !(x.FromType == x.ToType && x.FromType == LedgerPartyRefType.Cash))
            .WithMessage("From and To cannot both be Cash.")
            .Must(x => !(x.FromType == x.ToType && x.FromType == LedgerPartyRefType.Bank))
            .WithMessage("From and To cannot both be Bank.");
    }
}

public class CreateJournalRequestValidator : AbstractValidator<CreateJournalRequest>
{
    public CreateJournalRequestValidator()
    {
        RuleFor(x => x.SeasonId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.DebitAccountId).GreaterThan(0);
        RuleFor(x => x.CreditAccountId).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.DebitAccountId != x.CreditAccountId)
            .WithMessage("Debit and credit accounts must be different.");
    }
}
