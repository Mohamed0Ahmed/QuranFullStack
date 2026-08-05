using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationAccessFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "users",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "access_audit_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    action_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    actor_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    actor_user_id = table.Column<int>(type: "integer", nullable: true),
                    target_user_id = table.Column<int>(type: "integer", nullable: false),
                    actor_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    target_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    permission_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    before_state = table.Column<string>(type: "jsonb", nullable: true),
                    after_state = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_audit_events", x => x.id);
                    table.CheckConstraint("ck_access_audit_events_documents_are_objects", "jsonb_typeof(actor_snapshot) = 'object'\nAND jsonb_typeof(target_snapshot) = 'object'\nAND (before_state IS NULL OR jsonb_typeof(before_state) = 'object')\nAND (after_state IS NULL OR jsonb_typeof(after_state) = 'object')");
                    table.CheckConstraint("ck_access_audit_events_metadata_schema_version", "jsonb_typeof(metadata) = 'object'\nAND jsonb_exists(metadata, 'schemaVersion')\nAND jsonb_typeof(metadata -> 'schemaVersion') = 'number'\nAND (metadata ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (metadata ->> 'schemaVersion')::numeric <= 2147483647");
                    table.ForeignKey(
                        name: "FK_access_audit_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_access_audit_events_users_target_user_id",
                        column: x => x.target_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    arabic_label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    english_description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                    table.CheckConstraint("ck_permissions_code_format", "code ~ '^[a-z0-9]+(\\.[a-z0-9_]+)+$'");
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false),
                    granted_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_permissions", x => new { x.user_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_user_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_permissions_users_granted_by_user_id",
                        column: x => x.granted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_status_id",
                table: "users",
                columns: new[] { "status", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_access_audit_events_action_type_occurred_at",
                table: "access_audit_events",
                columns: new[] { "action_type", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_access_audit_events_actor_user_id",
                table: "access_audit_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_access_audit_events_occurred_at_id",
                table: "access_audit_events",
                columns: new[] { "occurred_at", "id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_access_audit_events_permission_code",
                table: "access_audit_events",
                column: "permission_code",
                filter: "permission_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_access_audit_events_target_user_id_occurred_at_id",
                table: "access_audit_events",
                columns: new[] { "target_user_id", "occurred_at", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_display_order",
                table: "permissions",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_granted_by_user_id",
                table: "user_permissions",
                column: "granted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_permission_id",
                table: "user_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_user_id",
                table: "user_permissions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_audit_events");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropIndex(
                name: "IX_users_status_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "users");
        }
    }
}
