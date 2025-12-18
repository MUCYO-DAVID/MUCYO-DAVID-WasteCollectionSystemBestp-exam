using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToWasteRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "WasteRequests",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "WasteRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "WasteRequests",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "WasteRequests");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "WasteRequests");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "WasteRequests");
        }
    }
}
