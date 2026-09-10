using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileKachiOnlyDeductionSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reconciles an already-seeded database (SeedData.cs only runs against an empty
            // database) with the new default deduction setup: only three Kachi-stage rules —
            // Labour (Palledari) 0.75% (Farmer), Brokerage 0.15% (Farmer), Commission 1.60%
            // (Buyer). Existing rows are soft-deleted (never hard-deleted — historical
            // KachiDeductionLine rows reference them and the FK is Restrict), matching
            // DeductionRuleService.DeleteAsync's own soft-delete pattern.

            migrationBuilder.Sql(@"
                INSERT INTO ""ChartOfAccounts"" (""Code"", ""Name"", ""NameUrdu"", ""AccountType"", ""IsProtected"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT '4110', 'Brokerage Income', N'آمدنی بروکری', 3, FALSE, TRUE, NOW(), FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" WHERE ""Code"" = '4110');
            ");

            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""CalculationType"" = 2, ""Value"" = 0.75, ""AppliesTo"" = 1, ""ChargedTo"" = 1, ""SortOrder"" = 1, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" = 'Labour (Palledari)' AND ""IsDeleted"" = FALSE;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""NameUrdu"" = N'کمیشن', ""Value"" = 1.60, ""AppliesTo"" = 1, ""ChargedTo"" = 2, ""SortOrder"" = 3, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" = 'Commission' AND ""AppliesTo"" = 2 AND ""IsDeleted"" = FALSE;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""DeductionRules""
                    (""Name"", ""NameUrdu"", ""CalculationType"", ""Value"", ""AppliesTo"", ""ChargedTo"", ""SortOrder"", ""IsActive"", ""RequiresVehicleNumber"", ""IncomeAccountId"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT 'Brokerage', N'بروکری', 2, 0.15, 1, 1, 2, TRUE, FALSE, (SELECT ""Id"" FROM ""ChartOfAccounts"" WHERE ""Code"" = '4110'), NOW(), FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""DeductionRules"" WHERE ""Name"" = 'Brokerage' AND ""AppliesTo"" = 1 AND ""IsDeleted"" = FALSE);
            ");

            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""IsDeleted"" = TRUE, ""IsActive"" = FALSE, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" IN ('Market Fee', 'Association Fund', 'Octroi', 'Withholding Tax', 'Bagging/Stitching', 'Freight',
                                   'Commission (Kachi)', 'Market Fee (Kachi)', 'Association Fund (Kachi)', 'Withholding Tax (Kachi)')
                  AND ""IsDeleted"" = FALSE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort reversal — restores the soft-deleted rows and the two edited rows' prior
            // values; only removes the Brokerage rule/account if nothing has referenced them since.
            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""IsDeleted"" = FALSE, ""IsActive"" = TRUE, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" IN ('Market Fee', 'Association Fund', 'Octroi', 'Withholding Tax', 'Bagging/Stitching', 'Freight',
                                   'Commission (Kachi)', 'Market Fee (Kachi)', 'Association Fund (Kachi)', 'Withholding Tax (Kachi)');
            ");

            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""NameUrdu"" = N'بروکری / کمیشن', ""Value"" = 2, ""AppliesTo"" = 2, ""ChargedTo"" = 1, ""SortOrder"" = 1, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" = 'Commission' AND ""AppliesTo"" = 1 AND ""IsDeleted"" = FALSE;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""CalculationType"" = 3, ""Value"" = 0.3, ""AppliesTo"" = 3, ""SortOrder"" = 6, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" = 'Labour (Palledari)' AND ""IsDeleted"" = FALSE;
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""DeductionRules""
                WHERE ""Name"" = 'Brokerage' AND ""AppliesTo"" = 1
                  AND NOT EXISTS (SELECT 1 FROM ""KachiDeductionLines"" l WHERE l.""DeductionRuleId"" = ""DeductionRules"".""Id"");
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""ChartOfAccounts""
                WHERE ""Code"" = '4110'
                  AND NOT EXISTS (SELECT 1 FROM ""DeductionRules"" r WHERE r.""IncomeAccountId"" = ""ChartOfAccounts"".""Id"");
            ");
        }
    }
}
