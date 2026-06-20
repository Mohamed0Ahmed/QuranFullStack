namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

public sealed record LayoutDto(
    int PagesCount,
    int LinesPerPage,
    IReadOnlyDictionary<int, IReadOnlyList<LineDto>> Pages);
