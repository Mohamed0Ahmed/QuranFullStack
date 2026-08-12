using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingUnitAyahWordConfiguration : IEntityTypeConfiguration<LinkingUnitAyahWord>
{
    public void Configure(EntityTypeBuilder<LinkingUnitAyahWord> builder)
    {
        builder.ToTable("linking_unit_ayah_words");

        builder.HasKey(word => new { word.UnitAyahId, word.QuranWordId });

        builder.Property(word => word.UnitAyahId)
            .IsRequired()
            .HasColumnName("unit_ayah_id");

        builder.Property(word => word.QuranWordId)
            .IsRequired()
            .HasColumnName("quran_word_id");

        builder.Property(word => word.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.HasOne<LinkingUnitAyah>()
            .WithMany()
            .HasForeignKey(word => word.UnitAyahId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<QuranWord>()
            .WithMany()
            .HasForeignKey(word => word.QuranWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(word => word.AyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(word => word.QuranWordId);
    }
}
