using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingDoorAyahWordConfiguration : IEntityTypeConfiguration<LinkingDoorAyahWord>
{
    public void Configure(EntityTypeBuilder<LinkingDoorAyahWord> builder)
    {
        builder.ToTable("linking_door_ayah_words");

        builder.HasKey(word => new { word.DoorAyahId, word.QuranWordId });

        builder.Property(word => word.DoorAyahId)
            .IsRequired()
            .HasColumnName("door_ayah_id");

        builder.Property(word => word.QuranWordId)
            .IsRequired()
            .HasColumnName("quran_word_id");

        builder.Property(word => word.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.Property(word => word.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(word => word.CreatedBy)
            .IsRequired()
            .HasColumnName("created_by");

        builder.HasOne<LinkingDoorAyah>()
            .WithMany()
            .HasForeignKey(word => word.DoorAyahId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<QuranWord>()
            .WithMany()
            .HasForeignKey(word => word.QuranWordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(word => word.AyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(word => word.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(word => word.QuranWordId);

        builder.HasIndex(word => word.AyahId);
    }
}
