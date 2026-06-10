using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Words.Morphology;

public sealed class QuranRootConfiguration : IEntityTypeConfiguration<QuranRoot>
{
    public void Configure(EntityTypeBuilder<QuranRoot> builder)
    {
        builder.ToTable("quran_roots");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(r => r.RootText)
            .IsRequired()
            .HasColumnName("root_text");

        builder.Property(r => r.RootBuckwalter)
            .HasColumnName("root_buckwalter");

        builder.Property(r => r.WordsCount)
            .IsRequired()
            .HasColumnName("words_count");

        builder.Property(r => r.DistinctLemmasCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("distinct_lemmas_count");

        builder.Property(r => r.FirstWordOrderInMushaf)
            .IsRequired()
            .HasColumnName("first_word_order_in_mushaf");

        builder.HasIndex(r => r.RootText).IsUnique();
        builder.HasIndex(r => r.FirstWordOrderInMushaf).IsUnique();
        builder.HasIndex(r => r.WordsCount);
    }
}
