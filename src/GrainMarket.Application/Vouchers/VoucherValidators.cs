using FluentValidation;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Vouchers;

public class CreatePaymentOrReceiptRequestValidator : AbstractValidator<CreatePaymentOrReceiptRequest>
{
    public CreatePaymentOrReceiptRequestValidator()
    {
        RuleFor(x => x.VoucherType).Must(t => t is VoucherType.Payment or VoucherType.Receipt or VoucherType.Journal);
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
            .WithMessage("From and To cannot both be Bank.")
            .Must(x => !VoucherSides.AreTheSame(x.FromType, x.FromPartyId, x.FromAccountId, x.ToType, x.ToPartyId, x.ToAccountId))
            .WithMessage("From and To must be different.");
    }
}

public class UpdatePaymentOrReceiptRequestValidator : AbstractValidator<UpdatePaymentOrReceiptRequest>
{
    public UpdatePaymentOrReceiptRequestValidator()
    {
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
            .WithMessage("From and To cannot both be Bank.")
            .Must(x => !VoucherSides.AreTheSame(x.FromType, x.FromPartyId, x.FromAccountId, x.ToType, x.ToPartyId, x.ToAccountId))
            .WithMessage("From and To must be different.");
    }
}

/// <summary>Shared by Create/UpdatePaymentOrReceiptRequestValidator — generalizes the old Journal
/// page's "Debit and credit accounts must be different" check across every voucher type and every
/// LedgerPartyRefType, not just Account.</summary>
internal static class VoucherSides
{
    public static bool AreTheSame(LedgerPartyRefType fromType, int? fromPartyId, int? fromAccountId, LedgerPartyRefType toType, int? toPartyId, int? toAccountId)
    {
        if (fromType != toType) return false;
        return fromType == LedgerPartyRefType.Party ? fromPartyId == toPartyId : fromAccountId == toAccountId;
    }
}
