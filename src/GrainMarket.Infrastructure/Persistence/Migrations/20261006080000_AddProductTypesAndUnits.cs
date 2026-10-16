using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductTypesAndUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // What kind of Input product (Seed, Fertilizer, Pesticide, …) — same role
            // AccountTypeDefinition plays for Chart of Accounts: a database-backed list so Setup >
            // Product Types can add more later without a code change, and so Stock can filter/group
            // by it.
            migrationBuilder.CreateTable(
                name: "ProductTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameUrdu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<System.DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<System.DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductTypes_Name",
                table: "ProductTypes",
                column: "Name",
                unique: true);

            // The managed choice list behind Setup > Products' "Unit" dropdown (kg, Litre, Bag,
            // Pack, Pcs, …) — replaces the old free-text input with a suggestions datalist.
            // Deliberately NOT a foreign key on Products: Products.BaseUnit stays the plain string
            // it already is, this table only supplies the dropdown's choices.
            migrationBuilder.CreateTable(
                name: "ProductUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    NameUrdu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<System.DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<System.DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUnits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnits_Name",
                table: "ProductUnits",
                column: "Name",
                unique: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductTypeId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductTypeId",
                table: "Products",
                column: "ProductTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductTypes_ProductTypeId",
                table: "Products",
                column: "ProductTypeId",
                principalTable: "ProductTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                INSERT INTO ""ProductTypes"" (""Name"", ""NameUrdu"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"") VALUES
                    ('Seed', N'بیج', TRUE, NOW(), FALSE),
                    ('Fertilizer', N'کھاد', TRUE, NOW(), FALSE),
                    ('Pesticide', N'زہر', TRUE, NOW(), FALSE);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""ProductUnits"" (""Name"", ""NameUrdu"", ""IsActive"", ""CreatedAtUtc"", ""IsDeleted"") VALUES
                    ('kg', N'کلوگرام', TRUE, NOW(), FALSE),
                    ('Litre', N'لیٹر', TRUE, NOW(), FALSE),
                    ('Bag', N'بیگ', TRUE, NOW(), FALSE),
                    ('Pack', N'پیک', TRUE, NOW(), FALSE),
                    ('Pcs', N'عدد', TRUE, NOW(), FALSE);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductTypes_ProductTypeId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductTypeId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductTypeId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "ProductUnits");

            migrationBuilder.DropTable(
                name: "ProductTypes");
        }
    }
}
