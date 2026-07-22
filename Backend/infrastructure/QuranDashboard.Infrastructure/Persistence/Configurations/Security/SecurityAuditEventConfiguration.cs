using QuranDashboard.Domain.Security.Audit;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Security;

public sealed class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEvent>
{
    public void Configure(EntityTypeBuilder<SecurityAuditEvent> builder)
    {
        builder.ToTable("security_audit_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");

        builder.Property(e => e.EventType).IsRequired().HasColumnName("event_type");
        builder.Property(e => e.ActorSubject).IsRequired().HasColumnName("actor_subject");
        builder.Property(e => e.Payload).IsRequired().HasColumnName("payload");
        builder.Property(e => e.ServerTimestampUtc).IsRequired().HasColumnName("server_timestamp");
    }
}
