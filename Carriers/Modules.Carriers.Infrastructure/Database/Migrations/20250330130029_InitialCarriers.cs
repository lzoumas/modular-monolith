using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Carriers.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCarriers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "carriers");

            migrationBuilder.CreateTable(
                name: "carriers",
                schema: "carriers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_carriers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "carrier_shipments",
                schema: "carriers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    carrier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    shipping_address_street = table.Column<string>(type: "text", nullable: false),
                    shipping_address_city = table.Column<string>(type: "text", nullable: false),
                    shipping_address_zip = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_carrier_shipments", x => x.id);
                    table.ForeignKey(
                        name: "fk_carrier_shipments_carriers_carrier_id",
                        column: x => x.carrier_id,
                        principalSchema: "carriers",
                        principalTable: "carriers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_carrier_shipments_carrier_id",
                schema: "carriers",
                table: "carrier_shipments",
                column: "carrier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "carrier_shipments",
                schema: "carriers");

            migrationBuilder.DropTable(
                name: "carriers",
                schema: "carriers");
        }
    }
}
