using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DhrnAsDisplayBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DhrnKg",
                table: "Kachis");

            // Dhrn (WeightUnit = 5) is now a display-only sub-Man denomination (default 5kg,
            // configurable under Setup > Unit Conversions like every other unit) used to break a
            // Kachi's net weight into Man/Dhrn/Kg. SeedData.cs only runs against an empty database,
            // so existing installs need this row inserted directly.
            migrationBuilder.Sql(@"
                INSERT INTO ""UnitConversions"" (""Unit"", ""FactorToKg"", ""ProductId"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT 5, 5.0, NULL, TRUE, NOW(), FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""UnitConversions"" WHERE ""Unit"" = 5 AND ""ProductId"" IS NULL);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""UnitConversions"" WHERE ""Unit"" = 5 AND ""ProductId"" IS NULL;");

            migrationBuilder.AddColumn<decimal>(
                name: "DhrnKg",
                table: "Kachis",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }
    }
}
