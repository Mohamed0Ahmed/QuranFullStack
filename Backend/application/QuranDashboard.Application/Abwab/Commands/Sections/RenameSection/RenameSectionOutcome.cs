using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Sections.RenameSection;

public abstract record RenameSectionOutcome
{
    private RenameSectionOutcome() { }

    public sealed record Success(AbwabSectionDto Section) : RenameSectionOutcome;
    public sealed record InvalidName : RenameSectionOutcome;
    public sealed record NotFound : RenameSectionOutcome;
    public sealed record StaleVersion : RenameSectionOutcome;
    public sealed record DuplicateName : RenameSectionOutcome;
}
