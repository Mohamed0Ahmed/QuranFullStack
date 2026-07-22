namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabPhysicalDeleteRejectedException(Type entityType)
    : Exception($"Physical delete of Abwab auditable entity '{entityType.Name}' is rejected; soft-delete is enforced.")
{
    public Type EntityType { get; } = entityType;
}
