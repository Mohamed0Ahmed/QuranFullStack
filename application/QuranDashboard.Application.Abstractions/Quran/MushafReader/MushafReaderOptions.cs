namespace QuranDashboard.Application.Abstractions.Quran.MushafReader;

public sealed class MushafReaderOptions
{
    public string? DefaultTafsirSourceKey { get; init; }

    public string? DefaultTranslationSourceKey { get; init; }

    public string? DefaultFullI3rabSourceKey { get; init; }
}
