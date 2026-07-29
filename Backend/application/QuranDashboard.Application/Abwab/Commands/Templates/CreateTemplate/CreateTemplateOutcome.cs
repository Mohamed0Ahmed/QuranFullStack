using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Templates.CreateTemplate;

public abstract record CreateTemplateOutcome
{
    private CreateTemplateOutcome() { }

    public sealed record Success(AbwabTemplateDto Template) : CreateTemplateOutcome;
    public sealed record InvalidName : CreateTemplateOutcome;
}
