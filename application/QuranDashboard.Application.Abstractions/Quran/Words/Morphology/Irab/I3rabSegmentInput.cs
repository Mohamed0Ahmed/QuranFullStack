namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public sealed record I3rabSegmentInput(
    int SegmentId,
    int QuranWordId,
    short SegmentNumber,
    string Kind,
    string Pos,
    string FeaturesRaw,
    string? CaseFeature,
    string? VerbTense,
    string? VerbVoice,
    bool IsAllahLemma,
    bool FormIsNull);
