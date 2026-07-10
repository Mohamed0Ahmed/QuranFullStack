namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public sealed record I3rabSegmentLabel(
    int SegmentId,
    string? I3rabArabic,
    string SignatureKey,
    string Status,
    string? ReviewReason);
