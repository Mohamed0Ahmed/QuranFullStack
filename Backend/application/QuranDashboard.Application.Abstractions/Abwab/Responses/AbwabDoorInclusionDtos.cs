namespace QuranDashboard.Application.Abstractions.Abwab.Responses;

public sealed record AbwabDirectInclusionDoorDto(
    int InclusionId,
    int DoorId,
    string DoorName,
    bool IsArchived);

public sealed record AbwabDoorInclusionTopologyDto(
    int DoorId,
    uint DoorVersion,
    IReadOnlyList<AbwabDirectInclusionDoorDto> Sources,
    IReadOnlyList<AbwabDirectInclusionDoorDto> Consumers);

public sealed record AbwabDoorInclusionDto(
    int InclusionId,
    int TargetDoorId,
    int SourceDoorId,
    string SourceDoorName,
    bool IsSourceArchived);

public sealed record AbwabDoorInclusionAddResultDto(
    int TargetDoorId,
    uint TargetDoorVersion,
    IReadOnlyList<AbwabDoorInclusionDto> Added);
