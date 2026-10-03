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

/// <summary>Where the app's navigation lives — a single, app-wide choice (there's only ever one
/// nav, not one per page), stored on the Global row's NavigationLayout and ignored on every other
/// scope's row.</summary>
public enum NavigationLayout
{
    Sidebar = 0,
    TopMenu = 1
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
    /// form heading text color only — it does not affect the sidebar or any other page.</summary>
    public string PrimaryColor { get; set; } = "#1B4332";
    public string AccentColor { get; set; } = "#2D6A4F";

    /// <summary>Still backs .main-content/staging-panel backgrounds, but not exposed as an editable
    /// Setup > Appearance control anymore (kept frozen at whatever it's set to).</summary>
    public string SurfaceColor { get; set; } = "#F7F9F7";

    /// <summary>Background of form/entry cards — the Purchase/Sale Invoice entry panel(s) and
    /// Setup forms. The results grid's own body background is separate — see GridBackgroundColor.</summary>
    public string PanelColor { get; set; } = "#FFFFFF";

    /// <summary>Background behind the page's own &lt;h1&gt; heading text.</summary>
    public string HeadingBackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>Background shown only while a text box is focused/selected — not applied to every
    /// text box at rest.</summary>
    public string InputBackgroundColor { get; set; } = "#FFFFFF";
    public string GridHeaderColor { get; set; } = "#EEF4EE";

    /// <summary>The results grid's own body background (behind every data row) — independent of
    /// PanelColor, so a client can give the entry form and the grid below it different backgrounds.</summary>
    public string GridBackgroundColor { get; set; } = "#FFFFFF";

    /// <summary>Font size (px) of field labels ("Invoice #", "Bill No", etc.) within this scope.</summary>
    public int LabelFontSizePx { get; set; } = 13;

    /// <summary>Whether this scope's data-grid tables show a full cell border (all four sides)
    /// instead of just a bottom divider between rows.</summary>
    public bool GridFullBorders { get; set; }

    /// <summary>App-wide sidebar vs. top menu choice. Only meaningful on the Global row — every
    /// other scope's row carries this column too (same shared table) but it's never read from them.</summary>
    public NavigationLayout NavigationLayout { get; set; } = NavigationLayout.Sidebar;
}
