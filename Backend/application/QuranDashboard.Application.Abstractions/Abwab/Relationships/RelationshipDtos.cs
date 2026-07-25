using QuranDashboard.Domain.Abwab.Relationships;

namespace QuranDashboard.Application.Abstractions.Abwab.Relationships;

public sealed record CategoryRelationshipEndpointDto(
    Guid CategoryId,
    string Name,
    IReadOnlyList<string> Path,
    bool IsDeleted);

public sealed record CategoryRelationshipDto(
    Guid CategoryRelationshipId,
    RelationshipType RelationshipType,
    bool IsDirectional,
    CategoryRelationshipEndpointDto From,
    CategoryRelationshipEndpointDto To,
    bool IsDeleted,
    uint Version);

public sealed record CategoryRelationshipListDto(
    int SchemaVersion,
    Guid CategoryId,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    DateTimeOffset ServerTimeUtc,
    IReadOnlyList<CategoryRelationshipDto> Relationships) : IAbwabActionableRead;

public sealed record DormantRelationshipCountDto(RelationshipType RelationshipType, int Count);

public sealed record DormantRelationshipCountsDto(
    int TotalDormant,
    IReadOnlyList<DormantRelationshipCountDto> ByType);
