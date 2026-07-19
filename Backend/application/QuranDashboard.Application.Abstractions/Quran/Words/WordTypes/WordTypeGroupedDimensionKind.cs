namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public enum WordTypeGroupedDimensionKind
{
    Root,
    Stem,
    Lemma,
}

public static class WordTypeGroupedDimensionKindKeys
{
    // Plural route keys for the URL; the DTO discriminator stays singular to match the
    // WordTypeTableRowDto `kind` values (root|stem|lemma).
    public const string Roots = "roots";
    public const string Stems = "stems";
    public const string Lemmas = "lemmas";

    public const string Root = "root";
    public const string Stem = "stem";
    public const string Lemma = "lemma";
}

public static class WordTypeGroupedDimensionKindParser
{
    // Only the plural route keys are accepted; a missing/blank/unknown value fails, so the caller MUST
    // check the bool.
    public static bool TryParse(string? value, out WordTypeGroupedDimensionKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case WordTypeGroupedDimensionKindKeys.Roots:
                kind = WordTypeGroupedDimensionKind.Root;
                return true;
            case WordTypeGroupedDimensionKindKeys.Stems:
                kind = WordTypeGroupedDimensionKind.Stem;
                return true;
            case WordTypeGroupedDimensionKindKeys.Lemmas:
                kind = WordTypeGroupedDimensionKind.Lemma;
                return true;
            default:
                return false;
        }
    }

    public static string ToRouteKey(this WordTypeGroupedDimensionKind kind) => kind switch
    {
        WordTypeGroupedDimensionKind.Root => WordTypeGroupedDimensionKindKeys.Roots,
        WordTypeGroupedDimensionKind.Stem => WordTypeGroupedDimensionKindKeys.Stems,
        WordTypeGroupedDimensionKind.Lemma => WordTypeGroupedDimensionKindKeys.Lemmas,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown grouped dimension kind."),
    };

    public static string ToDtoKind(this WordTypeGroupedDimensionKind kind) => kind switch
    {
        WordTypeGroupedDimensionKind.Root => WordTypeGroupedDimensionKindKeys.Root,
        WordTypeGroupedDimensionKind.Stem => WordTypeGroupedDimensionKindKeys.Stem,
        WordTypeGroupedDimensionKind.Lemma => WordTypeGroupedDimensionKindKeys.Lemma,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown grouped dimension kind."),
    };
}
