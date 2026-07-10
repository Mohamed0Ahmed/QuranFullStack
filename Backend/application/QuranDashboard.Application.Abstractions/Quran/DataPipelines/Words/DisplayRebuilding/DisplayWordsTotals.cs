namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;

public sealed record DisplayWordsTotals(
    int OrderedTashkeelRows,
    int OrderedSimpleRows,
    int UniqueTashkeelRows,
    int UniqueSimpleRows,
    int ReadableWords);
