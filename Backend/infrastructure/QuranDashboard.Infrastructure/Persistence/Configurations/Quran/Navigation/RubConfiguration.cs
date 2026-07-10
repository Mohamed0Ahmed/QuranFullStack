using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Navigation;

public sealed class RubConfiguration : IEntityTypeConfiguration<Rub>
{
    public void Configure(EntityTypeBuilder<Rub> builder)
    {
        builder.ToTable("quran_rubs");

        builder.HasKey(r => r.RubNumber);
        builder.Property(r => r.RubNumber)
            .ValueGeneratedNever()
            .HasColumnType("smallint")
            .HasColumnName("rub_number");

        builder.Property(r => r.HizbNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("hizb_number");

        builder.Property(r => r.VersesCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("verses_count");

        builder.Property(r => r.FirstAyahId)
            .IsRequired()
            .HasColumnName("first_ayah_id");

        builder.Property(r => r.LastAyahId)
            .IsRequired()
            .HasColumnName("last_ayah_id");

        builder.Property(r => r.FirstVerseKey)
            .IsRequired()
            .HasColumnName("first_verse_key");

        builder.Property(r => r.LastVerseKey)
            .IsRequired()
            .HasColumnName("last_verse_key");

        builder.HasIndex(r => r.HizbNumber);
        builder.HasIndex(r => r.FirstAyahId);
        builder.HasIndex(r => r.LastAyahId);

        builder.HasOne<Hizb>()
            .WithMany()
            .HasForeignKey(r => r.HizbNumber)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(r => r.FirstAyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(r => r.LastAyahId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
