using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DefaultNavigationLayoutToTopMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Switches the app-wide default from the sidebar to the top menu. New installs already
            // get this from AppearanceSettings.NavigationLayout's own default, but an install that
            // ran before this change has its Global row (Scope = 0) already stored as Sidebar (0) —
            // flip it here so everyone already running the app sees the new default too, no manual
            // trip to Setup > Appearance required. Still fully overridable there afterwards.
            migrationBuilder.Sql(@"UPDATE ""AppearanceSettings"" SET ""NavigationLayout"" = 1 WHERE ""Scope"" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE ""AppearanceSettings"" SET ""NavigationLayout"" = 0 WHERE ""Scope"" = 0;");
        }
    }
}
