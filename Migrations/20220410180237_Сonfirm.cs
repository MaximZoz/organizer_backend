using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnetClaimAuthorization.Migrations
{
    public partial class Сonfirm : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Сonfirm",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Сonfirm",
                table: "Tasks");
        }
    }
}
