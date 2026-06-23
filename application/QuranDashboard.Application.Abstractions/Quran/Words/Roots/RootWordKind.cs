namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots;

public enum RootWordKind
{
    Simple,
    Tashkeel,
}

public static class RootWordKindKeys
{
    public const string Simple = "simple";
    public const string Tashkeel = "tashkeel";
}

public static class RootWordKindParser
{
    public static bool TryParse(string? value, out RootWordKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case RootWordKindKeys.Simple:
                kind = RootWordKind.Simple;
                return true;
            case RootWordKindKeys.Tashkeel:
                kind = RootWordKind.Tashkeel;
                return true;
            default:
                return false;
        }
    }
}
