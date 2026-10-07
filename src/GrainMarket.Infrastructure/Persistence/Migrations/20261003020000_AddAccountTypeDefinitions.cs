using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTypeDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Turns the fixed AccountType enum (Asset/Liability/Income/Expense/Equity) into a
            // database-backed table so Setup > Account Types can add more later without a code
            // change. Seeded in enum-ordinal order below so the five rows land on ids 1-5 — the
            // same values every existing ChartOfAccount.AccountType already stores — which is what
            // lets the plain id copy a few statements down carry every row over unchanged.
            migrationBuilder.CreateTable(
                name: "AccountTypeDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameUrdu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSystemType = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<System.DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<System.DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTypeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountTypeDefinitions_Name",
                table: "AccountTypeDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.Sql(@"
                INSERT INTO ""AccountTypeDefinitions"" (""Name"", ""IsSystemType"", ""CreatedAtUtc"", ""IsDeleted"") VALUES
                    ('Asset', TRUE, NOW(), FALSE),
                    ('Liability', TRUE, NOW(), FALSE),
                    ('Income', TRUE, NOW(), FALSE),
                    ('Expense', TRUE, NOW(), FALSE),
                    ('Equity', TRUE, NOW(), FALSE);
            ");

            migrationBuilder.AddColumn<int>(
                name: "AccountTypeId",
                table: "ChartOfAccounts",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"UPDATE ""ChartOfAccounts"" SET ""AccountTypeId"" = ""AccountType"";");

            migrationBuilder.AlterColumn<int>(
                name: "AccountTypeId",
                table: "ChartOfAccounts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "AccountType",
                table: "ChartOfAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_AccountTypeId",
                table: "ChartOfAccounts",
                column: "AccountTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChartOfAccounts_AccountTypeDefinitions_AccountTypeId",
                table: "ChartOfAccounts",
                column: "AccountTypeId",
                principalTable: "AccountTypeDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChartOfAccounts_AccountTypeDefinitions_AccountTypeId",
                table: "ChartOfAccounts");

            migrationBuilder.DropIndex(
                name: "IX_ChartOfAccounts_AccountTypeId",
                table: "ChartOfAccounts");

            migrationBuilder.AddColumn<int>(
                name: "AccountType",
                table: "ChartOfAccounts",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"UPDATE ""ChartOfAccounts"" SET ""AccountType"" = ""AccountTypeId"";");

            migrationBuilder.AlterColumn<int>(
                name: "AccountType",
                table: "ChartOfAccounts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "AccountTypeId",
                table: "ChartOfAccounts");

            migrationBuilder.DropTable(
                name: "AccountTypeDefinitions");
        }
    }
}
