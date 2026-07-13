namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

// A single display-only member word-context row of a grouped root/stem/lemma. Shaped as a superset of
// the Words table row (WordTableRowDto): the same (TashkeelWordId, ContextCode) identity plus the active
// Case/Tense/Voice scope that produced it. Root/lemma/stem text are projection-only display fields and
// never participate in membership — the row belongs to the group solely by its numeric head dimension ID.
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
