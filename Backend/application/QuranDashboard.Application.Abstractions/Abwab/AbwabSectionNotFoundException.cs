namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabSectionNotFoundException : Exception
{
    public AbwabSectionNotFoundException()
        : base("The referenced section does not exist or is archived.")
    {
    }
}
