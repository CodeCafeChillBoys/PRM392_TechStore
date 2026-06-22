using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStore.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceFieldsToLoginSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "LoginSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "LoginSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "LoginSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FcmToken",
                table: "LoginSessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "LoginSessions");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "LoginSessions");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "LoginSessions");

            migrationBuilder.DropColumn(
                name: "FcmToken",
                table: "LoginSessions");
        }
    }
}
