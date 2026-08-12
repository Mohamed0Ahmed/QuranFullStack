using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkingWorkspaceDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "linking_workspace_source_descriptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workspace_source_id = table.Column<long>(type: "bigint", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspace_source_descriptions", x => x.id);
                    table.CheckConstraint("ck_linking_workspace_source_descriptions_body_not_blank", "btrim(body) <> ''");
                    table.CheckConstraint("ck_linking_workspace_source_descriptions_order_value", "order_value BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_descriptions_linking_workspace_sou~",
                        column: x => x.workspace_source_id,
                        principalTable: "linking_workspace_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_descriptions_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_descriptions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_descriptions_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_descriptions_ayah_id",
                table: "linking_workspace_source_descriptions",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_descriptions_created_by",
                table: "linking_workspace_source_descriptions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_descriptions_updated_by",
                table: "linking_workspace_source_descriptions",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_descriptions_workspace_source_id_a~",
                table: "linking_workspace_source_descriptions",
                columns: new[] { "workspace_source_id", "ayah_id", "order_value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "linking_workspace_source_descriptions");
        }
    }
}
