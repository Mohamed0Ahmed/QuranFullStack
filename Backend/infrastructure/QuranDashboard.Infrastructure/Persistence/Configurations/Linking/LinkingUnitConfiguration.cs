using QuranDashboard.Domain.Linking;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingUnitConfiguration : IEntityTypeConfiguration<LinkingUnit>
{
    public void Configure(EntityTypeBuilder<LinkingUnit> builder)
    {
        builder.ToTable("linking_units");

        builder.HasKey(unit => unit.Id);
        builder.Property(unit => unit.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(unit => unit.DoorId)
            .IsRequired()
            .HasColumnName("door_id");

        builder.Property(unit => unit.Identity)
            .IsRequired()
            .HasColumnName("identity");

        builder.Property(unit => unit.IdentityHash)
            .IsRequired()
            .HasColumnName("identity_hash");

        builder.Property(unit => unit.IsGrouped)
            .IsRequired()
            .HasColumnName("is_grouped");

        builder.Property(unit => unit.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(unit => unit.CreatedBy)
            .IsRequired()
            .HasColumnName("created_by");

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(unit => unit.DoorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(unit => unit.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(unit => new { unit.DoorId, unit.IdentityHash })
            .IsUnique();
    }
}
