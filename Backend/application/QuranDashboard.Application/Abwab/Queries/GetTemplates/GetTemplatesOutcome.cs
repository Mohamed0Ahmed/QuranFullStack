using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Queries.GetTemplates;

public abstract record GetTemplatesOutcome
{
    private GetTemplatesOutcome() { }

    public sealed record Success(IReadOnlyList<AbwabTemplateSummaryDto> Templates) : GetTemplatesOutcome;
}
