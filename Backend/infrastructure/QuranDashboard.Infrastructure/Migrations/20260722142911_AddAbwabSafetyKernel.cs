using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbwabSafetyKernel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abwab_change_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timeline_generation = table.Column<long>(type: "bigint", nullable: false),
                    change_set_sequence = table.Column<long>(type: "bigint", nullable: false),
                    actor_subject = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_change_sets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "abwab_revision_state",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    audit_head_sequence = table.Column<long>(type: "bigint", nullable: false),
                    timeline_generation = table.Column<long>(type: "bigint", nullable: false),
                    tree_revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_revision_state", x => x.id);
                    table.CheckConstraint("ck_abwab_revision_state_singleton", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "abwab_timeline_generation_boundaries",
                columns: table => new
                {
                    generation = table.Column<long>(type: "bigint", nullable: false),
                    is_root = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_timeline_generation_boundaries", x => x.generation);
                });

            migrationBuilder.CreateTable(
                name: "abwab_write_barrier",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_write_barrier", x => x.id);
                    table.CheckConstraint("ck_abwab_write_barrier_singleton", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "abwab_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_ordinal = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    server_timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_abwab_audit_events_abwab_change_sets_change_set_id",
                        column: x => x.change_set_id,
                        principalTable: "abwab_change_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "abwab_revision_state",
                columns: new[] { "id", "audit_head_sequence", "timeline_generation", "tree_revision" },
                values: new object[] { 1, 0L, 0L, 0L });

            migrationBuilder.InsertData(
                table: "abwab_timeline_generation_boundaries",
                columns: new[] { "generation", "created_at", "is_root", "reason" },
                values: new object[] { 0L, new DateTimeOffset(new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "genesis" });

            migrationBuilder.InsertData(
                table: "abwab_write_barrier",
                columns: new[] { "id", "state" },
                values: new object[] { 1, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_abwab_audit_events_change_set_id_event_ordinal",
                table: "abwab_audit_events",
                columns: new[] { "change_set_id", "event_ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_abwab_change_sets_change_set_sequence",
                table: "abwab_change_sets",
                column: "change_set_sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_abwab_timeline_generation_boundaries_is_root",
                table: "abwab_timeline_generation_boundaries",
                column: "is_root",
                unique: true,
                filter: "\"is_root\"");

            // --- Manually authored DB defenses (objects EF Core cannot model): append-only/TRUNCATE
            // triggers on the audit tables, the immutable generation-zero root protection, and the
            // restricted application role. Kept in the tool-generated migration's Up() per the EF workflow;
            // the .Designer.cs and ModelSnapshot are untouched (they do not track triggers or roles). ---

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION abwab_forbid_mutation() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'abwab audit tables are append-only: % on % is forbidden', TG_OP, TG_TABLE_NAME
                        USING ERRCODE = 'P0001';
                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
                CREATE TRIGGER abwab_change_sets_append_only
                    BEFORE UPDATE OR DELETE OR TRUNCATE ON abwab_change_sets
                    FOR EACH STATEMENT EXECUTE FUNCTION abwab_forbid_mutation();");

            migrationBuilder.Sql(@"
                CREATE TRIGGER abwab_audit_events_append_only
                    BEFORE UPDATE OR DELETE OR TRUNCATE ON abwab_audit_events
                    FOR EACH STATEMENT EXECUTE FUNCTION abwab_forbid_mutation();");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION abwab_forbid_root_boundary_change() RETURNS trigger AS $$
                BEGIN
                    IF (OLD.is_root) THEN
                        RAISE EXCEPTION 'the generation-zero timeline root boundary is immutable'
                            USING ERRCODE = 'P0001';
                    END IF;
                    IF (TG_OP = 'DELETE') THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
                CREATE TRIGGER abwab_boundaries_protect_root
                    BEFORE UPDATE OR DELETE ON abwab_timeline_generation_boundaries
                    FOR EACH ROW EXECUTE FUNCTION abwab_forbid_root_boundary_change();");

            // Restricted application role: append-only access to the audit tables (SELECT + INSERT), never
            // UPDATE/DELETE/TRUNCATE. Idempotent so re-applying against a persistent DB is safe.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'abwab_app') THEN
                        CREATE ROLE abwab_app NOLOGIN;
                    END IF;
                END
                $$;");

            migrationBuilder.Sql(@"
                GRANT SELECT, INSERT ON abwab_change_sets TO abwab_app;
                GRANT SELECT, INSERT ON abwab_audit_events TO abwab_app;
                REVOKE UPDATE, DELETE, TRUNCATE ON abwab_change_sets FROM abwab_app;
                REVOKE UPDATE, DELETE, TRUNCATE ON abwab_audit_events FROM abwab_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abwab_audit_events");

            migrationBuilder.DropTable(
                name: "abwab_revision_state");

            migrationBuilder.DropTable(
                name: "abwab_timeline_generation_boundaries");

            migrationBuilder.DropTable(
                name: "abwab_write_barrier");

            migrationBuilder.DropTable(
                name: "abwab_change_sets");

            // Triggers drop with their tables; drop the functions and the restricted role afterwards, once
            // the role no longer holds any table privileges.
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS abwab_forbid_mutation();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS abwab_forbid_root_boundary_change();");
            migrationBuilder.Sql("DROP ROLE IF EXISTS abwab_app;");
        }
    }
}
