using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class foreignkey_Player : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Account_Player_PlayerDbId",
                table: "Account");

            migrationBuilder.DropIndex(
                name: "IX_Account_PlayerDbId",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "PlayerDbId",
                table: "Account");

            migrationBuilder.AddColumn<int>(
                name: "AccountDbId",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Player_AccountDbId",
                table: "Player",
                column: "AccountDbId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Player_Account_AccountDbId",
                table: "Player",
                column: "AccountDbId",
                principalTable: "Account",
                principalColumn: "AccountDbId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Player_Account_AccountDbId",
                table: "Player");

            migrationBuilder.DropIndex(
                name: "IX_Player_AccountDbId",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "AccountDbId",
                table: "Player");

            migrationBuilder.AddColumn<int>(
                name: "PlayerDbId",
                table: "Account",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Account_PlayerDbId",
                table: "Account",
                column: "PlayerDbId");

            migrationBuilder.AddForeignKey(
                name: "FK_Account_Player_PlayerDbId",
                table: "Account",
                column: "PlayerDbId",
                principalTable: "Player",
                principalColumn: "PlayerDbId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
