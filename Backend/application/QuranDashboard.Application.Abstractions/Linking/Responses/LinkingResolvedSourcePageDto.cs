namespace QuranDashboard.Application.Abstractions.Linking.Responses;

public sealed record LinkingResolvedSourcePageDto(
    string ResolutionIdentity,
    string SourceViewIdentity,
    long LinkingDataRevision,
    int TotalAyahCount,
    int LinkingAyahCount,
    int Page,
    int PageSize,
    int TotalPages,
    IReadOnlyList<LinkingSourceTypeDto> AvailableTypes,
    IReadOnlyList<int> LinkingAyahIds,
    IReadOnlyDictionary<int, IReadOnlyList<int>> LinkingMatchedWordIdsByAyahId,
    IReadOnlyList<LinkingResolvedAyahDto> Items);
