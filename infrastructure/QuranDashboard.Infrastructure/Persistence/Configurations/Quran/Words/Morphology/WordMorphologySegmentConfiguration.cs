using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Words.Morphology;

public sealed class WordMorphologySegmentConfiguration : IEntityTypeConfiguration<WordMorphologySegment>
{
    public void Configure(EntityTypeBuilder<WordMorphologySegment> builder)
    {
        builder.ToTable("quran_word_morphology_segments", table =>
        {
            table.HasCheckConstraint(
                "CK_quran_word_morphology_segments_i3rab_status",
                "i3rab_status IN ('approved', 'needs_review', 'unsupported')");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(s => s.QuranWordId)
            .IsRequired()
            .HasColumnName("quran_word_id");

        builder.Property(s => s.SegmentLocation)
            .IsRequired()
            .HasColumnName("segment_location");

        builder.Property(s => s.SegmentNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("segment_number");

        builder.Property(s => s.Kind)
            .IsRequired()
            .HasColumnName("kind");

        builder.Property(s => s.Pos)
            .IsRequired()
            .HasColumnName("pos");

        builder.Property(s => s.FormBuckwalter)
            .IsRequired()
            .HasColumnName("form_buckwalter");

        builder.Property(s => s.FormArabicNormalized)
            .HasColumnName("form_arabic_normalized");

        builder.Property(s => s.ArabicRenderTier)
            .HasColumnName("arabic_render_tier");

        builder.Property(s => s.ArabicRenderSource)
            .IsRequired()
            .HasColumnName("arabic_render_source");

        builder.Property(s => s.RootBuckwalter)
            .HasColumnName("root_buckwalter");

        builder.Property(s => s.LemmaBuckwalter)
            .HasColumnName("lemma_buckwalter");

        builder.Property(s => s.RootId)
            .HasColumnName("root_id");

        builder.Property(s => s.LemmaId)
            .HasColumnName("lemma_id");

        builder.Property(s => s.FeaturesRaw)
            .IsRequired()
            .HasColumnName("features_raw");

        builder.Property(s => s.FeaturesJson)
            .HasColumnType("jsonb")
            .HasColumnName("features_json");

        builder.Property(s => s.I3rabArabic)
            .HasColumnName("i3rab_arabic");

        builder.Property(s => s.I3rabRuleId)
            .HasColumnName("i3rab_rule_id");

        builder.Property(s => s.I3rabStatus)
            .IsRequired()
            .HasDefaultValue("unsupported")
            .HasColumnName("i3rab_status");

        builder.Property(s => s.I3rabReviewReason)
            .HasColumnName("i3rab_review_reason");

        builder.HasIndex(s => new { s.QuranWordId, s.SegmentNumber }).IsUnique();
        builder.HasIndex(s => s.Pos);
        builder.HasIndex(s => s.QuranWordId)
            .HasDatabaseName("IX_quran_word_morphology_segments_stem")
            .HasFilter("kind = 'STEM'");
        builder.HasIndex(s => s.ArabicRenderTier);
        builder.HasIndex(s => s.RootId)
            .HasDatabaseName("IX_quran_word_morphology_segments_root_id");
        builder.HasIndex(s => s.LemmaId)
            .HasDatabaseName("IX_quran_word_morphology_segments_lemma_id");

        builder.HasOne(s => s.QuranWord)
            .WithMany()
            .HasForeignKey(s => s.QuranWordId);

        builder.HasOne(s => s.Root)
            .WithMany()
            .HasForeignKey(s => s.RootId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Lemma)
            .WithMany()
            .HasForeignKey(s => s.LemmaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PosTag>()
            .WithMany()
            .HasForeignKey(s => s.Pos)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.I3rabRuleId);

        builder.HasOne(s => s.I3rabRule)
            .WithMany()
            .HasForeignKey(s => s.I3rabRuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
