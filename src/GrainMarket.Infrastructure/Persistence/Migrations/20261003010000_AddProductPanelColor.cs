using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPanelColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Purchase/Sale Invoice's "Add Product" panel's own background, independent of
            // PanelColor (used by "Invoice Info" and every other form panel) — defaultValue
            // '#FFFFFF' backfills every existing row so nothing changes visually until a client
            // picks a different one from Setup > Appearance.
            migrationBuilder.AddColumn<string>(
                name: "ProductPanelColor",
                table: "AppearanceSettings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#FFFFFF");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ProductPanelColor", table: "AppearanceSettings");
        }
    }
}
