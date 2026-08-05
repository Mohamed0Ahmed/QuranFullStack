using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Access;

public sealed class AccessAuditEventConfiguration : IEntityTypeConfiguration<AccessAuditEvent>
{
    public void Configure(EntityTypeBuilder<AccessAuditEvent> builder)
    {
        builder.ToTable("access_audit_events");

        builder.HasKey(eventItem => eventItem.Id);
        builder.Property(eventItem => eventItem.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(eventItem => eventItem.OccurredAtUtc)
            .IsRequired()
            .HasColumnName("occurred_at");

        builder.Property(eventItem => eventItem.ActionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(64)
            .HasColumnName("action_type");

        builder.Property(eventItem => eventItem.ActorType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasColumnName("actor_type");

        builder.Property(eventItem => eventItem.ActorUserId)
            .HasColumnName("actor_user_id");

        builder.Property(eventItem => eventItem.TargetUserId)
            .IsRequired()
            .HasColumnName("target_user_id");

        builder.Property(eventItem => eventItem.ActorSnapshotJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("actor_snapshot");

        builder.Property(eventItem => eventItem.TargetSnapshotJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("target_snapshot");

        builder.Property(eventItem => eventItem.PermissionCode)
            .HasMaxLength(128)
            .HasColumnName("permission_code");

        builder.Property(eventItem => eventItem.BeforeStateJson)
            .HasColumnType("jsonb")
            .HasColumnName("before_state");

        builder.Property(eventItem => eventItem.AfterStateJson)
            .HasColumnType("jsonb")
            .HasColumnName("after_state");

        builder.Property(eventItem => eventItem.Reason)
            .HasMaxLength(1024)
            .HasColumnName("reason");

        builder.Property(eventItem => eventItem.MetadataJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("metadata");

        builder.HasOne(eventItem => eventItem.ActorUser)
            .WithMany()
            .HasForeignKey(eventItem => eventItem.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(eventItem => eventItem.TargetUser)
            .WithMany()
            .HasForeignKey(eventItem => eventItem.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(eventItem => new { eventItem.OccurredAtUtc, eventItem.Id })
            .IsDescending(true, true);
        builder.HasIndex(eventItem => new
            { eventItem.TargetUserId, eventItem.OccurredAtUtc, eventItem.Id })
            .IsDescending(false, true, true);
        builder.HasIndex(eventItem => new { eventItem.ActionType, eventItem.OccurredAtUtc })
            .IsDescending(false, true);
        builder.HasIndex(eventItem => eventItem.PermissionCode)
            .HasFilter("permission_code IS NOT NULL");
    }
}
