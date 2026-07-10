using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuranLemmaAnalyses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quran_lemma_analyses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lemma_id = table.Column<int>(type: "integer", nullable: false),
                    lemma_buckwalter = table.Column<string>(type: "text", nullable: false),
                    root_id = table.Column<int>(type: "integer", nullable: true),
                    head_pos = table.Column<string>(type: "text", nullable: true),
                    words_count = table.Column<int>(type: "integer", nullable: false),
                    first_word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false),
                    first_location = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_lemma_analyses", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_lemma_analyses_quran_lemmas_lemma_id",
                        column: x => x.lemma_id,
                        principalTable: "quran_lemmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_lemma_analyses_quran_roots_root_id",
                        column: x => x.root_id,
                        principalTable: "quran_roots",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_lemma_analyses_first_word_order_in_mushaf",
                table: "quran_lemma_analyses",
                column: "first_word_order_in_mushaf");

            migrationBuilder.CreateIndex(
                name: "IX_quran_lemma_analyses_lemma_buckwalter",
                table: "quran_lemma_analyses",
                column: "lemma_buckwalter",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_lemma_analyses_lemma_id",
                table: "quran_lemma_analyses",
                column: "lemma_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_lemma_analyses_root_id",
                table: "quran_lemma_analyses",
                column: "root_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quran_lemma_analyses");
        }
    }
}
