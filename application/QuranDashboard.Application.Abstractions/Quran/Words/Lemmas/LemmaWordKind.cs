namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

public enum LemmaWordKind
{
    Simple,
    Tashkeel,
}

public static class LemmaWordKindKeys
{
    public const string Simple = "simple";
    public const string Tashkeel = "tashkeel";
}

public static class LemmaWordKindParser
{
    public static bool TryParse(string? value, out LemmaWordKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case LemmaWordKindKeys.Simple:
                kind = LemmaWordKind.Simple;
                return true;
            case LemmaWordKindKeys.Tashkeel:
                kind = LemmaWordKind.Tashkeel;
                return true;
            default:
                return false;
        }
    }
}
