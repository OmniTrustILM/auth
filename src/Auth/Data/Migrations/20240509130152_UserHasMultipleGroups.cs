using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserHasMultipleGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "group_name",
                schema: "auth",
                table: "user",
                newName: "groups");

            migrationBuilder.Sql(@"UPDATE auth.user SET groups = CONCAT(group_uuid,':',groups) WHERE group_uuid IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "group_uuid",
                schema: "auth",
                table: "user");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "group_uuid",
                schema: "auth",
                table: "user",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_name",
                schema: "auth",
                table: "user",
                type: "text",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "groups",
                schema: "auth",
                table: "user");
        }
    }
}
