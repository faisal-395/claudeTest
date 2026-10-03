using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitPakkiCoreDeductionsToBuyerCharged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reconciles an already-seeded database (SeedData.cs only runs against an empty
            // database) with the same split now baked into SeedDeductionRulesAsync: in Pakki, every
            // tax is paid by the Buyer, unlike Kachi where the farmer bears these five. Labour,
            // Brokerage, Withholding Tax, Association Fund and Arhat go back to Kachi-only
            // (AppliesTo = 1, unchanged ChargedTo = Seller) and each gets a Pakki-only twin
            // (AppliesTo = 2, ChargedTo = Buyer) cloning its rate/account. Commission is already
            // Buyer-charged for both stages and is left untouched — no twin needed.
            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""AppliesTo"" = 1, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" IN ('Labour (Palledari)', 'Brokerage', 'Withholding Tax', 'Association Fund', 'Arhat')
                  AND ""AppliesTo"" = 3 AND ""IsDeleted"" = FALSE;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""DeductionRules""
                    (""Name"", ""NameUrdu"", ""CalculationType"", ""Value"", ""AppliesTo"", ""ChargedTo"", ""SortOrder"", ""IsActive"", ""RequiresVehicleNumber"", ""IncomeAccountId"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.new_name, v.new_name_urdu, r.""CalculationType"", r.""Value"", 2, 2, v.sort_order, r.""IsActive"", r.""RequiresVehicleNumber"", r.""IncomeAccountId"", NOW(), FALSE
                FROM ""DeductionRules"" r
                JOIN (VALUES
                    ('Labour (Palledari)', 'Labour (Palledari) (Pakki)', N'پلیداری (پکی)', 15),
                    ('Brokerage', 'Brokerage (Pakki)', N'بروکری (پکی)', 16),
                    ('Withholding Tax', 'Withholding Tax (Pakki)', N'ویدہولڈنگ ٹیکس (پکی)', 17),
                    ('Association Fund', 'Association Fund (Pakki)', N'انجمن فنڈ (پکی)', 18),
                    ('Arhat', 'Arhat (Pakki)', N'آڑت (پکی)', 19)
                ) AS v(old_name, new_name, new_name_urdu, sort_order)
                    ON r.""Name"" = v.old_name AND r.""AppliesTo"" = 1 AND r.""IsDeleted"" = FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""DeductionRules"" x WHERE x.""Name"" = v.new_name AND x.""IsDeleted"" = FALSE);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort reversal — never hard-deletes a Pakki twin that a historical
            // PakkiDeductionLine already references (the FK is Restrict), soft-deleting it instead.
            migrationBuilder.Sql(@"
                DELETE FROM ""DeductionRules""
                WHERE ""Name"" IN ('Labour (Palledari) (Pakki)', 'Brokerage (Pakki)', 'Withholding Tax (Pakki)', 'Association Fund (Pakki)', 'Arhat (Pakki)')
                  AND ""AppliesTo"" = 2
                  AND NOT EXISTS (SELECT 1 FROM ""PakkiDeductionLines"" l WHERE l.""DeductionRuleId"" = ""DeductionRules"".""Id"");
            ");

            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""IsDeleted"" = TRUE, ""IsActive"" = FALSE, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" IN ('Labour (Palledari) (Pakki)', 'Brokerage (Pakki)', 'Withholding Tax (Pakki)', 'Association Fund (Pakki)', 'Arhat (Pakki)')
                  AND ""AppliesTo"" = 2 AND ""IsDeleted"" = FALSE;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""DeductionRules""
                SET ""AppliesTo"" = 3, ""UpdatedAtUtc"" = NOW()
                WHERE ""Name"" IN ('Labour (Palledari)', 'Brokerage', 'Withholding Tax', 'Association Fund', 'Arhat')
                  AND ""AppliesTo"" = 1 AND ""IsDeleted"" = FALSE;
            ");
        }
    }
}
