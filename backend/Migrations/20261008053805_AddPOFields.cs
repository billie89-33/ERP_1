using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JamineERP.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPOFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "PurchaseOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "PurchaseOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "PurchaseOrders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTotal",
                table: "PurchaseOrders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "PurchaseOrders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "PurchaseOrders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryDate",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SubTotal",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "PurchaseOrders");
        }
    }
}
