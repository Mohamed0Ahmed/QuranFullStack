namespace QuranDashboard.Application.Abstractions.Quran.Import;

public sealed record LineDto(
    int PageNumber,
    int LineNumber,
    string LineType,
    bool IsCentered,
    int? SurahNumber,
    int? FirstWordId,
    int? LastWordId);
