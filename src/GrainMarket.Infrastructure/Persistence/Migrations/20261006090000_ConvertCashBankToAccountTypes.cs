using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertCashBankToAccountTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Per feedback on the legacy software's own Account Type list: "Cash" and "Bank" belong
            // there as Account TYPES (matching that list), not as ChartOfAccount rows the way
            // AddCashAccountGroup modeled them (a "Cash" parent account with "Cash in Hand" as its
            // child, and "Bank" doing double duty as both the default bank account AND a parent for
            // any others). This migration supersedes that: "Cash"/"Bank" become AccountTypeDefinition
            // rows, and every cash/bank ChartOfAccount is typed under them and un-parented instead —
            // a specific account like "Cash in Hand" or a particular bank account is now classified
            // by its Account Type (Cash or Bank), exactly like every other account, instead of living
            // under a stand-in parent account that only existed to represent the category.
            //
            // Also directly creates the "Cash in Hand" (code 1000) / "Bank Account" (code 1010) rows
            // here rather than leaving that purely to SeedData.SeedChartOfAccountsAsync: migrations
            // always run before SeedAsync, even on a brand-new database (SeedAsync already knows to
            // skip any code a migration got to first — see its own comment), so creating them here
            // with the right Account Type from the start is what makes a fresh install correct too,
            // not just an install that already had the old "Cash" group.
            migrationBuilder.Sql(@"
                INSERT INTO ""AccountTypeDefinitions"" (""Name"", ""NameUrdu"", ""IsSystemType"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.""Name"", v.""NameUrdu"", FALSE, NOW(), FALSE
                FROM (VALUES ('Cash', N'نقد'), ('Bank', N'بینک')) AS v(""Name"", ""NameUrdu"")
                WHERE NOT EXISTS (SELECT 1 FROM ""AccountTypeDefinitions"" d WHERE d.""Name"" = v.""Name"");
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""ChartOfAccounts"" (""Code"", ""Name"", ""NameUrdu"", ""AccountTypeId"", ""ParentAccountId"", ""IsProtected"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT '1000', 'Cash in Hand', N'نقد', (SELECT ""Id"" FROM ""AccountTypeDefinitions"" WHERE ""Name"" = 'Cash'), NULL, FALSE, TRUE, NOW(), FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" WHERE ""Code"" = '1000');
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""ChartOfAccounts"" (""Code"", ""Name"", ""NameUrdu"", ""AccountTypeId"", ""ParentAccountId"", ""IsProtected"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT '1010', 'Bank Account', N'بینک', (SELECT ""Id"" FROM ""AccountTypeDefinitions"" WHERE ""Name"" = 'Bank'), NULL, FALSE, TRUE, NOW(), FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" WHERE ""Code"" = '1010');
            ");

            // Covers an install that already had these rows (from an earlier migration run): retype
            // and un-parent "Cash in Hand" itself, plus anything a client already added as a sibling
            // under the old "Cash" group (code 1090) — e.g. "Dasti Cash".
            migrationBuilder.Sql(@"
                UPDATE ""ChartOfAccounts""
                SET ""AccountTypeId"" = (SELECT ""Id"" FROM ""AccountTypeDefinitions"" WHERE ""Name"" = 'Cash'), ""ParentAccountId"" = NULL
                WHERE ""Code"" = '1000'
                   OR ""ParentAccountId"" = (SELECT ""Id"" FROM ""ChartOfAccounts"" WHERE ""Code"" = '1090');
            ");

            // The old "Cash" parent/group account (code 1090) has no further purpose — delete it.
            // By now every former child has been re-parented away above, so this never leaves an
            // orphaned child behind.
            migrationBuilder.Sql(@"
                DELETE FROM ""ChartOfAccounts""
                WHERE ""Code"" = '1090'
                  AND NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" c2 WHERE c2.""ParentAccountId"" = ""ChartOfAccounts"".""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""LedgerEntries"" l WHERE l.""ChartOfAccountId"" = ""ChartOfAccounts"".""Id"");
            ");

            // Same for Bank (code 1010): rename away from "Bank" (now the Type's name) to "Bank
            // Account", retype, and un-parent anything already added under it as a sibling.
            migrationBuilder.Sql(@"
                UPDATE ""ChartOfAccounts""
                SET ""AccountTypeId"" = (SELECT ""Id"" FROM ""AccountTypeDefinitions"" WHERE ""Name"" = 'Bank'), ""ParentAccountId"" = NULL
                WHERE ""ParentAccountId"" = (SELECT ""Id"" FROM ""ChartOfAccounts"" WHERE ""Code"" = '1010');
            ");

            migrationBuilder.Sql(@"
                UPDATE ""ChartOfAccounts""
                SET ""Name"" = 'Bank Account', ""AccountTypeId"" = (SELECT ""Id"" FROM ""AccountTypeDefinitions"" WHERE ""Name"" = 'Bank')
                WHERE ""Code"" = '1010';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""ChartOfAccounts""
                SET ""Name"" = 'Bank', ""AccountTypeId"" = 1
                WHERE ""Code"" = '1010';
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""ChartOfAccounts"" (""Code"", ""Name"", ""NameUrdu"", ""AccountTypeId"", ""ParentAccountId"", ""IsProtected"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT '1090', 'Cash', N'نقد', 1, NULL, FALSE, TRUE, NOW(), FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" WHERE ""Code"" = '1090');
            ");

            migrationBuilder.Sql(@"
                UPDATE ""ChartOfAccounts""
                SET ""AccountTypeId"" = 1, ""ParentAccountId"" = (SELECT ""Id"" FROM ""ChartOfAccounts"" WHERE ""Code"" = '1090')
                WHERE ""Code"" = '1000';
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""AccountTypeDefinitions""
                WHERE ""Name"" IN ('Cash', 'Bank')
                  AND NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" c WHERE c.""AccountTypeId"" = ""AccountTypeDefinitions"".""Id"");
            ");
        }
    }
}
