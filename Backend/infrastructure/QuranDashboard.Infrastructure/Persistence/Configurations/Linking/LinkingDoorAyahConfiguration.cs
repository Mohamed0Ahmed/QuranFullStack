using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Access;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Quran.Ayahs;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingDoorAyahConfiguration : IEntityTypeConfiguration<LinkingDoorAyah>
{
    public void Configure(EntityTypeBuilder<LinkingDoorAyah> builder)
    {
        builder.ToTable("linking_door_ayahs");

        builder.HasKey(doorAyah => doorAyah.Id);

        builder.Property(doorAyah => doorAyah.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(doorAyah => doorAyah.DoorId)
            .IsRequired()
            .HasColumnName("door_id");

        builder.Property(doorAyah => doorAyah.AyahId)
            .IsRequired()
            .HasColumnName("ayah_id");

        builder.Property(doorAyah => doorAyah.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(doorAyah => doorAyah.CreatedBy)
            .IsRequired()
            .HasColumnName("created_by");

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(doorAyah => doorAyah.DoorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Ayah>()
            .WithMany()
            .HasForeignKey(doorAyah => doorAyah.AyahId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(doorAyah => doorAyah.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(doorAyah => new { doorAyah.DoorId, doorAyah.AyahId })
            .IsUnique();

        builder.HasIndex(doorAyah => doorAyah.AyahId);
    }
}
