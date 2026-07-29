using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Relations.AddDoorRelations;

public sealed record AddDoorRelationsBody(
    AbwabRelationType Type,
    AbwabRelationDirection? Direction,
    IReadOnlyList<int> TargetDoorIds);

public sealed record AddDoorRelationsCommand(
    int DoorId,
    AbwabRelationType Type,
    AbwabRelationDirection? Direction,
    IReadOnlyList<int> TargetDoorIds);
