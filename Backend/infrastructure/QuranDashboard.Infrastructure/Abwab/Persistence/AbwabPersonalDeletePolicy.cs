namespace QuranDashboard.Infrastructure.Abwab.Persistence;

// Default-deny allowlist for physical deletes of IAbwabAuditable; the set is fixed at construction so it can
// never be widened at runtime into a general hard-delete path.
public sealed class AbwabPersonalDeletePolicy
{
    private readonly HashSet<Type> _allowedTypes;

    public AbwabPersonalDeletePolicy(IEnumerable<Type> allowedTypes)
    {
        ArgumentNullException.ThrowIfNull(allowedTypes);
        _allowedTypes = [.. allowedTypes];
    }

    public static AbwabPersonalDeletePolicy Default { get; } = new([]);

    public bool AllowsPhysicalDelete(Type entityType) => _allowedTypes.Contains(entityType);
}
