using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JamineERP.Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixStockSwap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Products\" SET \"OnHandQuantity\" = \"ReservedQuantity\", \"ReservedQuantity\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
