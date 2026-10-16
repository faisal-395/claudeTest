using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Lets Setup > Users & Roles mark a role's entries as needing Manager/Admin review before
            // they post to the ledger (see ModuleName.Approvals below) — configurable per role rather
            // than hardcoded to any one role. defaultValue false keeps every existing role posting
            // straight through, unchanged, until an admin turns it on.
            migrationBuilder.AddColumn<bool>(
                name: "RequiresApproval",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            AddApprovalColumns(migrationBuilder, "Kachis");
            AddApprovalColumns(migrationBuilder, "Pakkis");
            AddApprovalColumns(migrationBuilder, "Purchases");
            AddApprovalColumns(migrationBuilder, "SaleInvoices");

            // A brand-new ModuleName (Approvals = 25). SeedRolesAndPermissionsAsync grants it
            // automatically on a fresh database (it loops every ModuleName); an already-seeded
            // database needs its existing roles backfilled directly here — same approach as
            // AddAppearanceSettings's SetupAppearance backfill. Mirrors exactly what a fresh seed run
            // would produce for the four built-in roles: Owner/Admin full; Manager can view and
            // approve/reject (CanCreate has no meaning here but is included for consistency with
            // Manager's blanket "everything except Users/Roles and Appearance" rule); Accountant can
            // view only (its CanView is unconditional in SeedData); Clerk sees none of it.
            migrationBuilder.Sql(@"
                INSERT INTO ""RolePermissions"" (""RoleId"", ""Module"", ""CanView"", ""CanCreate"", ""CanEdit"", ""CanDelete"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT r.""Id"", 25,
                    (r.""Name"" IN ('Owner/Admin', 'Manager', 'Accountant')),
                    (r.""Name"" IN ('Owner/Admin', 'Manager')),
                    (r.""Name"" IN ('Owner/Admin', 'Manager')),
                    (r.""Name"" = 'Owner/Admin'),
                    NOW(), FALSE
                FROM ""Roles"" r
                WHERE NOT EXISTS (SELECT 1 FROM ""RolePermissions"" p WHERE p.""RoleId"" = r.""Id"" AND p.""Module"" = 25);
            ");
        }

        private static void AddApprovalColumns(MigrationBuilder migrationBuilder, string table)
        {
            // Approved (0) by default — matches ApprovalStatus.Approved and backfills every existing
            // row as already-posted, since historical data was live in the ledger long before this
            // feature existed.
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: table,
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: table,
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: table,
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByUserId",
                table: table,
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByUserId",
                table: table,
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""Module"" = 25;");

            migrationBuilder.DropColumn(name: "RequiresApproval", table: "Roles");

            DropApprovalColumns(migrationBuilder, "Kachis");
            DropApprovalColumns(migrationBuilder, "Pakkis");
            DropApprovalColumns(migrationBuilder, "Purchases");
            DropApprovalColumns(migrationBuilder, "SaleInvoices");
        }

        private static void DropApprovalColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.DropColumn(name: "ApprovalStatus", table: table);
            migrationBuilder.DropColumn(name: "RejectionReason", table: table);
            migrationBuilder.DropColumn(name: "ReviewedAtUtc", table: table);
            migrationBuilder.DropColumn(name: "ReviewedByUserId", table: table);
            migrationBuilder.DropColumn(name: "SubmittedByUserId", table: table);
        }
    }
}
