using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class deletestonerecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlackDrawCount",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "BlackLoseCount",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "BlackPlayCount",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "BlackWinCount",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "WhiteDrawCount",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "WhiteLoseCount",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "WhitePlayCount",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "WhiteWinCount",
                table: "Player");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlackDrawCount",
                table: "Player",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlackLoseCount",
                table: "Player",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlackPlayCount",
                table: "Player",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlackWinCount",
                table: "Player",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhiteDrawCount",
                table: "Player",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhiteLoseCount",
                table: "Player",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhitePlayCount",
                table: "Player",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhiteWinCount",
                table: "Player",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
