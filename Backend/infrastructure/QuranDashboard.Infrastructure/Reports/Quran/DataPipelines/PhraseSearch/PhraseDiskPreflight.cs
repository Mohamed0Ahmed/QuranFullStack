namespace QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.PhraseSearch;

internal sealed record PhraseDiskPreflight(
    long DatabaseBytes,
    long ExistingPhraseIndexBytes,
    long BuildWorkingSpaceBytes,
    long WalHeadroomBytes,
    long SafetyMarginBytes,
    long AvailableDatabaseFilesystemBytes,
    long RequiredFreeBytes,
    string ProofKind,
    bool ProofVerified,
    bool Passed)
{
    internal static PhraseDiskPreflight Unavailable { get; } =
        new(0, 0, 0, 0, 0, 0, 0, "unavailable", false, false);
}
