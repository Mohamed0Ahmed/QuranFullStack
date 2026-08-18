namespace QuranDashboard.Application.Abwab.Commands.AddDoorInclusions;

public sealed record AddDoorInclusionsCommand(
    int TargetDoorId,
    uint ExpectedTargetDoorVersion,
    IReadOnlyList<int> SourceDoorIds,
    int ActorUserId);
