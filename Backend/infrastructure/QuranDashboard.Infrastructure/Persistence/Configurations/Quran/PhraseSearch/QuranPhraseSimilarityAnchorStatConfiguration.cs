using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.PhraseSearch;

public sealed class QuranPhraseSimilarityAnchorStatConfiguration
    : IEntityTypeConfiguration<QuranPhraseSimilarityAnchorStat>
{
    public void Configure(EntityTypeBuilder<QuranPhraseSimilarityAnchorStat> builder)
    {
        builder.ToTable("quran_phrase_similarity_anchor_stats", table =>
        {
            table.HasCheckConstraint(
                "ck_quran_phrase_similarity_anchor_stats_mode",
                "mode IN (1, 2)");
            table.HasCheckConstraint(
                "ck_quran_phrase_similarity_anchor_stats_word_count",
                "word_count >= 4");
            table.HasCheckConstraint(
                "ck_quran_phrase_similarity_anchor_stats_threshold",
                "threshold IN (50, 60, 70, 80, 90)");
            table.HasCheckConstraint(
                "ck_quran_phrase_similarity_anchor_stats_counts",
                "neighbor_count >= 0 AND (best_matched_count IS NULL "
                + "OR (best_matched_count >= 0 AND best_matched_count <= word_count))");
        });

        builder.HasKey(stat => new { stat.BuildId, stat.VariantId, stat.Threshold });

        builder.Property(stat => stat.BuildId)
            .ValueGeneratedNever()
            .HasColumnName("build_id");

        builder.Property(stat => stat.VariantId)
            .HasColumnName("variant_id");

        builder.Property(stat => stat.Threshold)
            .HasColumnType("smallint")
            .HasColumnName("threshold");

        builder.Property(stat => stat.Mode)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .HasColumnName("mode");

        builder.Property(stat => stat.WordCount)
            .HasColumnType("smallint")
            .HasColumnName("word_count");

        builder.Property(stat => stat.NeighborCount)
            .HasColumnName("neighbor_count");

        builder.Property(stat => stat.BestMatchedCount)
            .HasColumnType("smallint")
            .HasColumnName("best_matched_count");

        builder.HasOne<QuranPhraseVariant>()
            .WithMany()
            .HasForeignKey(stat => new
            {
                stat.BuildId,
                stat.VariantId,
                stat.Mode,
                stat.WordCount,
            })
            .HasPrincipalKey(variant => new
            {
                variant.BuildId,
                variant.Id,
                variant.Mode,
                variant.WordCount,
            })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(stat => new
            {
                stat.BuildId,
                stat.Mode,
                stat.WordCount,
                stat.Threshold,
                stat.NeighborCount,
                stat.VariantId,
            })
            .IsDescending(false, false, false, false, true, false);
    }
}
