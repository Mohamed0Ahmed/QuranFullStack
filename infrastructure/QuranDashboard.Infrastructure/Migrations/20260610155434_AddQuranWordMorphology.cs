using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuranWordMorphology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quran_pos_tags",
                columns: table => new
                {
                    code = table.Column<string>(type: "text", nullable: false),
                    arabic_label = table.Column<string>(type: "text", nullable: false),
                    english_label = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_pos_tags", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "quran_roots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    root_text = table.Column<string>(type: "text", nullable: false),
                    root_buckwalter = table.Column<string>(type: "text", nullable: true),
                    words_count = table.Column<int>(type: "integer", nullable: false),
                    distinct_lemmas_count = table.Column<short>(type: "smallint", nullable: false),
                    first_word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_roots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quran_stems",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stem_text = table.Column<string>(type: "text", nullable: false),
                    words_count = table.Column<int>(type: "integer", nullable: false),
                    first_word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_stems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quran_word_morphology_segments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    segment_location = table.Column<string>(type: "text", nullable: false),
                    segment_number = table.Column<short>(type: "smallint", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    pos = table.Column<string>(type: "text", nullable: false),
                    form_buckwalter = table.Column<string>(type: "text", nullable: false),
                    form_arabic_normalized = table.Column<string>(type: "text", nullable: true),
                    arabic_render_tier = table.Column<string>(type: "text", nullable: true),
                    arabic_render_source = table.Column<string>(type: "text", nullable: false),
                    root_buckwalter = table.Column<string>(type: "text", nullable: true),
                    lemma_buckwalter = table.Column<string>(type: "text", nullable: true),
                    features_raw = table.Column<string>(type: "text", nullable: false),
                    features_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_word_morphology_segments", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_segments_quran_pos_tags_pos",
                        column: x => x.pos,
                        principalTable: "quran_pos_tags",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_segments_quran_words_quran_word_id",
                        column: x => x.quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_lemmas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lemma_text = table.Column<string>(type: "text", nullable: false),
                    lemma_buckwalter = table.Column<string>(type: "text", nullable: true),
                    root_id = table.Column<int>(type: "integer", nullable: true),
                    words_count = table.Column<int>(type: "integer", nullable: false),
                    first_word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_lemmas", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_lemmas_quran_roots_root_id",
                        column: x => x.root_id,
                        principalTable: "quran_roots",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "quran_word_morphology",
                columns: table => new
                {
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "text", nullable: false),
                    head_pos = table.Column<string>(type: "text", nullable: false),
                    segment_count = table.Column<short>(type: "smallint", nullable: false),
                    root_id = table.Column<int>(type: "integer", nullable: true),
                    lemma_id = table.Column<int>(type: "integer", nullable: true),
                    stem_id = table.Column<int>(type: "integer", nullable: true),
                    is_verb = table.Column<bool>(type: "boolean", nullable: false),
                    verb_tense = table.Column<string>(type: "text", nullable: true),
                    verb_voice = table.Column<string>(type: "text", nullable: true),
                    case_feature = table.Column<string>(type: "text", nullable: true),
                    head_features_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_word_morphology", x => x.quran_word_id);
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_quran_lemmas_lemma_id",
                        column: x => x.lemma_id,
                        principalTable: "quran_lemmas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_quran_pos_tags_head_pos",
                        column: x => x.head_pos,
                        principalTable: "quran_pos_tags",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_quran_roots_root_id",
                        column: x => x.root_id,
                        principalTable: "quran_roots",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_quran_stems_stem_id",
                        column: x => x.stem_id,
                        principalTable: "quran_stems",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_quran_words_quran_word_id",
                        column: x => x.quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_lemmas_first_word_order_in_mushaf",
                table: "quran_lemmas",
                column: "first_word_order_in_mushaf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_lemmas_lemma_text",
                table: "quran_lemmas",
                column: "lemma_text",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_lemmas_root_id",
                table: "quran_lemmas",
                column: "root_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_pos_tags_category",
                table: "quran_pos_tags",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_quran_pos_tags_sort_order",
                table: "quran_pos_tags",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "IX_quran_roots_first_word_order_in_mushaf",
                table: "quran_roots",
                column: "first_word_order_in_mushaf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_roots_root_text",
                table: "quran_roots",
                column: "root_text",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_roots_words_count",
                table: "quran_roots",
                column: "words_count");

            migrationBuilder.CreateIndex(
                name: "IX_quran_stems_first_word_order_in_mushaf",
                table: "quran_stems",
                column: "first_word_order_in_mushaf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_stems_stem_text",
                table: "quran_stems",
                column: "stem_text",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_case_feature",
                table: "quran_word_morphology",
                column: "case_feature");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_head_pos",
                table: "quran_word_morphology",
                column: "head_pos");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_lemma_id",
                table: "quran_word_morphology",
                column: "lemma_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_quran_word_id",
                table: "quran_word_morphology",
                column: "quran_word_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_root_id",
                table: "quran_word_morphology",
                column: "root_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_stem_id",
                table: "quran_word_morphology",
                column: "stem_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_verb_tense",
                table: "quran_word_morphology",
                column: "verb_tense",
                filter: "is_verb");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_verb_voice",
                table: "quran_word_morphology",
                column: "verb_voice",
                filter: "is_verb");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_segments_arabic_render_tier",
                table: "quran_word_morphology_segments",
                column: "arabic_render_tier");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_segments_pos",
                table: "quran_word_morphology_segments",
                column: "pos");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_segments_quran_word_id_segment_number",
                table: "quran_word_morphology_segments",
                columns: new[] { "quran_word_id", "segment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_segments_stem",
                table: "quran_word_morphology_segments",
                column: "quran_word_id",
                filter: "kind = 'STEM'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quran_word_morphology");

            migrationBuilder.DropTable(
                name: "quran_word_morphology_segments");

            migrationBuilder.DropTable(
                name: "quran_lemmas");

            migrationBuilder.DropTable(
                name: "quran_stems");

            migrationBuilder.DropTable(
                name: "quran_pos_tags");

            migrationBuilder.DropTable(
                name: "quran_roots");
        }
    }
}
