namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

// A validated root/stem/lemma selection: the numeric dimension identity plus the identical five-field
// grammatical scope (type/childCode/case/tense/voice) the grouped table row was displayed under.
public sealed record WordTypeGroupedSelection(
    WordTypeGroupedDimensionKind Kind,
    int DimensionId,
    WordTypeFilter Filter)
{
    public bool IsValid => DimensionId > 0;
}
