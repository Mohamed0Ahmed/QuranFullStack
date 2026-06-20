namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public sealed record I3rabRefusalReport(
    string RefusalReason,
    I3rabMorphologyReadiness Readiness,
    bool Forced);
