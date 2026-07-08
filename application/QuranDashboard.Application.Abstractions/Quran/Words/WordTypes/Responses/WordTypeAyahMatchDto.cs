using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

public sealed record WordTypeAyahMatchDto(
    string VerseKey,
    int SurahNumber,
    int AyahNumber,
    short PageNumber,
    IReadOnlyList<int> MatchedWordPositions,
    IReadOnlyList<int> MatchedWordIds,
    IReadOnlyList<AyahWordForHighlightDto> Words);
