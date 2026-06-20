namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

public sealed record AyahMetaDto(
    int Id,
    int SurahNumber,
    int AyahNumber,
    string VerseKey,
    int WordsCount,
    string Text);
