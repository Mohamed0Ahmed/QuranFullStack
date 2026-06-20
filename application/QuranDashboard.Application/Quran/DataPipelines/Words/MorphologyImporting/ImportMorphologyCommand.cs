using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;

public sealed record ImportMorphologyCommand(
    string SourcePath,
    bool Force,
    int ExpectedReadableWords = MorphologyInvariants.ExpectedReadableWords,
    string? ReportOutDir = null);
