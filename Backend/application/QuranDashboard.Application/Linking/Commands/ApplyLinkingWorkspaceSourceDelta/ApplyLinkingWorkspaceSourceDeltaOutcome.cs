using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Commands.ApplyLinkingWorkspaceSourceDelta;

public abstract record ApplyLinkingWorkspaceSourceDeltaOutcome
{
    private ApplyLinkingWorkspaceSourceDeltaOutcome() { }

    public sealed record Success(LinkingWorkspaceDeltaAcknowledgement Acknowledgement)
        : ApplyLinkingWorkspaceSourceDeltaOutcome;
    public sealed record InvalidRequest(LinkingWorkspaceViolation Violation)
        : ApplyLinkingWorkspaceSourceDeltaOutcome;
    public sealed record SourceNotFound : ApplyLinkingWorkspaceSourceDeltaOutcome;
    public sealed record StaleVersion : ApplyLinkingWorkspaceSourceDeltaOutcome;
    public sealed record LinkingDataStale : ApplyLinkingWorkspaceSourceDeltaOutcome;
}
