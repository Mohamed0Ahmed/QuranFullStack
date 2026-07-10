using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{

    public partial class AddQuranNavigationMetadata : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "hizb_number",
                table: "quran_ayahs",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "juz_number",
                table: "quran_ayahs",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "rub_number",
                table: "quran_ayahs",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "quran_juzs",
                columns: table => new
                {
                    juz_number = table.Column<short>(type: "smallint", nullable: false),
                    verses_count = table.Column<short>(type: "smallint", nullable: false),
                    first_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    last_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    first_verse_key = table.Column<string>(type: "text", nullable: false),
                    last_verse_key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_juzs", x => x.juz_number);
                    table.ForeignKey(
                        name: "FK_quran_juzs_quran_ayahs_first_ayah_id",
                        column: x => x.first_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_juzs_quran_ayahs_last_ayah_id",
                        column: x => x.last_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quran_sajdas",
                columns: table => new
                {
                    sajdah_number = table.Column<short>(type: "smallint", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: false),
                    sajdah_type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_sajdas", x => x.sajdah_number);
                    table.ForeignKey(
                        name: "FK_quran_sajdas_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quran_hizbs",
                columns: table => new
                {
                    hizb_number = table.Column<short>(type: "smallint", nullable: false),
                    juz_number = table.Column<short>(type: "smallint", nullable: false),
                    verses_count = table.Column<short>(type: "smallint", nullable: false),
                    first_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    last_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    first_verse_key = table.Column<string>(type: "text", nullable: false),
                    last_verse_key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_hizbs", x => x.hizb_number);
                    table.ForeignKey(
                        name: "FK_quran_hizbs_quran_ayahs_first_ayah_id",
                        column: x => x.first_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_hizbs_quran_ayahs_last_ayah_id",
                        column: x => x.last_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_hizbs_quran_juzs_juz_number",
                        column: x => x.juz_number,
                        principalTable: "quran_juzs",
                        principalColumn: "juz_number",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quran_rubs",
                columns: table => new
                {
                    rub_number = table.Column<short>(type: "smallint", nullable: false),
                    hizb_number = table.Column<short>(type: "smallint", nullable: false),
                    verses_count = table.Column<short>(type: "smallint", nullable: false),
                    first_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    last_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    first_verse_key = table.Column<string>(type: "text", nullable: false),
                    last_verse_key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_rubs", x => x.rub_number);
                    table.ForeignKey(
                        name: "FK_quran_rubs_quran_ayahs_first_ayah_id",
                        column: x => x.first_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_rubs_quran_ayahs_last_ayah_id",
                        column: x => x.last_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_rubs_quran_hizbs_hizb_number",
                        column: x => x.hizb_number,
                        principalTable: "quran_hizbs",
                        principalColumn: "hizb_number",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quran_ayahs_hizb_number",
                table: "quran_ayahs",
                column: "hizb_number");

            migrationBuilder.CreateIndex(
                name: "IX_quran_ayahs_juz_number",
                table: "quran_ayahs",
                column: "juz_number");

            migrationBuilder.CreateIndex(
                name: "IX_quran_ayahs_rub_number",
                table: "quran_ayahs",
                column: "rub_number");

            migrationBuilder.CreateIndex(
                name: "IX_quran_hizbs_first_ayah_id",
                table: "quran_hizbs",
                column: "first_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_hizbs_juz_number",
                table: "quran_hizbs",
                column: "juz_number");

            migrationBuilder.CreateIndex(
                name: "IX_quran_hizbs_last_ayah_id",
                table: "quran_hizbs",
                column: "last_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_juzs_first_ayah_id",
                table: "quran_juzs",
                column: "first_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_juzs_last_ayah_id",
                table: "quran_juzs",
                column: "last_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_rubs_first_ayah_id",
                table: "quran_rubs",
                column: "first_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_rubs_hizb_number",
                table: "quran_rubs",
                column: "hizb_number");

            migrationBuilder.CreateIndex(
                name: "IX_quran_rubs_last_ayah_id",
                table: "quran_rubs",
                column: "last_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_sajdas_ayah_id",
                table: "quran_sajdas",
                column: "ayah_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_quran_ayahs_quran_hizbs_hizb_number",
                table: "quran_ayahs",
                column: "hizb_number",
                principalTable: "quran_hizbs",
                principalColumn: "hizb_number",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_quran_ayahs_quran_juzs_juz_number",
                table: "quran_ayahs",
                column: "juz_number",
                principalTable: "quran_juzs",
                principalColumn: "juz_number",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_quran_ayahs_quran_rubs_rub_number",
                table: "quran_ayahs",
                column: "rub_number",
                principalTable: "quran_rubs",
                principalColumn: "rub_number",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quran_ayahs_quran_hizbs_hizb_number",
                table: "quran_ayahs");

            migrationBuilder.DropForeignKey(
                name: "FK_quran_ayahs_quran_juzs_juz_number",
                table: "quran_ayahs");

            migrationBuilder.DropForeignKey(
                name: "FK_quran_ayahs_quran_rubs_rub_number",
                table: "quran_ayahs");

            migrationBuilder.DropTable(
                name: "quran_rubs");

            migrationBuilder.DropTable(
                name: "quran_sajdas");

            migrationBuilder.DropTable(
                name: "quran_hizbs");

            migrationBuilder.DropTable(
                name: "quran_juzs");

            migrationBuilder.DropIndex(
                name: "IX_quran_ayahs_hizb_number",
                table: "quran_ayahs");

            migrationBuilder.DropIndex(
                name: "IX_quran_ayahs_juz_number",
                table: "quran_ayahs");

            migrationBuilder.DropIndex(
                name: "IX_quran_ayahs_rub_number",
                table: "quran_ayahs");

            migrationBuilder.DropColumn(
                name: "hizb_number",
                table: "quran_ayahs");

            migrationBuilder.DropColumn(
                name: "juz_number",
                table: "quran_ayahs");

            migrationBuilder.DropColumn(
                name: "rub_number",
                table: "quran_ayahs");
        }
    }
}
