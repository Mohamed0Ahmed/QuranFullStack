namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingDataStaleException(long expectedRevision, long actualRevision)
    : Exception("The linking data revision no longer matches the displayed Quran data.")
{
    public long ExpectedRevision { get; } = expectedRevision;

    public long ActualRevision { get; } = actualRevision;
}
