using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreAppearanceColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Extends Setup > Appearance beyond the sidebar/buttons to form/panel backgrounds,
            // text box colors and the results grid's header row. defaultValue backfills the one
            // existing AppearanceSettings row (seeded by the previous migration) with the same
            // colors the app already hardcoded for these elements, so nothing visibly changes
            // until the super user picks something different from Setup > Appearance.
            migrationBuilder.AddColumn<string>(
                name: "PanelColor",
                table: "AppearanceSettings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#FFFFFF");

            migrationBuilder.AddColumn<string>(
                name: "InputBackgroundColor",
                table: "AppearanceSettings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#FFFFFF");

            migrationBuilder.AddColumn<string>(
                name: "InputBorderColor",
                table: "AppearanceSettings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#CCCCCC");

            migrationBuilder.AddColumn<string>(
                name: "GridHeaderColor",
                table: "AppearanceSettings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#EEF4EE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PanelColor", table: "AppearanceSettings");
            migrationBuilder.DropColumn(name: "InputBackgroundColor", table: "AppearanceSettings");
            migrationBuilder.DropColumn(name: "InputBorderColor", table: "AppearanceSettings");
            migrationBuilder.DropColumn(name: "GridHeaderColor", table: "AppearanceSettings");
        }
    }
}
