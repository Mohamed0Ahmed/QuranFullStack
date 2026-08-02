namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabParentStillArchivedException : Exception
{
    public AbwabParentStillArchivedException()
        : base("The door's parent is still archived; restoring it would surface an inconsistent tree.")
    {
    }
}
