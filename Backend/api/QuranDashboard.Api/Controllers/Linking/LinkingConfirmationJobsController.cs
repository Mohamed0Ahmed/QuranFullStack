using QuranDashboard.Api.Authorization;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Contracts.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Application.Linking.ConfirmationJobs;

namespace QuranDashboard.Api.Controllers.Linking;

[ApiController]
[Route("api/linking/confirmation-jobs")]
public sealed class LinkingConfirmationJobsController(
    AuthorizationStateAccessEvaluator stateEvaluator,
    GetLinkingConfirmationJobHandler getHandler,
    CancelLinkingConfirmationJobHandler cancelHandler) : ControllerBase
{
    [HttpGet("{jobId:guid}")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingConfirmationJobStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid jobId, CancellationToken cancellationToken)
    {
        var status = await getHandler.HandleAsync(
            await ResolveUserIdAsync(),
            jobId,
            cancellationToken);
        return status is null
            ? NotFound(ApiResponse<object>.Fail(ApiMessages.LinkingConfirmationJobNotFound))
            : Ok(ApiResponse<LinkingConfirmationJobStatusDto>.Ok(
                status,
                ApiMessages.LinkingConfirmationJobLoaded));
    }

    [HttpDelete("{jobId:guid}")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingConfirmationJobStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid jobId, CancellationToken cancellationToken)
    {
        var outcome = await cancelHandler.HandleAsync(
            await ResolveUserIdAsync(),
            jobId,
            cancellationToken);
        return outcome switch
        {
            CancelLinkingConfirmationJobOutcome.Success success =>
                Ok(ApiResponse<LinkingConfirmationJobStatusDto>.Ok(
                    success.Status,
                    ApiMessages.LinkingConfirmationJobCancelled)),
            CancelLinkingConfirmationJobOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.LinkingConfirmationJobNotFound)),
            CancelLinkingConfirmationJobOutcome.Conflict conflict =>
                Conflict(LifecycleError(conflict.FailureCode)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(CancelLinkingConfirmationJobOutcome)} variant."),
        };
    }

    private async Task<int> ResolveUserIdAsync()
    {
        var state = await stateEvaluator.ResolveActiveStateAsync(User);
        return state?.UserId
            ?? throw new InvalidOperationException(
                "An authorized confirmation job request resolved no authorization state.");
    }

    private static ApiResponse<LinkingLifecycleErrorData> LifecycleError(string code) => new()
    {
        IsSuccess = false,
        Message = ApiMessages.LinkingLifecycleMessage(code),
        Data = new LinkingLifecycleErrorData(code),
        Errors = [],
    };
}
