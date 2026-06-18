namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

/// <summary>
/// Configured default source keys for the Mushaf reader's ayah-study view.
/// Bound from the <c>MushafReader</c> configuration section. A null/unknown key
/// yields a per-kind empty state in the handler (never a silent substitution).
/// </summary>
public sealed class MushafReaderOptions
{
    public string? DefaultTafsirSourceKey { get; init; }

    public string? DefaultTranslationSourceKey { get; init; }

    public string? DefaultFullI3rabSourceKey { get; init; }
}
