namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

public sealed record LineDto(
    int PageNumber,
    int LineNumber,
    string LineType,
    bool IsCentered,
    int? SurahNumber,
    int? FirstWordId,
    int? LastWordId);
