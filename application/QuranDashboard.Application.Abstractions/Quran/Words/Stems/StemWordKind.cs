namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems;

public enum StemWordKind
{
    Simple,
    Tashkeel,
}

public static class StemWordKindKeys
{
    public const string Simple = "simple";
    public const string Tashkeel = "tashkeel";
}

public static class StemWordKindParser
{
    public static bool TryParse(string? value, out StemWordKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case StemWordKindKeys.Simple:
                kind = StemWordKind.Simple;
                return true;
            case StemWordKindKeys.Tashkeel:
                kind = StemWordKind.Tashkeel;
                return true;
            default:
                return false;
        }
    }
}
