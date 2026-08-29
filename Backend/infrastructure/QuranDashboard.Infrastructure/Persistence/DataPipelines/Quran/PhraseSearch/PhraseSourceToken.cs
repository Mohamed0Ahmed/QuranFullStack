namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseSourceToken(
    int Id,
    int AyahId,
    short SurahNumber,
    short WordNumber,
    string TextUthmani,
    string WordKeyImlaeiSimple,
    string TashkilIdentity,
    int UniqueSimpleWordId,
    int UniqueTashkeelWordId);
