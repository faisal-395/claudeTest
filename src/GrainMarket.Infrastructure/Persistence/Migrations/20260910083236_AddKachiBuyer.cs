using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKachiBuyer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BuyerId",
                table: "Kachis",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kachis_BuyerId",
                table: "Kachis",
                column: "BuyerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kachis_Parties_BuyerId",
                table: "Kachis",
                column: "BuyerId",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kachis_Parties_BuyerId",
                table: "Kachis");

            migrationBuilder.DropIndex(
                name: "IX_Kachis_BuyerId",
                table: "Kachis");

            migrationBuilder.DropColumn(
                name: "BuyerId",
                table: "Kachis");
        }
    }
}
