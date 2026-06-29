using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSegmentStemId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "stem_id",
                table: "quran_word_morphology_segments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_segments_stem_id",
                table: "quran_word_morphology_segments",
                column: "stem_id");

            migrationBuilder.AddForeignKey(
                name: "FK_quran_word_morphology_segments_quran_stems_stem_id",
                table: "quran_word_morphology_segments",
                column: "stem_id",
                principalTable: "quran_stems",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quran_word_morphology_segments_quran_stems_stem_id",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropIndex(
                name: "IX_quran_word_morphology_segments_stem_id",
                table: "quran_word_morphology_segments");

            migrationBuilder.DropColumn(
                name: "stem_id",
                table: "quran_word_morphology_segments");
        }
    }
}
