using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPakkiBuyerChargesTotalAndLineChargedTo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mirrors AddDeductionChargedToAndBuyerCharges (the same fix already applied to Kachis)
            // — Pakki.TotalDeductions was wrongly netting every deduction line (both seller- and
            // buyer-charged) out of NetPayableToFarmer, because PakkiDeductionLine never recorded
            // which side a line was charged to in the first place. These two columns let
            // PakkiService split them the same way KachiService already does: TotalDeductions
            // becomes seller-charged only, BuyerChargesTotal holds the rest, added to what the
            // buyer owes instead. Defaults backfill existing rows — their already-posted ledger
            // entries are left untouched, only new Pakkis (created or edited after this migration)
            // get the corrected split.
            migrationBuilder.AddColumn<decimal>(
                name: "BuyerChargesTotal",
                table: "Pakkis",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Default 1 (Seller), not 0 (not a valid DeductionChargedTo member) — preserves today's
            // behavior (every existing line reads as seller-charged) for rows that predate this
            // column, matching AddDeductionChargedToAndBuyerCharges's identical default for
            // KachiDeductionLines.
            migrationBuilder.AddColumn<int>(
                name: "ChargedTo",
                table: "PakkiDeductionLines",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyerChargesTotal",
                table: "Pakkis");

            migrationBuilder.DropColumn(
                name: "ChargedTo",
                table: "PakkiDeductionLines");
        }
    }
}
