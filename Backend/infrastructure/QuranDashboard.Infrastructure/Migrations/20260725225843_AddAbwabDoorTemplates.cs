using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbwabDoorTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abwab_door_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    normalized_name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    template_revision = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_door_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "abwab_template_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    door_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_template_node_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    normalized_name = table.Column<string>(type: "text", nullable: false),
                    representative_quran_excerpt = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    sibling_order = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_template_nodes", x => x.id);
                    table.CheckConstraint("ck_abwab_template_nodes_no_self_parent", "parent_template_node_id IS NULL OR parent_template_node_id <> id");
                    table.ForeignKey(
                        name: "FK_abwab_template_nodes_abwab_door_templates_door_template_id",
                        column: x => x.door_template_id,
                        principalTable: "abwab_door_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_template_nodes_abwab_template_nodes_parent_template_n~",
                        column: x => x.parent_template_node_id,
                        principalTable: "abwab_template_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "abwab_template_node_search_aliases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    normalized_value = table.Column<string>(type: "text", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_template_node_search_aliases", x => x.id);
                    table.ForeignKey(
                        name: "FK_abwab_template_node_search_aliases_abwab_template_nodes_tem~",
                        column: x => x.template_node_id,
                        principalTable: "abwab_template_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "permission_codes",
                columns: new[] { "code", "dashboard_admin_baseline", "system_owner_only" },
                values: new object[,]
                {
                    { "template.add", false, false },
                    { "template.apply", false, false },
                    { "template.delete", false, false },
                    { "template.edit", false, false },
                    { "template.restore", false, false },
                    { "template.view", false, false }
                });

            migrationBuilder.CreateIndex(
                name: "ix_abwab_door_templates_normalized_name",
                table: "abwab_door_templates",
                column: "normalized_name",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_abwab_template_node_search_aliases_normalized_value",
                table: "abwab_template_node_search_aliases",
                columns: new[] { "template_node_id", "normalized_value" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_abwab_template_nodes_normalized_name",
                table: "abwab_template_nodes",
                columns: new[] { "door_template_id", "normalized_name" });

            migrationBuilder.CreateIndex(
                name: "IX_abwab_template_nodes_parent_template_node_id",
                table: "abwab_template_nodes",
                column: "parent_template_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_abwab_template_nodes_template_parent",
                table: "abwab_template_nodes",
                columns: new[] { "door_template_id", "parent_template_node_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abwab_template_node_search_aliases");

            migrationBuilder.DropTable(
                name: "abwab_template_nodes");

            migrationBuilder.DropTable(
                name: "abwab_door_templates");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "template.add");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "template.apply");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "template.delete");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "template.edit");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "template.restore");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "template.view");
        }
    }
}
