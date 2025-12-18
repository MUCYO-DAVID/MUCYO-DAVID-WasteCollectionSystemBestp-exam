using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WasteCollectionSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestCarts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GuestPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestCarts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuestCartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuestCartId = table.Column<int>(type: "int", nullable: false),
                    WasteRequestId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestCartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestCartItems_GuestCarts_GuestCartId",
                        column: x => x.GuestCartId,
                        principalTable: "GuestCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuestCartItems_WasteRequests_WasteRequestId",
                        column: x => x.WasteRequestId,
                        principalTable: "WasteRequests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestCartItems_GuestCartId",
                table: "GuestCartItems",
                column: "GuestCartId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestCartItems_WasteRequestId",
                table: "GuestCartItems",
                column: "WasteRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestCarts_SessionId",
                table: "GuestCarts",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestCartItems");

            migrationBuilder.DropTable(
                name: "GuestCarts");
        }
    }
}
