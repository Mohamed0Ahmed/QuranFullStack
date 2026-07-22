namespace QuranDashboard.Application.Abstractions.Abwab;

// Server-authoritative time. Every ChangeSet/AuditEvent timestamp comes from here, never from client
// input, so audit ordering and server stamps cannot be spoofed.
public interface IServerClock
{
    DateTimeOffset UtcNow { get; }
}
