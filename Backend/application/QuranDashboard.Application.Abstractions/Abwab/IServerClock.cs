namespace QuranDashboard.Application.Abstractions.Abwab;

// Server-authoritative time; audit timestamps come from here, never client input (anti-spoof).
public interface IServerClock
{
    DateTimeOffset UtcNow { get; }
}
