using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Navigation;

public sealed class HizbConfiguration : IEntityTypeConfiguration<Hizb>
{
    public void Configure(EntityTypeBuilder<Hizb> builder)
    {
        builder.ToTable("quran_hizbs");

        builder.HasKey(h => h.HizbNumber);
        builder.Property(h => h.HizbNumber)
            .ValueGeneratedNever()
            .HasColumnType("smallint")
            .HasColumnName("hizb_number");

        builder.Property(h => h.JuzNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("juz_number");

        builder.Property(h => h.VersesCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("verses_count");

        builder.Property(h => h.FirstAyahId)
            .IsRequired()
            .HasColumnName("first_ayah_id");

        builder.Property(h => h.LastAyahId)
            .IsRequired()
            .HasColumnName("last_ayah_id");

        builder.Property(h => h.FirstVerseKey)
            .IsRequired()
            .HasColumnName("first_verse_key");

        builder.Property(h => h.LastVerseKey)
            .IsRequired()
            .HasColumnName("last_verse_key");

        builder.HasIndex(h => h.JuzNumber);
        builder.HasIndex(h => h.FirstAyahId);
        builder.HasIndex(h => h.LastAyahId);

        builder.HasOne<Juz>()
            .WithMany()
            .HasForeignKey(h => h.JuzNumber)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(h => h.FirstAyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(h => h.LastAyahId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
