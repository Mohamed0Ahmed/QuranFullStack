using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Words.Morphology;

public sealed class QuranLemmaConfiguration : IEntityTypeConfiguration<QuranLemma>
{
    public void Configure(EntityTypeBuilder<QuranLemma> builder)
    {
        builder.ToTable("quran_lemmas");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(l => l.LemmaText)
            .IsRequired()
            .HasColumnName("lemma_text");

        builder.Property(l => l.LemmaBuckwalter)
            .HasColumnName("lemma_buckwalter");

        builder.Property(l => l.RootId)
            .HasColumnName("root_id");

        builder.Property(l => l.WordsCount)
            .IsRequired()
            .HasColumnName("words_count");

        builder.Property(l => l.FirstWordOrderInMushaf)
            .IsRequired()
            .HasColumnName("first_word_order_in_mushaf");

        builder.HasIndex(l => l.LemmaText).IsUnique();
        builder.HasIndex(l => l.FirstWordOrderInMushaf).IsUnique();
        builder.HasIndex(l => l.RootId);

        builder.HasOne(l => l.Root)
            .WithMany()
            .HasForeignKey(l => l.RootId);
    }
}
