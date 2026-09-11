using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedMatch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.Role });
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Preserve existing users' roles before dropping the legacy column.
            migrationBuilder.Sql(
                "INSERT INTO \"user_roles\" (\"UserId\", \"Role\") SELECT \"Id\", \"Role\" FROM \"users\" WHERE \"Role\" IS NOT NULL AND \"Role\" <> '';");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_Role",
                table: "user_roles",
                column: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Patient");

            // Restore a single role per user (first alphabetical role).
            migrationBuilder.Sql(
                "UPDATE \"users\" u SET \"Role\" = COALESCE((SELECT ur.\"Role\" FROM \"user_roles\" ur WHERE ur.\"UserId\" = u.\"Id\" ORDER BY ur.\"Role\" LIMIT 1), 'Patient');");

            migrationBuilder.DropTable(
                name: "user_roles");
        }
    }
}
