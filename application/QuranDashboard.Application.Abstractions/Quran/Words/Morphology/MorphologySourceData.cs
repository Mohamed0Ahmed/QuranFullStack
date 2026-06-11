namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology;

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
    MorphologyRenderStats RenderStats);

/// <summary>
/// Informational rendering statistics gathered during assembly, surfaced as report warnings
/// (FR-029): the whole-word transliteration-vs-Uthmani agreement rate and the review/multiword/
/// empty-form lists for manual sign-off. None of these change the import verdict.
/// </summary>
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
    string FeaturesRaw,
    string? FeaturesJson);

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
