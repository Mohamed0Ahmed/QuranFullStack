using QuranDashboard.Domain.Abwab.Notifications;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

// FK to notification_records is within-Abwab, never a Quran foreign key (FK-prohibition guard).
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

        builder.HasIndex(s => new { s.NotificationId, s.RecipientSubject }).IsUnique();

        builder.HasOne<NotificationRecord>()
            .WithMany()
            .HasForeignKey(s => s.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
