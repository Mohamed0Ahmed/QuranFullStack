using QuranDashboard.Application.Abstractions.Quran.Words.Display;

namespace QuranDashboard.Application.Quran.Words.RebuildDisplayWords;

public sealed record RebuildDisplayWordsCommand(
    bool Force,
    string? ReportOutDir = null,
    int ExpectedReadableWords = DisplayWordsInvariants.ExpectedReadableWords);
