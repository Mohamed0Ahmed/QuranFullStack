using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSegmentDimensionIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "lemma_id",
                table: "quran_word_morphology_segments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "root_id",
                table: "quran_word_morphology_segments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_segments_lemma_id",
                table: "quran_word_morphology_segments",
                column: "lemma_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_segments_root_id",
                table: "quran_word_morphology_segments",
                column: "root_id");

            migrationBuilder.AddForeignKey(
                name: "FK_quran_word_morphology_segments_quran_lemmas_lemma_id",
                table: "quran_word_morphology_segments",
                column: "lemma_id",
                principalTable: "quran_lemmas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_quran_word_morphology_segments_quran_roots_root_id",
                table: "quran_word_morphology_segments",
                column: "root_id",
                principalTable: "quran_roots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quran_word_morphology_segments_quran_lemmas_lemma_id",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropForeignKey(
                name: "FK_quran_word_morphology_segments_quran_roots_root_id",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropIndex(
                name: "IX_quran_word_morphology_segments_lemma_id",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropIndex(
                name: "IX_quran_word_morphology_segments_root_id",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropColumn(
                name: "lemma_id",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropColumn(
                name: "root_id",
                table: "quran_word_morphology_segments");
        }
    }
}
