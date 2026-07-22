using QuranDashboard.Domain.Abwab.Audit;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("abwab_audit_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(e => e.ChangeSetId)
            .IsRequired()
            .HasColumnName("change_set_id");

        builder.Property(e => e.EventOrdinal)
            .IsRequired()
            .HasColumnName("event_ordinal");

        builder.Property(e => e.Payload)
            .IsRequired()
            .HasColumnName("payload");

        builder.Property(e => e.ServerTimestampUtc)
            .IsRequired()
            .HasColumnName("server_timestamp");

        builder.HasIndex(e => new { e.ChangeSetId, e.EventOrdinal }).IsUnique();
    }
}
