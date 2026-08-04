namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabRelationArchivedDoorException : Exception
{
    public AbwabRelationArchivedDoorException()
        : base("A relation can only be created between two live doors.")
    {
    }
}
