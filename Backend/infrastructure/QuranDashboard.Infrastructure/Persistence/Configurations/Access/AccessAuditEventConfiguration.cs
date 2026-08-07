using Microsoft.EntityFrameworkCore.ChangeTracking;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Access;

public sealed class AccessAuditEventConfiguration : IEntityTypeConfiguration<AccessAuditEvent>
{
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Configure(EntityTypeBuilder<AccessAuditEvent> builder)
    {
        builder.ToTable("access_audit_events", table =>
        {
            table.HasCheckConstraint(
                "ck_access_audit_events_documents_are_objects",
                """
                jsonb_typeof(actor_snapshot) = 'object'
                AND jsonb_typeof(target_snapshot) = 'object'
                AND (before_state IS NULL OR jsonb_typeof(before_state) = 'object')
                AND (after_state IS NULL OR jsonb_typeof(after_state) = 'object')
                """);
            table.HasCheckConstraint(
                "ck_access_audit_events_metadata_schema_version",
                """
                jsonb_typeof(metadata) = 'object'
                AND jsonb_exists(metadata, 'schemaVersion')
                AND jsonb_typeof(metadata -> 'schemaVersion') = 'number'
                AND (metadata ->> 'schemaVersion') ~ '^[1-9][0-9]*$'
                AND (metadata ->> 'schemaVersion')::numeric <= 2147483647
                """);
        });

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

        builder.Property(eventItem => eventItem.Metadata)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("metadata")
            .HasConversion(
                metadata => Serialize(metadata),
                json => Deserialize(json),
                new ValueComparer<AccessAuditMetadata>(
                    (left, right) => Serialize(left) == Serialize(right),
                    metadata => Serialize(metadata).GetHashCode(StringComparison.Ordinal),
                    metadata => Deserialize(Serialize(metadata))));

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

    private static string Serialize(AccessAuditMetadata? metadata)
    {
        return JsonSerializer.Serialize(metadata, MetadataSerializerOptions);
    }

    private static AccessAuditMetadata Deserialize(string json)
    {
        return JsonSerializer.Deserialize<AccessAuditMetadata>(json, MetadataSerializerOptions)
            ?? throw new InvalidOperationException("Audit metadata JSON must deserialize to metadata.");
    }
}
