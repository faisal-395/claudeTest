using FluentValidation;

namespace GrainMarket.Application.Appearance;

public class UpdateAppearanceSettingsRequestValidator : AbstractValidator<UpdateAppearanceSettingsRequest>
{
    private const string HexColorPattern = "^#[0-9A-Fa-f]{6}$";

    // Primary doubles as the text color sitting directly on three backgrounds: the page's own
    // form-heading (HeadingBackgroundColor), the Grid Header row (GridHeaderColor), and the
    // readonly "Invoice #"/"Balance" figures (SurfaceColor, fixed). None of those auto-pick a
    // readable text color the way Primary/Accent-as-BACKGROUND cases do (see index.html's
    // contrastTextFor) — so this checks Primary's perceived-brightness CONTRAST against each of
    // those three, rather than rejecting Primary for being light outright. That used to block even
    // a perfectly readable "dark heading background + white heading text" pairing just because the
    // text color itself was light. Same perceived-brightness formula as the JS, so both sides of
    // the system draw the line the same way.
    private const int MinContrastDifference = 125;

    public UpdateAppearanceSettingsRequestValidator()
    {
        RuleFor(x => x.PrimaryColor).NotEmpty().Matches(HexColorPattern).WithMessage("Primary color must be a hex code like #1B4332.");
        RuleFor(x => x.AccentColor).NotEmpty().Matches(HexColorPattern).WithMessage("Accent color must be a hex code like #2D6A4F.");
        RuleFor(x => x.SurfaceColor).NotEmpty().Matches(HexColorPattern).WithMessage("Surface color must be a hex code like #F7F9F7.");
        RuleFor(x => x.PanelColor).NotEmpty().Matches(HexColorPattern).WithMessage("Panel color must be a hex code like #FFFFFF.");
        RuleFor(x => x.HeadingBackgroundColor).NotEmpty().Matches(HexColorPattern).WithMessage("Heading background must be a hex code like #FFFFFF.");
        RuleFor(x => x.InputBackgroundColor).NotEmpty().Matches(HexColorPattern).WithMessage("Selected text box background must be a hex code like #FFFFFF.");
        RuleFor(x => x.GridHeaderColor).NotEmpty().Matches(HexColorPattern).WithMessage("Grid header color must be a hex code like #EEF4EE.");
        RuleFor(x => x.GridBackgroundColor).NotEmpty().Matches(HexColorPattern).WithMessage("Grid background color must be a hex code like #FFFFFF.");
        RuleFor(x => x.LabelFontSizePx).InclusiveBetween(8, 32).WithMessage("Label font size must be between 8 and 32 px.");

        RuleFor(x => x).Must(x => HasContrast(x.PrimaryColor, x.HeadingBackgroundColor))
            .WithName(nameof(UpdateAppearanceSettingsRequest.PrimaryColor))
            .WithMessage("Heading Text and Heading Background are too close in brightness to read clearly — pick a lighter/darker pair.");
        RuleFor(x => x).Must(x => HasContrast(x.PrimaryColor, x.GridHeaderColor))
            .WithName(nameof(UpdateAppearanceSettingsRequest.PrimaryColor))
            .WithMessage("Heading Text and Grid Header Background are too close in brightness to read clearly — pick a lighter/darker pair.");
        RuleFor(x => x).Must(x => HasContrast(x.PrimaryColor, x.SurfaceColor))
            .WithName(nameof(UpdateAppearanceSettingsRequest.PrimaryColor))
            .WithMessage("Heading Text doesn't contrast enough against the page background for the Invoice #/Balance figures to stay readable.");
    }

    // Returns true (passes validation) for a malformed hex — the Matches rule above already
    // reports that, and failing a contrast check too would just show two confusing errors for one typo.
    private static bool HasContrast(string hexA, string hexB) =>
        !TryBrightness(hexA, out var a) || !TryBrightness(hexB, out var b) || Math.Abs(a - b) >= MinContrastDifference;

    private static bool TryBrightness(string hex, out int brightness)
    {
        brightness = 0;
        if (hex.Length != 7) return false;

        var r = Convert.ToInt32(hex.Substring(1, 2), 16);
        var g = Convert.ToInt32(hex.Substring(3, 2), 16);
        var b = Convert.ToInt32(hex.Substring(5, 2), 16);
        brightness = (r * 299 + g * 587 + b * 114) / 1000;
        return true;
    }
}
