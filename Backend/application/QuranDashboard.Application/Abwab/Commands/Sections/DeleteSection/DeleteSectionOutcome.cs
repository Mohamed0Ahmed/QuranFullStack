namespace QuranDashboard.Application.Abwab.Commands.Sections.DeleteSection;

public abstract record DeleteSectionOutcome
{
    private DeleteSectionOutcome() { }

    public sealed record Success : DeleteSectionOutcome;
    public sealed record NotFound : DeleteSectionOutcome;
    public sealed record HasLiveDoors : DeleteSectionOutcome;
    public sealed record StaleVersion : DeleteSectionOutcome;
}
