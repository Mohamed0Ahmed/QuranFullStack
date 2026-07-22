using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Infrastructure.Abwab.Time;

public sealed class ServerClock : IServerClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
