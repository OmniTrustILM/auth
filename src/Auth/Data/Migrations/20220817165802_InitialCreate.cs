using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "action",
                schema: "auth",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_action", x => x.uuid);
                });

            migrationBuilder.CreateTable(
                name: "resource",
                schema: "auth",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    list_objects_endpoint = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource", x => x.uuid);
                });

            migrationBuilder.CreateTable(
                name: "role",
                schema: "auth",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    system_role = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.uuid);
                });

            migrationBuilder.CreateTable(
                name: "user",
                schema: "auth",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_system_user = table.Column<bool>(type: "boolean", nullable: false),
                    certificate_uuid = table.Column<Guid>(type: "uuid", nullable: true),
                    certificate_fingerprint = table.Column<string>(type: "text", nullable: true),
                    auth_token_sub = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.uuid);
                });

            migrationBuilder.CreateTable(
                name: "resource_action",
                schema: "auth",
                columns: table => new
                {
                    action_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_uuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_action", x => new { x.action_uuid, x.resource_uuid });
                    table.ForeignKey(
                        name: "FK_resource_action_action_action_uuid",
                        column: x => x.action_uuid,
                        principalSchema: "auth",
                        principalTable: "action",
                        principalColumn: "uuid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resource_action_resource_resource_uuid",
                        column: x => x.resource_uuid,
                        principalSchema: "auth",
                        principalTable: "resource",
                        principalColumn: "uuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "permission",
                schema: "auth",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    role_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_uuid = table.Column<Guid>(type: "uuid", nullable: true),
                    action_uuid = table.Column<Guid>(type: "uuid", nullable: true),
                    object_uuid = table.Column<Guid>(type: "uuid", nullable: true),
                    object_name = table.Column<string>(type: "text", nullable: true),
                    is_allowed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission", x => x.uuid);
                    table.ForeignKey(
                        name: "FK_permission_action_action_uuid",
                        column: x => x.action_uuid,
                        principalSchema: "auth",
                        principalTable: "action",
                        principalColumn: "uuid");
                    table.ForeignKey(
                        name: "FK_permission_resource_resource_uuid",
                        column: x => x.resource_uuid,
                        principalSchema: "auth",
                        principalTable: "resource",
                        principalColumn: "uuid");
                    table.ForeignKey(
                        name: "FK_permission_role_role_uuid",
                        column: x => x.role_uuid,
                        principalSchema: "auth",
                        principalTable: "role",
                        principalColumn: "uuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_role",
                schema: "auth",
                columns: table => new
                {
                    role_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    user_uuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_role", x => new { x.role_uuid, x.user_uuid });
                    table.ForeignKey(
                        name: "FK_user_role_role_role_uuid",
                        column: x => x.role_uuid,
                        principalSchema: "auth",
                        principalTable: "role",
                        principalColumn: "uuid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_role_user_user_uuid",
                        column: x => x.user_uuid,
                        principalSchema: "auth",
                        principalTable: "user",
                        principalColumn: "uuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_action_name",
                schema: "auth",
                table: "action",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permission_action_uuid",
                schema: "auth",
                table: "permission",
                column: "action_uuid");

            migrationBuilder.CreateIndex(
                name: "IX_permission_resource_uuid",
                schema: "auth",
                table: "permission",
                column: "resource_uuid");

            migrationBuilder.CreateIndex(
                name: "IX_permission_role_uuid",
                schema: "auth",
                table: "permission",
                column: "role_uuid");

            migrationBuilder.CreateIndex(
                name: "IX_resource_name",
                schema: "auth",
                table: "resource",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resource_action_resource_uuid",
                schema: "auth",
                table: "resource_action",
                column: "resource_uuid");

            migrationBuilder.CreateIndex(
                name: "IX_role_name",
                schema: "auth",
                table: "role",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_username",
                schema: "auth",
                table: "user",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_role_user_uuid",
                schema: "auth",
                table: "user_role",
                column: "user_uuid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permission",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "resource_action",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "user_role",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "action",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "resource",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "role",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "user",
                schema: "auth");
        }
    }
}
