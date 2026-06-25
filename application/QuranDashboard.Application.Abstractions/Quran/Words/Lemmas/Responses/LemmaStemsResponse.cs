namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

/// <summary>
/// Related stems for a lemma — distinct non-null <c>stem_id</c> values from the
/// lemma's matching morphology rows, with scoped counts and deterministic
/// ordering by count then earliest Mushaf occurrence. The invariant
/// <c>StemsCount == Stems.Count</c> holds.
/// </summary>
public sealed record LemmaStemsResponse(
    int Id,
    string LemmaText,
    int StemsCount,
    IReadOnlyList<LemmaStemItemDto> Stems);

public sealed record LemmaStemItemDto(
    int StemId,
    string StemText,
    int OccurrencesCount);
