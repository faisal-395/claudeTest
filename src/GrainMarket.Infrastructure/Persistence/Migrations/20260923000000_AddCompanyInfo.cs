using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NameEnglish = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NameUrdu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MarketName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    NtnNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProprietorName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyInfos", x => x.Id);
                });

            // One default row with the letterhead the operator already uses on paper — editable
            // afterwards from Setup > Company Info. Migrations always run, fresh install or not, so
            // this single INSERT covers both; SeedCompanyInfoAsync in SeedData.cs does the same
            // idempotent insert for symmetry with SeedSeasonAsync, and is a no-op once this has run.
            migrationBuilder.Sql(@"
                INSERT INTO ""CompanyInfos"" (""NameEnglish"", ""NameUrdu"", ""MarketName"", ""Phone"", ""Mobile"", ""Email"", ""NtnNumber"", ""ProprietorName"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT 'Umer Farooq & Brothers', N'عمر فاروق اینڈ برادرز', 'Grain Market Chishtian', '03026914604', '03347064086', 'umerfarooq.8088@gmail.com', '7306513-7', 'Hafiz Umer Farooq', NOW(), FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""CompanyInfos"");
            ");

            // A brand-new ModuleName (SetupCompanyInfo = 23). SeedRolesAndPermissionsAsync grants it
            // automatically on a fresh database (it loops every ModuleName), but an already-seeded
            // database needs its existing roles backfilled directly here. Every role can view it —
            // print pages need to read the letterhead regardless of the caller's Setup access — while
            // only Owner/Admin and Manager can edit it, matching how those two roles get Edit on every
            // other Setup module except User Account.
            migrationBuilder.Sql(@"
                INSERT INTO ""RolePermissions"" (""RoleId"", ""Module"", ""CanView"", ""CanCreate"", ""CanEdit"", ""CanDelete"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT r.""Id"", 23, TRUE, (r.""Name"" IN ('Owner/Admin', 'Manager')), (r.""Name"" IN ('Owner/Admin', 'Manager')), FALSE, NOW(), FALSE
                FROM ""Roles"" r
                WHERE NOT EXISTS (SELECT 1 FROM ""RolePermissions"" p WHERE p.""RoleId"" = r.""Id"" AND p.""Module"" = 23);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""Module"" = 23;");

            migrationBuilder.DropTable(
                name: "CompanyInfos");
        }
    }
}
