namespace QuranDashboard.Application.Quran.DataPipelines.PhraseSearch;

public sealed record BuildPhraseIndexCommand(bool Force, string ReportRootDirectory);
