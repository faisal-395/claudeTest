using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppearanceSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppearanceSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    SurfaceColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppearanceSettings", x => x.Id);
                });

            // One default row matching the app's original hardcoded green theme — editable
            // afterwards from Setup > Appearance. Migrations always run, fresh install or not, so
            // this single INSERT covers both; SeedAppearanceSettingsAsync in SeedData.cs does the
            // same idempotent insert for symmetry with SeedCompanyInfoAsync, and is a no-op once
            // this has run.
            migrationBuilder.Sql(@"
                INSERT INTO ""AppearanceSettings"" (""PrimaryColor"", ""AccentColor"", ""SurfaceColor"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT '#1B4332', '#2D6A4F', '#F7F9F7', NOW(), FALSE
                WHERE NOT EXISTS (SELECT 1 FROM ""AppearanceSettings"");
            ");

            // A brand-new ModuleName (SetupAppearance = 24). SeedRolesAndPermissionsAsync grants it
            // automatically on a fresh database (it loops every ModuleName), but an already-seeded
            // database needs its existing roles backfilled directly here. Unlike every other Setup
            // module, this one is Owner/Admin-only — changing the client's brand colors is a
            // super-user action, not something Manager gets by default like other Setup screens.
            migrationBuilder.Sql(@"
                INSERT INTO ""RolePermissions"" (""RoleId"", ""Module"", ""CanView"", ""CanCreate"", ""CanEdit"", ""CanDelete"", ""CreatedAtUtc"", ""IsDeleted"")
                SELECT r.""Id"", 24, (r.""Name"" = 'Owner/Admin'), (r.""Name"" = 'Owner/Admin'), (r.""Name"" = 'Owner/Admin'), (r.""Name"" = 'Owner/Admin'), NOW(), FALSE
                FROM ""Roles"" r
                WHERE NOT EXISTS (SELECT 1 FROM ""RolePermissions"" p WHERE p.""RoleId"" = r.""Id"" AND p.""Module"" = 24);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""RolePermissions"" WHERE ""Module"" = 24;");

            migrationBuilder.DropTable(
                name: "AppearanceSettings");
        }
    }
}
