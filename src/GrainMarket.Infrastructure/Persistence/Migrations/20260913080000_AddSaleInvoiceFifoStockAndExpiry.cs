using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GrainMarket.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleInvoiceFifoStockAndExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SalePrice (fixed) / SaleMarkupPercent (over FIFO cost) let Sale Invoice auto-fill a
            // line's price instead of the operator typing one every time. Neither is required —
            // leaving both null keeps today's fully-manual behavior for a product.
            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleMarkupPercent",
                table: "Products",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            // ExpiryDate is optional — only products with a shelf life (fertilizer, pesticide, seed)
            // need it. RemainingQuantity is the new unit of stock-on-hand: Sale Invoice now draws
            // down specific purchase lots FIFO (oldest lot first) instead of only checking an
            // aggregate product total, so each lot must track what's left of itself.
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "PurchaseLines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingQuantity",
                table: "PurchaseLines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "SaleInvoiceLineAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SaleInvoiceLineId = table.Column<int>(type: "integer", nullable: false),
                    PurchaseLineId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleInvoiceLineAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleInvoiceLineAllocations_SaleInvoiceLines_SaleInvoiceLineId",
                        column: x => x.SaleInvoiceLineId,
                        principalTable: "SaleInvoiceLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SaleInvoiceLineAllocations_PurchaseLines_PurchaseLineId",
                        column: x => x.PurchaseLineId,
                        principalTable: "PurchaseLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoiceLineAllocations_SaleInvoiceLineId",
                table: "SaleInvoiceLineAllocations",
                column: "SaleInvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleInvoiceLineAllocations_PurchaseLineId",
                table: "SaleInvoiceLineAllocations",
                column: "PurchaseLineId");

            // Reconcile RemainingQuantity for an already-seeded database: start every lot fully
            // available, then FIFO-walk existing non-cancelled Sale Invoice quantity (summed per
            // product, oldest lot first by Purchase date) back out of the lots that would have
            // supplied it. No SaleInvoiceLineAllocation rows are backfilled for these historical
            // sales (their per-lot cost breakdown wasn't tracked before this migration) — only the
            // resulting on-hand balance matters going forward; every sale from this point on gets a
            // real allocation.
            migrationBuilder.Sql(@"UPDATE ""PurchaseLines"" SET ""RemainingQuantity"" = ""Quantity"";");

            migrationBuilder.Sql(@"
                WITH sold_totals AS (
                    SELECT sil.""ProductId"" AS ""ProductId"", SUM(sil.""Quantity"") AS total_sold
                    FROM ""SaleInvoiceLines"" sil
                    JOIN ""SaleInvoices"" si ON si.""Id"" = sil.""SaleInvoiceId""
                    WHERE si.""IsCancelled"" = FALSE AND si.""IsDeleted"" = FALSE AND sil.""IsDeleted"" = FALSE
                    GROUP BY sil.""ProductId""
                ),
                ordered_lines AS (
                    SELECT pl.""Id"" AS ""Id"", pl.""ProductId"" AS ""ProductId"", pl.""Quantity"" AS ""Quantity"",
                           SUM(pl.""Quantity"") OVER (PARTITION BY pl.""ProductId"" ORDER BY p.""Date"", pl.""Id""
                               ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS cumulative_inclusive
                    FROM ""PurchaseLines"" pl
                    JOIN ""Purchases"" p ON p.""Id"" = pl.""PurchaseId""
                    WHERE p.""IsCancelled"" = FALSE AND p.""IsDeleted"" = FALSE AND pl.""IsDeleted"" = FALSE
                )
                UPDATE ""PurchaseLines"" pl
                SET ""RemainingQuantity"" = pl.""Quantity"" - GREATEST(0, LEAST(pl.""Quantity"",
                        COALESCE(st.total_sold, 0) - (ol.cumulative_inclusive - pl.""Quantity"")))
                FROM ordered_lines ol
                LEFT JOIN sold_totals st ON st.""ProductId"" = ol.""ProductId""
                WHERE pl.""Id"" = ol.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaleInvoiceLineAllocations");

            migrationBuilder.DropColumn(
                name: "RemainingQuantity",
                table: "PurchaseLines");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "PurchaseLines");

            migrationBuilder.DropColumn(
                name: "SaleMarkupPercent",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "Products");
        }
    }
}
