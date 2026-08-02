namespace QuranDashboard.Application.Abwab.Commands.Templates.DeleteTemplateNode;

public abstract record DeleteTemplateNodeOutcome
{
    private DeleteTemplateNodeOutcome() { }

    public sealed record Success : DeleteTemplateNodeOutcome;
    public sealed record NotFound : DeleteTemplateNodeOutcome;
    public sealed record IsRoot : DeleteTemplateNodeOutcome;
}
