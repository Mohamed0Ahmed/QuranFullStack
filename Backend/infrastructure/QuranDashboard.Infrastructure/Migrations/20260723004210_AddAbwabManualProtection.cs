using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbwabManualProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abwab_manual_protections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protection_type = table.Column<int>(type: "integer", nullable: false),
                    protection_scope = table.Column<int>(type: "integer", nullable: false),
                    applied_by_actor_subject = table.Column<string>(type: "text", nullable: false),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lifted_by_actor_subject = table.Column<string>(type: "text", nullable: true),
                    lifted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_manual_protections", x => x.id);
                    table.ForeignKey(
                        name: "FK_abwab_manual_protections_abwab_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "abwab_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_abwab_manual_protections_active_category_type",
                table: "abwab_manual_protections",
                columns: new[] { "category_id", "protection_type" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abwab_manual_protections");
        }
    }
}
