using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGridBackgroundColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The results grid's own body background, independent of PanelColor (the entry
            // form/card background) — defaultValue '#FFFFFF' backfills every existing row so
            // nothing changes visually until a client picks a different one from Setup > Appearance.
            migrationBuilder.AddColumn<string>(
                name: "GridBackgroundColor",
                table: "AppearanceSettings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#FFFFFF");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "GridBackgroundColor", table: "AppearanceSettings");
        }
    }
}
