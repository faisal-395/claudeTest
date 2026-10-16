using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DefaultSaleInvoiceBrandColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Matches the legacy software's own purple look for Sale Invoice (New Sale General) —
            // see AppearanceSettingsService.NewDefaultsFor, which a brand-new row already picks up.
            // Flips an already-seeded row too, same as DefaultNavigationLayoutToTopMenu /
            // DefaultKachiPakkiBrandColors. Still fully overridable from Setup > Appearance.
            migrationBuilder.Sql(@"
                UPDATE ""AppearanceSettings""
                SET ""HeadingBackgroundColor"" = '#C5AEE0', ""ProductPanelColor"" = '#D9C7EC',
                    ""GridHeaderColor"" = '#B7C2D1', ""GridBackgroundColor"" = '#D3DAE3'
                WHERE ""Scope"" = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""AppearanceSettings""
                SET ""HeadingBackgroundColor"" = '#FFFFFF', ""ProductPanelColor"" = '#FFFFFF',
                    ""GridHeaderColor"" = '#EEF4EE', ""GridBackgroundColor"" = '#FFFFFF'
                WHERE ""Scope"" = 1;
            ");
        }
    }
}
