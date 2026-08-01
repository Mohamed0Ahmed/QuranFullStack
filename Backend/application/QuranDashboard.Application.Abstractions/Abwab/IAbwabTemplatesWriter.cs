using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabTemplatesWriter
{
    // Creates the template AND its root node — a template without a root cannot be applied, so the
    // two are never separately creatable.
    Task<AbwabTemplateDto> CreateAsync(
        string name,
        string? description,
        string? representativeAyahText,
        IReadOnlyList<string> aliases,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int templateId, CancellationToken cancellationToken);

    // parentNodeId is NOT nullable: every added node is a child of something. A second root would
    // leave the apply's children-only copy unable to tell which root's children to copy.
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
