using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{

    public partial class AddWordSimpleI3rab : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "i3rab_arabic",
                table: "quran_word_morphology_segments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "i3rab_review_reason",
                table: "quran_word_morphology_segments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "i3rab_rule_id",
                table: "quran_word_morphology_segments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "i3rab_status",
                table: "quran_word_morphology_segments",
                type: "text",
                nullable: false,
                defaultValue: "unsupported");

            migrationBuilder.CreateTable(
                name: "quran_i3rab_rules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    signature_key = table.Column<string>(type: "text", nullable: false),
                    rule_family = table.Column<string>(type: "text", nullable: false),
                    i3rab_arabic = table.Column<string>(type: "text", nullable: false),
                    default_status = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_i3rab_rules", x => x.id);
                    table.CheckConstraint("CK_quran_i3rab_rules_default_status", "default_status IN ('approved', 'needs_review', 'unsupported')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_segments_i3rab_rule_id",
                table: "quran_word_morphology_segments",
                column: "i3rab_rule_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_quran_word_morphology_segments_i3rab_status",
                table: "quran_word_morphology_segments",
                sql: "i3rab_status IN ('approved', 'needs_review', 'unsupported')");

            migrationBuilder.CreateIndex(
                name: "IX_quran_i3rab_rules_rule_family",
                table: "quran_i3rab_rules",
                column: "rule_family");

            migrationBuilder.CreateIndex(
                name: "IX_quran_i3rab_rules_signature_key",
                table: "quran_i3rab_rules",
                column: "signature_key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_quran_word_morphology_segments_quran_i3rab_rules_i3rab_rule~",
                table: "quran_word_morphology_segments",
                column: "i3rab_rule_id",
                principalTable: "quran_i3rab_rules",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quran_word_morphology_segments_quran_i3rab_rules_i3rab_rule~",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropTable(
                name: "quran_i3rab_rules");

            migrationBuilder.DropIndex(
                name: "IX_quran_word_morphology_segments_i3rab_rule_id",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_quran_word_morphology_segments_i3rab_status",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropColumn(
                name: "i3rab_arabic",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropColumn(
                name: "i3rab_review_reason",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropColumn(
                name: "i3rab_rule_id",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropColumn(
                name: "i3rab_status",
                table: "quran_word_morphology_segments");
        }
    }
}
