namespace QuranDashboard.Application.Abstractions.Quran.Words.Display;

public sealed record DisplayWordsTotals(
    int OrderedTashkeelRows,
    int OrderedSimpleRows,
    int UniqueTashkeelRows,
    int UniqueSimpleRows,
    int ReadableWords);
