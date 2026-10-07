using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMarketFeeDeductionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reconciles an already-seeded database (SeedData.cs only runs against an empty
            // database) with two new Market Fee deduction rules — Rs 2 per 100kg (0.02/kg),
            // charged to the Buyer, kept separate per stage (Kachi/Pakki) since the rate can differ
            // between them, matching SeedData.cs's SeedDeductionRulesAsync.

            migrationBuilder.Sql(@"
                INSERT INTO ""ChartOfAccounts"" (""Code"", ""Name"", ""NameUrdu"", ""AccountType"", ""IsProtected"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT '4200', 'Market Fee Income', N'آمدنی مارکیٹ فیس', 3, FALSE, TRUE, NOW(), FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" WHERE ""Code"" = '4200');
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""DeductionRules""
                    (""Name"", ""NameUrdu"", ""CalculationType"", ""Value"", ""AppliesTo"", ""ChargedTo"", ""SortOrder"", ""IsActive"", ""RequiresVehicleNumber"", ""IncomeAccountId"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.name, v.name_urdu, 3, 0.02, v.applies_to, 2, v.sort_order, TRUE, FALSE,
                       (SELECT ""Id"" FROM ""ChartOfAccounts"" WHERE ""Code"" = '4200'), NOW(), FALSE
                FROM (VALUES
                    ('Market Fee (Kachi)', N'مارکیٹ فیس (کچی)', 1, 13),
                    ('Market Fee (Pakki)', N'مارکیٹ فیس (پکی)', 2, 14)
                ) AS v(name, name_urdu, applies_to, sort_order)
                WHERE NOT EXISTS (SELECT 1 FROM ""DeductionRules"" WHERE ""Name"" = v.name AND ""AppliesTo"" = v.applies_to AND ""IsDeleted"" = FALSE);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""DeductionRules""
                WHERE ""Name"" IN ('Market Fee (Kachi)', 'Market Fee (Pakki)')
                  AND NOT EXISTS (SELECT 1 FROM ""KachiDeductionLines"" l WHERE l.""DeductionRuleId"" = ""DeductionRules"".""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""PakkiDeductionLines"" l WHERE l.""DeductionRuleId"" = ""DeductionRules"".""Id"");
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""ChartOfAccounts""
                WHERE ""Code"" = '4200'
                  AND NOT EXISTS (SELECT 1 FROM ""DeductionRules"" r WHERE r.""IncomeAccountId"" = ""ChartOfAccounts"".""Id"");
            ");
        }
    }
}
