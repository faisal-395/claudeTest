using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationLayoutAndGridBorders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Per-scope: show a full cell border (all four sides) on that scope's data-grid tables
            // instead of just a bottom divider between rows. defaultValue false backfills existing
            // rows with today's look (bottom divider only).
            migrationBuilder.AddColumn<bool>(
                name: "GridFullBorders",
                table: "AppearanceSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // App-wide sidebar-vs-top-menu choice (0 = Sidebar, 1 = TopMenu) — only the Global row's
            // value is ever read; every other scope carries the column but it's ignored there.
            // defaultValue 0 backfills every row as Sidebar, matching the app's current layout.
            migrationBuilder.AddColumn<int>(
                name: "NavigationLayout",
                table: "AppearanceSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "GridFullBorders", table: "AppearanceSettings");
            migrationBuilder.DropColumn(name: "NavigationLayout", table: "AppearanceSettings");
        }
    }
}
