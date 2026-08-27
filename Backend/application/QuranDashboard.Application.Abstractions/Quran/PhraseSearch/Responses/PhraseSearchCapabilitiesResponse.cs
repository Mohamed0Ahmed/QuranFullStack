namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

public sealed record PhraseSearchCapabilitiesResponse(
    Guid ActiveBuildId,
    bool ExactReady,
    bool SimilarityReady,
    string DefaultMode,
    short DefaultRepetitionLength,
    string DefaultRepetitionSort,
    int DefaultPageSize,
    int MaximumPageSize,
    int MaximumRepetitionPageSize,
    short MinimumSimilarityPercent,
    IReadOnlyList<short> SimilarityThresholds,
    IReadOnlyList<PhraseTextModeCapabilitiesDto> Modes);

public sealed record PhraseTextModeCapabilitiesDto(
    string Mode,
    IReadOnlyList<short> SupportedLengths,
    IReadOnlyList<short> RepeatedLengths,
    IReadOnlyList<short> SimilarityLengths,
    short MaximumSupportedLength,
    short MaximumRepeatedLength,
    short MaximumSimilarityLength);
