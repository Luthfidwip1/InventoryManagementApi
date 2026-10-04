using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InventoryManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class Task3SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { 1001, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Electronic devices and accessories", "Electronics" },
                    { 1002, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Office and stationery products", "Office Supplies" },
                    { 1003, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Networking devices and equipment", "Networking" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Description", "Name", "QuantityInStock", "ReorderLevel", "Sku", "UnitPrice", "UpdatedAt" },
                values: new object[,]
                {
                    { 1001, 1001, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2.4 GHz wireless mouse", "Wireless Mouse", 20, 5, "ELEC-001", 150000m, null },
                    { 1002, 1001, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USB mechanical keyboard", "Mechanical Keyboard", 15, 5, "ELEC-002", 650000m, null },
                    { 1003, 1001, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USB headset with microphone", "USB Headset", 12, 4, "ELEC-003", 325000m, null },
                    { 1004, 1002, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A4 paper 80 gsm", "A4 Paper", 50, 10, "OFF-001", 65000m, null },
                    { 1005, 1002, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Blue ballpoint pen", "Ballpoint Pen", 100, 20, "OFF-002", 5000m, null },
                    { 1006, 1002, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A5 ruled notebook", "Notebook", 40, 10, "OFF-003", 25000m, null },
                    { 1007, 1003, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dual band wireless router", "WiFi Router", 8, 3, "NET-001", 550000m, null },
                    { 1008, 1003, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "8 port gigabit switch", "Network Switch", 6, 2, "NET-002", 475000m, null },
                    { 1009, 1003, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CAT6 ethernet cable", "Ethernet Cable", 30, 10, "NET-003", 45000m, null },
                    { 1010, 1001, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "4 port USB hub", "USB Hub", 18, 5, "ELEC-004", 125000m, null }
                });

            migrationBuilder.InsertData(
                table: "StockTransactions",
                columns: new[] { "Id", "Note", "ProductId", "Quantity", "Source", "TransactionDate", "Type" },
                values: new object[,]
                {
                    { 1001, "Initial stock", 1001, 20, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 1002, "Initial stock", 1002, 15, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 1003, "Initial stock", 1004, 50, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 1004, "Initial stock", 1007, 10, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1 },
                    { 1005, "Initial sale", 1007, 2, 1, new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Utc), 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1005);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1006);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1008);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1009);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1010);

            migrationBuilder.DeleteData(
                table: "StockTransactions",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "StockTransactions",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "StockTransactions",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "StockTransactions",
                keyColumn: "Id",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "StockTransactions",
                keyColumn: "Id",
                keyValue: 1005);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1007);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1003);
        }
    }
}
