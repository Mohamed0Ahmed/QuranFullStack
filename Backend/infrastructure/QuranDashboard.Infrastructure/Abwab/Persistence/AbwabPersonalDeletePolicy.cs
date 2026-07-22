namespace QuranDashboard.Infrastructure.Abwab.Persistence;

// Sealed, default-deny personal-delete exception. Physical delete of an IAbwabAuditable entity is
// rejected unless its CLR type is explicitly allowlisted here. 028 ships the exception empty (deny all);
// 032 later binds the two exact personal-data shapes. The set is fixed at construction — it cannot be
// widened at runtime — so the exception can never be quietly broadened into a general hard-delete path.
public sealed class AbwabPersonalDeletePolicy
{
    private readonly HashSet<Type> _allowedTypes;

    public AbwabPersonalDeletePolicy(IEnumerable<Type> allowedTypes)
    {
        ArgumentNullException.ThrowIfNull(allowedTypes);
        _allowedTypes = [.. allowedTypes];
    }

    // Production default: default-deny (no type may be physically deleted).
    public static AbwabPersonalDeletePolicy Default { get; } = new([]);

    public bool AllowsPhysicalDelete(Type entityType) => _allowedTypes.Contains(entityType);
}
