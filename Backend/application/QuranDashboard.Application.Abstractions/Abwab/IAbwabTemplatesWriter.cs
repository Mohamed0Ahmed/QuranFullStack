using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabTemplatesWriter
{
    Task<AbwabTemplateDto> CreateAsync(
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int templateId, CancellationToken cancellationToken);

    Task<AbwabTemplateNodeDto> AddNodeAsync(
        int templateId,
        int parentNodeId,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken);

    Task<AbwabTemplateNodeDto?> EditNodeAsync(
        int nodeId,
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken);

    Task<AbwabTemplateNodeDto?> ReorderNodeAsync(int nodeId, int position, CancellationToken cancellationToken);

    Task<AbwabTemplateNodeDeleteResult> DeleteNodeAsync(int nodeId, CancellationToken cancellationToken);
}
