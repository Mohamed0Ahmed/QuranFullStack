namespace QuranDashboard.Application.Abstractions.Quran.Words;

/// <summary>
/// The two Unique Words explorer modes (Feature 014).
/// <list type="table">
/// <item><c>Tashkeel</c> — unique words distinguished by Uthmani text with tashkeel.</item>
/// <item><c>Simple</c> — unique words grouped by the simplified imlaei key,
/// displayed with representative Uthmani text.</item>
/// </list>
/// </summary>
public enum UniqueWordKind
{
    Tashkeel,
    Simple,
}

/// <summary>
/// Route/JSON key strings for <see cref="UniqueWordKind"/>. Stable,
/// label-independent route keys are required by the frontend routing contract.
/// </summary>
public static class UniqueWordKindKeys
{
    public const string Tashkeel = "tashkeel";
    public const string Simple = "simple";
}

public static class UniqueWordKindParser
{
    /// <summary>
    /// Parses a route/query <paramref name="value"/> into a
    /// <see cref="UniqueWordKind"/>. Case-insensitive. Returns <see langword="false"/>
    /// for unsupported or empty values so the handler maps to a controlled
    /// <c>400</c> instead of throwing.
    /// </summary>
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
