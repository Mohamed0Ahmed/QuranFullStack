namespace QuranDashboard.Application.Abwab.Commands.Templates.DeleteTemplate;

public abstract record DeleteTemplateOutcome
{
    private DeleteTemplateOutcome() { }

    public sealed record Success : DeleteTemplateOutcome;
    public sealed record NotFound : DeleteTemplateOutcome;
}
