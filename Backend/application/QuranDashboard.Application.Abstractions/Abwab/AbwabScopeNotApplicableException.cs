namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabScopeNotApplicableException : Exception
{
    public AbwabScopeNotApplicableException()
        : base("The Global reorder scope does not apply to a nested door.")
    {
    }
}
