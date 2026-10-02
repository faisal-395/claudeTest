using FluentValidation;

namespace GrainMarket.Application.Appearance;

public class UpdateAppearanceSettingsRequestValidator : AbstractValidator<UpdateAppearanceSettingsRequest>
{
    private const string HexColorPattern = "^#[0-9A-Fa-f]{6}$";

    public UpdateAppearanceSettingsRequestValidator()
    {
        RuleFor(x => x.PrimaryColor).NotEmpty().Matches(HexColorPattern).WithMessage("Primary color must be a hex code like #1B4332.");
        RuleFor(x => x.AccentColor).NotEmpty().Matches(HexColorPattern).WithMessage("Accent color must be a hex code like #2D6A4F.");
        RuleFor(x => x.SurfaceColor).NotEmpty().Matches(HexColorPattern).WithMessage("Surface color must be a hex code like #F7F9F7.");
    }
}
