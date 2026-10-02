using GrainMarket.Domain.Common;

namespace GrainMarket.Domain.Entities;

/// <summary>Which part of the app a row of brand colors applies to. Global is the app-wide
/// fallback — the persistent sidebar/nav, and any page that isn't one of the other scopes (Setup,
/// Reports, Ledger, vouchers, etc.). The others each override Global only within their own page, so
/// e.g. Sale Invoice can have its own form heading/textbox/button colors without touching the
/// sidebar or Purchase's colors.</summary>
public enum ThemeScope
{
    Global = 0,
    SaleInvoice = 1,
    Purchase = 2,
    Kachi = 3,
    Pakki = 4
}

/// <summary>Per-install, per-scope brand colors — one row per ThemeScope, edited from Setup &gt;
/// Appearance by Owner/Admin only, never created or deleted through the UI (a migration seeds one
/// row per scope; see SeedAppearanceSettingsAsync). Stored as "#RRGGBB" hex strings and applied
/// client-side as scoped CSS custom properties, so a re-skin for a different client — or a single
/// screen within it — needs no code change or redeploy.</summary>
public class AppearanceSettings : BaseEntity
{
    public ThemeScope Scope { get; set; }

    /// <summary>Global scope: sidebar background, page headings. Other scopes: that page's own
    /// form heading color only — it does not affect the sidebar or any other page.</summary>
    public string PrimaryColor { get; set; } = "#1B4332";
    public string AccentColor { get; set; } = "#2D6A4F";
    public string SurfaceColor { get; set; } = "#F7F9F7";

    /// <summary>Background of form/entry cards — the Purchase/Sale Invoice entry panel, Setup
    /// forms, and the results grid's own background (not its header row — see GridHeaderColor).</summary>
    public string PanelColor { get; set; } = "#FFFFFF";
    public string InputBackgroundColor { get; set; } = "#FFFFFF";
    public string InputBorderColor { get; set; } = "#CCCCCC";
    public string GridHeaderColor { get; set; } = "#EEF4EE";

    /// <summary>Font size (px) of field labels ("Invoice #", "Bill No", etc.) within this scope.</summary>
    public int LabelFontSizePx { get; set; } = 13;
}
