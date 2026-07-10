using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Navigation;

public sealed class JuzConfiguration : IEntityTypeConfiguration<Juz>
{
    public void Configure(EntityTypeBuilder<Juz> builder)
    {
        builder.ToTable("quran_juzs");

        builder.HasKey(j => j.JuzNumber);
        builder.Property(j => j.JuzNumber)
            .ValueGeneratedNever()
            .HasColumnType("smallint")
            .HasColumnName("juz_number");

        builder.Property(j => j.VersesCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("verses_count");

        builder.Property(j => j.FirstAyahId)
            .IsRequired()
            .HasColumnName("first_ayah_id");

        builder.Property(j => j.LastAyahId)
            .IsRequired()
            .HasColumnName("last_ayah_id");

        builder.Property(j => j.FirstVerseKey)
            .IsRequired()
            .HasColumnName("first_verse_key");

        builder.Property(j => j.LastVerseKey)
            .IsRequired()
            .HasColumnName("last_verse_key");

        builder.HasIndex(j => j.FirstAyahId);
        builder.HasIndex(j => j.LastAyahId);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(j => j.FirstAyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(j => j.LastAyahId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
