using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JamineERP.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddErpInventoryFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StockQuantity",
                table: "Products",
                newName: "ReservedQuantity");

            migrationBuilder.AddColumn<int>(
                name: "OnHandQuantity",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GoodsIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GiNumber = table.Column<string>(type: "text", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsIssues_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsIssues_Users_IssuedByUserId",
                        column: x => x.IssuedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrNumber = table.Column<string>(type: "text", nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Users_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoodsIssueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsIssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedQuantity = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsIssueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsIssueItems_GoodsIssues_GoodsIssueId",
                        column: x => x.GoodsIssueId,
                        principalTable: "GoodsIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsIssueItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItems_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueItems_GoodsIssueId",
                table: "GoodsIssueItems",
                column: "GoodsIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssueItems_ProductId",
                table: "GoodsIssueItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_GiNumber",
                table: "GoodsIssues",
                column: "GiNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_IssuedByUserId",
                table: "GoodsIssues",
                column: "IssuedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsIssues_SalesOrderId",
                table: "GoodsIssues",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItems_GoodsReceiptId",
                table: "GoodsReceiptItems",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptItems_ProductId",
                table: "GoodsReceiptItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_GrNumber",
                table: "GoodsReceipts",
                column: "GrNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId",
                table: "GoodsReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_ReceivedByUserId",
                table: "GoodsReceipts",
                column: "ReceivedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodsIssueItems");

            migrationBuilder.DropTable(
                name: "GoodsReceiptItems");

            migrationBuilder.DropTable(
                name: "GoodsIssues");

            migrationBuilder.DropTable(
                name: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "OnHandQuantity",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "ReservedQuantity",
                table: "Products",
                newName: "StockQuantity");
        }
    }
}
