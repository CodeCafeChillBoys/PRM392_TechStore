using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStore.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletRefund : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_OrderId",
                table: "WalletTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_OrderId_Type",
                table: "WalletTransactions",
                columns: new[] { "OrderId", "Type" },
                unique: true,
                filter: "\"OrderId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_OrderId_Type",
                table: "WalletTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_OrderId",
                table: "WalletTransactions",
                column: "OrderId",
                unique: true,
                filter: "\"OrderId\" IS NOT NULL");
        }
    }
}
