using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Application.Linking;

public abstract record LinkingWorkspaceOutcome
{
    private LinkingWorkspaceOutcome() { }

    public sealed record Success(LinkingWorkspaceDto Workspace) : LinkingWorkspaceOutcome;

    public sealed record InvalidRequest(LinkingWorkspaceViolation Violation) : LinkingWorkspaceOutcome;

    public sealed record InvalidDescriptor(LinkingDescriptorViolation Violation) : LinkingWorkspaceOutcome;

    public sealed record SourceNotFound(long SourceId) : LinkingWorkspaceOutcome;

    public sealed record StaleVersion : LinkingWorkspaceOutcome;

    public sealed record DuplicateSource : LinkingWorkspaceOutcome;

    public sealed record LinkingDataStale : LinkingWorkspaceOutcome;
}
