using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdditionalGrainProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SeedData.cs only runs against an empty database, so an already-seeded database (which
            // only had Wheat/Rice/Maize) needs the rest of the ten-product default catalog inserted
            // directly. Each row is guarded so re-running this — or a fresh install that already
            // seeded all ten via SeedData.cs — never duplicates a product.
            migrationBuilder.Sql(@"
                INSERT INTO ""Products"" (""Name"", ""NameUrdu"", ""Category"", ""BaseUnit"", ""DefaultRate"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.""Name"", v.""NameUrdu"", 'Grain', 'kg', 0, TRUE, NOW(), FALSE
                FROM (VALUES
                    ('Gram', N'چنا'),
                    ('Barley', N'جو'),
                    ('Millet', N'باجرہ'),
                    ('Sorghum', N'جوار'),
                    ('Mustard', N'سرسوں'),
                    ('Moong', N'مونگ'),
                    ('Masoor', N'مسور')
                ) AS v(""Name"", ""NameUrdu"")
                WHERE NOT EXISTS (SELECT 1 FROM ""Products"" p WHERE p.""Name"" = v.""Name"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Soft-delete only, matching ProductService.DeleteAsync's own pattern — other tables
            // (Kachi, Pakki, DeductionRule, UnitConversion, …) reference Products with FK Restrict,
            // so a hard delete could fail once any of these products has been used.
            migrationBuilder.Sql(@"
                UPDATE ""Products""
                SET ""IsDeleted"" = TRUE, ""IsActive"" = FALSE, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" IN ('Gram', 'Barley', 'Millet', 'Sorghum', 'Mustard', 'Moong', 'Masoor')
                  AND ""IsDeleted"" = FALSE;
            ");
        }
    }
}
