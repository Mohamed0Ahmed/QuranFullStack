namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseRepetitionOccurrences;

public sealed record GetPhraseRepetitionOccurrencesQuery(
    Guid BuildId,
    long VariantId,
    int? Page,
    int? PageSize);
