using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Mutashabihat;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Mutashabihat;

public sealed class SimilarAyahLinkConfiguration : IEntityTypeConfiguration<SimilarAyahLink>
{
    public void Configure(EntityTypeBuilder<SimilarAyahLink> builder)
    {
        builder.ToTable("quran_similar_ayah_links", table =>
        {
            table.HasCheckConstraint(
                "CK_quran_similar_ayah_links_no_self",
                "source_ayah_id <> target_ayah_id");
        });

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(l => l.SourceAyahId)
            .IsRequired()
            .HasColumnName("source_ayah_id");

        builder.Property(l => l.TargetAyahId)
            .IsRequired()
            .HasColumnName("target_ayah_id");

        builder.Property(l => l.Score)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("score");

        builder.Property(l => l.Coverage)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("coverage");

        builder.Property(l => l.MatchedWordsCount)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("matched_words_count");

        builder.Property(l => l.MatchWords)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("match_words");

        builder.HasIndex(l => new { l.SourceAyahId, l.TargetAyahId }).IsUnique();
        builder.HasIndex(l => l.TargetAyahId);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(l => l.SourceAyahId);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(l => l.TargetAyahId);
    }
}
