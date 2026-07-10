namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Related lemmas for a stem — distinct non-null <c>lemma_id</c> values from the
/// stem's matching morphology rows, with scoped counts, ordered deterministically
/// by count then earliest Mushaf occurrence.
/// </summary>
public sealed record StemLemmasResponse(
    IReadOnlyList<StemLemmaItemDto> Lemmas);

public sealed record StemLemmaItemDto(
    int LemmaId,
    string LemmaText,
    int OccurrencesCount);
