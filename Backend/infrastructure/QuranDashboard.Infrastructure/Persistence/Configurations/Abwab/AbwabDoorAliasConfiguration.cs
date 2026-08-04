using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabDoorAliasConfiguration : IEntityTypeConfiguration<AbwabDoorAlias>
{
    public void Configure(EntityTypeBuilder<AbwabDoorAlias> builder)
    {
        builder.ToTable("abwab_door_aliases");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(a => a.DoorId)
            .IsRequired()
            .HasColumnName("door_id");

        builder.Property(a => a.Value)
            .IsRequired()
            .HasColumnName("value");

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(a => a.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(a => a.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(a => a.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(a => a.ApprovedAtUtc)
            .HasColumnName("approved_at");

        builder.Property(a => a.ApprovedBy)
            .HasColumnName("approved_by");

        builder.Property(a => a.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(a => a.DeletedBy)
            .HasColumnName("deleted_by");

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(a => a.DoorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.DoorId);
    }
}
