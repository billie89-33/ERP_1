using System;
using JamineERP.Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JamineERP.Backend.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261113035000_AddErpVectorEmbeddings")]
    public partial class AddErpVectorEmbeddings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float[]>(
                name: "Embedding",
                table: "Products",
                type: "real[]",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddingUpdatedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<float[]>(
                name: "Embedding",
                table: "SalesOrders",
                type: "real[]",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddingUpdatedAt",
                table: "SalesOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<float[]>(
                name: "Embedding",
                table: "PurchaseOrders",
                type: "real[]",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddingUpdatedAt",
                table: "PurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<float[]>(
                name: "Embedding",
                table: "Suppliers",
                type: "real[]",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddingUpdatedAt",
                table: "Suppliers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<float[]>(
                name: "Embedding",
                table: "Customers",
                type: "real[]",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddingUpdatedAt",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Embedding", table: "Products");
            migrationBuilder.DropColumn(name: "EmbeddingUpdatedAt", table: "Products");

            migrationBuilder.DropColumn(name: "Embedding", table: "SalesOrders");
            migrationBuilder.DropColumn(name: "EmbeddingUpdatedAt", table: "SalesOrders");

            migrationBuilder.DropColumn(name: "Embedding", table: "PurchaseOrders");
            migrationBuilder.DropColumn(name: "EmbeddingUpdatedAt", table: "PurchaseOrders");

            migrationBuilder.DropColumn(name: "Embedding", table: "Suppliers");
            migrationBuilder.DropColumn(name: "EmbeddingUpdatedAt", table: "Suppliers");

            migrationBuilder.DropColumn(name: "Embedding", table: "Customers");
            migrationBuilder.DropColumn(name: "EmbeddingUpdatedAt", table: "Customers");
        }
    }
}
