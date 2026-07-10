using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Words.Morphology;

public sealed class WordMorphologyConfiguration : IEntityTypeConfiguration<WordMorphology>
{
    public void Configure(EntityTypeBuilder<WordMorphology> builder)
    {
        builder.ToTable("quran_word_morphology");

        builder.HasKey(m => m.QuranWordId);
        builder.Property(m => m.QuranWordId)
            .ValueGeneratedNever()
            .HasColumnName("quran_word_id");

        builder.Property(m => m.Location)
            .IsRequired()
            .HasColumnName("location");

        builder.Property(m => m.HeadPos)
            .IsRequired()
            .HasColumnName("head_pos");

        builder.Property(m => m.SegmentCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("segment_count");

        builder.Property(m => m.RootId)
            .HasColumnName("root_id");

        builder.Property(m => m.LemmaId)
            .HasColumnName("lemma_id");

        builder.Property(m => m.StemId)
            .HasColumnName("stem_id");

        builder.Property(m => m.IsVerb)
            .IsRequired()
            .HasColumnName("is_verb");

        builder.Property(m => m.VerbTense)
            .HasColumnName("verb_tense");

        builder.Property(m => m.VerbVoice)
            .HasColumnName("verb_voice");

        builder.Property(m => m.CaseFeature)
            .HasColumnName("case_feature");

        builder.Property(m => m.HeadFeaturesJson)
            .HasColumnType("jsonb")
            .HasColumnName("head_features_json");

        builder.HasIndex(m => m.QuranWordId).IsUnique();
        builder.HasIndex(m => m.HeadPos);
        builder.HasIndex(m => m.VerbTense)
            .HasDatabaseName("IX_quran_word_morphology_verb_tense")
            .HasFilter("is_verb");
        builder.HasIndex(m => m.VerbVoice)
            .HasDatabaseName("IX_quran_word_morphology_verb_voice")
            .HasFilter("is_verb");
        builder.HasIndex(m => m.CaseFeature);
        builder.HasIndex(m => m.RootId);
        builder.HasIndex(m => m.LemmaId);
        builder.HasIndex(m => m.StemId);

        builder.HasOne(m => m.QuranWord)
            .WithMany()
            .HasForeignKey(m => m.QuranWordId);

        builder.HasOne(m => m.Root)
            .WithMany()
            .HasForeignKey(m => m.RootId);

        builder.HasOne(m => m.Lemma)
            .WithMany()
            .HasForeignKey(m => m.LemmaId);

        builder.HasOne(m => m.Stem)
            .WithMany()
            .HasForeignKey(m => m.StemId);

        builder.HasOne<PosTag>()
            .WithMany()
            .HasForeignKey(m => m.HeadPos)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
