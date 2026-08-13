using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QuranDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "abwab_sections",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_abwab_sections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "abwab_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                    table.PrimaryKey("PK_abwab_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    arabic_label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    english_description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                    table.CheckConstraint("ck_permissions_code_format", "code ~ '^[a-z0-9]+(\\.[a-z0-9_]+)+$'");
                });

            migrationBuilder.CreateTable(
                name: "quran_full_i3rab_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_key = table.Column<string>(type: "text", nullable: false),
                    display_name_ar = table.Column<string>(type: "text", nullable: false),
                    short_name_ar = table.Column<string>(type: "text", nullable: false),
                    display_name_en = table.Column<string>(type: "text", nullable: false),
                    short_name_en = table.Column<string>(type: "text", nullable: false),
                    language_code = table.Column<string>(type: "text", nullable: false),
                    direction = table.Column<string>(type: "text", nullable: false),
                    contributor_name_ar = table.Column<string>(type: "text", nullable: true),
                    contributor_name_en = table.Column<string>(type: "text", nullable: true),
                    resource_kind = table.Column<string>(type: "text", nullable: false),
                    markup_format = table.Column<string>(type: "text", nullable: false),
                    has_quran_quotation_markup = table.Column<bool>(type: "boolean", nullable: false),
                    content_coverage_count = table.Column<short>(type: "smallint", nullable: false),
                    package_file = table.Column<string>(type: "text", nullable: false),
                    source_file_original = table.Column<string>(type: "text", nullable: false),
                    sha256 = table.Column<string>(type: "text", nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    license_status = table.Column<string>(type: "text", nullable: false),
                    provenance_status = table.Column<string>(type: "text", nullable: false),
                    usage_scope = table.Column<string>(type: "text", nullable: false),
                    manifest_metadata = table.Column<string>(type: "jsonb", nullable: true),
                    imported_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_full_i3rab_sources", x => x.id);
                    table.CheckConstraint("CK_quran_full_i3rab_sources_content_coverage_count", "content_coverage_count = 6236");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_direction", "direction IN ('rtl', 'ltr')");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_language_code", "language_code = 'ar'");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_license_status", "license_status = 'unknown'");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_markup_format", "markup_format = 'html'");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_provenance_status", "provenance_status = 'unknown'");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_resource_kind", "resource_kind = 'full_i3rab'");
                    table.CheckConstraint("CK_quran_full_i3rab_sources_usage_scope", "usage_scope = 'internal-only-until-cleared'");
                });

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

            migrationBuilder.CreateTable(
                name: "quran_mushaf_pages",
                columns: table => new
                {
                    page_number = table.Column<short>(type: "smallint", nullable: false),
                    first_surah_number = table.Column<short>(type: "smallint", nullable: false),
                    first_ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    last_surah_number = table.Column<short>(type: "smallint", nullable: false),
                    last_ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    lines_count = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_mushaf_pages", x => x.page_number);
                });

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
                name: "quran_surahs",
                columns: table => new
                {
                    surah_number = table.Column<short>(type: "smallint", nullable: false),
                    name_arabic = table.Column<string>(type: "text", nullable: false),
                    name_simple = table.Column<string>(type: "text", nullable: false),
                    name_transliteration = table.Column<string>(type: "text", nullable: false),
                    revelation_place = table.Column<string>(type: "text", nullable: false),
                    revelation_order = table.Column<short>(type: "smallint", nullable: false),
                    verses_count = table.Column<short>(type: "smallint", nullable: false),
                    bismillah_pre = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_surahs", x => x.surah_number);
                });

            migrationBuilder.CreateTable(
                name: "quran_tafsir_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_key = table.Column<string>(type: "text", nullable: false),
                    language_code = table.Column<string>(type: "text", nullable: false),
                    language_name_ar = table.Column<string>(type: "text", nullable: false),
                    language_name_en = table.Column<string>(type: "text", nullable: false),
                    direction = table.Column<string>(type: "text", nullable: false),
                    display_name_ar = table.Column<string>(type: "text", nullable: false),
                    short_name_ar = table.Column<string>(type: "text", nullable: false),
                    display_name_en = table.Column<string>(type: "text", nullable: false),
                    short_name_en = table.Column<string>(type: "text", nullable: false),
                    contributor_key = table.Column<string>(type: "text", nullable: true),
                    contributor_name_ar = table.Column<string>(type: "text", nullable: true),
                    contributor_name_en = table.Column<string>(type: "text", nullable: true),
                    contributor_type = table.Column<string>(type: "text", nullable: false),
                    resource_kind = table.Column<string>(type: "text", nullable: false),
                    tafsir_kind = table.Column<string>(type: "text", nullable: false),
                    content_coverage_count = table.Column<short>(type: "smallint", nullable: false),
                    package_file = table.Column<string>(type: "text", nullable: false),
                    source_file_original = table.Column<string>(type: "text", nullable: false),
                    sha256 = table.Column<string>(type: "text", nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    license_status = table.Column<string>(type: "text", nullable: false),
                    provenance_status = table.Column<string>(type: "text", nullable: false),
                    manifest_metadata = table.Column<string>(type: "jsonb", nullable: true),
                    imported_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_tafsir_sources", x => x.id);
                    table.CheckConstraint("CK_quran_tafsir_sources_content_coverage_count", "content_coverage_count = 6236");
                    table.CheckConstraint("CK_quran_tafsir_sources_direction", "direction IN ('rtl', 'ltr')");
                    table.CheckConstraint("CK_quran_tafsir_sources_resource_kind", "resource_kind = 'tafsir'");
                });

            migrationBuilder.CreateTable(
                name: "quran_translation_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_key = table.Column<string>(type: "text", nullable: false),
                    language_code = table.Column<string>(type: "text", nullable: false),
                    language_name_en = table.Column<string>(type: "text", nullable: false),
                    language_name_ar = table.Column<string>(type: "text", nullable: false),
                    native_name = table.Column<string>(type: "text", nullable: true),
                    direction = table.Column<string>(type: "text", nullable: false),
                    translation_type = table.Column<string>(type: "text", nullable: false),
                    display_name_en = table.Column<string>(type: "text", nullable: false),
                    display_name_ar = table.Column<string>(type: "text", nullable: false),
                    translator_key = table.Column<string>(type: "text", nullable: true),
                    translator_name_en = table.Column<string>(type: "text", nullable: true),
                    translator_name_ar = table.Column<string>(type: "text", nullable: true),
                    contains_inline_footnotes = table.Column<bool>(type: "boolean", nullable: false),
                    contains_html_markup = table.Column<bool>(type: "boolean", nullable: false),
                    content_coverage_count = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_translation_sources", x => x.id);
                    table.CheckConstraint("CK_quran_translation_sources_content_coverage_count", "content_coverage_count = 6236");
                    table.CheckConstraint("CK_quran_translation_sources_direction", "direction IN ('rtl', 'ltr')");
                    table.CheckConstraint("CK_quran_translation_sources_required_fields", "btrim(source_key) <> '' AND\nbtrim(language_code) <> '' AND\nbtrim(language_name_en) <> '' AND\nbtrim(language_name_ar) <> '' AND\nbtrim(direction) <> '' AND\nbtrim(translation_type) <> '' AND\nbtrim(display_name_en) <> '' AND\nbtrim(display_name_ar) <> ''");
                    table.CheckConstraint("CK_quran_translation_sources_translation_type", "translation_type IN ('simple', 'with_footnotes')");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "abwab_doors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    section_id = table.Column<int>(type: "integer", nullable: false),
                    parent_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    representative_ayah_text = table.Column<string>(type: "text", nullable: true),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    global_order_value = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_abwab_doors", x => x.id);
                    table.ForeignKey(
                        name: "FK_abwab_doors_abwab_doors_parent_id",
                        column: x => x.parent_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_doors_abwab_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "abwab_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "abwab_template_nodes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_id = table.Column<int>(type: "integer", nullable: false),
                    parent_node_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    representative_ayah_text = table.Column<string>(type: "text", nullable: true),
                    aliases = table.Column<string[]>(type: "text[]", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_abwab_template_nodes", x => x.id);
                    table.ForeignKey(
                        name: "FK_abwab_template_nodes_abwab_template_nodes_parent_node_id",
                        column: x => x.parent_node_id,
                        principalTable: "abwab_template_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_abwab_template_nodes_abwab_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "abwab_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    logto_sub = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    normalized_email = table.Column<string>(type: "text", nullable: false),
                    user_name = table.Column<string>(type: "text", nullable: true),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    title = table.Column<string>(type: "text", nullable: true),
                    role_id = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "abwab_door_aliases",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    door_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<int>(type: "integer", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abwab_door_aliases", x => x.id);
                    table.ForeignKey(
                        name: "FK_abwab_door_aliases_abwab_doors_door_id",
                        column: x => x.door_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "access_audit_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    action_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    actor_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    actor_user_id = table.Column<int>(type: "integer", nullable: true),
                    target_user_id = table.Column<int>(type: "integer", nullable: false),
                    actor_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    target_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    permission_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    before_state = table.Column<string>(type: "jsonb", nullable: true),
                    after_state = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_audit_events", x => x.id);
                    table.CheckConstraint("ck_access_audit_events_documents_are_objects", "jsonb_typeof(actor_snapshot) = 'object'\nAND jsonb_typeof(target_snapshot) = 'object'\nAND (before_state IS NULL OR jsonb_typeof(before_state) = 'object')\nAND (after_state IS NULL OR jsonb_typeof(after_state) = 'object')");
                    table.CheckConstraint("ck_access_audit_events_metadata_schema_version", "jsonb_typeof(metadata) = 'object'\nAND jsonb_exists(metadata, 'schemaVersion')\nAND jsonb_typeof(metadata -> 'schemaVersion') = 'number'\nAND (metadata ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (metadata ->> 'schemaVersion')::numeric <= 2147483647");
                    table.ForeignKey(
                        name: "FK_access_audit_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_access_audit_events_users_target_user_id",
                        column: x => x.target_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_operations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    door_id = table.Column<int>(type: "integer", nullable: false),
                    actor_user_id = table.Column<int>(type: "integer", nullable: false),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_count = table.Column<int>(type: "integer", nullable: false),
                    ayah_count = table.Column<int>(type: "integer", nullable: false),
                    outcome = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_operations", x => x.id);
                    table.CheckConstraint("ck_linking_operations_outcome_schema_version", "jsonb_typeof(outcome) = 'object'\nAND jsonb_exists(outcome, 'schemaVersion')\nAND jsonb_typeof(outcome -> 'schemaVersion') = 'number'\nAND (outcome ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (outcome ->> 'schemaVersion')::numeric <= 2147483647");
                    table.ForeignKey(
                        name: "FK_linking_operations_abwab_doors_door_id",
                        column: x => x.door_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_operations_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_units",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    door_id = table.Column<int>(type: "integer", nullable: false),
                    identity = table.Column<string>(type: "text", nullable: false),
                    identity_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    is_grouped = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_units", x => x.id);
                    table.ForeignKey(
                        name: "FK_linking_units_abwab_doors_door_id",
                        column: x => x.door_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_units_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_workspaces",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspaces", x => x.id);
                    table.ForeignKey(
                        name: "FK_linking_workspaces_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspaces_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspaces_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false),
                    granted_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_permissions", x => new { x.user_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_user_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_permissions_users_granted_by_user_id",
                        column: x => x.granted_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_door_ayah_words",
                columns: table => new
                {
                    door_ayah_id = table.Column<long>(type: "bigint", nullable: false),
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_door_ayah_words", x => new { x.door_ayah_id, x.quran_word_id });
                    table.ForeignKey(
                        name: "FK_linking_door_ayah_words_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_door_ayahs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    door_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_door_ayahs", x => x.id);
                    table.ForeignKey(
                        name: "FK_linking_door_ayahs_abwab_doors_door_id",
                        column: x => x.door_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_door_ayahs_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_source_contribution_units",
                columns: table => new
                {
                    source_contribution_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_id = table.Column<long>(type: "bigint", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_source_contribution_units", x => new { x.source_contribution_id, x.unit_id });
                    table.ForeignKey(
                        name: "FK_linking_source_contribution_units_linking_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "linking_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_source_contributions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    operation_id = table.Column<long>(type: "bigint", nullable: false),
                    door_id = table.Column<int>(type: "integer", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    contribution_mode = table.Column<string>(type: "text", nullable: false),
                    source_kind = table.Column<string>(type: "text", nullable: false),
                    source_identity = table.Column<string>(type: "text", nullable: false),
                    source_identity_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<string>(type: "jsonb", nullable: false),
                    root_id = table.Column<int>(type: "integer", nullable: true),
                    lemma_id = table.Column<int>(type: "integer", nullable: true),
                    stem_id = table.Column<int>(type: "integer", nullable: true),
                    unique_simple_word_id = table.Column<int>(type: "integer", nullable: true),
                    unique_tashkeel_word_id = table.Column<int>(type: "integer", nullable: true),
                    word_type_tashkeel_word_id = table.Column<int>(type: "integer", nullable: true),
                    resolved_ayah_count = table.Column<int>(type: "integer", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_source_contributions", x => x.id);
                    table.UniqueConstraint("AK_linking_source_contributions_id_door_id", x => new { x.id, x.door_id });
                    table.CheckConstraint("ck_linking_source_contributions_contribution_mode", "contribution_mode IN ('automatic', 'manual_single', 'manual_independent', 'manual_grouped')");
                    table.CheckConstraint("ck_linking_source_contributions_kind_reference_coherence", "(source_kind = 'root'\n    AND root_id IS NOT NULL\n    AND num_nonnulls(lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'lemma'\n    AND lemma_id IS NOT NULL\n    AND num_nonnulls(root_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'stem'\n    AND stem_id IS NOT NULL\n    AND num_nonnulls(root_id, lemma_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'unique_word'\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 1\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'word_type'\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 1\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 0)\nOR (source_kind = 'manual_mushaf_ayahs'\n    AND num_nonnulls(root_id, lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)");
                    table.CheckConstraint("ck_linking_source_contributions_manual_mode_coherence", "(source_kind = 'manual_mushaf_ayahs'\n    AND contribution_mode IN ('manual_single', 'manual_independent', 'manual_grouped'))\nOR (source_kind <> 'manual_mushaf_ayahs'\n    AND contribution_mode = 'automatic')");
                    table.CheckConstraint("ck_linking_source_contributions_scope_schema_version", "jsonb_typeof(scope) = 'object'\nAND jsonb_exists(scope, 'schemaVersion')\nAND jsonb_typeof(scope -> 'schemaVersion') = 'number'\nAND (scope ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (scope ->> 'schemaVersion')::numeric <= 2147483647");
                    table.CheckConstraint("ck_linking_source_contributions_source_kind", "source_kind IN ('unique_word', 'root', 'lemma', 'stem', 'word_type', 'manual_mushaf_ayahs')");
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_abwab_doors_door_id",
                        column: x => x.door_id,
                        principalTable: "abwab_doors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_linking_operations_operation_id",
                        column: x => x.operation_id,
                        principalTable: "linking_operations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_quran_lemmas_lemma_id",
                        column: x => x.lemma_id,
                        principalTable: "quran_lemmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_quran_roots_root_id",
                        column: x => x.root_id,
                        principalTable: "quran_roots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_quran_stems_stem_id",
                        column: x => x.stem_id,
                        principalTable: "quran_stems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_users_deleted_by",
                        column: x => x.deleted_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_source_contributions_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_unit_ayah_descriptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    unit_ayah_id = table.Column<long>(type: "bigint", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_unit_ayah_descriptions", x => x.id);
                    table.CheckConstraint("ck_linking_unit_ayah_descriptions_body_not_blank", "btrim(body) <> ''");
                    table.CheckConstraint("ck_linking_unit_ayah_descriptions_order_value", "order_value BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_linking_unit_ayah_descriptions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_unit_ayah_descriptions_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_unit_ayah_words",
                columns: table => new
                {
                    unit_ayah_id = table.Column<long>(type: "bigint", nullable: false),
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_unit_ayah_words", x => new { x.unit_ayah_id, x.quran_word_id });
                });

            migrationBuilder.CreateTable(
                name: "linking_unit_ayahs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    unit_id = table.Column<long>(type: "bigint", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_unit_ayahs", x => x.id);
                    table.ForeignKey(
                        name: "FK_linking_unit_ayahs_linking_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "linking_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_workspace_source_ayah_overrides",
                columns: table => new
                {
                    workspace_source_id = table.Column<long>(type: "bigint", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspace_source_ayah_overrides", x => new { x.workspace_source_id, x.ayah_id });
                });

            migrationBuilder.CreateTable(
                name: "linking_workspace_source_descriptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workspace_source_id = table.Column<long>(type: "bigint", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspace_source_descriptions", x => x.id);
                    table.CheckConstraint("ck_linking_workspace_source_descriptions_body_not_blank", "btrim(body) <> ''");
                    table.CheckConstraint("ck_linking_workspace_source_descriptions_order_value", "order_value BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_descriptions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_source_descriptions_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linking_workspace_source_manual_ayahs",
                columns: table => new
                {
                    workspace_source_id = table.Column<long>(type: "bigint", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    page_hint = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspace_source_manual_ayahs", x => new { x.workspace_source_id, x.ayah_id });
                });

            migrationBuilder.CreateTable(
                name: "linking_workspace_source_words",
                columns: table => new
                {
                    workspace_source_id = table.Column<long>(type: "bigint", nullable: false),
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspace_source_words", x => new { x.workspace_source_id, x.quran_word_id });
                });

            migrationBuilder.CreateTable(
                name: "linking_workspace_sources",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workspace_id = table.Column<long>(type: "bigint", nullable: false),
                    order_value = table.Column<int>(type: "integer", nullable: false),
                    source_kind = table.Column<string>(type: "text", nullable: false),
                    source_identity = table.Column<string>(type: "text", nullable: false),
                    source_identity_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<string>(type: "jsonb", nullable: false),
                    root_id = table.Column<int>(type: "integer", nullable: true),
                    lemma_id = table.Column<int>(type: "integer", nullable: true),
                    stem_id = table.Column<int>(type: "integer", nullable: true),
                    unique_simple_word_id = table.Column<int>(type: "integer", nullable: true),
                    unique_tashkeel_word_id = table.Column<int>(type: "integer", nullable: true),
                    word_type_tashkeel_word_id = table.Column<int>(type: "integer", nullable: true),
                    inclusion_mode = table.Column<string>(type: "text", nullable: false),
                    automatic_word_matches_enabled = table.Column<bool>(type: "boolean", nullable: true),
                    manual_link_shape = table.Column<string>(type: "text", nullable: true),
                    last_resolved_count = table.Column<int>(type: "integer", nullable: true),
                    last_resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linking_workspace_sources", x => x.id);
                    table.CheckConstraint("ck_linking_workspace_sources_inclusion_mode", "inclusion_mode IN ('all_except', 'only')");
                    table.CheckConstraint("ck_linking_workspace_sources_kind_configuration_coherence", "(source_kind = 'manual_mushaf_ayahs'\n    AND automatic_word_matches_enabled IS NULL\n    AND manual_link_shape IS NOT NULL)\nOR (source_kind <> 'manual_mushaf_ayahs'\n    AND automatic_word_matches_enabled IS NOT NULL\n    AND manual_link_shape IS NULL)");
                    table.CheckConstraint("ck_linking_workspace_sources_kind_reference_coherence", "(source_kind = 'root'\n    AND root_id IS NOT NULL\n    AND num_nonnulls(lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'lemma'\n    AND lemma_id IS NOT NULL\n    AND num_nonnulls(root_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'stem'\n    AND stem_id IS NOT NULL\n    AND num_nonnulls(root_id, lemma_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'unique_word'\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 1\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 0)\nOR (source_kind = 'word_type'\n    AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 1\n    AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 0)\nOR (source_kind = 'manual_mushaf_ayahs'\n    AND num_nonnulls(root_id, lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)");
                    table.CheckConstraint("ck_linking_workspace_sources_manual_link_shape", "manual_link_shape IS NULL OR manual_link_shape IN ('grouped', 'independent')");
                    table.CheckConstraint("ck_linking_workspace_sources_scope_schema_version", "jsonb_typeof(scope) = 'object'\nAND jsonb_exists(scope, 'schemaVersion')\nAND jsonb_typeof(scope -> 'schemaVersion') = 'number'\nAND (scope ->> 'schemaVersion') ~ '^[1-9][0-9]*$'\nAND (scope ->> 'schemaVersion')::numeric <= 2147483647");
                    table.CheckConstraint("ck_linking_workspace_sources_source_kind", "source_kind IN ('unique_word', 'root', 'lemma', 'stem', 'word_type', 'manual_mushaf_ayahs')");
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_linking_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "linking_workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_quran_lemmas_lemma_id",
                        column: x => x.lemma_id,
                        principalTable: "quran_lemmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_quran_roots_root_id",
                        column: x => x.root_id,
                        principalTable: "quran_roots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_quran_stems_stem_id",
                        column: x => x.stem_id,
                        principalTable: "quran_stems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linking_workspace_sources_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quran_ayahs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    surah_number = table.Column<short>(type: "smallint", nullable: false),
                    ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: false),
                    text_uthmani = table.Column<string>(type: "text", nullable: false),
                    words_count_source = table.Column<short>(type: "smallint", nullable: false),
                    words_count_real = table.Column<short>(type: "smallint", nullable: false),
                    page_from = table.Column<short>(type: "smallint", nullable: false),
                    page_to = table.Column<short>(type: "smallint", nullable: false),
                    juz_number = table.Column<short>(type: "smallint", nullable: true),
                    hizb_number = table.Column<short>(type: "smallint", nullable: true),
                    rub_number = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_ayahs", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_ayahs_quran_surahs_surah_number",
                        column: x => x.surah_number,
                        principalTable: "quran_surahs",
                        principalColumn: "surah_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_full_i3rab_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    source_entry_key = table.Column<string>(type: "text", nullable: false),
                    leader_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    i3rab_html = table.Column<string>(type: "text", nullable: false),
                    covered_ayah_count = table.Column<short>(type: "smallint", nullable: false),
                    covered_ayah_keys = table.Column<string>(type: "jsonb", nullable: false),
                    source_shape = table.Column<string>(type: "text", nullable: false),
                    text_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_full_i3rab_entries", x => x.id);
                    table.CheckConstraint("CK_quran_full_i3rab_entries_covered_ayah_count", "covered_ayah_count >= 1");
                    table.CheckConstraint("CK_quran_full_i3rab_entries_i3rab_html", "i3rab_html <> ''");
                    table.CheckConstraint("CK_quran_full_i3rab_entries_source_shape", "source_shape IN ('grouped_leader', 'flat')");
                    table.ForeignKey(
                        name: "FK_quran_full_i3rab_entries_quran_ayahs_leader_ayah_id",
                        column: x => x.leader_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_full_i3rab_entries_quran_full_i3rab_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "quran_full_i3rab_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "quran_tafsir_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    source_entry_key = table.Column<string>(type: "text", nullable: false),
                    leader_ayah_id = table.Column<int>(type: "integer", nullable: false),
                    tafsir_text = table.Column<string>(type: "text", nullable: false),
                    covered_ayah_count = table.Column<short>(type: "smallint", nullable: false),
                    covered_ayah_keys = table.Column<string>(type: "jsonb", nullable: false),
                    source_shape = table.Column<string>(type: "text", nullable: false),
                    text_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_tafsir_entries", x => x.id);
                    table.CheckConstraint("CK_quran_tafsir_entries_covered_ayah_count", "covered_ayah_count >= 1");
                    table.CheckConstraint("CK_quran_tafsir_entries_source_shape", "source_shape IN ('grouped_leader', 'flat')");
                    table.CheckConstraint("CK_quran_tafsir_entries_tafsir_text", "tafsir_text <> ''");
                    table.ForeignKey(
                        name: "FK_quran_tafsir_entries_quran_ayahs_leader_ayah_id",
                        column: x => x.leader_ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_tafsir_entries_quran_tafsir_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "quran_tafsir_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_translation_ayah_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: true),
                    text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_translation_ayah_entries", x => x.id);
                    table.CheckConstraint("CK_quran_translation_ayah_entries_text", "text <> ''");
                    table.ForeignKey(
                        name: "FK_quran_translation_ayah_entries_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_translation_ayah_entries_quran_translation_sources_so~",
                        column: x => x.source_id,
                        principalTable: "quran_translation_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_words",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "text", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    surah_number = table.Column<short>(type: "smallint", nullable: false),
                    ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    word_number = table.Column<short>(type: "smallint", nullable: false),
                    page_number = table.Column<short>(type: "smallint", nullable: false),
                    line_number = table.Column<short>(type: "smallint", nullable: false),
                    line_word_order = table.Column<short>(type: "smallint", nullable: false),
                    qpc_glyph = table.Column<string>(type: "text", nullable: false),
                    text_uthmani = table.Column<string>(type: "text", nullable: false),
                    text_uthmani_simple = table.Column<string>(type: "text", nullable: false),
                    text_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    word_key_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    is_ayah_marker = table.Column<bool>(type: "boolean", nullable: false),
                    unique_tashkeel_word_id = table.Column<int>(type: "integer", nullable: true),
                    unique_simple_word_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_words", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_words_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_words_quran_mushaf_pages_page_number",
                        column: x => x.page_number,
                        principalTable: "quran_mushaf_pages",
                        principalColumn: "page_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_full_i3rab_ayah_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    entry_id = table.Column<long>(type: "bigint", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: false),
                    source_value_kind = table.Column<string>(type: "text", nullable: false),
                    source_leader_verse_key = table.Column<string>(type: "text", nullable: false),
                    is_group_leader = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_full_i3rab_ayah_entries", x => x.id);
                    table.CheckConstraint("CK_quran_full_i3rab_ayah_entries_source_value_kind", "source_value_kind IN ('leader', 'member_pointer', 'flat')");
                    table.ForeignKey(
                        name: "FK_quran_full_i3rab_ayah_entries_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_full_i3rab_ayah_entries_quran_full_i3rab_entries_entr~",
                        column: x => x.entry_id,
                        principalTable: "quran_full_i3rab_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_full_i3rab_ayah_entries_quran_full_i3rab_sources_sour~",
                        column: x => x.source_id,
                        principalTable: "quran_full_i3rab_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "quran_tafsir_ayah_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<int>(type: "integer", nullable: false),
                    ayah_id = table.Column<int>(type: "integer", nullable: false),
                    tafsir_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: false),
                    source_value_kind = table.Column<string>(type: "text", nullable: false),
                    source_leader_verse_key = table.Column<string>(type: "text", nullable: false),
                    is_group_leader = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_tafsir_ayah_entries", x => x.id);
                    table.CheckConstraint("CK_quran_tafsir_ayah_entries_source_value_kind", "source_value_kind IN ('leader', 'member_pointer', 'flat')");
                    table.ForeignKey(
                        name: "FK_quran_tafsir_ayah_entries_quran_ayahs_ayah_id",
                        column: x => x.ayah_id,
                        principalTable: "quran_ayahs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_tafsir_ayah_entries_quran_tafsir_entries_tafsir_entry~",
                        column: x => x.tafsir_entry_id,
                        principalTable: "quran_tafsir_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_tafsir_ayah_entries_quran_tafsir_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "quran_tafsir_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_mushaf_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    page_number = table.Column<short>(type: "smallint", nullable: false),
                    line_number = table.Column<short>(type: "smallint", nullable: false),
                    line_type = table.Column<string>(type: "text", nullable: false),
                    is_centered = table.Column<bool>(type: "boolean", nullable: false),
                    surah_number = table.Column<short>(type: "smallint", nullable: true),
                    first_word_id = table.Column<int>(type: "integer", nullable: true),
                    last_word_id = table.Column<int>(type: "integer", nullable: true),
                    words_count = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_mushaf_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_mushaf_lines_quran_mushaf_pages_page_number",
                        column: x => x.page_number,
                        principalTable: "quran_mushaf_pages",
                        principalColumn: "page_number",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quran_mushaf_lines_quran_surahs_surah_number",
                        column: x => x.surah_number,
                        principalTable: "quran_surahs",
                        principalColumn: "surah_number");
                    table.ForeignKey(
                        name: "FK_quran_mushaf_lines_quran_words_first_word_id",
                        column: x => x.first_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_quran_mushaf_lines_quran_words_last_word_id",
                        column: x => x.last_word_id,
                        principalTable: "quran_words",
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
                    root_id = table.Column<int>(type: "integer", nullable: true),
                    lemma_id = table.Column<int>(type: "integer", nullable: true),
                    stem_id = table.Column<int>(type: "integer", nullable: true),
                    features_raw = table.Column<string>(type: "text", nullable: false),
                    features_json = table.Column<string>(type: "jsonb", nullable: true),
                    i3rab_arabic = table.Column<string>(type: "text", nullable: true),
                    i3rab_rule_id = table.Column<int>(type: "integer", nullable: true),
                    i3rab_status = table.Column<string>(type: "text", nullable: false, defaultValue: "unsupported"),
                    i3rab_review_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_word_morphology_segments", x => x.id);
                    table.CheckConstraint("CK_quran_word_morphology_segments_i3rab_status", "i3rab_status IN ('approved', 'needs_review', 'unsupported')");
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_segments_quran_i3rab_rules_i3rab_rule~",
                        column: x => x.i3rab_rule_id,
                        principalTable: "quran_i3rab_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_segments_quran_lemmas_lemma_id",
                        column: x => x.lemma_id,
                        principalTable: "quran_lemmas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_segments_quran_pos_tags_pos",
                        column: x => x.pos,
                        principalTable: "quran_pos_tags",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_segments_quran_roots_root_id",
                        column: x => x.root_id,
                        principalTable: "quran_roots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_segments_quran_stems_stem_id",
                        column: x => x.stem_id,
                        principalTable: "quran_stems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quran_word_morphology_segments_quran_words_quran_word_id",
                        column: x => x.quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_words_ordered_simple",
                columns: table => new
                {
                    word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false),
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "text", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: false),
                    surah_number = table.Column<short>(type: "smallint", nullable: false),
                    ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    page_number = table.Column<short>(type: "smallint", nullable: false),
                    line_number = table.Column<short>(type: "smallint", nullable: false),
                    word_order_in_ayah = table.Column<short>(type: "smallint", nullable: false),
                    word_order_in_surah = table.Column<short>(type: "smallint", nullable: false),
                    word_key_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    text_uthmani_simple = table.Column<string>(type: "text", nullable: false),
                    text_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    occurrences_count = table.Column<int>(type: "integer", nullable: false),
                    ayahs_count = table.Column<short>(type: "smallint", nullable: false),
                    surahs_count = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_words_ordered_simple", x => x.word_order_in_mushaf);
                    table.ForeignKey(
                        name: "FK_quran_words_ordered_simple_quran_words_quran_word_id",
                        column: x => x.quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_words_ordered_tashkeel",
                columns: table => new
                {
                    word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false),
                    quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "text", nullable: false),
                    verse_key = table.Column<string>(type: "text", nullable: false),
                    surah_number = table.Column<short>(type: "smallint", nullable: false),
                    ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    page_number = table.Column<short>(type: "smallint", nullable: false),
                    line_number = table.Column<short>(type: "smallint", nullable: false),
                    word_order_in_ayah = table.Column<short>(type: "smallint", nullable: false),
                    word_order_in_surah = table.Column<short>(type: "smallint", nullable: false),
                    text_uthmani = table.Column<string>(type: "text", nullable: false),
                    text_uthmani_simple = table.Column<string>(type: "text", nullable: false),
                    text_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    occurrences_count = table.Column<int>(type: "integer", nullable: false),
                    ayahs_count = table.Column<short>(type: "smallint", nullable: false),
                    surahs_count = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_words_ordered_tashkeel", x => x.word_order_in_mushaf);
                    table.ForeignKey(
                        name: "FK_quran_words_ordered_tashkeel_quran_words_quran_word_id",
                        column: x => x.quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_words_unique_simple",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    word_key_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    text_uthmani = table.Column<string>(type: "text", nullable: false),
                    text_uthmani_simple = table.Column<string>(type: "text", nullable: false),
                    text_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    qpc_glyph = table.Column<string>(type: "text", nullable: false),
                    occurrences_count = table.Column<int>(type: "integer", nullable: false),
                    ayahs_count = table.Column<short>(type: "smallint", nullable: false),
                    surahs_count = table.Column<short>(type: "smallint", nullable: false),
                    first_quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    first_location = table.Column<string>(type: "text", nullable: false),
                    first_surah_number = table.Column<short>(type: "smallint", nullable: false),
                    first_ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    first_word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false),
                    first_page_number = table.Column<short>(type: "smallint", nullable: false),
                    first_line_number = table.Column<short>(type: "smallint", nullable: false),
                    search_text_normalized = table.Column<string>(type: "text", nullable: true, computedColumnSql: "translate(lower(text_uthmani_simple || ' ' || text_imlaei_simple || ' ' || word_key_imlaei_simple), 'أإآٱؤئةىي', 'ااااواهيي')", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_words_unique_simple", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_words_unique_simple_quran_words_first_quran_word_id",
                        column: x => x.first_quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quran_words_unique_tashkeel",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    text_uthmani = table.Column<string>(type: "text", nullable: false),
                    text_uthmani_simple = table.Column<string>(type: "text", nullable: false),
                    text_imlaei_simple = table.Column<string>(type: "text", nullable: false),
                    occurrences_count = table.Column<int>(type: "integer", nullable: false),
                    ayahs_count = table.Column<short>(type: "smallint", nullable: false),
                    surahs_count = table.Column<short>(type: "smallint", nullable: false),
                    first_quran_word_id = table.Column<int>(type: "integer", nullable: false),
                    first_location = table.Column<string>(type: "text", nullable: false),
                    first_surah_number = table.Column<short>(type: "smallint", nullable: false),
                    first_ayah_number = table.Column<short>(type: "smallint", nullable: false),
                    first_word_order_in_mushaf = table.Column<int>(type: "integer", nullable: false),
                    first_page_number = table.Column<short>(type: "smallint", nullable: false),
                    first_line_number = table.Column<short>(type: "smallint", nullable: false),
                    search_text_normalized = table.Column<string>(type: "text", nullable: true, computedColumnSql: "translate(lower(text_uthmani_simple || ' ' || text_imlaei_simple), 'أإآٱؤئةىي', 'ااااواهيي')", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quran_words_unique_tashkeel", x => x.id);
                    table.ForeignKey(
                        name: "FK_quran_words_unique_tashkeel_quran_words_first_quran_word_id",
                        column: x => x.first_quran_word_id,
                        principalTable: "quran_words",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "display_name", "name" },
                values: new object[] { 1, "المالك", "Owner" });

            migrationBuilder.CreateIndex(
                name: "IX_abwab_door_aliases_door_id",
                table: "abwab_door_aliases",
                column: "door_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_abwab_doors_deleted_at",
                table: "abwab_doors",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_doors_global_order_value",
                table: "abwab_doors",
                column: "global_order_value",
                filter: "parent_id IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_doors_parent_id",
                table: "abwab_doors",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_doors_section_id_parent_id_name",
                table: "abwab_doors",
                columns: new[] { "section_id", "parent_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_abwab_doors_section_id_parent_id_order_value",
                table: "abwab_doors",
                columns: new[] { "section_id", "parent_id", "order_value" });

            migrationBuilder.CreateIndex(
                name: "IX_abwab_sections_deleted_at",
                table: "abwab_sections",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_sections_name",
                table: "abwab_sections",
                column: "name",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_template_nodes_deleted_at",
                table: "abwab_template_nodes",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_template_nodes_parent_node_id",
                table: "abwab_template_nodes",
                column: "parent_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_template_nodes_template_id",
                table: "abwab_template_nodes",
                column: "template_id",
                unique: true,
                filter: "parent_node_id IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_abwab_template_nodes_template_id_parent_node_id_name",
                table: "abwab_template_nodes",
                columns: new[] { "template_id", "parent_node_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_abwab_template_nodes_template_id_parent_node_id_order_value",
                table: "abwab_template_nodes",
                columns: new[] { "template_id", "parent_node_id", "order_value" });

            migrationBuilder.CreateIndex(
                name: "IX_abwab_templates_deleted_at",
                table: "abwab_templates",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_access_audit_events_action_type_occurred_at",
                table: "access_audit_events",
                columns: new[] { "action_type", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_access_audit_events_actor_user_id",
                table: "access_audit_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_access_audit_events_occurred_at_id",
                table: "access_audit_events",
                columns: new[] { "occurred_at", "id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_access_audit_events_permission_code",
                table: "access_audit_events",
                column: "permission_code",
                filter: "permission_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_access_audit_events_target_user_id_occurred_at_id",
                table: "access_audit_events",
                columns: new[] { "target_user_id", "occurred_at", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_linking_door_ayah_words_ayah_id",
                table: "linking_door_ayah_words",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_door_ayah_words_created_by",
                table: "linking_door_ayah_words",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_door_ayah_words_quran_word_id",
                table: "linking_door_ayah_words",
                column: "quran_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_door_ayahs_ayah_id",
                table: "linking_door_ayahs",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_door_ayahs_created_by",
                table: "linking_door_ayahs",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_door_ayahs_door_id_ayah_id",
                table: "linking_door_ayahs",
                columns: new[] { "door_id", "ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_operations_actor_user_id",
                table: "linking_operations",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_operations_door_id_confirmed_at",
                table: "linking_operations",
                columns: new[] { "door_id", "confirmed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_linking_operations_idempotency_key",
                table: "linking_operations",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contribution_units_source_contribution_id_or~",
                table: "linking_source_contribution_units",
                columns: new[] { "source_contribution_id", "order_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contribution_units_unit_id",
                table: "linking_source_contribution_units",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_created_by",
                table: "linking_source_contributions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_deleted_by",
                table: "linking_source_contributions",
                column: "deleted_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_door_id",
                table: "linking_source_contributions",
                column: "door_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_door_id_source_identity_hash",
                table: "linking_source_contributions",
                columns: new[] { "door_id", "source_identity_hash" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_lemma_id",
                table: "linking_source_contributions",
                column: "lemma_id",
                filter: "lemma_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_operation_id_order_value",
                table: "linking_source_contributions",
                columns: new[] { "operation_id", "order_value" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_root_id",
                table: "linking_source_contributions",
                column: "root_id",
                filter: "root_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_stem_id",
                table: "linking_source_contributions",
                column: "stem_id",
                filter: "stem_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_unique_simple_word_id",
                table: "linking_source_contributions",
                column: "unique_simple_word_id",
                filter: "unique_simple_word_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_unique_tashkeel_word_id",
                table: "linking_source_contributions",
                column: "unique_tashkeel_word_id",
                filter: "unique_tashkeel_word_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_updated_by",
                table: "linking_source_contributions",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_source_contributions_word_type_tashkeel_word_id",
                table: "linking_source_contributions",
                column: "word_type_tashkeel_word_id",
                filter: "word_type_tashkeel_word_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayah_descriptions_created_by",
                table: "linking_unit_ayah_descriptions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayah_descriptions_unit_ayah_id_order_value",
                table: "linking_unit_ayah_descriptions",
                columns: new[] { "unit_ayah_id", "order_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayah_descriptions_updated_by",
                table: "linking_unit_ayah_descriptions",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayah_words_ayah_id",
                table: "linking_unit_ayah_words",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayah_words_quran_word_id",
                table: "linking_unit_ayah_words",
                column: "quran_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayahs_ayah_id",
                table: "linking_unit_ayahs",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayahs_unit_id_ayah_id",
                table: "linking_unit_ayahs",
                columns: new[] { "unit_id", "ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_unit_ayahs_unit_id_order_value",
                table: "linking_unit_ayahs",
                columns: new[] { "unit_id", "order_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_units_created_by",
                table: "linking_units",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_units_door_id_identity_hash",
                table: "linking_units",
                columns: new[] { "door_id", "identity_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_ayah_overrides_ayah_id",
                table: "linking_workspace_source_ayah_overrides",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_descriptions_ayah_id",
                table: "linking_workspace_source_descriptions",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_descriptions_created_by",
                table: "linking_workspace_source_descriptions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_descriptions_updated_by",
                table: "linking_workspace_source_descriptions",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_descriptions_workspace_source_id_a~",
                table: "linking_workspace_source_descriptions",
                columns: new[] { "workspace_source_id", "ayah_id", "order_value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_manual_ayahs_ayah_id",
                table: "linking_workspace_source_manual_ayahs",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_manual_ayahs_workspace_source_id_o~",
                table: "linking_workspace_source_manual_ayahs",
                columns: new[] { "workspace_source_id", "order_value" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_words_ayah_id",
                table: "linking_workspace_source_words",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_words_quran_word_id",
                table: "linking_workspace_source_words",
                column: "quran_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_source_words_workspace_source_id_ayah_id",
                table: "linking_workspace_source_words",
                columns: new[] { "workspace_source_id", "ayah_id" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_created_by",
                table: "linking_workspace_sources",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_lemma_id",
                table: "linking_workspace_sources",
                column: "lemma_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_root_id",
                table: "linking_workspace_sources",
                column: "root_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_stem_id",
                table: "linking_workspace_sources",
                column: "stem_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_unique_simple_word_id",
                table: "linking_workspace_sources",
                column: "unique_simple_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_unique_tashkeel_word_id",
                table: "linking_workspace_sources",
                column: "unique_tashkeel_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_updated_by",
                table: "linking_workspace_sources",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_word_type_tashkeel_word_id",
                table: "linking_workspace_sources",
                column: "word_type_tashkeel_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_workspace_id_order_value",
                table: "linking_workspace_sources",
                columns: new[] { "workspace_id", "order_value" });

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspace_sources_workspace_id_source_identity_hash",
                table: "linking_workspace_sources",
                columns: new[] { "workspace_id", "source_identity_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspaces_created_by",
                table: "linking_workspaces",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspaces_updated_by",
                table: "linking_workspaces",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_linking_workspaces_user_id",
                table: "linking_workspaces",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_display_order",
                table: "permissions",
                column: "display_order");

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
                name: "IX_quran_ayahs_surah_number_ayah_number",
                table: "quran_ayahs",
                columns: new[] { "surah_number", "ayah_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_ayahs_verse_key",
                table: "quran_ayahs",
                column: "verse_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_ayah_entries_ayah_id_source_id",
                table: "quran_full_i3rab_ayah_entries",
                columns: new[] { "ayah_id", "source_id" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_ayah_entries_entry_id",
                table: "quran_full_i3rab_ayah_entries",
                column: "entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_ayah_entries_source_id_ayah_id",
                table: "quran_full_i3rab_ayah_entries",
                columns: new[] { "source_id", "ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_ayah_entries_source_id_verse_key",
                table: "quran_full_i3rab_ayah_entries",
                columns: new[] { "source_id", "verse_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_entries_leader_ayah_id",
                table: "quran_full_i3rab_entries",
                column: "leader_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_entries_source_id_leader_ayah_id",
                table: "quran_full_i3rab_entries",
                columns: new[] { "source_id", "leader_ayah_id" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_entries_source_id_source_entry_key",
                table: "quran_full_i3rab_entries",
                columns: new[] { "source_id", "source_entry_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_sources_package_file",
                table: "quran_full_i3rab_sources",
                column: "package_file",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_full_i3rab_sources_source_key",
                table: "quran_full_i3rab_sources",
                column: "source_key",
                unique: true);

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
                name: "IX_quran_i3rab_rules_rule_family",
                table: "quran_i3rab_rules",
                column: "rule_family");

            migrationBuilder.CreateIndex(
                name: "IX_quran_i3rab_rules_signature_key",
                table: "quran_i3rab_rules",
                column: "signature_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_juzs_first_ayah_id",
                table: "quran_juzs",
                column: "first_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_juzs_last_ayah_id",
                table: "quran_juzs",
                column: "last_ayah_id");

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
                name: "IX_quran_mushaf_lines_first_word_id",
                table: "quran_mushaf_lines",
                column: "first_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_mushaf_lines_last_word_id",
                table: "quran_mushaf_lines",
                column: "last_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_mushaf_lines_page_number_line_number",
                table: "quran_mushaf_lines",
                columns: new[] { "page_number", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_mushaf_lines_surah_number",
                table: "quran_mushaf_lines",
                column: "surah_number");

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

            migrationBuilder.CreateIndex(
                name: "IX_quran_similar_ayah_links_source_ayah_id_target_ayah_id",
                table: "quran_similar_ayah_links",
                columns: new[] { "source_ayah_id", "target_ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_similar_ayah_links_target_ayah_id",
                table: "quran_similar_ayah_links",
                column: "target_ayah_id");

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
                name: "IX_quran_surahs_name_arabic",
                table: "quran_surahs",
                column: "name_arabic",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_ayah_entries_ayah_id_source_id",
                table: "quran_tafsir_ayah_entries",
                columns: new[] { "ayah_id", "source_id" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_ayah_entries_source_id_ayah_id",
                table: "quran_tafsir_ayah_entries",
                columns: new[] { "source_id", "ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_ayah_entries_source_id_verse_key",
                table: "quran_tafsir_ayah_entries",
                columns: new[] { "source_id", "verse_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_ayah_entries_tafsir_entry_id",
                table: "quran_tafsir_ayah_entries",
                column: "tafsir_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_entries_leader_ayah_id",
                table: "quran_tafsir_entries",
                column: "leader_ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_entries_source_id_leader_ayah_id",
                table: "quran_tafsir_entries",
                columns: new[] { "source_id", "leader_ayah_id" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_entries_source_id_source_entry_key",
                table: "quran_tafsir_entries",
                columns: new[] { "source_id", "source_entry_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_sources_language_code",
                table: "quran_tafsir_sources",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_sources_language_code_tafsir_kind",
                table: "quran_tafsir_sources",
                columns: new[] { "language_code", "tafsir_kind" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_sources_package_file",
                table: "quran_tafsir_sources",
                column: "package_file",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_tafsir_sources_source_key",
                table: "quran_tafsir_sources",
                column: "source_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_translation_ayah_entries_ayah_id_source_id",
                table: "quran_translation_ayah_entries",
                columns: new[] { "ayah_id", "source_id" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_translation_ayah_entries_source_id_ayah_id",
                table: "quran_translation_ayah_entries",
                columns: new[] { "source_id", "ayah_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_translation_sources_language_code",
                table: "quran_translation_sources",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "IX_quran_translation_sources_language_code_translation_type",
                table: "quran_translation_sources",
                columns: new[] { "language_code", "translation_type" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_translation_sources_source_key",
                table: "quran_translation_sources",
                column: "source_key",
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
                name: "IX_quran_word_morphology_segments_i3rab_rule_id",
                table: "quran_word_morphology_segments",
                column: "i3rab_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_segments_lemma_id",
                table: "quran_word_morphology_segments",
                column: "lemma_id");

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
                name: "IX_quran_word_morphology_segments_root_id",
                table: "quran_word_morphology_segments",
                column: "root_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_segments_stem",
                table: "quran_word_morphology_segments",
                column: "quran_word_id",
                filter: "kind = 'STEM'");

            migrationBuilder.CreateIndex(
                name: "IX_quran_word_morphology_segments_stem_id",
                table: "quran_word_morphology_segments",
                column: "stem_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ayah_id",
                table: "quran_words",
                column: "ayah_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_location",
                table: "quran_words",
                column: "location",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_page_number_line_number_line_word_order",
                table: "quran_words",
                columns: new[] { "page_number", "line_number", "line_word_order" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_readable_surah_ayah_word",
                table: "quran_words",
                columns: new[] { "surah_number", "ayah_number", "word_number" },
                filter: "is_ayah_marker = false");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_surah_ayah_word",
                table: "quran_words",
                columns: new[] { "surah_number", "ayah_number", "word_number" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_word_id",
                table: "quran_words",
                column: "unique_simple_word_id",
                filter: "is_ayah_marker = false AND unique_simple_word_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_tashkeel_word_id",
                table: "quran_words",
                column: "unique_tashkeel_word_id",
                filter: "is_ayah_marker = false AND unique_tashkeel_word_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_word_key_imlaei_simple",
                table: "quran_words",
                column: "word_key_imlaei_simple",
                filter: "is_ayah_marker = false");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_simple_quran_word_id",
                table: "quran_words_ordered_simple",
                column: "quran_word_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_simple_surah_number_ayah_number_word_or~",
                table: "quran_words_ordered_simple",
                columns: new[] { "surah_number", "ayah_number", "word_order_in_ayah" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_simple_surah_number_word_order_in_surah",
                table: "quran_words_ordered_simple",
                columns: new[] { "surah_number", "word_order_in_surah" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_tashkeel_quran_word_id",
                table: "quran_words_ordered_tashkeel",
                column: "quran_word_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_tashkeel_surah_number_ayah_number_word_~",
                table: "quran_words_ordered_tashkeel",
                columns: new[] { "surah_number", "ayah_number", "word_order_in_ayah" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_ordered_tashkeel_surah_number_word_order_in_sur~",
                table: "quran_words_ordered_tashkeel",
                columns: new[] { "surah_number", "word_order_in_surah" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_first_quran_word_id",
                table: "quran_words_unique_simple",
                column: "first_quran_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_first_word_order_in_mushaf",
                table: "quran_words_unique_simple",
                column: "first_word_order_in_mushaf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_search_text_normalized",
                table: "quran_words_unique_simple",
                column: "search_text_normalized")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_simple_word_key_imlaei_simple",
                table: "quran_words_unique_simple",
                column: "word_key_imlaei_simple",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_tashkeel_first_quran_word_id",
                table: "quran_words_unique_tashkeel",
                column: "first_quran_word_id");

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_tashkeel_first_word_order_in_mushaf",
                table: "quran_words_unique_tashkeel",
                column: "first_word_order_in_mushaf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_tashkeel_search_text_normalized",
                table: "quran_words_unique_tashkeel",
                column: "search_text_normalized")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_quran_words_unique_tashkeel_text_uthmani",
                table: "quran_words_unique_tashkeel",
                column: "text_uthmani",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_granted_by_user_id",
                table: "user_permissions",
                column: "granted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_permission_id",
                table: "user_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_user_id",
                table: "user_permissions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_logto_sub",
                table: "users",
                column: "logto_sub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_status_id",
                table: "users",
                columns: new[] { "status", "id" });

            migrationBuilder.AddForeignKey(
                name: "FK_linking_door_ayah_words_linking_door_ayahs_door_ayah_id",
                table: "linking_door_ayah_words",
                column: "door_ayah_id",
                principalTable: "linking_door_ayahs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_door_ayah_words_quran_ayahs_ayah_id",
                table: "linking_door_ayah_words",
                column: "ayah_id",
                principalTable: "quran_ayahs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_door_ayah_words_quran_words_quran_word_id",
                table: "linking_door_ayah_words",
                column: "quran_word_id",
                principalTable: "quran_words",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_door_ayahs_quran_ayahs_ayah_id",
                table: "linking_door_ayahs",
                column: "ayah_id",
                principalTable: "quran_ayahs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_source_contribution_units_linking_source_contributi~",
                table: "linking_source_contribution_units",
                column: "source_contribution_id",
                principalTable: "linking_source_contributions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_source_contributions_quran_words_unique_simple_uniq~",
                table: "linking_source_contributions",
                column: "unique_simple_word_id",
                principalTable: "quran_words_unique_simple",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_source_contributions_quran_words_unique_tashkeel_un~",
                table: "linking_source_contributions",
                column: "unique_tashkeel_word_id",
                principalTable: "quran_words_unique_tashkeel",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_source_contributions_quran_words_unique_tashkeel_wo~",
                table: "linking_source_contributions",
                column: "word_type_tashkeel_word_id",
                principalTable: "quran_words_unique_tashkeel",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_unit_ayah_descriptions_linking_unit_ayahs_unit_ayah~",
                table: "linking_unit_ayah_descriptions",
                column: "unit_ayah_id",
                principalTable: "linking_unit_ayahs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_unit_ayah_words_linking_unit_ayahs_unit_ayah_id",
                table: "linking_unit_ayah_words",
                column: "unit_ayah_id",
                principalTable: "linking_unit_ayahs",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_unit_ayah_words_quran_ayahs_ayah_id",
                table: "linking_unit_ayah_words",
                column: "ayah_id",
                principalTable: "quran_ayahs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_unit_ayah_words_quran_words_quran_word_id",
                table: "linking_unit_ayah_words",
                column: "quran_word_id",
                principalTable: "quran_words",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_unit_ayahs_quran_ayahs_ayah_id",
                table: "linking_unit_ayahs",
                column: "ayah_id",
                principalTable: "quran_ayahs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_source_ayah_overrides_linking_workspace_s~",
                table: "linking_workspace_source_ayah_overrides",
                column: "workspace_source_id",
                principalTable: "linking_workspace_sources",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_source_ayah_overrides_quran_ayahs_ayah_id",
                table: "linking_workspace_source_ayah_overrides",
                column: "ayah_id",
                principalTable: "quran_ayahs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_source_descriptions_linking_workspace_sou~",
                table: "linking_workspace_source_descriptions",
                column: "workspace_source_id",
                principalTable: "linking_workspace_sources",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_source_descriptions_quran_ayahs_ayah_id",
                table: "linking_workspace_source_descriptions",
                column: "ayah_id",
                principalTable: "quran_ayahs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_source_manual_ayahs_linking_workspace_sou~",
                table: "linking_workspace_source_manual_ayahs",
                column: "workspace_source_id",
                principalTable: "linking_workspace_sources",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_source_manual_ayahs_quran_ayahs_ayah_id",
                table: "linking_workspace_source_manual_ayahs",
                column: "ayah_id",
                principalTable: "quran_ayahs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_source_words_linking_workspace_sources_wo~",
                table: "linking_workspace_source_words",
                column: "workspace_source_id",
                principalTable: "linking_workspace_sources",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_source_words_quran_ayahs_ayah_id",
                table: "linking_workspace_source_words",
                column: "ayah_id",
                principalTable: "quran_ayahs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_source_words_quran_words_quran_word_id",
                table: "linking_workspace_source_words",
                column: "quran_word_id",
                principalTable: "quran_words",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_sources_quran_words_unique_simple_unique_~",
                table: "linking_workspace_sources",
                column: "unique_simple_word_id",
                principalTable: "quran_words_unique_simple",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_sources_quran_words_unique_tashkeel_uniqu~",
                table: "linking_workspace_sources",
                column: "unique_tashkeel_word_id",
                principalTable: "quran_words_unique_tashkeel",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_linking_workspace_sources_quran_words_unique_tashkeel_word_~",
                table: "linking_workspace_sources",
                column: "word_type_tashkeel_word_id",
                principalTable: "quran_words_unique_tashkeel",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_quran_hizbs_quran_ayahs_first_ayah_id",
                table: "quran_hizbs");

            migrationBuilder.DropForeignKey(
                name: "FK_quran_hizbs_quran_ayahs_last_ayah_id",
                table: "quran_hizbs");

            migrationBuilder.DropForeignKey(
                name: "FK_quran_juzs_quran_ayahs_first_ayah_id",
                table: "quran_juzs");

            migrationBuilder.DropForeignKey(
                name: "FK_quran_juzs_quran_ayahs_last_ayah_id",
                table: "quran_juzs");

            migrationBuilder.DropForeignKey(
                name: "FK_quran_rubs_quran_ayahs_first_ayah_id",
                table: "quran_rubs");

            migrationBuilder.DropForeignKey(
                name: "FK_quran_rubs_quran_ayahs_last_ayah_id",
                table: "quran_rubs");

            migrationBuilder.DropTable(
                name: "abwab_door_aliases");

            migrationBuilder.DropTable(
                name: "abwab_door_relations");

            migrationBuilder.DropTable(
                name: "abwab_template_nodes");

            migrationBuilder.DropTable(
                name: "access_audit_events");

            migrationBuilder.DropTable(
                name: "linking_door_ayah_words");

            migrationBuilder.DropTable(
                name: "linking_source_contribution_units");

            migrationBuilder.DropTable(
                name: "linking_unit_ayah_descriptions");

            migrationBuilder.DropTable(
                name: "linking_unit_ayah_words");

            migrationBuilder.DropTable(
                name: "linking_workspace_source_ayah_overrides");

            migrationBuilder.DropTable(
                name: "linking_workspace_source_descriptions");

            migrationBuilder.DropTable(
                name: "linking_workspace_source_manual_ayahs");

            migrationBuilder.DropTable(
                name: "linking_workspace_source_words");

            migrationBuilder.DropTable(
                name: "quran_full_i3rab_ayah_entries");

            migrationBuilder.DropTable(
                name: "quran_lemma_analyses");

            migrationBuilder.DropTable(
                name: "quran_mushaf_lines");

            migrationBuilder.DropTable(
                name: "quran_mutashabihat_occurrences");

            migrationBuilder.DropTable(
                name: "quran_sajdas");

            migrationBuilder.DropTable(
                name: "quran_similar_ayah_links");

            migrationBuilder.DropTable(
                name: "quran_tafsir_ayah_entries");

            migrationBuilder.DropTable(
                name: "quran_translation_ayah_entries");

            migrationBuilder.DropTable(
                name: "quran_word_morphology");

            migrationBuilder.DropTable(
                name: "quran_word_morphology_segments");

            migrationBuilder.DropTable(
                name: "quran_words_ordered_simple");

            migrationBuilder.DropTable(
                name: "quran_words_ordered_tashkeel");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "abwab_templates");

            migrationBuilder.DropTable(
                name: "linking_door_ayahs");

            migrationBuilder.DropTable(
                name: "linking_source_contributions");

            migrationBuilder.DropTable(
                name: "linking_unit_ayahs");

            migrationBuilder.DropTable(
                name: "linking_workspace_sources");

            migrationBuilder.DropTable(
                name: "quran_full_i3rab_entries");

            migrationBuilder.DropTable(
                name: "quran_mutashabihat_groups");

            migrationBuilder.DropTable(
                name: "quran_tafsir_entries");

            migrationBuilder.DropTable(
                name: "quran_translation_sources");

            migrationBuilder.DropTable(
                name: "quran_i3rab_rules");

            migrationBuilder.DropTable(
                name: "quran_pos_tags");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "linking_operations");

            migrationBuilder.DropTable(
                name: "linking_units");

            migrationBuilder.DropTable(
                name: "linking_workspaces");

            migrationBuilder.DropTable(
                name: "quran_lemmas");

            migrationBuilder.DropTable(
                name: "quran_stems");

            migrationBuilder.DropTable(
                name: "quran_words_unique_simple");

            migrationBuilder.DropTable(
                name: "quran_words_unique_tashkeel");

            migrationBuilder.DropTable(
                name: "quran_full_i3rab_sources");

            migrationBuilder.DropTable(
                name: "quran_tafsir_sources");

            migrationBuilder.DropTable(
                name: "abwab_doors");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "quran_roots");

            migrationBuilder.DropTable(
                name: "quran_words");

            migrationBuilder.DropTable(
                name: "abwab_sections");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "quran_mushaf_pages");

            migrationBuilder.DropTable(
                name: "quran_ayahs");

            migrationBuilder.DropTable(
                name: "quran_rubs");

            migrationBuilder.DropTable(
                name: "quran_surahs");

            migrationBuilder.DropTable(
                name: "quran_hizbs");

            migrationBuilder.DropTable(
                name: "quran_juzs");
        }
    }
}
