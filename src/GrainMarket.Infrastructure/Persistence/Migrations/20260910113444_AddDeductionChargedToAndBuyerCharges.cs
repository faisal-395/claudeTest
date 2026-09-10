using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeductionChargedToAndBuyerCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BuyerChargesTotal",
                table: "Kachis",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Default 1 (Farmer), not 0 (not a valid DeductionChargedTo member) — this both sets the
            // column default and backfills every existing row, preserving today's behavior (every
            // deduction currently reduces the farmer's payable) for data that predates this column.
            migrationBuilder.AddColumn<int>(
                name: "ChargedTo",
                table: "KachiDeductionLines",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ChargedTo",
                table: "DeductionRules",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyerChargesTotal",
                table: "Kachis");

            migrationBuilder.DropColumn(
                name: "ChargedTo",
                table: "KachiDeductionLines");

            migrationBuilder.DropColumn(
                name: "ChargedTo",
                table: "DeductionRules");
        }
    }
}
