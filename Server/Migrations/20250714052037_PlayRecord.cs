using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class PlayRecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlackDrawCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlackLoseCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlackPlayCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlackWinCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalDrawCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalLoseCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPlayCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalWinCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhiteDrawCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhiteLoseCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhitePlayCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WhiteWinCount",
                table: "Player",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "TotalDrawCount",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "TotalLoseCount",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "TotalPlayCount",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "TotalWinCount",
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
    }
}
