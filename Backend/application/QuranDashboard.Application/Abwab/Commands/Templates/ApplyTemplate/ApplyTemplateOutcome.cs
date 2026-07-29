using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abwab.Commands.Templates.ApplyTemplate;

public abstract record ApplyTemplateOutcome
{
    private ApplyTemplateOutcome() { }

    public sealed record Success(IReadOnlyList<AbwabDoorDto> CreatedDoors) : ApplyTemplateOutcome;
    public sealed record InvalidRequest : ApplyTemplateOutcome;
    public sealed record TemplateNotFound : ApplyTemplateOutcome;
    public sealed record TargetNotFound : ApplyTemplateOutcome;
    public sealed record TargetArchived : ApplyTemplateOutcome;
    public sealed record Collision(IReadOnlyList<string> DoorNames) : ApplyTemplateOutcome;
}
