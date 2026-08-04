namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabCycleException : Exception
{
    public AbwabCycleException()
        : base("The requested move would make a door its own descendant.")
    {
    }
}
