using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;

namespace QuranDashboard.Api.Contracts.Linking;

public sealed record CreateLinkingConfirmationJobBody
{
    public string? PreflightToken { get; init; }
    public Guid? IdempotencyKey { get; init; }
}

public sealed record LinkingConfirmationSubmissionResponse(
    string ResourceKind,
    LinkingConfirmationJobStatusDto? Job,
    LinkingDurableConfirmationOutcomeDto? DurableOutcome);

internal static class LinkingConfirmationJobBodyMapper
{
    internal static bool TryMap(
        Guid preflightId,
        CreateLinkingConfirmationJobBody? body,
        out CreateLinkingConfirmationJobRequest request)
    {
        request = null!;
        if (preflightId == Guid.Empty
            || string.IsNullOrWhiteSpace(body?.PreflightToken)
            || body.IdempotencyKey is not { } idempotencyKey
            || idempotencyKey == Guid.Empty)
        {
            return false;
        }

        request = new CreateLinkingConfirmationJobRequest(
            preflightId,
            body.PreflightToken.Trim(),
            idempotencyKey);
        return true;
    }

    internal static LinkingConfirmationSubmissionResponse ToResponse(
        LinkingConfirmationSubmissionDto submission) => submission switch
        {
            LinkingConfirmationSubmissionDto.Job job =>
                new LinkingConfirmationSubmissionResponse(job.ResourceKind, job.Resource, null),
            LinkingConfirmationSubmissionDto.DurableOutcome outcome =>
                new LinkingConfirmationSubmissionResponse(outcome.ResourceKind, null, outcome.Resource),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(LinkingConfirmationSubmissionDto)} variant."),
        };
}
