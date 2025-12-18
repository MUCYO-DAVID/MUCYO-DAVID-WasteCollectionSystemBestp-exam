using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionSystem.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceTruckModelWithDriverSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert existing string status values to enum integers
            migrationBuilder.Sql(@"
                UPDATE Trucks SET Status = CASE 
                    WHEN Status = 'Available' THEN 0
                    WHEN Status = 'Busy' THEN 1
                    WHEN Status = 'InTransit' OR Status = 'In Transit' THEN 2
                    WHEN Status = 'Maintenance' THEN 3
                    ELSE 0
                END
            ");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Trucks",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DriverName",
                table: "Trucks",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<int>(
                name: "CurrentAssignmentId",
                table: "Trucks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverId",
                table: "Trucks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trucks_CurrentAssignmentId",
                table: "Trucks",
                column: "CurrentAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Trucks_DriverId",
                table: "Trucks",
                column: "DriverId",
                unique: true,
                filter: "[DriverId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Trucks_AspNetUsers_DriverId",
                table: "Trucks",
                column: "DriverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Trucks_Assignments_CurrentAssignmentId",
                table: "Trucks",
                column: "CurrentAssignmentId",
                principalTable: "Assignments",
                principalColumn: "AssignmentID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trucks_AspNetUsers_DriverId",
                table: "Trucks");

            migrationBuilder.DropForeignKey(
                name: "FK_Trucks_Assignments_CurrentAssignmentId",
                table: "Trucks");

            migrationBuilder.DropIndex(
                name: "IX_Trucks_CurrentAssignmentId",
                table: "Trucks");

            migrationBuilder.DropIndex(
                name: "IX_Trucks_DriverId",
                table: "Trucks");

            migrationBuilder.DropColumn(
                name: "CurrentAssignmentId",
                table: "Trucks");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Trucks");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Trucks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "DriverName",
                table: "Trucks",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);
        }
    }
}
