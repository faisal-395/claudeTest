using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKachiReceiptNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "Kachis",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "Kachis");
        }
    }
}
