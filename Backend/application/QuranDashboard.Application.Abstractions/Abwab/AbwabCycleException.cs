namespace QuranDashboard.Application.Abstractions.Abwab;

// A door may not become its own descendant — the mandatory cycle guard on move (plan §4).
public sealed class AbwabCycleException : Exception
{
    public AbwabCycleException()
        : base("The requested move would make a door its own descendant.")
    {
    }
}
