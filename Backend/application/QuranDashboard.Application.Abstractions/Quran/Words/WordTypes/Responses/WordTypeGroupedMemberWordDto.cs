namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

// Root/lemma/stem text are projection-only display fields and never participate in membership — the row
// belongs to its group solely by its numeric head dimension ID.
public sealed record WordTypeGroupedMemberWordDto(
    int TashkeelWordId,
    string ContextCode,
    string? Case,
    string? Tense,
    string? Voice,
    string DisplayText,
    string TypeCode,
    WordTypeLabelDto TypeLabel,
    WordTypeLabelDto BroadLabel,
    string? CaseOrFeature,
    string? RootText,
    string? LemmaText,
    string? StemText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount);
