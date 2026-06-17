using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

/// <summary>
/// Reads one ayah's study context: core identity plus the three selected
/// source kinds (tafsir, translation, full i3rab) loaded TOGETHER. Resolved
/// source keys are passed in already (defaults applied by the handler); a kind
/// whose source resolves to nothing returns <c>null</c> for that block only.
/// </summary>
public interface IAyahStudyReader
{
    Task<AyahStudyResponse?> GetAyahStudyAsync(
        string verseKey,
        string? tafsirSourceKey,
        string? translationSourceKey,
        string? fullI3rabSourceKey,
        CancellationToken ct);
}
