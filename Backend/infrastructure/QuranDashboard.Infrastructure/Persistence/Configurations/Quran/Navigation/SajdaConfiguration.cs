using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Navigation;

public sealed class SajdaConfiguration : IEntityTypeConfiguration<Sajda>
{
    public void Configure(EntityTypeBuilder<Sajda> builder)
    {
        builder.ToTable("quran_sajdas");

        builder.HasKey(s => s.SajdahNumber);
        builder.Property(s => s.SajdahNumber)
            .ValueGeneratedNever()
            .HasColumnType("smallint")
            .HasColumnName("sajdah_number");

        builder.Property(s => s.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.Property(s => s.VerseKey)
            .IsRequired()
            .HasColumnName("verse_key");

        builder.Property(s => s.SajdahType)
            .HasConversion(
                value => ToDatabaseValue(value),
                value => ToSajdahType(value))
            .IsRequired()
            .HasColumnName("sajdah_type");

        builder.HasIndex(s => s.AyahId).IsUnique();

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(s => s.AyahId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static string ToDatabaseValue(SajdahType value) => value switch
    {
        SajdahType.Required => "required",
        SajdahType.Optional => "optional",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown sajdah type.")
    };

    private static SajdahType ToSajdahType(string value) => value switch
    {
        "required" => SajdahType.Required,
        "optional" => SajdahType.Optional,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown sajdah type value.")
    };
}
