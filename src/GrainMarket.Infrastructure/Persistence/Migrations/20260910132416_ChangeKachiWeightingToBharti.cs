using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeKachiWeightingToBharti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop + add rather than rename: Man/Kilo/Gram and Bharti/TotalWeight/Dhrn are different
            // measurement concepts (maund-based vs. bharti-based weighing), so a renamed column
            // would silently relabel old Man/Kilo/Gram figures as if they were Bharti/TotalWeight/
            // Dhrn kg values — quietly wrong, not just stale. Existing rows lose their old weight
            // entry under the new model instead (BoriQty and NetWeightKg, unaffected columns, are
            // untouched).
            migrationBuilder.DropColumn(name: "ManQty", table: "Kachis");
            migrationBuilder.DropColumn(name: "KiloQty", table: "Kachis");
            migrationBuilder.DropColumn(name: "GramQty", table: "Kachis");

            migrationBuilder.AddColumn<decimal>(
                name: "BhartiKgPerBag",
                table: "Kachis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWeightKg",
                table: "Kachis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DhrnKg",
                table: "Kachis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BhartiKgPerBag", table: "Kachis");
            migrationBuilder.DropColumn(name: "TotalWeightKg", table: "Kachis");
            migrationBuilder.DropColumn(name: "DhrnKg", table: "Kachis");

            migrationBuilder.AddColumn<decimal>(
                name: "ManQty",
                table: "Kachis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KiloQty",
                table: "Kachis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GramQty",
                table: "Kachis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }
    }
}
