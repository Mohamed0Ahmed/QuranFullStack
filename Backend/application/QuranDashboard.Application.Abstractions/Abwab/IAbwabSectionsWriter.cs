using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabSectionsWriter
{
    Task<AbwabSectionDto> CreateAsync(string name, CancellationToken cancellationToken);

    Task<AbwabSectionDto?> RenameAsync(int id, string name, uint expectedVersion, CancellationToken cancellationToken);

    Task<AbwabSectionDeleteResult> DeleteAsync(int id, CancellationToken cancellationToken);

    Task<AbwabSectionDto?> ReorderAsync(int id, int position, uint expectedVersion, CancellationToken cancellationToken);
}

public enum AbwabSectionDeleteResult
{
    Deleted,
    NotFound,
    HasLiveDoors,
}
