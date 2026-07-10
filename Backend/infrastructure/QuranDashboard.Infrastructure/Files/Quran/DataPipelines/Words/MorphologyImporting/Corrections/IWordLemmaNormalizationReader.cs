namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

public interface IWordLemmaNormalizationReader
{
    WordLemmaNormalizationLoaded Load();

    WordLemmaNormalizationResult Apply(
        IReadOnlyDictionary<string, string> rawLemmas,
        WordLemmaNormalizationLoaded loaded,
        IReadOnlySet<string>? readableWordLocations = null,
        string? rawLemmasSha256 = null);
}
