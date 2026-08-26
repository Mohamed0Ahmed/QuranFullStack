namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

public sealed record PhraseOccurrencePageResponse(
    Guid ActiveBuildId,
    PhraseRepetitionDetailDto Phrase,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<PhraseOccurrenceDto> Items);

public sealed record PhraseRepetitionDetailDto(
    long VariantId,
    string Mode,
    short WordCount,
    string DisplayText,
    long OccurrenceCount,
    int AyahCount,
    short SurahCount);

public sealed record PhraseOccurrenceDto(
    long OccurrenceId,
    int AyahId,
    string VerseKey,
    short SurahNumber,
    string SurahNameArabic,
    short AyahNumber,
    short PageFrom,
    short PageTo,
    short StartWordNumber,
    short EndWordNumber,
    IReadOnlyList<PhraseAyahWordDto> Words,
    PhraseOccurrenceHighlightsDto Highlights);

public sealed record PhraseAyahWordDto(
    int QuranWordId,
    short WordNumber,
    short PageNumber,
    string TextUthmani);

public sealed record PhraseOccurrenceHighlightsDto(
    IReadOnlyList<int> QueryQuranWordIds);
