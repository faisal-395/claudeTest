using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitPaymentReceiptThemeScopesAndBrandColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ThemeScope 5 ("Voucher") used to cover both Payment and Receipt — they're now split
            // into their own independently-colorable scopes (8 = Payment, 9 = Receipt) so each can
            // match the legacy software's red Payment / green Receipt. Repurpose any existing row at
            // the old shared id into Payment's new one rather than deleting it, carrying forward
            // whatever was already customized there.
            migrationBuilder.Sql(@"UPDATE ""AppearanceSettings"" SET ""Scope"" = 8 WHERE ""Scope"" = 5;");

            // Guarantee a row exists for both new scopes (covers a fresh install with no prior
            // "Voucher" row to repurpose, and Receipt either way) before forcing their colors below —
            // same column set/defaults as every other scope's class-level defaults.
            migrationBuilder.Sql(@"
                INSERT INTO ""AppearanceSettings"" (
                    ""Scope"", ""PrimaryColor"", ""AccentColor"", ""SurfaceColor"", ""PanelColor"", ""ProductPanelColor"",
                    ""CreditPanelColor"", ""DebitPanelColor"", ""HeadingBackgroundColor"", ""InputBackgroundColor"",
                    ""GridHeaderColor"", ""GridBackgroundColor"", ""LabelFontSizePx"", ""GridFullBorders"", ""NavigationLayout"",
                    ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.""Scope"", '#1B4332', '#2D6A4F', '#F7F9F7', '#FFFFFF', '#FFFFFF',
                    '#E8F5E9', '#FCE4EC', '#FFFFFF', '#FFFFFF',
                    '#EEF4EE', '#FFFFFF', 13, FALSE, 1,
                    NOW(), FALSE
                FROM (VALUES (8), (9)) AS v(""Scope"")
                WHERE NOT EXISTS (SELECT 1 FROM ""AppearanceSettings"" a WHERE a.""Scope"" = v.""Scope"");
            ");

            // Force the new defaults (see AppearanceSettingsService.NewDefaultsFor) onto whatever
            // row now sits at each scope — the repurposed one, or the freshly-inserted one.
            migrationBuilder.Sql(@"
                UPDATE ""AppearanceSettings""
                SET ""HeadingBackgroundColor"" = '#F4A7A7', ""GridHeaderColor"" = '#B7C2D1', ""GridBackgroundColor"" = '#D3DAE3'
                WHERE ""Scope"" = 8;
            ");
            migrationBuilder.Sql(@"
                UPDATE ""AppearanceSettings""
                SET ""HeadingBackgroundColor"" = '#A5D6A7', ""GridHeaderColor"" = '#B7C2D1', ""GridBackgroundColor"" = '#D3DAE3'
                WHERE ""Scope"" = 9;
            ");

            // Purchase ("New Purchase") gets the same red family as Payment, matching its own
            // legacy screenshot — its Invoice Info and Add Product panels are visibly red-tinted
            // there too, unlike Payment/Receipt's plain white form.
            migrationBuilder.Sql(@"
                UPDATE ""AppearanceSettings""
                SET ""HeadingBackgroundColor"" = '#F4A7A7', ""PanelColor"" = '#F2B6B6', ""ProductPanelColor"" = '#EF9A9A',
                    ""GridHeaderColor"" = '#B7C2D1', ""GridBackgroundColor"" = '#D3DAE3'
                WHERE ""Scope"" = 2;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""AppearanceSettings""
                SET ""HeadingBackgroundColor"" = '#FFFFFF', ""PanelColor"" = '#FFFFFF', ""ProductPanelColor"" = '#FFFFFF',
                    ""GridHeaderColor"" = '#EEF4EE', ""GridBackgroundColor"" = '#FFFFFF'
                WHERE ""Scope"" = 2;
            ");

            migrationBuilder.Sql(@"DELETE FROM ""AppearanceSettings"" WHERE ""Scope"" = 9;");
            migrationBuilder.Sql(@"UPDATE ""AppearanceSettings"" SET ""Scope"" = 5 WHERE ""Scope"" = 8;");
        }
    }
}
