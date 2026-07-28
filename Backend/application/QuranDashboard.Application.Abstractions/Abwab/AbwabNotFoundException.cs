namespace QuranDashboard.Application.Abstractions.Abwab;

// Single-door operations signal "missing" with a nullable return (mirrors the read-side convention).
// Bulk operations use this instead: a list can partly resolve, so "was every id found" needs its own
// signal, thrown before any mutation is attempted to keep the whole batch all-or-nothing.
public sealed class AbwabNotFoundException : Exception
{
    public AbwabNotFoundException()
        : base("One or more referenced doors do not exist or are archived.")
    {
    }
}
