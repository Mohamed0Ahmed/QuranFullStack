namespace QuranDashboard.Application.Abstractions.Abwab;

// Application never references EF Core (Clean Architecture boundary), so a writer catches
// DbUpdateConcurrencyException itself and rethrows this plain type instead — the same shape as
// AbwabDuplicateNameException for the uniqueness violation.
public sealed class AbwabStaleVersionException : Exception
{
    public AbwabStaleVersionException()
        : base("The Abwab entity was modified by another request; the supplied version is stale.")
    {
    }
}
