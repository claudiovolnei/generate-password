using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PasswordManager.Api.Migrations
{
    public partial class AddPasswordOwnership : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserAccountId",
                table: "PasswordEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE p
                SET p.UserAccountId = u.Id
                FROM PasswordEntries p
                CROSS APPLY (
                    SELECT TOP 1 Id
                    FROM UserAccounts
                    ORDER BY CreatedAtUtc ASC
                ) u
                WHERE p.UserAccountId IS NULL
                """);

            migrationBuilder.Sql("""
                DELETE FROM PasswordEntries
                WHERE UserAccountId IS NULL
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserAccountId",
                table: "PasswordEntries",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordEntries_UserAccountId",
                table: "PasswordEntries",
                column: "UserAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordEntries_UserAccounts_UserAccountId",
                table: "PasswordEntries",
                column: "UserAccountId",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordEntries_UserAccounts_UserAccountId",
                table: "PasswordEntries");

            migrationBuilder.DropIndex(
                name: "IX_PasswordEntries_UserAccountId",
                table: "PasswordEntries");

            migrationBuilder.DropColumn(
                name: "UserAccountId",
                table: "PasswordEntries");
        }
    }
}