using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Templates.EditTemplateNode;

public abstract record EditTemplateNodeOutcome
{
    private EditTemplateNodeOutcome() { }

    public sealed record Success(AbwabTemplateNodeDto Node) : EditTemplateNodeOutcome;
    public sealed record InvalidName : EditTemplateNodeOutcome;
    public sealed record NotFound : EditTemplateNodeOutcome;
    public sealed record DuplicateName : EditTemplateNodeOutcome;
}
