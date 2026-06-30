namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

public sealed record WordTypeAyahMatchDto(
    string VerseKey,
    int SurahNumber,
    int AyahNumber,
    string AyahText,
    IReadOnlyList<int> MatchedWordPositions,
    IReadOnlyList<int> MatchedWordIds);
