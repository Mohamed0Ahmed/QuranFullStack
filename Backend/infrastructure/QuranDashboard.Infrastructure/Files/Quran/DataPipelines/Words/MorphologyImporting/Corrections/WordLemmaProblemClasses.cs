namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

internal static class WordLemmaProblemClasses
{
    public const string MissingRecovery = "missing-recovery";
    public const string MultiStem = "multi-stem";
    public const string Shift59 = "shift-59";
    public const string Shift63 = "shift-63";
    public const string Shift63Replace = "shift-63-replace";
    public const string Uncertain = "uncertain";

    public static readonly IReadOnlySet<string> Known = new HashSet<string>(StringComparer.Ordinal)
    {
        MissingRecovery,
        MultiStem,
        Shift59,
        Shift63,
        Shift63Replace,
        Uncertain,
    };
}
