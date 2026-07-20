using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStore.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundFieldsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: chạy được nhiều lần / trên DB đã có sẵn cột (shared Supabase Postgres).
            migrationBuilder.Sql("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"RefundReason\" text NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"RefundImageUrl\" text NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"RefundRequestedAt\" timestamp with time zone NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Orders\" DROP COLUMN IF EXISTS \"RefundReason\";");
            migrationBuilder.Sql("ALTER TABLE \"Orders\" DROP COLUMN IF EXISTS \"RefundImageUrl\";");
            migrationBuilder.Sql("ALTER TABLE \"Orders\" DROP COLUMN IF EXISTS \"RefundRequestedAt\";");
        }
    }
}
