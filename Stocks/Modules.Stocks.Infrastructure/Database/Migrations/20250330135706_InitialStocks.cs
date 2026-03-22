using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Stocks.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "stocks");

            migrationBuilder.CreateTable(
                name: "product_stocks",
                schema: "stocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "text", nullable: false),
                    available_quantity = table.Column<int>(type: "integer", nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_stocks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_stocks_product_name",
                schema: "stocks",
                table: "product_stocks",
                column: "product_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_stocks",
                schema: "stocks");
        }
    }
}
