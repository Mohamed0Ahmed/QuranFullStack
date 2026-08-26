using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.PhraseSearch;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.PhraseSearch;

public sealed class QuranPhraseOccurrenceConfiguration : IEntityTypeConfiguration<QuranPhraseOccurrence>
{
    public void Configure(EntityTypeBuilder<QuranPhraseOccurrence> builder)
    {
        builder.ToTable("quran_phrase_occurrences", table =>
        {
            table.HasCheckConstraint(
                "ck_quran_phrase_occurrences_mode",
                "mode IN (1, 2)");
            table.HasCheckConstraint(
                "ck_quran_phrase_occurrences_word_count",
                "word_count > 0");
            table.HasCheckConstraint(
                "ck_quran_phrase_occurrences_word_range",
                "start_word_number > 0 AND end_word_number - start_word_number + 1 = word_count");
        });

        builder.HasKey(occurrence => new { occurrence.BuildId, occurrence.Id });

        builder.Property(occurrence => occurrence.BuildId)
            .ValueGeneratedNever()
            .HasColumnName("build_id");

        builder.Property(occurrence => occurrence.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(occurrence => occurrence.VariantId)
            .HasColumnName("variant_id");

        builder.Property(occurrence => occurrence.Mode)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .HasColumnName("mode");

        builder.Property(occurrence => occurrence.WordCount)
            .HasColumnType("smallint")
            .HasColumnName("word_count");

        builder.Property(occurrence => occurrence.AyahId)
            .HasColumnName("ayah_id");

        builder.Property(occurrence => occurrence.StartWordNumber)
            .HasColumnType("smallint")
            .HasColumnName("start_word_number");

        builder.Property(occurrence => occurrence.EndWordNumber)
            .HasColumnType("smallint")
            .HasColumnName("end_word_number");

        builder.Property(occurrence => occurrence.FirstQuranWordId)
            .HasColumnName("first_quran_word_id");

        builder.Property(occurrence => occurrence.LastQuranWordId)
            .HasColumnName("last_quran_word_id");

        builder.HasOne<QuranPhraseVariant>()
            .WithMany()
            .HasForeignKey(occurrence => new
            {
                occurrence.BuildId,
                occurrence.VariantId,
                occurrence.Mode,
                occurrence.WordCount,
            })
            .HasPrincipalKey(variant => new
            {
                variant.BuildId,
                variant.Id,
                variant.Mode,
                variant.WordCount,
            })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(occurrence => occurrence.AyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<QuranWord>()
            .WithMany()
            .HasForeignKey(occurrence => occurrence.FirstQuranWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<QuranWord>()
            .WithMany()
            .HasForeignKey(occurrence => occurrence.LastQuranWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(occurrence => new
            {
                occurrence.BuildId,
                occurrence.VariantId,
                occurrence.AyahId,
                occurrence.StartWordNumber,
            })
            .IsUnique();

        builder.HasIndex(occurrence => new
        {
            occurrence.BuildId,
            occurrence.AyahId,
            occurrence.StartWordNumber,
            occurrence.EndWordNumber,
        });
    }
}
