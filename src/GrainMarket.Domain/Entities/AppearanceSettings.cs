using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

/// <summary>Per-install brand colors shown across the app (sidebar, headings, buttons/links,
/// panel backgrounds) — a single row, edited from Setup &gt; Appearance by Owner/Admin only, never
/// created or deleted through the UI. Stored as "#RRGGBB" hex strings and applied client-side as
/// CSS custom properties, so a re-skin for a different client needs no code change or redeploy.</summary>
public class AppearanceSettings : BaseEntity
{
    public string PrimaryColor { get; set; } = "#1B4332";
    public string AccentColor { get; set; } = "#2D6A4F";
    public string SurfaceColor { get; set; } = "#F7F9F7";

    /// <summary>Background of form/entry cards — the Purchase/Sale Invoice entry panel, Setup
    /// forms, and the results grid's own background (not its header row — see GridHeaderColor).</summary>
    public string PanelColor { get; set; } = "#FFFFFF";
    public string InputBackgroundColor { get; set; } = "#FFFFFF";
    public string InputBorderColor { get; set; } = "#CCCCCC";
    public string GridHeaderColor { get; set; } = "#EEF4EE";
}
