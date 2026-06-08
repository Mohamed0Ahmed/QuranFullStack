namespace QuranDashboard.Application.Abstractions.Quran.Import;

public sealed record WordRecordDto(
    int Id,
    int Surah,
    int Ayah,
    int Word,
    string Location,
    string Text);
