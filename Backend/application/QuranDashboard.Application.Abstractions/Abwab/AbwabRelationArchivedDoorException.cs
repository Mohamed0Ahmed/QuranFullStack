namespace QuranDashboard.Application.Abstractions.Abwab;

// Distinct from AbwabNotFoundException: an archived endpoint exists, so this is a 400 (the relation
// would be born dormant and invisible), not a 404.
public sealed class AbwabRelationArchivedDoorException : Exception
{
    public AbwabRelationArchivedDoorException()
        : base("A relation can only be created between two live doors.")
    {
    }
}
