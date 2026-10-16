using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddThemeScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Splits the single AppearanceSettings row into one row per ThemeScope (Global=0,
            // SaleInvoice=1, Purchase=2, Kachi=3, Pakki=4), so each of those pages can have its own
            // form heading/textbox/button/grid-header colors and label font size without touching
            // the sidebar or any other page. defaultValue: 0 on Scope makes the existing row Global
            // automatically — it was already the one driving the whole app's colors, which is
            // exactly what Global means.
            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "AppearanceSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LabelFontSizePx",
                table: "AppearanceSettings",
                type: "integer",
                nullable: false,
                defaultValue: 13);

            migrationBuilder.CreateIndex(
                name: "IX_AppearanceSettings_Scope",
                table: "AppearanceSettings",
                column: "Scope",
                unique: true);

            // Give Sale Invoice/Purchase/Kachi/Pakki their own row, starting out identical to
            // Global (copied from whichever row is Global) so nothing visibly changes until the
            // super user customizes one of them from Setup > Appearance. Idempotent — skips scopes
            // that already have a row, so re-running this (or a from-scratch install that also runs
            // SeedAppearanceSettingsAsync) never duplicates rows.
            migrationBuilder.Sql(@"
                INSERT INTO ""AppearanceSettings""
                    (""Scope"", ""PrimaryColor"", ""AccentColor"", ""SurfaceColor"", ""PanelColor"",
                     ""InputBackgroundColor"", ""InputBorderColor"", ""GridHeaderColor"", ""LabelFontSizePx"",
                     ""CreatedAtUtc"", ""IsDeleted"")
                SELECT scope.value, g.""PrimaryColor"", g.""AccentColor"", g.""SurfaceColor"", g.""PanelColor"",
                       g.""InputBackgroundColor"", g.""InputBorderColor"", g.""GridHeaderColor"", g.""LabelFontSizePx"",
                       NOW(), FALSE
                FROM (VALUES (1), (2), (3), (4)) AS scope(value)
                CROSS JOIN (SELECT * FROM ""AppearanceSettings"" WHERE ""Scope"" = 0 LIMIT 1) g
                WHERE NOT EXISTS (SELECT 1 FROM ""AppearanceSettings"" WHERE ""Scope"" = scope.value);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""AppearanceSettings"" WHERE ""Scope"" <> 0;");

            migrationBuilder.DropIndex(
                name: "IX_AppearanceSettings_Scope",
                table: "AppearanceSettings");

            migrationBuilder.DropColumn(name: "Scope", table: "AppearanceSettings");
            migrationBuilder.DropColumn(name: "LabelFontSizePx", table: "AppearanceSettings");
        }
    }
}
