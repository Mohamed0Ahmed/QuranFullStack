using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Words.Morphology;

public sealed class QuranStemConfiguration : IEntityTypeConfiguration<QuranStem>
{
    public void Configure(EntityTypeBuilder<QuranStem> builder)
    {
        builder.ToTable("quran_stems");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(s => s.StemText)
            .IsRequired()
            .HasColumnName("stem_text");

        builder.Property(s => s.WordsCount)
            .IsRequired()
            .HasColumnName("words_count");

        builder.Property(s => s.FirstWordOrderInMushaf)
            .IsRequired()
            .HasColumnName("first_word_order_in_mushaf");

        builder.HasIndex(s => s.StemText).IsUnique();
        builder.HasIndex(s => s.FirstWordOrderInMushaf).IsUnique();
    }
}
