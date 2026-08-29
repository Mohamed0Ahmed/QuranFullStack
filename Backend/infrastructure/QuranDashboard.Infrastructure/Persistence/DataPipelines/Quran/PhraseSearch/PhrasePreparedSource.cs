namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhrasePreparedSource(
    IReadOnlyList<PhrasePreparedSourceToken> Tokens,
    IReadOnlyList<PhrasePreparedSearchToken> SearchTokens);

internal sealed record PhrasePreparedSourceToken(
    int Id,
    int AyahId,
    short SurahNumber,
    short WordNumber,
    string TextUthmani,
    int SimpleExactId,
    int SimpleSearchId,
    int TashkilExactId,
    int TashkilSearchId);

internal sealed record PhrasePreparedSearchToken(
    short Mode,
    int Id,
    string SearchText,
    int[] ExactTokenIds);
