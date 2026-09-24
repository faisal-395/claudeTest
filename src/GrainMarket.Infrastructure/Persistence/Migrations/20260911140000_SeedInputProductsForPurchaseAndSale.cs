using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedInputProductsForPurchaseAndSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reconciles an already-seeded database (SeedData.cs only runs against an empty
            // database) with a starter farm-input catalog (Category = 'Input') for Purchase (bought
            // from a Supplier) and Sale Invoice (sold to a Farmer) — Purchase/SaleInvoice.razor now
            // only offer Input-category products, so without this they'd have an empty picker on an
            // upgraded database. No schema change: Product.Category is already a free-text column.
            migrationBuilder.Sql(@"
                INSERT INTO ""Products"" (""Name"", ""NameUrdu"", ""Category"", ""BaseUnit"", ""DefaultRate"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.name, v.name_urdu, 'Input', 'kg', 0, TRUE, NOW(), FALSE
                FROM (VALUES
                    ('Pesticide', N'کیڑے مار دوا'),
                    ('Seed', N'بیج'),
                    ('Fertilizer (Urea)', N'کھاد (یوریا)'),
                    ('Fertilizer (DAP)', N'کھاد (ڈی اے پی)')
                ) AS v(name, name_urdu)
                WHERE NOT EXISTS (SELECT 1 FROM ""Products"" p WHERE p.""Name"" = v.name AND p.""IsDeleted"" = FALSE);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Never hard-deletes a product a historical Purchase/SaleInvoice line already references
            // (the FK is Restrict) — soft-deletes it instead, same pattern as other seed rollbacks.
            migrationBuilder.Sql(@"
                DELETE FROM ""Products""
                WHERE ""Name"" IN ('Pesticide', 'Seed', 'Fertilizer (Urea)', 'Fertilizer (DAP)')
                  AND ""Category"" = 'Input'
                  AND NOT EXISTS (SELECT 1 FROM ""PurchaseLines"" l WHERE l.""ProductId"" = ""Products"".""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""SaleInvoiceLines"" l WHERE l.""ProductId"" = ""Products"".""Id"");
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Products""
                SET ""IsDeleted"" = TRUE, ""IsActive"" = FALSE, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" IN ('Pesticide', 'Seed', 'Fertilizer (Urea)', 'Fertilizer (DAP)')
                  AND ""Category"" = 'Input' AND ""IsDeleted"" = FALSE;
            ");
        }
    }
}
