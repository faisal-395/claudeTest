using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowSharedInvoiceNoForMultiFarmerBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kachi/Pakki InvoiceNo was uniquely indexed, which forced MultiPurchase/MultiSale
            // (one buyer, several farmer rows entered together) to mint a separate invoice number
            // per farmer row — several invoices for what the buyer experiences as one purchase/sale
            // session. MultiPurchaseService/MultiSaleService now generate a single InvoiceNo for the
            // whole batch and pass it to every row, so the index needs to allow duplicates; each row
            // is still its own Kachi/Pakki underneath (separate weight/deductions/ledger postings —
            // farmers have separate ledger accounts, so those can't be merged), just sharing one
            // invoice number.
            migrationBuilder.DropIndex(
                name: "IX_Kachis_InvoiceNo",
                table: "Kachis");

            migrationBuilder.CreateIndex(
                name: "IX_Kachis_InvoiceNo",
                table: "Kachis",
                column: "InvoiceNo");

            migrationBuilder.DropIndex(
                name: "IX_Pakkis_InvoiceNo",
                table: "Pakkis");

            migrationBuilder.CreateIndex(
                name: "IX_Pakkis_InvoiceNo",
                table: "Pakkis",
                column: "InvoiceNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kachis_InvoiceNo",
                table: "Kachis");

            migrationBuilder.CreateIndex(
                name: "IX_Kachis_InvoiceNo",
                table: "Kachis",
                column: "InvoiceNo",
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_Pakkis_InvoiceNo",
                table: "Pakkis");

            migrationBuilder.CreateIndex(
                name: "IX_Pakkis_InvoiceNo",
                table: "Pakkis",
                column: "InvoiceNo",
                unique: true);
        }
    }
}
