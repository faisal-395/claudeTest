using FluentValidation;

namespace GrainMarket.Application.Recovery;

public class CreateRecoveryNoteRequestValidator : AbstractValidator<CreateRecoveryNoteRequest>
{
    public CreateRecoveryNoteRequestValidator()
    {
        RuleFor(x => x.PartyId).GreaterThan(0);
        RuleFor(x => x.Note).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.PromisedAmount).GreaterThanOrEqualTo(0).When(x => x.PromisedAmount.HasValue);
    }
}
