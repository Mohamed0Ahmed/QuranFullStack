namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

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
