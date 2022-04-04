using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnetClaimAuthorization.Migrations
{
    public partial class AddCompletedTodoItemTask : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Completed",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Completed",
                table: "Tasks");
        }
    }
}
