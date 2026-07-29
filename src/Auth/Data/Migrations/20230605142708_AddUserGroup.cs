using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Data.Migrations
{
    public partial class AddUserGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "group_name",
                schema: "auth",
                table: "user",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "group_uuid",
                schema: "auth",
                table: "user",
                type: "uuid",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "group_name",
                schema: "auth",
                table: "user");

            migrationBuilder.DropColumn(
                name: "group_uuid",
                schema: "auth",
                table: "user");
        }
    }
}
