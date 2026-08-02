using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabSectionsWriter
{
    // Throws AbwabDuplicateNameException on a name collision among live sections.
    Task<AbwabSectionDto> CreateAsync(string name, CancellationToken cancellationToken);

    // Null return = section missing or already archived. Throws AbwabStaleVersionException on a
    // stale expectedVersion, AbwabDuplicateNameException on a name collision.
    Task<AbwabSectionDto?> RenameAsync(int id, string name, uint expectedVersion, CancellationToken cancellationToken);

    // Throws AbwabStaleVersionException when the section is mutated between this call's own read and its
    // save — the route takes no client token, so that race is the only way the check can fail.
    Task<AbwabSectionDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken);

    // Null = section missing or archived. Throws AbwabStaleVersionException, AbwabInvalidPositionException.
    // Resequences EVERY live section to 1..N — sections have one order space, so there is no scope to
    // narrow; the ordered read tie-breaks on Id to match EfAbwabTreeReader's own order (duplicate
    // OrderValues are reachable, see the area README).
    Task<AbwabSectionDto?> ReorderAsync(int id, int position, uint expectedVersion, CancellationToken cancellationToken);
}

public enum AbwabSectionDeleteResult
{
    Deleted,
    NotFound,
    HasLiveDoors,
}
