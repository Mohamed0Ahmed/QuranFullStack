namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

public sealed record PhraseRepetitionsPageResponse(
    Guid ActiveBuildId,
    string Mode,
    short WordCount,
    string Sort,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<PhraseRepetitionListItemDto> Items);

public sealed record PhraseRepetitionListItemDto(
    long VariantId,
    string DisplayText,
    long OccurrenceCount,
    int AyahCount,
    short SurahCount,
    int FirstQuranWordId);
