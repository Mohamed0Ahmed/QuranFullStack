namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed class WordTypeGroupedSelection
{
    private WordTypeGroupedSelection(WordTypeGroupedDimensionKind kind, int dimensionId, WordTypeScope scope)
    {
        Kind = kind;
        DimensionId = dimensionId;
        Scope = scope;
    }

    public WordTypeGroupedDimensionKind Kind { get; }
    public int DimensionId { get; }
    public WordTypeScope Scope { get; }

    public static WordTypeGroupedSelection? Create(WordTypeGroupedDimensionKind? kind, int dimensionId, WordTypeScope? scope) =>
        kind is null || dimensionId <= 0 || scope is null
            ? null
            : new WordTypeGroupedSelection(kind, dimensionId, scope);
}
