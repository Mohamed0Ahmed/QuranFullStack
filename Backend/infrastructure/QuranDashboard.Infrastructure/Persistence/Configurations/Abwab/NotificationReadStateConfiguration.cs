using QuranDashboard.Domain.Abwab.Notifications;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

// NotificationReadState is a plain MUTABLE table kept outside product audit/restore (FR-034): NO append-only
// trigger, NO restricted-role revoke, and it is not IAbwabAuditable. The foreign key to notification_records
// is a within-Abwab reference (never a Quran foreign key), so the FK-prohibition guard stays green.
public sealed class NotificationReadStateConfiguration : IEntityTypeConfiguration<NotificationReadState>
{
    public void Configure(EntityTypeBuilder<NotificationReadState> builder)
    {
        builder.ToTable("abwab_notification_read_states");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(s => s.NotificationId).IsRequired().HasColumnName("notification_id");
        builder.Property(s => s.RecipientSubject).IsRequired().HasColumnName("recipient_subject");
        builder.Property(s => s.IsRead).IsRequired().HasColumnName("is_read");
        builder.Property(s => s.ReadAtUtc).HasColumnName("read_at");

        // At most one read-state row per (notification, recipient).
        builder.HasIndex(s => new { s.NotificationId, s.RecipientSubject }).IsUnique();

        builder.HasOne<NotificationRecord>()
            .WithMany()
            .HasForeignKey(s => s.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
