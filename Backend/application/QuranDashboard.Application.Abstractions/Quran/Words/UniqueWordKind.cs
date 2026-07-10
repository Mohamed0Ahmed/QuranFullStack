namespace QuranDashboard.Application.Abstractions.Quran.Words;

public enum UniqueWordKind
{
    Tashkeel,
    Simple,
}

public static class UniqueWordKindKeys
{
    public const string Tashkeel = "tashkeel";
    public const string Simple = "simple";
}

public static class UniqueWordKindParser
{

    public static bool TryParse(string? value, out UniqueWordKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case UniqueWordKindKeys.Tashkeel:
                kind = UniqueWordKind.Tashkeel;
                return true;
            case UniqueWordKindKeys.Simple:
                kind = UniqueWordKind.Simple;
                return true;
            default:
                return false;
        }
    }
}
