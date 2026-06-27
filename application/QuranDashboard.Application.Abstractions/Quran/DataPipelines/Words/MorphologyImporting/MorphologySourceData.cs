namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

public sealed record MorphologySourceData(
    IReadOnlyList<AlignedWordDto> Words,
    IReadOnlyDictionary<string, string> Roots,
    IReadOnlyDictionary<string, string> Lemmas,
    IReadOnlyDictionary<string, string> Stems,
    IReadOnlyList<ResolvedRootDto> ResolvedRoots,
    IReadOnlyList<ResolvedLemmaDto> ResolvedLemmas,
    IReadOnlyList<ResolvedStemDto> ResolvedStems,
    IReadOnlyList<string> CharsetWarnings,
    IReadOnlyList<string> UnknownPosCodes,
    MorphologyRenderStats RenderStats,
    IReadOnlyList<SegmentDimensionIssue> SegmentDimensionIssues);

public sealed record MorphologyRenderStats(
    int WholeWordAgreementMatches,
    int WholeWordAgreementTotal,
    IReadOnlyList<string> ReviewTierForms,
    IReadOnlyList<string> MultiwordForms,
    IReadOnlyList<string> EmptyFormLocations);

public sealed record AlignedWordDto(
    string Location,
    string HeadPos,
    bool IsVerb,
    string? VerbTense,
    string? VerbVoice,
    string? CaseFeature,
    string? HeadFeaturesJson,
    IReadOnlyList<AlignedSegmentDto> Segments,
    int? RootId,
    int? LemmaId,
    int? StemId);

public sealed record AlignedSegmentDto(
    short SegmentNumber,
    string Kind,
    string Pos,
    string FormBuckwalter,
    string? FormArabicNormalized,
    string? RenderTier,
    string RenderSource,
    string? RootBuckwalter,
    string? LemmaBuckwalter,
    int? RootId,
    int? LemmaId,
    string FeaturesRaw,
    string? FeaturesJson);

public sealed record SegmentDimensionIssue(
    string CheckId,
    string SegmentLocation,
    string Message);

public sealed record ResolvedRootDto(
    int AssignedId,
    string RootText,
    string? RootBuckwalter,
    int WordsCount,
    short DistinctLemmasCount,
    int FirstWordOrderInMushaf);

public sealed record ResolvedLemmaDto(
    int AssignedId,
    string LemmaText,
    string? LemmaBuckwalter,
    int? RootId,
    int WordsCount,
    int FirstWordOrderInMushaf);

public sealed record ResolvedStemDto(
    int AssignedId,
    string StemText,
    int WordsCount,
    int FirstWordOrderInMushaf);
