namespace QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

public sealed record MutashabihatSourceData(
    IReadOnlyList<PhraseGroupDto> Groups,
    IReadOnlyList<SimilarLinkDto> Links);

public sealed record PhraseGroupDto(
    int SourceGroupId,
    int RepresentativeAyahId,
    short RepresentativeWordFrom,
    short RepresentativeWordTo,
    short OccurrenceCount,
    short DistinctAyahCount,
    short DistinctSurahCount,
    string? RawSourceCountsJson,
    IReadOnlyList<OccurrenceDto> Occurrences);

public sealed record OccurrenceDto(
    int AyahId,
    short WordFrom,
    short WordTo,
    bool IsRepresentative);

public sealed record SimilarLinkDto(
    int SourceAyahId,
    int TargetAyahId,
    short Score,
    short Coverage,
    short MatchedWordsCount,
    string MatchWordsJson);
