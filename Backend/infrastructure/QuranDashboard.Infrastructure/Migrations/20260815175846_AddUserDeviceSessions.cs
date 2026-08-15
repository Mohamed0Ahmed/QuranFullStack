using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDeviceSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_device_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    csrf_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_device_sessions", x => x.id);
                    table.CheckConstraint("ck_user_device_sessions_expiry", "expires_at > created_at");
                    table.CheckConstraint("ck_user_device_sessions_revocation", "revoked_at IS NULL OR revoked_at >= created_at");
                    table.ForeignKey(
                        name: "FK_user_device_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_device_sessions_revoked_at_expires_at",
                table: "user_device_sessions",
                columns: new[] { "revoked_at", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_device_sessions_token_hash",
                table: "user_device_sessions",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_device_sessions_user_id_expires_at",
                table: "user_device_sessions",
                columns: new[] { "user_id", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_device_sessions");
        }
    }
}
