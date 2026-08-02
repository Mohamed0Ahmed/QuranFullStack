namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabSectionRequiredException : Exception
{
    public AbwabSectionRequiredException()
        : base("Every door belongs to a section; this write has no section to put it in.")
    {
    }
}
