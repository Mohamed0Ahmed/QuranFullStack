namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

public sealed record WordRecordDto(
    int Id,
    int Surah,
    int Ayah,
    int Word,
    string Location,
    string Text,
    string? TextClean = null);
