using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedExtraAccountTypesAndExpenseAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every entry from the legacy software's own "Account Type" list (screenshot) becomes
            // its own AccountTypeDefinition row, exactly as named there (spelling corrected) —
            // alongside the original Asset/Liability/Income/Expense/Equity five, not folded into
            // them. "Bank"/"Cash" and "Owner's Equity" from that same list are deliberately NOT
            // repeated here: this app already has "Bank"/"Cash" as actual ChartOfAccount group
            // accounts (see AddCashAccountGroup) and "Owner's Equity" as both the existing "Equity"
            // type and a seeded leaf account under it — adding same-named types on top would just be
            // a confusing duplicate with no purpose. "Party" and "Stock" don't correspond to
            // anything postable in this schema (Party balances live in the Parties module, Stock in
            // Products/Inventory) — added anyway as inert placeholder types, per explicit
            // instruction, since neither can become a real postable account here.
            migrationBuilder.Sql(@"
                INSERT INTO ""AccountTypeDefinitions"" (""Name"", ""NameUrdu"", ""IsSystemType"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.""Name"", v.""NameUrdu"", FALSE, NOW(), FALSE
                FROM (VALUES
                    ('Accounts Receivable', N'قابل وصول رقوم'),
                    ('Accounts Payable', N'قابل ادا رقوم'),
                    ('Accumulated Depreciation', N'جمع شدہ فرسودگی'),
                    ('Discount Allowed', N'اجازت شدہ رعایت'),
                    ('Electricity Expenses', N'بجلی کے اخراجات'),
                    ('Employee Related Expenses', N'ملازمین کے متعلقہ اخراجات'),
                    ('Fixed Assets', N'فکسڈ اثاثے'),
                    ('Maintenance Expenses', N'مرمت کے اخراجات'),
                    ('Miscellaneous Expenses', N'متفرق اخراجات'),
                    ('Notes Payable', N'قابل ادا نوٹس'),
                    ('Notes Receivable', N'قابل وصول نوٹس'),
                    ('Party', N'پارٹی'),
                    ('Payable to GOVT (Tax)', N'حکومت کو قابل ادا (ٹیکس)'),
                    ('Phone Expenses', N'فون کے اخراجات'),
                    ('Profit', N'منافع'),
                    ('Rental Expenses', N'کرایہ کے اخراجات'),
                    ('Revenue Earned', N'حاصل شدہ آمدنی'),
                    ('Sale Fee', N'فروخت فیس'),
                    ('Stock', N'اسٹاک'),
                    ('Traveling Expenses', N'سفر کے اخراجات')
                ) AS v(""Name"", ""NameUrdu"")
                WHERE NOT EXISTS (SELECT 1 FROM ""AccountTypeDefinitions"" d WHERE d.""Name"" = v.""Name"");
            ");

            // Only the 8 expense-style entries actually get a matching ChartOfAccount leaf row —
            // these are what the Expense Voucher's "To" picker needs to have anything to post
            // against (see Expense.razor's ExpenseAccounts, matched by AccountType.Name since these
            // are no longer lumped under the single generic "Expense" type). The other twelve
            // (Accounts Receivable, Accounts Payable, Accumulated Depreciation, Fixed Assets, Notes
            // Payable, Notes Receivable, Payable to GOVT (Tax), Profit, Revenue Earned, Sale Fee,
            // Party, Stock) stay types only, with no account created — nothing in this app posts to
            // them automatically, so a pre-made account for each would just be unused clutter; Setup
            // > Chart of Accounts can always add one by hand later if a client actually needs it.
            migrationBuilder.Sql(@"
                INSERT INTO ""ChartOfAccounts"" (""Code"", ""Name"", ""NameUrdu"", ""AccountTypeId"", ""ParentAccountId"", ""IsProtected"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT v.""Code"", v.""Name"", v.""NameUrdu"", d.""Id"", NULL, FALSE, TRUE, NOW(), FALSE
                FROM (VALUES
                    ('5200', 'Discount Allowed', N'اجازت شدہ رعایت'),
                    ('5300', 'Electricity Expenses', N'بجلی کے اخراجات'),
                    ('5400', 'Employee Related Expenses', N'ملازمین کے متعلقہ اخراجات'),
                    ('5500', 'Maintenance Expenses', N'مرمت کے اخراجات'),
                    ('5600', 'Miscellaneous Expenses', N'متفرق اخراجات'),
                    ('5700', 'Phone Expenses', N'فون کے اخراجات'),
                    ('5800', 'Rental Expenses', N'کرایہ کے اخراجات'),
                    ('5900', 'Traveling Expenses', N'سفر کے اخراجات')
                ) AS v(""Code"", ""Name"", ""NameUrdu"")
                JOIN ""AccountTypeDefinitions"" d ON d.""Name"" = v.""Name""
                WHERE NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" c WHERE c.""Code"" = v.""Code"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""ChartOfAccounts""
                WHERE ""Code"" IN ('5200','5300','5400','5500','5600','5700','5800','5900')
                  AND NOT EXISTS (SELECT 1 FROM ""LedgerEntries"" l WHERE l.""ChartOfAccountId"" = ""ChartOfAccounts"".""Id"");
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""AccountTypeDefinitions""
                WHERE ""Name"" IN (
                    'Accounts Receivable','Accounts Payable','Accumulated Depreciation','Discount Allowed',
                    'Electricity Expenses','Employee Related Expenses','Fixed Assets','Maintenance Expenses',
                    'Miscellaneous Expenses','Notes Payable','Notes Receivable','Party','Payable to GOVT (Tax)',
                    'Phone Expenses','Profit','Rental Expenses','Revenue Earned','Sale Fee','Stock','Traveling Expenses')
                  AND NOT EXISTS (SELECT 1 FROM ""ChartOfAccounts"" c WHERE c.""AccountTypeId"" = ""AccountTypeDefinitions"".""Id"");
            ");
        }
    }
}
