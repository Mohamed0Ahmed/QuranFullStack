using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AbwabDoorInclusionConfiguration : IEntityTypeConfiguration<AbwabDoorInclusion>
{
    public void Configure(EntityTypeBuilder<AbwabDoorInclusion> builder)
    {
        builder.ToTable("abwab_door_inclusions", table =>
        {
            table.HasCheckConstraint(
                "ck_abwab_door_inclusions_distinct_doors",
                "target_door_id <> source_door_id");
        });

        builder.HasKey(inclusion => inclusion.Id);
        builder.Property(inclusion => inclusion.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(inclusion => inclusion.TargetDoorId)
            .IsRequired()
            .HasColumnName("target_door_id");

        builder.Property(inclusion => inclusion.SourceDoorId)
            .IsRequired()
            .HasColumnName("source_door_id");

        builder.Property(inclusion => inclusion.CreatedAtUtc)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(inclusion => inclusion.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(inclusion => inclusion.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at");

        builder.Property(inclusion => inclusion.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(inclusion => inclusion.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.Property(inclusion => inclusion.DeletedBy)
            .HasColumnName("deleted_by");

        builder.Property(inclusion => inclusion.Version)
            .IsRowVersion();

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(inclusion => inclusion.TargetDoorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AbwabDoor>()
            .WithMany()
            .HasForeignKey(inclusion => inclusion.SourceDoorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(inclusion => new { inclusion.TargetDoorId, inclusion.SourceDoorId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(inclusion => inclusion.SourceDoorId)
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(inclusion => inclusion.DeletedAtUtc);
    }
}
