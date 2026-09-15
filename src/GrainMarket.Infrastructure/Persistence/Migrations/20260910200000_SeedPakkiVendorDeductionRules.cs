using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedPakkiVendorDeductionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reconciles an already-seeded database (SeedData.cs only runs against an empty
            // database) with six new Pakki-only, vendor/buyer-charged deduction rules — bharai,
            // silvai, dhaga lagai, dumra karai, sotli, bardana — added inactive at 0% (same
            // placeholder pattern as Withholding Tax / Association Fund / Arhat) until the
            // market's real rate is entered under Setup > Format.

            migrationBuilder.Sql(@"
                INSERT INTO ""ChartOfAccounts"" (""Code"", ""Name"", ""NameUrdu"", ""AccountType"", ""IsProtected"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.code, v.name, v.name_urdu, 3, FALSE, TRUE, NOW(), FALSE
                FROM (VALUES
                    ('4610', 'Bharai Income', N'آمدنی بھرائی'),
                    ('4620', 'Silvai Income', N'آمدنی سلائی'),
                    ('4630', 'Dhaga Lagai Income', N'آمدنی دھاگہ لگائی'),
                    ('4640', 'Dumra Karai Income', N'آمدنی ڈمرہ کرائی'),
                    ('4650', 'Sotli Income', N'آمدنی سوتلی'),
                    ('4660', 'Bardana Income', N'آمدنی بردانہ')
                ) AS v(code, name, name_urdu)
                WHERE NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" WHERE ""Code"" = v.code);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""DeductionRules""
                    (""Name"", ""NameUrdu"", ""CalculationType"", ""Value"", ""AppliesTo"", ""ChargedTo"", ""SortOrder"", ""IsActive"", ""RequiresVehicleNumber"", ""IncomeAccountId"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.name, v.name_urdu, 2, 0, 2, 2, v.sort_order, FALSE, FALSE,
                       (SELECT ""Id"" FROM ""ChartOfAccounts"" WHERE ""Code"" = v.account_code), NOW(), FALSE
                FROM (VALUES
                    ('Bharai', N'بھرائی', 7, '4610'),
                    ('Silvai', N'سلائی', 8, '4620'),
                    ('Dhaga Lagai', N'دھاگہ لگائی', 9, '4630'),
                    ('Dumra Karai', N'ڈمرہ کرائی', 10, '4640'),
                    ('Sotli', N'سوتلی', 11, '4650'),
                    ('Bardana', N'بردانہ', 12, '4660')
                ) AS v(name, name_urdu, sort_order, account_code)
                WHERE NOT EXISTS (SELECT 1 FROM ""DeductionRules"" WHERE ""Name"" = v.name AND ""AppliesTo"" = 2 AND ""IsDeleted"" = FALSE);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""DeductionRules""
                WHERE ""Name"" IN ('Bharai', 'Silvai', 'Dhaga Lagai', 'Dumra Karai', 'Sotli', 'Bardana')
                  AND ""AppliesTo"" = 2
                  AND NOT EXISTS (SELECT 1 FROM ""PakkiDeductionLines"" l WHERE l.""DeductionRuleId"" = ""DeductionRules"".""Id"");
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""ChartOfAccounts""
                WHERE ""Code"" IN ('4610', '4620', '4630', '4640', '4650', '4660')
                  AND NOT EXISTS (SELECT 1 FROM ""DeductionRules"" r WHERE r.""IncomeAccountId"" = ""ChartOfAccounts"".""Id"");
            ");
        }
    }
}
