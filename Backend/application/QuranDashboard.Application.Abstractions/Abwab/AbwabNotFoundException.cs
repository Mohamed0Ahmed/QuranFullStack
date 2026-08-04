namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabNotFoundException : Exception
{
    public AbwabNotFoundException()
        : base("One or more referenced doors do not exist or are archived.")
    {
    }
}
