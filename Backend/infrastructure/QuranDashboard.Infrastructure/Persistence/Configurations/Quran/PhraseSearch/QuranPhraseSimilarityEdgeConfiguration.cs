using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.PhraseSearch;

public sealed class QuranPhraseSimilarityEdgeConfiguration : IEntityTypeConfiguration<QuranPhraseSimilarityEdge>
{
    public void Configure(EntityTypeBuilder<QuranPhraseSimilarityEdge> builder)
    {
        builder.ToTable("quran_phrase_similarity_edges", table =>
        {
            table.HasCheckConstraint(
                "ck_quran_phrase_similarity_edges_mode",
                "mode IN (1, 2)");
            table.HasCheckConstraint(
                "ck_quran_phrase_similarity_edges_word_count",
                "word_count >= 4");
            table.HasCheckConstraint(
                "ck_quran_phrase_similarity_edges_order",
                "left_variant_id < right_variant_id");
            table.HasCheckConstraint(
                "ck_quran_phrase_similarity_edges_counts",
                "matched_count > 0 AND difference_count > 0 "
                + "AND matched_count + difference_count = word_count");
            table.HasCheckConstraint(
                "ck_quran_phrase_similarity_edges_difference_positions",
                "cardinality(difference_positions) = difference_count "
                + "AND 0 < ALL (difference_positions) AND word_count >= ALL (difference_positions)");
            table.HasCheckConstraint(
                "ck_quran_phrase_similarity_edges_minimum_match",
                "matched_count * 2 >= word_count");
        });

        builder.HasKey(edge => new { edge.BuildId, edge.LeftVariantId, edge.RightVariantId });

        builder.Property(edge => edge.BuildId)
            .ValueGeneratedNever()
            .HasColumnName("build_id");

        builder.Property(edge => edge.Mode)
            .HasConversion<short>()
            .HasColumnType("smallint")
            .HasColumnName("mode");

        builder.Property(edge => edge.WordCount)
            .HasColumnType("smallint")
            .HasColumnName("word_count");

        builder.Property(edge => edge.LeftVariantId)
            .HasColumnName("left_variant_id");

        builder.Property(edge => edge.RightVariantId)
            .HasColumnName("right_variant_id");

        builder.Property(edge => edge.MatchedCount)
            .HasColumnType("smallint")
            .HasColumnName("matched_count");

        builder.Property(edge => edge.DifferenceCount)
            .HasColumnType("smallint")
            .HasColumnName("difference_count");

        builder.Property(edge => edge.DifferencePositions)
            .IsRequired()
            .HasColumnType("smallint[]")
            .HasColumnName("difference_positions");

        builder.HasOne<QuranPhraseVariant>()
            .WithMany()
            .HasForeignKey(edge => new
            {
                edge.BuildId,
                edge.LeftVariantId,
                edge.Mode,
                edge.WordCount,
            })
            .HasPrincipalKey(variant => new
            {
                variant.BuildId,
                variant.Id,
                variant.Mode,
                variant.WordCount,
            })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<QuranPhraseVariant>()
            .WithMany()
            .HasForeignKey(edge => new
            {
                edge.BuildId,
                edge.RightVariantId,
                edge.Mode,
                edge.WordCount,
            })
            .HasPrincipalKey(variant => new
            {
                variant.BuildId,
                variant.Id,
                variant.Mode,
                variant.WordCount,
            })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(edge => new
            {
                edge.BuildId,
                edge.LeftVariantId,
                edge.MatchedCount,
                edge.RightVariantId,
            })
            .IsDescending(false, false, true, false);

        builder.HasIndex(edge => new
            {
                edge.BuildId,
                edge.RightVariantId,
                edge.MatchedCount,
                edge.LeftVariantId,
            })
            .IsDescending(false, false, true, false);
    }
}
