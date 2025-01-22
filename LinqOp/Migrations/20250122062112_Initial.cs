using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinqOp.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblPOSOrderMasters",
                columns: table => new
                {
                    OrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Vendor = table.Column<int>(type: "int", nullable: false),
                    StoreID = table.Column<long>(type: "bigint", nullable: false),
                    IsFinished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPOSOrderMasters", x => x.OrderID);
                });

            migrationBuilder.CreateTable(
                name: "tblVendorsItems",
                columns: table => new
                {
                    Itemkey = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorItemID = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitRetail = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblVendorsItems", x => x.Itemkey);
                });

            migrationBuilder.CreateTable(
                name: "tblPOSOrderDetails",
                columns: table => new
                {
                    OrderDetailID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    Itemkey = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPOSOrderDetails", x => x.OrderDetailID);
                    table.ForeignKey(
                        name: "FK_tblPOSOrderDetails_tblPOSOrderMasters_OrderID",
                        column: x => x.OrderID,
                        principalTable: "tblPOSOrderMasters",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblPOSOrderDetails_OrderID",
                table: "tblPOSOrderDetails",
                column: "OrderID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblPOSOrderDetails");

            migrationBuilder.DropTable(
                name: "tblVendorsItems");

            migrationBuilder.DropTable(
                name: "tblPOSOrderMasters");
        }
    }
}
