using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;

public sealed record RebuildDisplayWordsCommand(
    bool Force,
    string? ReportOutDir = null,
    int ExpectedReadableWords = DisplayWordsInvariants.ExpectedReadableWords);
