using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdjustAppearanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Text box border color is no longer a Setup > Appearance control — textboxes use a
            // fixed neutral border now (see app.css), so this column has nothing left reading it.
            migrationBuilder.DropColumn(
                name: "InputBorderColor",
                table: "AppearanceSettings");

            // New: background behind each scope's own <h1> page heading. defaultValue backfills
            // every existing row with white, matching how headings already render today (no
            // background), so nothing visibly changes until a scope sets its own.
            migrationBuilder.AddColumn<string>(
                name: "HeadingBackgroundColor",
                table: "AppearanceSettings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#FFFFFF");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeadingBackgroundColor",
                table: "AppearanceSettings");

            migrationBuilder.AddColumn<string>(
                name: "InputBorderColor",
                table: "AppearanceSettings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#CCCCCC");
        }
    }
}
