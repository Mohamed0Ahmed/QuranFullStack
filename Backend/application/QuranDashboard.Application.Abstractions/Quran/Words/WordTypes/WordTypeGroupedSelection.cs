namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed record WordTypeGroupedSelection(
    WordTypeGroupedDimensionKind Kind,
    int DimensionId,
    WordTypeFilter Filter)
{
    public bool IsValid => DimensionId > 0;
}
