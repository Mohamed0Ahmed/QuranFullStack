using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Sections.ReorderSection;

public abstract record ReorderSectionOutcome
{
    private ReorderSectionOutcome() { }

    public sealed record Success(AbwabSectionDto Section) : ReorderSectionOutcome;
    public sealed record NotFound : ReorderSectionOutcome;
    public sealed record InvalidPosition : ReorderSectionOutcome;
    public sealed record StaleVersion : ReorderSectionOutcome;
}
