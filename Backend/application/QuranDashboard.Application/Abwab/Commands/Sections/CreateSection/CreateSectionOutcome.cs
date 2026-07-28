using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Sections.CreateSection;

public abstract record CreateSectionOutcome
{
    private CreateSectionOutcome() { }

    public sealed record Success(AbwabSectionDto Section) : CreateSectionOutcome;
    public sealed record InvalidName : CreateSectionOutcome;
    public sealed record DuplicateName : CreateSectionOutcome;
}
