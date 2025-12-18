using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTruckStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Trucks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Trucks");
        }
    }
}
