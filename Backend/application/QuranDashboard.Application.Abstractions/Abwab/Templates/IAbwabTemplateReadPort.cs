namespace QuranDashboard.Application.Abstractions.Abwab.Templates;

public interface IAbwabTemplateReadPort
{
    // The history of one template grows without bound as it is edited, and every entry carries a full
    // before/after tree payload, so the projection is capped rather than left to scale with the audit
    // log. Truncation is reported through TemplateHistoryDto.HasMore, never silently.
    const int MaxHistoryEntries = 100;

    Task<DoorTemplateListDto> GetTemplatesAsync(bool includeDeleted, CancellationToken cancellationToken);

    Task<DoorTemplateDetailDto?> GetTemplateAsync(Guid doorTemplateId, bool includeDeleted, CancellationToken cancellationToken);

    Task<TemplateHistoryDto> GetHistoryAsync(Guid doorTemplateId, CancellationToken cancellationToken);
}
