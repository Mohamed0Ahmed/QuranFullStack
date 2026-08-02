namespace QuranDashboard.Application.Abstractions.Abwab.Responses;

// Nodes is FLAT, like AbwabTreeDto.Doors: each node carries its own ParentNodeId so a consumer
// assembles the tree at any depth. Name is the root node's name (AbwabTemplate has no name column).
public sealed record AbwabTemplateDto(
    int Id,
    string Name,
    IReadOnlyList<AbwabTemplateNodeDto> Nodes);
