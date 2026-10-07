using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditDebitPanelColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The Journal Voucher page's Credit/Debit side panels get their own backgrounds,
            // independent of PanelColor (same pattern as ProductPanelColor for Purchase/Sale
            // Invoice's "Add Product" panel) — defaults match the legacy software's green/pink
            // convention and backfill every existing row so nothing changes visually until a
            // client picks different ones from Setup > Appearance.
            migrationBuilder.AddColumn<string>(
                name: "CreditPanelColor",
                table: "AppearanceSettings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#E8F5E9");

            migrationBuilder.AddColumn<string>(
                name: "DebitPanelColor",
                table: "AppearanceSettings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#FCE4EC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreditPanelColor", table: "AppearanceSettings");
            migrationBuilder.DropColumn(name: "DebitPanelColor", table: "AppearanceSettings");
        }
    }
}
