using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangePakkiWeightingToBhartiAndAddBillNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop + add rather than rename: Man/Kilo/Gram and Bharti/TotalWeight are different
            // measurement concepts (maund-based vs. bharti-based weighing), so a renamed column
            // would silently relabel old Man/Kilo/Gram figures as if they were Bharti/TotalWeight kg
            // values — quietly wrong, not just stale. Existing rows lose their old weight entry under
            // the new model instead (BoriQty and NetWeightKg, unaffected columns, are untouched).
            // Mirrors ChangeKachiWeightingToBharti exactly, applied to Pakkis instead of Kachis.
            migrationBuilder.DropColumn(name: "ManQty", table: "Pakkis");
            migrationBuilder.DropColumn(name: "KiloQty", table: "Pakkis");
            migrationBuilder.DropColumn(name: "GramQty", table: "Pakkis");

            migrationBuilder.AddColumn<decimal>(
                name: "BhartiKgPerBag",
                table: "Pakkis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWeightKg",
                table: "Pakkis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillNumber",
                table: "Pakkis",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BhartiKgPerBag", table: "Pakkis");
            migrationBuilder.DropColumn(name: "TotalWeightKg", table: "Pakkis");
            migrationBuilder.DropColumn(name: "BillNumber", table: "Pakkis");

            migrationBuilder.AddColumn<decimal>(
                name: "ManQty",
                table: "Pakkis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KiloQty",
                table: "Pakkis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GramQty",
                table: "Pakkis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }
    }
}
