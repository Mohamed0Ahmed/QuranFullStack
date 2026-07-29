using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbwabGlobalOrderValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "global_order_value",
                table: "abwab_doors",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_abwab_doors_global_order_value",
                table: "abwab_doors",
                column: "global_order_value",
                filter: "parent_id IS NULL AND deleted_at IS NULL");

            // One-time backfill so the superset renders identically on first load. ORDER BY
            // order_value, id reproduces today's render order exactly (abwab-tree.builder.ts:5-7
            // sorts roots the same way, and Array.prototype.sort is spec-stable) — plan §7 T103.
            migrationBuilder.Sql("""
                WITH ordered AS (
                    SELECT id, ROW_NUMBER() OVER (ORDER BY order_value, id) AS rn
                    FROM abwab_doors
                    WHERE parent_id IS NULL AND deleted_at IS NULL
                )
                UPDATE abwab_doors d SET global_order_value = ordered.rn
                FROM ordered WHERE d.id = ordered.id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_abwab_doors_global_order_value",
                table: "abwab_doors");

            migrationBuilder.DropColumn(
                name: "global_order_value",
                table: "abwab_doors");
        }
    }
}
