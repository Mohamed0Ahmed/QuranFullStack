namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public sealed record I3rabSegmentLabel(
    int SegmentId,
    string? I3rabArabic,
    string SignatureKey,
    string Status,
    string? ReviewReason);
