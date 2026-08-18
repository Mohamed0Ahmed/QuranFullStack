namespace QuranDashboard.Application.Abwab.Commands.DeleteDoorInclusion;

public sealed record DeleteDoorInclusionCommand(
    int TargetDoorId,
    int InclusionId,
    uint ExpectedTargetDoorVersion,
    int ActorUserId);
