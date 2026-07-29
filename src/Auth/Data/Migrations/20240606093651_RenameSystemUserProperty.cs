using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameSystemUserProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE auth.user ADD COLUMN IF NOT EXISTS is_system_user BOOLEAN");
            migrationBuilder.Sql(@"ALTER TABLE auth.user ADD COLUMN IF NOT EXISTS ""system_user"" BOOLEAN");
            migrationBuilder.Sql(@"UPDATE auth.user SET is_system_user = ""system_user""");
            migrationBuilder.Sql(@"ALTER TABLE auth.user DROP COLUMN ""system_user""");
            migrationBuilder.Sql(@"ALTER TABLE auth.user ALTER COLUMN is_system_user SET NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE auth.user ADD COLUMN IF NOT EXISTS ""system_user"" BOOLEAN");
            migrationBuilder.Sql(@"UPDATE auth.user SET ""system_user"" = is_system_user");
            migrationBuilder.Sql(@"ALTER TABLE auth.user DROP COLUMN is_system_user");
            migrationBuilder.Sql(@"ALTER TABLE auth.user ALTER COLUMN ""system_user"" SET NOT NULL");

        }
    }
}
