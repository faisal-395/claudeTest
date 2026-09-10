using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandDeductionRulesToBothStagesAndSeedSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SeedData.cs only runs against an empty database, so an already-seeded database needs
            // reconciling directly: the three configured rules (Labour/Brokerage/Commission) now
            // apply to both Kachi and Pakki instead of Kachi only, and three more rules (Withholding
            // Tax, Association Fund, Arhat) are added — seeded inactive at 0 until a real rate is
            // entered under Setup > Format, matching SeedDeductionRulesAsync.

            migrationBuilder.Sql(@"
                INSERT INTO ""ChartOfAccounts"" (""Code"", ""Name"", ""NameUrdu"", ""AccountType"", ""IsProtected"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.""Code"", v.""Name"", v.""NameUrdu"", 3, FALSE, TRUE, NOW(), FALSE
                FROM (VALUES
                    ('4120', 'Arhat Income', N'آمدنی آڑت'),
                    ('4300', 'Association Fund Income', N'آمدنی انجمن فنڈ')
                ) AS v(""Code"", ""Name"", ""NameUrdu"")
                WHERE NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" c WHERE c.""Code"" = v.""Code"");
            ");

            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""AppliesTo"" = 3, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" IN ('Labour (Palledari)', 'Brokerage', 'Commission') AND ""AppliesTo"" = 1 AND ""IsDeleted"" = FALSE;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""DeductionRules""
                    (""Name"", ""NameUrdu"", ""CalculationType"", ""Value"", ""AppliesTo"", ""ChargedTo"", ""SortOrder"", ""IsActive"", ""RequiresVehicleNumber"", ""IncomeAccountId"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.""Name"", v.""NameUrdu"", v.""CalculationType"", 0, 3, 1, v.""SortOrder"", FALSE, FALSE,
                       (SELECT ""Id"" FROM ""ChartOfAccounts"" WHERE ""Code"" = v.""AccountCode""), NOW(), FALSE
                FROM (VALUES
                    ('Withholding Tax', N'ویدہولڈنگ ٹیکس', 2, 4, '4500'),
                    ('Association Fund', N'انجمن فنڈ', 1, 5, '4300'),
                    ('Arhat', N'آڑت', 2, 6, '4120')
                ) AS v(""Name"", ""NameUrdu"", ""CalculationType"", ""SortOrder"", ""AccountCode"")
                WHERE NOT EXISTS (SELECT 1 FROM ""DeductionRules"" r WHERE r.""Name"" = v.""Name"" AND r.""AppliesTo"" = 3 AND r.""IsDeleted"" = FALSE);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""Seasons"" (""Name"", ""StartDate"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT '2026-27', TIMESTAMPTZ '2026-07-01', TRUE, NOW(), FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""Seasons"" WHERE ""Name"" = '2026-27');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort reversal — restores the three rules to Kachi-only and soft-deletes the
            // three newly-added ones (never hard-deleted: historical KachiDeductionLine/
            // PakkiDeductionLine rows may already reference them, and the FK is Restrict).
            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""IsDeleted"" = TRUE, ""IsActive"" = FALSE, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" IN ('Withholding Tax', 'Association Fund', 'Arhat') AND ""AppliesTo"" = 3;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""AppliesTo"" = 1, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" IN ('Labour (Palledari)', 'Brokerage', 'Commission') AND ""AppliesTo"" = 3 AND ""IsDeleted"" = FALSE;
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""Seasons""
                WHERE ""Name"" = '2026-27'
                  AND NOT EXISTS (SELECT 1 FROM ""Kachis"" k WHERE k.""SeasonId"" = ""Seasons"".""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""Pakkis"" p WHERE p.""SeasonId"" = ""Seasons"".""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""Vouchers"" v WHERE v.""SeasonId"" = ""Seasons"".""Id"");
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""ChartOfAccounts""
                WHERE ""Code"" IN ('4120', '4300')
                  AND NOT EXISTS (SELECT 1 FROM ""DeductionRules"" r WHERE r.""IncomeAccountId"" = ""ChartOfAccounts"".""Id"");
            ");
        }
    }
}
