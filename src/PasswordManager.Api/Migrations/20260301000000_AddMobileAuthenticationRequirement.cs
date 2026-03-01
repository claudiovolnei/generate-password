using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PasswordManager.Api.Migrations
{
    public partial class AddMobileAuthenticationRequirement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireMobileAuthentication",
                table: "UserAccounts",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequireMobileAuthentication",
                table: "UserAccounts");
        }
    }
}
