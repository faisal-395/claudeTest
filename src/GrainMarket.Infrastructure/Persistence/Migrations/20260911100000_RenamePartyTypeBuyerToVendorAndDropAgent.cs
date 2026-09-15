using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenamePartyTypeBuyerToVendorAndDropAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PartyType is now a [Flags] enum: Farmer = 1, Vendor = 2, Other = 4. Vendor keeps the
            // old Buyer's value (2), so already-stored Buyer rows read correctly as Vendor with no
            // data change. The old Agent value (3) is dropped entirely — under the new flags scheme
            // 3 means "Farmer|Vendor combined", not "Other", so existing Agent rows must be moved to
            // Other (4) or they'd be silently misclassified as a combined Farmer+Vendor party.
            migrationBuilder.Sql(@"
                UPDATE ""Parties""
                SET ""PartyType"" = 4, ""UpdatedAtUtc"" = NOW()
                WHERE ""PartyType"" = 3;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort only: rows that were already Other (4) before this migration ran are
            // indistinguishable from former Agent rows, so this cannot be reversed precisely.
        }
    }
}
