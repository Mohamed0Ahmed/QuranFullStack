namespace QuranDashboard.Domain.Quran.PhraseSearch;

public enum PhraseIndexBuildStatus : short
{
    Building = 1,
    Validated = 2,
    Active = 3,
    Superseded = 4,
    Failed = 5,
}
