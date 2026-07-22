namespace QuranDashboard.Application.Abstractions.Abwab;

// Raised at SavingChanges when a physical/hard delete is attempted on an IAbwabAuditable entity that
// is not covered by the sealed, default-deny personal-delete exception. Soft-delete is enforced instead.
public sealed class AbwabPhysicalDeleteRejectedException(Type entityType)
    : Exception($"Physical delete of Abwab auditable entity '{entityType.Name}' is rejected; soft-delete is enforced.")
{
    public Type EntityType { get; } = entityType;
}
