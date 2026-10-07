using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixLedgerReversalDatesAndRecomputeBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // VoucherService/KachiService/PakkiService/PurchaseService/SaleInvoiceService all dated
            // their reversal postings (Cancel, Update's reverse-then-repost) with _clock.UtcNow — a
            // real timestamp carrying a time-of-day — while every normal posting is dated with the
            // transaction's own business Date, which is always midnight (it comes from a date-only
            // form field). LedgerQueryService's display order and LedgerPostingService's "find the
            // latest balance" lookup both sort primarily by Date, so on any same-day edit or cancel a
            // reversal's later time-of-day made it sort as "more recent" than entries actually posted
            // after it, corrupting both the displayed order and every RunningBalance computed from
            // that point on. The services are now fixed to date reversals with the transaction's own
            // business Date instead; this repairs the rows that already went out wrong. Every
            // reversal row carries "Reversal: ..." as its Description (see the `reason` string each
            // service builds), so they're identifiable without guessing from the Date value itself.
            migrationBuilder.Sql(@"
                UPDATE ""LedgerEntries""
                SET ""Date"" = date_trunc('day', ""Date"")
                WHERE ""Description"" LIKE 'Reversal:%' AND ""Date"" <> date_trunc('day', ""Date"");
            ");

            // Same recompute as RecomputeLedgerRunningBalances — now that every row's Date is
            // consistent (midnight-of-day), walking (Date, Id) order correctly reflects true posting
            // order again, so this restores every RunningBalance the corrupted dates had cascaded
            // wrong.
            migrationBuilder.Sql(@"
                WITH ordered_party AS (
                    SELECT ""Id"",
                           SUM(""Debit"" - ""Credit"") OVER (
                               PARTITION BY ""PartyId"" ORDER BY ""Date"", ""Id""
                               ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                           ) AS running
                    FROM ""LedgerEntries""
                    WHERE ""PartyId"" IS NOT NULL AND ""IsDeleted"" = FALSE
                )
                UPDATE ""LedgerEntries"" le
                SET ""RunningBalance"" = op.running
                FROM ordered_party op
                WHERE le.""Id"" = op.""Id"";
            ");

            migrationBuilder.Sql(@"
                WITH ordered_account AS (
                    SELECT ""Id"",
                           SUM(""Debit"" - ""Credit"") OVER (
                               PARTITION BY ""ChartOfAccountId"" ORDER BY ""Date"", ""Id""
                               ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                           ) AS running
                    FROM ""LedgerEntries""
                    WHERE ""ChartOfAccountId"" IS NOT NULL AND ""IsDeleted"" = FALSE
                )
                UPDATE ""LedgerEntries"" le
                SET ""RunningBalance"" = oa.running
                FROM ordered_account oa
                WHERE le.""Id"" = oa.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: this only corrects derived/mis-set values (RunningBalance, and a reversal row's
            // time-of-day), it doesn't add or remove any schema or row — there's nothing meaningful
            // to revert to (the previous values were wrong).
        }
    }
}
