using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartFoods.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixPantryInventoryLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PantryInventoryLog_AspNetUsers_UserId",
                table: "PantryInventoryLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PantryInventoryLog",
                table: "PantryInventoryLog");

            migrationBuilder.RenameTable(
                name: "PantryInventoryLog",
                newName: "PantryInventoryLogs");

            migrationBuilder.RenameIndex(
                name: "IX_PantryInventoryLog_UserId_Resolution",
                table: "PantryInventoryLogs",
                newName: "IX_PantryInventoryLogs_UserId_Resolution");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PantryInventoryLogs",
                table: "PantryInventoryLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PantryInventoryLogs_AspNetUsers_UserId",
                table: "PantryInventoryLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PantryInventoryLogs_AspNetUsers_UserId",
                table: "PantryInventoryLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PantryInventoryLogs",
                table: "PantryInventoryLogs");

            migrationBuilder.RenameTable(
                name: "PantryInventoryLogs",
                newName: "PantryInventoryLog");

            migrationBuilder.RenameIndex(
                name: "IX_PantryInventoryLogs_UserId_Resolution",
                table: "PantryInventoryLog",
                newName: "IX_PantryInventoryLog_UserId_Resolution");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PantryInventoryLog",
                table: "PantryInventoryLog",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PantryInventoryLog_AspNetUsers_UserId",
                table: "PantryInventoryLog",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
