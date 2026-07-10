using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Mutashabihat;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Quran.Mutashabihat;

public sealed class MutashabihatOccurrenceConfiguration : IEntityTypeConfiguration<MutashabihatOccurrence>
{
    public void Configure(EntityTypeBuilder<MutashabihatOccurrence> builder)
    {
        builder.ToTable("quran_mutashabihat_occurrences");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(o => o.GroupId)
            .IsRequired()
            .HasColumnName("group_id");

        builder.Property(o => o.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.Property(o => o.WordFrom)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("word_from");

        builder.Property(o => o.WordTo)
            .IsRequired()
            .HasColumnType("smallint")
            .HasColumnName("word_to");

        builder.Property(o => o.IsRepresentative)
            .IsRequired()
            .HasDefaultValue(false)
            .HasColumnName("is_representative");

        builder.HasIndex(o => new { o.GroupId, o.AyahId, o.WordFrom, o.WordTo }).IsUnique();
        builder.HasIndex(o => o.AyahId);

        builder.HasOne<MutashabihatGroup>()
            .WithMany()
            .HasForeignKey(o => o.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(o => o.AyahId);
    }
}
