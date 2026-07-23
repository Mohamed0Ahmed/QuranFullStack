using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbwabCoreSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abwab_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    normalized_name = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_permanent_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_sections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "abwab_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    normalized_name = table.Column<string>(type: "text", nullable: false),
                    representative_quran_excerpt = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    section_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sibling_order = table.Column<int>(type: "integer", nullable: true),
                    section_order = table.Column<int>(type: "integer", nullable: true),
                    global_order = table.Column<int>(type: "integer", nullable: true),
                    ancestor_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false),
                    ordinary_protection_actor_subject = table.Column<string>(type: "text", nullable: true),
                    ordinary_protection_last_edited_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    category_content_revision = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_abwab_categories_abwab_categories_parent_category_id",
                        column: x => x.parent_category_id,
                        principalTable: "abwab_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_categories_abwab_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "abwab_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "abwab_category_search_aliases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    normalized_value = table.Column<string>(type: "text", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_category_search_aliases", x => x.id);
                    table.ForeignKey(
                        name: "FK_abwab_category_search_aliases_abwab_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "abwab_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "abwab_sections",
                columns: new[] { "id", "deleted_at", "is_deleted", "is_permanent_default", "name", "normalized_name", "sort_order" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, false, true, "أبواب غير مصنفة", "ابواب غير مصنفة", 0 });

            migrationBuilder.CreateIndex(
                name: "ix_abwab_categories_parent_category_id",
                table: "abwab_categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_abwab_categories_root_normalized_name_active",
                table: "abwab_categories",
                column: "normalized_name",
                unique: true,
                filter: "is_deleted = false AND parent_category_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_abwab_categories_section_id",
                table: "abwab_categories",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_abwab_categories_sibling_normalized_name_active",
                table: "abwab_categories",
                columns: new[] { "parent_category_id", "normalized_name" },
                unique: true,
                filter: "is_deleted = false AND parent_category_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_abwab_category_search_aliases_active",
                table: "abwab_category_search_aliases",
                columns: new[] { "category_id", "normalized_value" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_abwab_sections_normalized_name_active",
                table: "abwab_sections",
                column: "normalized_name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_abwab_sections_permanent_default",
                table: "abwab_sections",
                column: "is_permanent_default",
                unique: true,
                filter: "is_permanent_default = true AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abwab_category_search_aliases");

            migrationBuilder.DropTable(
                name: "abwab_categories");

            migrationBuilder.DropTable(
                name: "abwab_sections");
        }
    }
}
