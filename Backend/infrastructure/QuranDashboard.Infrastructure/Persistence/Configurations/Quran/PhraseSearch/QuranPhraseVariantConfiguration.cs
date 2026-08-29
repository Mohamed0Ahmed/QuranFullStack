using QuranDashboard.Domain.Quran.PhraseSearch;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.PhraseSearch;

public sealed class QuranPhraseVariantConfiguration : IEntityTypeConfiguration<QuranPhraseVariant>
{
    public void Configure(EntityTypeBuilder<QuranPhraseVariant> builder)
    {
        builder.ToTable("quran_phrase_variants", table =>
        {
            table.HasCheckConstraint(
                "ck_quran_phrase_variants_mode",
                "mode IN (1, 2)");
            table.HasCheckConstraint(
                "ck_quran_phrase_variants_word_count",
                "word_count > 0");
            table.HasCheckConstraint(
                "ck_quran_phrase_variants_exact_token_ids",
                "cardinality(exact_token_ids) = word_count");
            table.HasCheckConstraint(
                "ck_quran_phrase_variants_search_token_ids",
                "cardinality(search_token_ids) = word_count");
            table.HasCheckConstraint(
                "ck_quran_phrase_variants_display_text",
                "btrim(display_text) <> ''");
            table.HasCheckConstraint(
                "ck_quran_phrase_variants_counts",
                "occurrence_count > 0 AND ayah_count > 0 AND surah_count > 0 "
                + "AND ayah_count <= occurrence_count AND surah_count <= ayah_count");
        });

        builder.HasKey(variant => new { variant.BuildId, variant.Id });

        builder.Property(variant => variant.BuildId)
            .ValueGeneratedNever()
            .HasColumnName("build_id");

        builder.Property(variant => variant.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(variant => variant.Mode)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .HasColumnName("mode");

        builder.Property(variant => variant.WordCount)
            .HasColumnType("smallint")
            .HasColumnName("word_count");

        builder.Property(variant => variant.ExactTokenIds)
            .IsRequired()
            .HasColumnType("integer[]")
            .HasColumnName("exact_token_ids");

        builder.Property(variant => variant.SearchTokenIds)
            .IsRequired()
            .HasColumnType("integer[]")
            .HasColumnName("search_token_ids");

        builder.Property(variant => variant.DisplayText)
            .IsRequired()
            .HasColumnName("display_text");

        builder.Property(variant => variant.OccurrenceCount)
            .HasColumnName("occurrence_count");

        builder.Property(variant => variant.AyahCount)
            .HasColumnName("ayah_count");

        builder.Property(variant => variant.SurahCount)
            .HasColumnType("smallint")
            .HasColumnName("surah_count");

        builder.Property(variant => variant.FirstQuranWordId)
            .HasColumnName("first_quran_word_id");

        builder.HasAlternateKey(variant => new
        {
            variant.BuildId,
            variant.Id,
            variant.Mode,
            variant.WordCount,
        });

        builder.HasOne<PhraseIndexBuild>()
            .WithMany()
            .HasForeignKey(variant => variant.BuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<QuranWord>()
            .WithMany()
            .HasForeignKey(variant => variant.FirstQuranWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(variant => new
            {
                variant.BuildId,
                variant.Mode,
                variant.WordCount,
                variant.ExactTokenIds,
            })
            .IsUnique();

        builder.HasIndex(variant => new
        {
            variant.BuildId,
            variant.Mode,
            variant.WordCount,
            variant.SearchTokenIds,
        });

        builder.HasIndex(variant => new
            {
                variant.BuildId,
                variant.Mode,
                variant.WordCount,
                variant.OccurrenceCount,
                variant.Id,
            })
            .IsDescending(false, false, false, true, false);
    }
}
