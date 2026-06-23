namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

/// <summary>
/// The root's stems derived via morphology (<c>DISTINCT stem_id</c>), joined to
/// <c>quran_stems</c> for text. Stems are static, non-interactive;
/// <see cref="StemId"/> is retained for future linking.
/// </summary>
public sealed record RootStemsResponse(
    int Id,
    string RootText,
    int StemsCount,
    IReadOnlyList<RootStemItemDto> Stems);

public sealed record RootStemItemDto(
    int StemId,
    string StemText,
    int OccurrencesCount);
