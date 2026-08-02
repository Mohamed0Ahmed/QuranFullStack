namespace QuranDashboard.Application.Abstractions.Abwab;

// Carries the (target, child) pairs where a target's live children already hold the name of one of
// the template root's direct children. The pre-check names them; 23505 names nothing, so the race
// backstop throws this with an empty list.
public sealed class AbwabTemplateApplyCollisionException(IReadOnlyList<AbwabTemplateApplyCollisionPair> collisions)
    : Exception
{
    public IReadOnlyList<AbwabTemplateApplyCollisionPair> Collisions { get; } = collisions;
}
