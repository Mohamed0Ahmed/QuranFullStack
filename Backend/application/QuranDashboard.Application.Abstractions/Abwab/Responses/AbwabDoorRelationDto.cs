using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Application.Abstractions.Abwab.Responses;

public sealed record AbwabDoorRelationDto(
    int Id,
    int OtherDoorId,
    string OtherDoorName,
    AbwabRelationType Type,
    AbwabRelationDirection? Direction);
