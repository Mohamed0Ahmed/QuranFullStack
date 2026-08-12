namespace QuranDashboard.Application.Abstractions.Linking.Responses;

public sealed record LinkingResolvedSourceDto(
    string SourceIdentity,
    DateTimeOffset ResolvedAtUtc,
    int TotalAyahCount,
    IReadOnlyList<LinkingResolvedAyahDto> Ayahs);
