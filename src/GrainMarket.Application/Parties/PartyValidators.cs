using FluentValidation;

namespace GrainMarket.Application.Parties;

public class UpsertPartyRequestValidator : AbstractValidator<UpsertPartyRequest>
{
    public UpsertPartyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameUrdu).MaximumLength(200);
        RuleFor(x => x.PartyType).IsInEnum();
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Cnic).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OpeningBalanceType).IsInEnum();
    }
}
