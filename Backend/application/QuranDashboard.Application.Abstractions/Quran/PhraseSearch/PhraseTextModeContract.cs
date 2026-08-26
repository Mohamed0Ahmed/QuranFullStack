using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Application.Abstractions.Quran.PhraseSearch;

public static class PhraseTextModeKeys
{
    public const string Simple = "simple";
    public const string Tashkil = "tashkil";
}

public static class PhraseTextModeContract
{
    public static bool TryParse(string? value, out PhraseTextMode mode)
    {
        if (string.Equals(value, PhraseTextModeKeys.Simple, StringComparison.OrdinalIgnoreCase))
        {
            mode = PhraseTextMode.Simple;
            return true;
        }

        if (string.Equals(value, PhraseTextModeKeys.Tashkil, StringComparison.OrdinalIgnoreCase))
        {
            mode = PhraseTextMode.Tashkil;
            return true;
        }

        mode = default;
        return false;
    }

    public static string CanonicalKey(PhraseTextMode mode) => mode switch
    {
        PhraseTextMode.Simple => PhraseTextModeKeys.Simple,
        PhraseTextMode.Tashkil => PhraseTextModeKeys.Tashkil,
        _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseTextMode)} value: {mode}."),
    };
}
