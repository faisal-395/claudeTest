using FluentValidation;

namespace GrainMarket.Application.Appearance;

public class UpdateAppearanceSettingsRequestValidator : AbstractValidator<UpdateAppearanceSettingsRequest>
{
    private const string HexColorPattern = "^#[0-9A-Fa-f]{6}$";

    // Primary doubles as the text color on light backgrounds (headings, the readonly "Invoice #"/
    // "Balance" figures) AND the page's own form-heading text sits directly on HeadingBackgroundColor
    // — a near-white Primary goes invisible in both places, with nothing else in the theme able to
    // compensate (unlike Primary/Accent-as-BACKGROUND cases, which auto-pick readable text — see
    // index.html's contrastTextFor). Same perceived-brightness formula as that JS, so both sides of
    // the system draw the line in the same place.
    private const int MaxTextColorBrightness = 160;

    public UpdateAppearanceSettingsRequestValidator()
    {
        RuleFor(x => x.PrimaryColor).NotEmpty().Matches(HexColorPattern).WithMessage("Primary color must be a hex code like #1B4332.")
            .Must(NotBeTooLight).WithMessage("Primary color is too light to read as text — pick a darker shade.");
        RuleFor(x => x.AccentColor).NotEmpty().Matches(HexColorPattern).WithMessage("Accent color must be a hex code like #2D6A4F.");
        RuleFor(x => x.SurfaceColor).NotEmpty().Matches(HexColorPattern).WithMessage("Surface color must be a hex code like #F7F9F7.");
        RuleFor(x => x.PanelColor).NotEmpty().Matches(HexColorPattern).WithMessage("Panel color must be a hex code like #FFFFFF.");
        RuleFor(x => x.HeadingBackgroundColor).NotEmpty().Matches(HexColorPattern).WithMessage("Heading background must be a hex code like #FFFFFF.");
        RuleFor(x => x.InputBackgroundColor).NotEmpty().Matches(HexColorPattern).WithMessage("Selected text box background must be a hex code like #FFFFFF.");
        RuleFor(x => x.GridHeaderColor).NotEmpty().Matches(HexColorPattern).WithMessage("Grid header color must be a hex code like #EEF4EE.");
        RuleFor(x => x.GridBackgroundColor).NotEmpty().Matches(HexColorPattern).WithMessage("Grid background color must be a hex code like #FFFFFF.");
        RuleFor(x => x.LabelFontSizePx).InclusiveBetween(8, 32).WithMessage("Label font size must be between 8 and 32 px.");
    }

    private static bool NotBeTooLight(string hex)
    {
        if (hex.Length != 7) return true; // malformed — the Matches rule above already reports this

        var r = Convert.ToInt32(hex.Substring(1, 2), 16);
        var g = Convert.ToInt32(hex.Substring(3, 2), 16);
        var b = Convert.ToInt32(hex.Substring(5, 2), 16);
        var brightness = (r * 299 + g * 587 + b * 114) / 1000;
        return brightness <= MaxTextColorBrightness;
    }
}
