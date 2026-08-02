using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbwabDoorRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abwab_door_relations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    door_a_id = table.Column<int>(type: "integer", nullable: false),
                    door_b_id = table.Column<int>(type: "integer", nullable: false),
                    relation_type = table.Column<int>(type: "integer", nullable: false),
                    broader_door_id = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_abwab_door_relations", x => x.id);
                    table.CheckConstraint("CK_abwab_door_relations_canonical_pair", "door_a_id < door_b_id");
                    table.CheckConstraint("CK_abwab_door_relations_direction", "(relation_type = 3) = (broader_door_id IS NOT NULL) AND (broader_door_id IS NULL OR broader_door_id IN (door_a_id, door_b_id))");
                    table.ForeignKey(
                        name: "FK_abwab_door_relations_abwab_doors_door_a_id",
                        column: x => x.door_a_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_door_relations_abwab_doors_door_b_id",
                        column: x => x.door_b_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_abwab_door_relations_deleted_at",
                table: "abwab_door_relations",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_door_relations_door_a_id_door_b_id_relation_type",
                table: "abwab_door_relations",
                columns: new[] { "door_a_id", "door_b_id", "relation_type" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_door_relations_door_b_id",
                table: "abwab_door_relations",
                column: "door_b_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abwab_door_relations");
        }
    }
}
