using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbwabCategoryRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abwab_category_relationships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_type = table.Column<int>(type: "integer", nullable: false),
                    lower_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    higher_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_category_relationships", x => x.id);
                    table.CheckConstraint("ck_abwab_category_relationships_canonical_order", "lower_category_id IS NULL OR lower_category_id < higher_category_id");
                    table.CheckConstraint("ck_abwab_category_relationships_no_self_link", "(lower_category_id IS NULL OR lower_category_id <> higher_category_id) AND (source_category_id IS NULL OR source_category_id <> target_category_id)");
                    table.CheckConstraint("ck_abwab_category_relationships_one_shape", "(relationship_type <> 2 AND lower_category_id IS NOT NULL AND higher_category_id IS NOT NULL AND source_category_id IS NULL AND target_category_id IS NULL) OR (relationship_type = 2 AND lower_category_id IS NULL AND higher_category_id IS NULL AND source_category_id IS NOT NULL AND target_category_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_abwab_category_relationships_abwab_categories_higher_catego~",
                        column: x => x.higher_category_id,
                        principalTable: "abwab_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_category_relationships_abwab_categories_lower_categor~",
                        column: x => x.lower_category_id,
                        principalTable: "abwab_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_category_relationships_abwab_categories_source_catego~",
                        column: x => x.source_category_id,
                        principalTable: "abwab_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_category_relationships_abwab_categories_target_catego~",
                        column: x => x.target_category_id,
                        principalTable: "abwab_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "permission_codes",
                columns: new[] { "code", "dashboard_admin_baseline", "system_owner_only" },
                values: new object[,]
                {
                    { "relationship.add", false, false },
                    { "relationship.delete", false, false },
                    { "relationship.edit", false, false },
                    { "relationship.restore", false, false },
                    { "relationship.view", false, false }
                });

            migrationBuilder.CreateIndex(
                name: "ix_abwab_category_relationships_directional_active",
                table: "abwab_category_relationships",
                columns: new[] { "source_category_id", "target_category_id" },
                unique: true,
                filter: "is_deleted = false AND relationship_type = 2");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_category_relationships_higher_category_id",
                table: "abwab_category_relationships",
                column: "higher_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_abwab_category_relationships_mutual_active",
                table: "abwab_category_relationships",
                columns: new[] { "lower_category_id", "higher_category_id", "relationship_type" },
                unique: true,
                filter: "is_deleted = false AND lower_category_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_category_relationships_target_category_id",
                table: "abwab_category_relationships",
                column: "target_category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abwab_category_relationships");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "relationship.add");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "relationship.delete");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "relationship.edit");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "relationship.restore");

            migrationBuilder.DeleteData(
                table: "permission_codes",
                keyColumn: "code",
                keyValue: "relationship.view");
        }
    }
}
