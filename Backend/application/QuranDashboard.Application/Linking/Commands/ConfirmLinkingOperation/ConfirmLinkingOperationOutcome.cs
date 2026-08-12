using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Application.Linking.Commands.ConfirmLinkingOperation;

public abstract record ConfirmLinkingOperationOutcome
{
    private ConfirmLinkingOperationOutcome() { }

    public sealed record Success(
        LinkingConfirmationResultDto Result,
        bool IsReplay) : ConfirmLinkingOperationOutcome;

    public sealed record InvalidRequest(
        LinkingOperationViolation Violation) : ConfirmLinkingOperationOutcome;

    public sealed record InvalidDescriptor(
        LinkingDescriptorViolation Violation) : ConfirmLinkingOperationOutcome;

    public sealed record InvalidClassification : ConfirmLinkingOperationOutcome;

    public sealed record DoorNotFound(int DoorId) : ConfirmLinkingOperationOutcome;

    public sealed record SourceNotFound(string Reference) : ConfirmLinkingOperationOutcome;

    public sealed record StaleVersion : ConfirmLinkingOperationOutcome;

    public sealed record StalePreflight(
        LinkingPreflightResultDto FreshClassification) : ConfirmLinkingOperationOutcome;

    public sealed record DuplicateContribution : ConfirmLinkingOperationOutcome;

    public sealed record IdempotencyConflict : ConfirmLinkingOperationOutcome;
}
