using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityOwnershipAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permission_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_kind = table.Column<int>(type: "integer", nullable: false),
                    target_key = table.Column<string>(type: "text", nullable: false),
                    permission_code = table.Column<string>(type: "text", nullable: false),
                    is_granted = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission_assignments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permission_codes",
                columns: table => new
                {
                    code = table.Column<string>(type: "text", nullable: false),
                    system_owner_only = table.Column<bool>(type: "boolean", nullable: false),
                    dashboard_admin_baseline = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission_codes", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "security_audit_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    actor_subject = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    server_timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_owner_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    issuer = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_account_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_owner_memberships", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "permission_codes",
                columns: new[] { "code", "dashboard_admin_baseline", "system_owner_only" },
                values: new object[,]
                {
                    { "attribution.manage", false, false },
                    { "attribution.view", true, false },
                    { "audit.restore", false, true },
                    { "permission.administer", false, true },
                    { "safetyPoint.manage", false, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_permission_assignments_target_kind_target_key_permission_co~",
                table: "permission_assignments",
                columns: new[] { "target_kind", "target_key", "permission_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_owner_memberships_issuer_subject",
                table: "system_owner_memberships",
                columns: new[] { "issuer", "subject" },
                unique: true);

            // --- Manually authored DB defense (objects EF Core cannot model): the security-audit stream is
            // permanent and append-only (FR-039), like the product audit tables. Reuses the abwab_forbid_
            // mutation() trigger function created by the kernel migration and the same restricted role. The
            // trigger drops with its table on Down; the shared function/role are owned by the kernel
            // migration. ---
            migrationBuilder.Sql(@"
                CREATE TRIGGER security_audit_events_append_only
                    BEFORE UPDATE OR DELETE OR TRUNCATE ON security_audit_events
                    FOR EACH STATEMENT EXECUTE FUNCTION abwab_forbid_mutation();");

            migrationBuilder.Sql(@"
                GRANT SELECT, INSERT ON security_audit_events TO abwab_app;
                REVOKE UPDATE, DELETE, TRUNCATE ON security_audit_events FROM abwab_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "permission_assignments");

            migrationBuilder.DropTable(
                name: "permission_codes");

            migrationBuilder.DropTable(
                name: "security_audit_events");

            migrationBuilder.DropTable(
                name: "system_owner_memberships");
        }
    }
}
