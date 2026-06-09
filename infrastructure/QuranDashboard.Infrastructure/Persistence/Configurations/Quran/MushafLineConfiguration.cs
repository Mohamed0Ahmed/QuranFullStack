using QuranDashboard.Domain.Quran.MushafPages;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran;

public sealed class MushafLineConfiguration : IEntityTypeConfiguration<MushafLine>
{
    public void Configure(EntityTypeBuilder<MushafLine> builder)
    {
        builder.ToTable("quran_mushaf_lines");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .UseIdentityByDefaultColumn()
            .HasColumnName("id");

        builder.Property(l => l.PageNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("page_number");

        builder.Property(l => l.LineNumber)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("line_number");

        builder.Property(l => l.LineType)
            .HasConversion(
                value => ToDatabaseValue(value),
                value => ToMushafLineType(value))
            .IsRequired()
            .HasColumnName("line_type");

        builder.Property(l => l.IsCentered)
            .IsRequired()
            .HasColumnName("is_centered");

        builder.Property(l => l.SurahNumber)
            .IsRequired(false)
            .HasColumnType("smallint")
            .HasColumnName("surah_number");

        builder.Property(l => l.FirstWordId)
            .IsRequired(false)
            .HasColumnName("first_word_id");

        builder.Property(l => l.LastWordId)
            .IsRequired(false)
            .HasColumnName("last_word_id");

        builder.Property(l => l.WordsCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("words_count");

        builder.HasIndex(l => new { l.PageNumber, l.LineNumber }).IsUnique();

        builder.HasOne(l => l.Page)
            .WithMany()
            .HasForeignKey(l => l.PageNumber);

        builder.HasOne(l => l.Surah)
            .WithMany()
            .HasForeignKey(l => l.SurahNumber)
            .IsRequired(false);

        builder.HasOne(l => l.FirstWord)
            .WithMany()
            .HasForeignKey(l => l.FirstWordId)
            .IsRequired(false);

        builder.HasOne(l => l.LastWord)
            .WithMany()
            .HasForeignKey(l => l.LastWordId)
            .IsRequired(false);
    }

    private static string ToDatabaseValue(MushafLineType value) => value switch
    {
        MushafLineType.Ayah => "ayah",
        MushafLineType.SurahName => "surah_name",
        MushafLineType.Basmallah => "basmallah",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown mushaf line type.")
    };

    private static MushafLineType ToMushafLineType(string value) => value switch
    {
        "ayah" => MushafLineType.Ayah,
        "surah_name" => MushafLineType.SurahName,
        "basmallah" => MushafLineType.Basmallah,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown mushaf line type value.")
    };
}
