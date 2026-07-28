using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabSectionsWriter
{
    // Throws AbwabDuplicateNameException on a name collision among live sections.
    Task<AbwabSectionDto> CreateAsync(string name, CancellationToken cancellationToken);

    // Null return = section missing or already archived. Throws AbwabStaleVersionException on a
    // stale expectedVersion, AbwabDuplicateNameException on a name collision.
    Task<AbwabSectionDto?> RenameAsync(int id, string name, uint expectedVersion, CancellationToken cancellationToken);

    Task<AbwabSectionDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken);
}

public enum AbwabSectionDeleteResult
{
    Deleted,
    NotFound,
    HasLiveDoors,
}
