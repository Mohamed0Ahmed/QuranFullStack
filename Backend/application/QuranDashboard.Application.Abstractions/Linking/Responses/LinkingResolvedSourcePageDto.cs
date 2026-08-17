namespace QuranDashboard.Application.Abstractions.Linking.Responses;

public sealed record LinkingResolvedSourcePageDto(
    string ResolutionIdentity,
    string SourceViewIdentity,
    long LinkingDataRevision,
    int TotalAyahCount,
    int Page,
    int PageSize,
    int TotalPages,
    IReadOnlyList<LinkingSourceTypeDto> AvailableTypes,
    IReadOnlyList<LinkingResolvedAyahDto> Items);
