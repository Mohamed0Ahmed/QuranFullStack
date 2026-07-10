using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Words.Morphology;

public sealed class QuranLemmaAnalysisConfiguration : IEntityTypeConfiguration<QuranLemmaAnalysis>
{
    public void Configure(EntityTypeBuilder<QuranLemmaAnalysis> builder)
    {
        builder.ToTable("quran_lemma_analyses");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(a => a.LemmaId)
            .IsRequired()
            .HasColumnName("lemma_id");

        builder.Property(a => a.LemmaBuckwalter)
            .IsRequired()
            .HasColumnName("lemma_buckwalter");

        builder.Property(a => a.RootId)
            .HasColumnName("root_id");

        builder.Property(a => a.HeadPos)
            .HasColumnName("head_pos");

        builder.Property(a => a.WordsCount)
            .IsRequired()
            .HasColumnName("words_count");

        builder.Property(a => a.FirstWordOrderInMushaf)
            .IsRequired()
            .HasColumnName("first_word_order_in_mushaf");

        builder.Property(a => a.FirstLocation)
            .IsRequired()
            .HasColumnName("first_location");

        // One analysis per distinct Corpus lemma_buckwalter.
        builder.HasIndex(a => a.LemmaBuckwalter).IsUnique();
        builder.HasIndex(a => a.LemmaId);
        builder.HasIndex(a => a.RootId);
        builder.HasIndex(a => a.FirstWordOrderInMushaf);

        builder.HasOne(a => a.Lemma)
            .WithMany()
            .HasForeignKey(a => a.LemmaId);

        builder.HasOne(a => a.Root)
            .WithMany()
            .HasForeignKey(a => a.RootId);
    }
}
