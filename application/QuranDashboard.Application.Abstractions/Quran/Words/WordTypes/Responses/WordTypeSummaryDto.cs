namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

public sealed record WordTypeSummaryDto(
    int TashkeelWordId,
    string ContextCode,
    string DisplayText,
    WordTypeLabelDto TypeLabel,
    WordTypeLabelDto BroadLabel,
    string? CaseOrFeature,
    string? RootText,
    string? LemmaText,
    string? StemText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount);
