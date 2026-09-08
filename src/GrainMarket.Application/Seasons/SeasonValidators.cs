using FluentValidation;

namespace GrainMarket.Application.Seasons;

public class UpsertSeasonRequestValidator : AbstractValidator<UpsertSeasonRequest>
{
    public UpsertSeasonRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
    }
}
