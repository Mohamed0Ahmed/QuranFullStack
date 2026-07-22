using QuranDashboard.Domain.Abwab.Notifications;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class NotificationRecordConfiguration : IEntityTypeConfiguration<NotificationRecord>
{
    public void Configure(EntityTypeBuilder<NotificationRecord> builder)
    {
        builder.ToTable("abwab_notification_records");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(n => n.RecipientSubject).IsRequired().HasColumnName("recipient_subject");
        builder.Property(n => n.SourceIdentity).IsRequired().HasColumnName("source_identity");
        builder.Property(n => n.Payload).IsRequired().HasColumnName("payload");
        builder.Property(n => n.CreatedAtUtc).IsRequired().HasColumnName("created_at");

        // Unique source_identity is the idempotency key: dedups duplicate notifications even under concurrency.
        builder.HasIndex(n => n.SourceIdentity).IsUnique();

        builder.HasIndex(n => n.RecipientSubject);
    }
}
