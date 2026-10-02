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
        RuleFor(x => x.PanelColor).NotEmpty().Matches(HexColorPattern).WithMessage("Panel color must be a hex code like #FFFFFF.");
        RuleFor(x => x.InputBackgroundColor).NotEmpty().Matches(HexColorPattern).WithMessage("Text box background must be a hex code like #FFFFFF.");
        RuleFor(x => x.InputBorderColor).NotEmpty().Matches(HexColorPattern).WithMessage("Text box border must be a hex code like #CCCCCC.");
        RuleFor(x => x.GridHeaderColor).NotEmpty().Matches(HexColorPattern).WithMessage("Grid header color must be a hex code like #EEF4EE.");
        RuleFor(x => x.LabelFontSizePx).InclusiveBetween(8, 32).WithMessage("Label font size must be between 8 and 32 px.");
    }
}
