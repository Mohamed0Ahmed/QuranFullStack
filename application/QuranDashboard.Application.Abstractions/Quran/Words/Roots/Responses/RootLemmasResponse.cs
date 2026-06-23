namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

/// <summary>
/// The root's lemmas derived by morphology co-occurrence
/// (<c>DISTINCT lemma_id</c> where <c>root_id = id</c>). The item count MUST
/// equal <see cref="RootListItemDto.LemmasCount"/> for the same root. Lemmas are
/// static, non-interactive; <see cref="LemmaId"/> is retained for future linking.
/// </summary>
public sealed record RootLemmasResponse(
    int Id,
    string RootText,
    int LemmasCount,
    IReadOnlyList<RootLemmaItemDto> Lemmas);

public sealed record RootLemmaItemDto(
    int LemmaId,
    string LemmaText,
    int OccurrencesCount);
