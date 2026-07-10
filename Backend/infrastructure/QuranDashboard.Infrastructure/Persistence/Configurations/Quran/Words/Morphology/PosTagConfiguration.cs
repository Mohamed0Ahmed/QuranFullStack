using QuranDashboard.Domain.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Words.Morphology;

public sealed class PosTagConfiguration : IEntityTypeConfiguration<PosTag>
{
    public void Configure(EntityTypeBuilder<PosTag> builder)
    {
        builder.ToTable("quran_pos_tags");

        builder.HasKey(p => p.Code);
        builder.Property(p => p.Code)
            .IsRequired()
            .HasColumnName("code");

        builder.Property(p => p.ArabicLabel)
            .IsRequired()
            .HasColumnName("arabic_label");

        builder.Property(p => p.EnglishLabel)
            .IsRequired()
            .HasColumnName("english_label");

        builder.Property(p => p.Category)
            .IsRequired()
            .HasColumnName("category");

        builder.Property(p => p.SortOrder)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("sort_order");

        builder.Property(p => p.Description)
            .HasColumnName("description");

        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.SortOrder);
    }
}
