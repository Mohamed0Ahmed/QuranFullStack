using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabTemplatesReader
{
    Task<IReadOnlyList<AbwabTemplateSummaryDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<AbwabTemplateDto?> GetAsync(int templateId, CancellationToken cancellationToken);
}
