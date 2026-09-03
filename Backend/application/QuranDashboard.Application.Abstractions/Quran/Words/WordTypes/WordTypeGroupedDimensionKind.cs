namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed class WordTypeGroupedDimensionKind
{
    private const string Roots = "roots";
    private const string Stems = "stems";
    private const string Lemmas = "lemmas";
    private const string Root = "root";
    private const string Stem = "stem";
    private const string Lemma = "lemma";

    private WordTypeGroupedDimensionKind(string routeKey, string dtoKind)
    {
        RouteKey = routeKey;
        DtoKind = dtoKind;
    }

    public string RouteKey { get; }
    public string DtoKind { get; }

    public static WordTypeGroupedDimensionKind? Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            Roots => new(Roots, Root),
            Stems => new(Stems, Stem),
            Lemmas => new(Lemmas, Lemma),
            _ => null,
        };
    }
}
