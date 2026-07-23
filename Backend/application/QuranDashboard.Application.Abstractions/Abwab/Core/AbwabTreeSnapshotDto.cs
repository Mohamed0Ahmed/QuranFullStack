namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record AbwabTreeSnapshotDto(
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    long TreeRevision,
    int SchemaVersion,
    DateTimeOffset ServerTimeUtc,
    IReadOnlyList<SectionSnapshotDto> Sections,
    IReadOnlyList<CategorySnapshotDto> Categories,
    IReadOnlyList<CategorySnapshotDto> AllCategoriesProjection) : IAbwabActionableRead
{
    public const int CurrentSchemaVersion = 1;
}
