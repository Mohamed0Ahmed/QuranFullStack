namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

public sealed record PhraseSimilaritySearchResponse(
    Guid ActiveBuildId,
    string Mode,
    short WordCount,
    short MinimumMatchedWords,
    int Page,
    int PageSize,
    int TotalAyahCount,
    long TotalOccurrenceCount,
    PhraseSimilarityPhraseDto Query,
    IReadOnlyList<PhraseSimilarityAyahDto> Items);

public sealed record PhraseSimilarityAyahDto(
    int AyahId,
    string VerseKey,
    short SurahNumber,
    string SurahNameArabic,
    short AyahNumber,
    short PageFrom,
    short PageTo,
    short BestMatchedCount,
    short MinimumDifferenceCount,
    decimal BestMatchPercent,
    int OccurrenceCount,
    IReadOnlyList<PhraseAyahWordDto> Words,
    PhraseSimilarityHighlightsDto Highlights);

public sealed record PhraseSimilarityGroupsResponse(
    Guid ActiveBuildId,
    string Mode,
    short WordCount,
    short Threshold,
    string Sort,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<PhraseSimilarityGroupDto> Items);

public sealed record PhraseSimilarityMatchesResponse(
    Guid ActiveBuildId,
    short Threshold,
    int Page,
    int PageSize,
    int TotalCount,
    PhraseSimilarityPhraseDto Anchor,
    PhraseSimilarityOccurrenceDto AnchorOccurrence,
    IReadOnlyList<PhraseSimilarityMatchDto> Items);

public sealed record PhraseSimilarityGroupDto(
    PhraseSimilarityPhraseDto Anchor,
    int NeighborCount,
    short? BestMatchedCount,
    decimal? BestMatchPercent,
    PhraseSimilarityPhraseDto? BestNeighbor,
    PhraseSimilarityOccurrencePreviewDto RepresentativeOccurrence);

public sealed record PhraseSimilarityMatchDto(
    PhraseSimilarityPhraseDto Phrase,
    short MatchedCount,
    short DifferenceCount,
    decimal MatchPercent,
    IReadOnlyList<short> MatchedPositions,
    IReadOnlyList<short> DifferingPositions,
    PhraseSimilarityOccurrenceDto ComparedOccurrence);

public sealed record PhraseSimilarityPhraseDto(
    long VariantId,
    string Mode,
    short WordCount,
    string DisplayText,
    long OccurrenceCount,
    int AyahCount,
    short SurahCount);

public sealed record PhraseSimilarityOccurrenceDto(
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
    PhraseSimilarityHighlightsDto Highlights);

public sealed record PhraseSimilarityHighlightsDto(
    IReadOnlyList<int> PhraseQuranWordIds,
    IReadOnlyList<int> MatchedQuranWordIds,
    IReadOnlyList<int> DifferingQuranWordIds);

public sealed record PhraseSimilarityOccurrencePreviewDto(
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
    IReadOnlyList<PhraseAyahWordDto> PhraseWords);
