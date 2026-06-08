namespace QuranDashboard.Application.Abstractions.Quran.Import;

public sealed record LayoutDto(
    int PagesCount,
    int LinesPerPage,
    IReadOnlyDictionary<int, IReadOnlyList<LineDto>> Pages);
