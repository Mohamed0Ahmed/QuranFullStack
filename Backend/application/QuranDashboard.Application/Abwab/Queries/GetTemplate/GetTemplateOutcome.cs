using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Queries.GetTemplate;

public abstract record GetTemplateOutcome
{
    private GetTemplateOutcome() { }

    public sealed record Success(AbwabTemplateDto Template) : GetTemplateOutcome;

    public sealed record NotFound : GetTemplateOutcome;
}
