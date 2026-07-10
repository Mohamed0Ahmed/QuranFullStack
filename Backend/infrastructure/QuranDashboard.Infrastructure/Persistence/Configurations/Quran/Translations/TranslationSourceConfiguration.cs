using QuranDashboard.Domain.Quran.Translations;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Translations;

public sealed class TranslationSourceConfiguration : IEntityTypeConfiguration<TranslationSource>
{
    public void Configure(EntityTypeBuilder<TranslationSource> builder)
    {
        builder.ToTable("quran_translation_sources", table =>
        {
            table.HasCheckConstraint(
                "CK_quran_translation_sources_direction",
                "direction IN ('rtl', 'ltr')");
            table.HasCheckConstraint(
                "CK_quran_translation_sources_translation_type",
                "translation_type IN ('simple', 'with_footnotes')");
            table.HasCheckConstraint(
                "CK_quran_translation_sources_content_coverage_count",
                "content_coverage_count = 6236");
            table.HasCheckConstraint(
                "CK_quran_translation_sources_required_fields",
                """
                btrim(source_key) <> '' AND
                btrim(language_code) <> '' AND
                btrim(language_name_en) <> '' AND
                btrim(language_name_ar) <> '' AND
                btrim(direction) <> '' AND
                btrim(translation_type) <> '' AND
                btrim(display_name_en) <> '' AND
                btrim(display_name_ar) <> ''
                """);
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(s => s.SourceKey)
            .IsRequired()
            .HasColumnName("source_key");

        builder.Property(s => s.LanguageCode)
            .IsRequired()
            .HasColumnName("language_code");

        builder.Property(s => s.LanguageNameEn)
            .IsRequired()
            .HasColumnName("language_name_en");

        builder.Property(s => s.LanguageNameAr)
            .IsRequired()
            .HasColumnName("language_name_ar");

        builder.Property(s => s.NativeName)
            .HasColumnName("native_name");

        builder.Property(s => s.Direction)
            .IsRequired()
            .HasColumnName("direction");

        builder.Property(s => s.TranslationType)
            .IsRequired()
            .HasColumnName("translation_type");

        builder.Property(s => s.DisplayNameEn)
            .IsRequired()
            .HasColumnName("display_name_en");

        builder.Property(s => s.DisplayNameAr)
            .IsRequired()
            .HasColumnName("display_name_ar");

        builder.Property(s => s.TranslatorKey)
            .HasColumnName("translator_key");

        builder.Property(s => s.TranslatorNameEn)
            .HasColumnName("translator_name_en");

        builder.Property(s => s.TranslatorNameAr)
            .HasColumnName("translator_name_ar");

        builder.Property(s => s.ContainsInlineFootnotes)
            .IsRequired()
            .HasColumnName("contains_inline_footnotes");

        builder.Property(s => s.ContainsHtmlMarkup)
            .IsRequired()
            .HasColumnName("contains_html_markup");

        builder.Property(s => s.ContentCoverageCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("content_coverage_count");

        builder.HasIndex(s => s.SourceKey).IsUnique();
        builder.HasIndex(s => s.LanguageCode);
        builder.HasIndex(s => new { s.LanguageCode, s.TranslationType });
    }
}
