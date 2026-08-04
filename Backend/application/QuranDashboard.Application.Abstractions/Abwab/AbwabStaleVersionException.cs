namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabStaleVersionException : Exception
{
    public AbwabStaleVersionException()
        : base("The Abwab entity was modified by another request; the supplied version is stale.")
    {
    }
}
