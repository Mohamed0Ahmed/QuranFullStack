namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingWorkspaceSourceNotFoundException(long sourceId)
    : Exception($"The prepared source {sourceId} does not exist in the caller's workspace.")
{
    public long SourceId { get; } = sourceId;
}
