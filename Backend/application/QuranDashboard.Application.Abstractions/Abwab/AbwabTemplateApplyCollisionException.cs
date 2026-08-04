namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabTemplateApplyCollisionException(IReadOnlyList<AbwabTemplateApplyCollisionPair> collisions)
    : Exception
{
    public IReadOnlyList<AbwabTemplateApplyCollisionPair> Collisions { get; } = collisions;
}
