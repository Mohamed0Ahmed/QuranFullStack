namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology;

public sealed record MorphologySourceData(
    IReadOnlyList<AlignedWordDto> Words,
    IReadOnlyDictionary<string, string> Roots,
    IReadOnlyDictionary<string, string> Lemmas,
    IReadOnlyDictionary<string, string> Stems,
    IReadOnlyList<string> CharsetWarnings);

public sealed record AlignedWordDto(
    string Location,
    string HeadPos,
    bool IsVerb,
    string? VerbTense,
    string? VerbVoice,
    string? CaseFeature,
    string? HeadFeaturesJson,
    IReadOnlyList<AlignedSegmentDto> Segments);

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
