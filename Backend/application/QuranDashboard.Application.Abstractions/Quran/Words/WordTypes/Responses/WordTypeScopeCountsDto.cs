namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

public sealed record WordTypeScopeCountsDto(
    int WordsCount,
    int RootsCount,
    int StemsCount,
    int LemmasCount);
