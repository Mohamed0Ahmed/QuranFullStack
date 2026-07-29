using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbwabTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abwab_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<int>(type: "integer", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "abwab_template_nodes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_id = table.Column<int>(type: "integer", nullable: false),
                    parent_node_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    representative_ayah_text = table.Column<string>(type: "text", nullable: true),
                    aliases = table.Column<string[]>(type: "text[]", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<int>(type: "integer", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_template_nodes", x => x.id);
                    table.ForeignKey(
                        name: "FK_abwab_template_nodes_abwab_template_nodes_parent_node_id",
                        column: x => x.parent_node_id,
                        principalTable: "abwab_template_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_template_nodes_abwab_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "abwab_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_abwab_template_nodes_deleted_at",
                table: "abwab_template_nodes",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_template_nodes_parent_node_id",
                table: "abwab_template_nodes",
                column: "parent_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_template_nodes_template_id",
                table: "abwab_template_nodes",
                column: "template_id",
                unique: true,
                filter: "parent_node_id IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_template_nodes_template_id_parent_node_id_name",
                table: "abwab_template_nodes",
                columns: new[] { "template_id", "parent_node_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_abwab_template_nodes_template_id_parent_node_id_order_value",
                table: "abwab_template_nodes",
                columns: new[] { "template_id", "parent_node_id", "order_value" });

            migrationBuilder.CreateIndex(
                name: "IX_abwab_templates_deleted_at",
                table: "abwab_templates",
                column: "deleted_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abwab_template_nodes");

            migrationBuilder.DropTable(
                name: "abwab_templates");
        }
    }
}
