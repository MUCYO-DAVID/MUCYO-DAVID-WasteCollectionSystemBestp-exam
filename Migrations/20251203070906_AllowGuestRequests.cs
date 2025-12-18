using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionSystem.Migrations
{
    /// <inheritdoc />
    public partial class AllowGuestRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WasteRequests_AspNetUsers_UserId",
                table: "WasteRequests");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "WasteRequests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                table: "WasteRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                table: "WasteRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WasteRequests_AspNetUsers_UserId",
                table: "WasteRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WasteRequests_AspNetUsers_UserId",
                table: "WasteRequests");

            migrationBuilder.DropColumn(
                name: "GuestName",
                table: "WasteRequests");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                table: "WasteRequests");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "WasteRequests",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WasteRequests_AspNetUsers_UserId",
                table: "WasteRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
