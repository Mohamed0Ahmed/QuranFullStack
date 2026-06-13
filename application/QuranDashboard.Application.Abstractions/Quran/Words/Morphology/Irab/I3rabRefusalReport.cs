namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public sealed record I3rabRefusalReport(
    string RefusalReason,
    I3rabMorphologyReadiness Readiness,
    bool Forced);
