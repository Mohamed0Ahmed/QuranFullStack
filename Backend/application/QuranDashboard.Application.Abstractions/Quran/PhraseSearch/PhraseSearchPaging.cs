namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public static class PhraseSearchPaging
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaximumPageSize = 100;
    public const int MaximumRepetitionPageSize = 1000;
    public const int MaximumRepetitionOccurrencePageSize = 250;
    public const int MaximumContextResultPageSize = 200;
    public const int MinimumRepetitionLength = 2;
    public const int MaximumSourceLength = 128;
}
