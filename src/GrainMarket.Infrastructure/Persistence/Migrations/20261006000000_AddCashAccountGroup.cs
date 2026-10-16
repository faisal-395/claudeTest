using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashAccountGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The seeded account at DomainConstants.CashAccountCode ("1000") was being used two
            // ways at once: as THE cash account every other module (Expense, Purchase, Sale
            // Invoice, Dashboard) hardcodes by that code, and — since the Voucher form's "Cash"
            // picker added in an earlier change — as the PARENT a site's other cash accounts (e.g.
            // "Dasti Cash") get added under. But it's named "Cash in Hand", a specific account, not
            // a parent/category — a new "Dasti Cash" sibling shouldn't have to live under another
            // specific account just to share its category.
            //
            // This adds a proper parent account named "Cash" above it and re-parents "Cash in
            // Hand" under that, mirroring "Bank" (already a plain category name with no code
            // elsewhere depending on it being a specific account). Code "1000" keeps pointing at
            // "Cash in Hand" itself — unchanged id, unchanged balance/history — so every existing
            // posting and every other module's hardcoded lookup is unaffected. Only the Voucher
            // form's Cash picker needs to know about the new "Cash" parent (it finds it via "Cash
            // in Hand".ParentAccountId rather than a fixed code, since this new row has none of
            // its own).
            migrationBuilder.Sql(@"
                INSERT INTO ""ChartOfAccounts"" (""Code"", ""Name"", ""NameUrdu"", ""AccountTypeId"", ""ParentAccountId"", ""IsProtected"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT '1090', 'Cash', N'نقد', 1, NULL, FALSE, TRUE, NOW(), FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" WHERE ""Code"" = '1090');
            ");

            migrationBuilder.Sql(@"
                UPDATE ""ChartOfAccounts""
                SET ""ParentAccountId"" = (SELECT ""Id"" FROM ""ChartOfAccounts"" WHERE ""Code"" = '1090')
                WHERE ""Code"" = '1000' AND ""ParentAccountId"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""ChartOfAccounts""
                SET ""ParentAccountId"" = NULL
                WHERE ""Code"" = '1000'
                  AND ""ParentAccountId"" = (SELECT ""Id"" FROM ""ChartOfAccounts"" WHERE ""Code"" = '1090');
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""ChartOfAccounts"" AS coa
                WHERE coa.""Code"" = '1090'
                  AND NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" c2 WHERE c2.""ParentAccountId"" = coa.""Id"");
            ");
        }
    }
}
