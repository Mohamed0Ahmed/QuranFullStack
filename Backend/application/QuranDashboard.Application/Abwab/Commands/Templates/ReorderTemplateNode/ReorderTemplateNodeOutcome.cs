using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Templates.ReorderTemplateNode;

public abstract record ReorderTemplateNodeOutcome
{
    private ReorderTemplateNodeOutcome() { }

    public sealed record Success(AbwabTemplateNodeDto Node) : ReorderTemplateNodeOutcome;
    public sealed record NotFound : ReorderTemplateNodeOutcome;
    public sealed record IsRoot : ReorderTemplateNodeOutcome;
    public sealed record InvalidPosition : ReorderTemplateNodeOutcome;
}
