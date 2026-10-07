using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecomputeLedgerRunningBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // LedgerPostingService.PostPartyEntryAsync/PostAccountEntryAsync computed each new
            // entry's RunningBalance by querying the database for the latest existing entry —
            // which doesn't see an entry this same request already added but hadn't SaveChanges'd
            // yet. Any flow that posts more than one entry for the same party/account per request
            // (Purchase and Sale Invoice both do: the transaction itself, then the cash applied
            // against it) got a RunningBalance chain that silently reset to the pre-request balance
            // partway through, corrupting every RunningBalance after that point. The service itself
            // is now fixed (an in-memory per-request cache), but that only prevents new corruption —
            // this recomputes every existing row's RunningBalance from scratch, per party and
            // separately per chart-of-account, walking Debit/Credit in (Date, Id) order.
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
            // No-op: this only corrects a derived (recomputable) column's existing values, it
            // doesn't add or remove any schema or row — there's nothing meaningful to revert to
            // (the previous values were wrong).
        }
    }
}
