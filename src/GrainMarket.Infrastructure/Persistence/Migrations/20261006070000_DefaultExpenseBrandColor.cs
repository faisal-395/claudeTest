using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DefaultExpenseBrandColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Red heading only, matching Payment/Purchase's red family — see
            // AppearanceSettingsService.NewDefaultsFor, which a brand-new row already picks up.
            // Flips an already-seeded row too, same pattern as the other scope default-color
            // migrations. Still fully overridable from Setup > Appearance.
            migrationBuilder.Sql(@"
                UPDATE ""AppearanceSettings""
                SET ""HeadingBackgroundColor"" = '#F4A7A7'
                WHERE ""Scope"" = 7;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""AppearanceSettings""
                SET ""HeadingBackgroundColor"" = '#FFFFFF'
                WHERE ""Scope"" = 7;
            ");
        }
    }
}
