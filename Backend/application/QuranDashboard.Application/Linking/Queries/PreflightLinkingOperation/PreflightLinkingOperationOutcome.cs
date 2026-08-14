using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Application.Linking.Queries.PreflightLinkingOperation;

public abstract record PreflightLinkingOperationOutcome
{
    private PreflightLinkingOperationOutcome() { }

    public sealed record Success(LinkingPreflightResultDto Result) : PreflightLinkingOperationOutcome;

    public sealed record InvalidRequest(LinkingOperationViolation Violation) : PreflightLinkingOperationOutcome;

    public sealed record InvalidDescriptor(LinkingDescriptorViolation Violation) : PreflightLinkingOperationOutcome;

    public sealed record DoorNotFound(int DoorId) : PreflightLinkingOperationOutcome;

    public sealed record SourceNotFound(string Reference) : PreflightLinkingOperationOutcome;

    public sealed record LinkingDataStale : PreflightLinkingOperationOutcome;
}
