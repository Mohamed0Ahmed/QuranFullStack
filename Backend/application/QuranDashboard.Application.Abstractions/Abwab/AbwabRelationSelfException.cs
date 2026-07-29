namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabRelationSelfException : Exception
{
    public AbwabRelationSelfException()
        : base("A door cannot be related to itself.")
    {
    }
}
