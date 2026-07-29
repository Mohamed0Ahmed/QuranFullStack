namespace QuranDashboard.Application.Abstractions.Abwab;

// Carries the TARGET door names whose live children already hold the template root's name. The
// pre-check names them; 23505 names nothing, so the race backstop throws this with an empty list.
public sealed class AbwabTemplateApplyCollisionException(IReadOnlyList<string> doorNames) : Exception
{
    public IReadOnlyList<string> DoorNames { get; } = doorNames;
}
