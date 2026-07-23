namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record CategorySearchResultDto(
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    IReadOnlyList<CategorySnapshotDto> Matches) : IAbwabActionableRead;
