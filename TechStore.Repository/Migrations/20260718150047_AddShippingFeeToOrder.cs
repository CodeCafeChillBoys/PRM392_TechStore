using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStore.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingFeeToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: DB chung với team, có thể cột ShippingFee đã tồn tại từ migration cũ đã bị gỡ khỏi code.
            migrationBuilder.Sql(
                "ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"ShippingFee\" numeric NOT NULL DEFAULT 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Orders\" DROP COLUMN IF EXISTS \"ShippingFee\";");
        }
    }
}
