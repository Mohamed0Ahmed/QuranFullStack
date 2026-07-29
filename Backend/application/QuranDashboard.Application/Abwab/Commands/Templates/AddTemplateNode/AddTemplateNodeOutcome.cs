using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Templates.AddTemplateNode;

public abstract record AddTemplateNodeOutcome
{
    private AddTemplateNodeOutcome() { }

    public sealed record Success(AbwabTemplateNodeDto Node) : AddTemplateNodeOutcome;
    public sealed record InvalidName : AddTemplateNodeOutcome;
    public sealed record MissingParent : AddTemplateNodeOutcome;
    public sealed record TemplateNotFound : AddTemplateNodeOutcome;
    public sealed record ParentNotFound : AddTemplateNodeOutcome;
    public sealed record DuplicateName : AddTemplateNodeOutcome;
}
