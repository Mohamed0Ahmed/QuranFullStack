using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{

    public partial class AddQuranMutashabihat : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quran_mutashabihat_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_group_id = table.Column<int>(type: "integer", nullable: false),
                    representative_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    representative_word_from = table.Column<short>(type: "smallint", nullable: false),
                    representative_word_to = table.Column<short>(type: "smallint", nullable: false),
                    occurrence_count = table.Column<short>(type: "smallint", nullable: false),
                    distinct_ayah_count = table.Column<short>(type: "smallint", nullable: false),
                    distinct_surah_count = table.Column<short>(type: "smallint", nullable: false),
                    raw_source_counts = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_mutashabihat_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_mutashabihat_groups_quran_ayahs_representative_ayah_id",
                        column: x => x.representative_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_similar_ayah_links",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    target_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<short>(type: "smallint", nullable: false),
                    coverage = table.Column<short>(type: "smallint", nullable: false),
                    matched_words_count = table.Column<short>(type: "smallint", nullable: false),
                    match_words = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_similar_ayah_links", x => x.id);
                    table.CheckConstraint("CK_quran_similar_ayah_links_no_self", "source_ayah_id <> target_ayah_id");
                    table.ForeignKey(
                        name: "FK_quran_similar_ayah_links_quran_ayahs_source_ayah_id",
                        column: x => x.source_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_similar_ayah_links_quran_ayahs_target_ayah_id",
                        column: x => x.target_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_mutashabihat_occurrences",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    word_from = table.Column<short>(type: "smallint", nullable: false),
                    word_to = table.Column<short>(type: "smallint", nullable: false),
                    is_representative = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_mutashabihat_occurrences", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_mutashabihat_occurrences_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_mutashabihat_occurrences_quran_mutashabihat_groups_gr~",
                        column: x => x.group_id,
                        principalTable: "quran_mutashabihat_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_mutashabihat_groups_representative_ayah_id",
                table: "quran_mutashabihat_groups",
                column: "representative_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_mutashabihat_groups_source_group_id",
                table: "quran_mutashabihat_groups",
                column: "source_group_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_mutashabihat_occurrences_ayah_id",
                table: "quran_mutashabihat_occurrences",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_mutashabihat_occurrences_group_id_ayah_id_word_from_w~",
                table: "quran_mutashabihat_occurrences",
                columns: new[] { "group_id", "ayah_id", "word_from", "word_to" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_similar_ayah_links_source_ayah_id_target_ayah_id",
                table: "quran_similar_ayah_links",
                columns: new[] { "source_ayah_id", "target_ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_similar_ayah_links_target_ayah_id",
                table: "quran_similar_ayah_links",
                column: "target_ayah_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quran_mutashabihat_occurrences");

            migrationBuilder.DropTable(
                name: "quran_similar_ayah_links");

            migrationBuilder.DropTable(
                name: "quran_mutashabihat_groups");
        }
    }
}
