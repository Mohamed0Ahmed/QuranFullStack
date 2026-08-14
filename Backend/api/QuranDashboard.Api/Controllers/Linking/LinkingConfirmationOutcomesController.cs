using QuranDashboard.Api.Authorization;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Contracts.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Application.Linking.ConfirmationJobs;

namespace QuranDashboard.Api.Controllers.Linking;

[ApiController]
[Route("api/linking/confirmation-outcomes")]
public sealed class LinkingConfirmationOutcomesController(
    AuthorizationStateAccessEvaluator stateEvaluator,
    GetLinkingConfirmationOutcomeHandler getHandler) : ControllerBase
{
    [HttpGet("{idempotencyKey:guid}")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingDurableConfirmationOutcomeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Get(Guid idempotencyKey, CancellationToken cancellationToken)
    {
        var outcome = await getHandler.HandleAsync(
            await ResolveUserIdAsync(),
            idempotencyKey,
            cancellationToken);
        return outcome switch
        {
            GetLinkingConfirmationOutcome.Success success =>
                Ok(ApiResponse<LinkingDurableConfirmationOutcomeDto>.Ok(
                    success.Outcome,
                    ApiMessages.LinkingConfirmationOutcomeLoaded)),
            GetLinkingConfirmationOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.LinkingConfirmationOutcomeNotFound)),
            GetLinkingConfirmationOutcome.Conflict conflict =>
                Conflict(LifecycleError(conflict.FailureCode)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(GetLinkingConfirmationOutcome)} variant."),
        };
    }

    private async Task<int> ResolveUserIdAsync()
    {
        var state = await stateEvaluator.ResolveActiveStateAsync(User);
        return state?.UserId
            ?? throw new InvalidOperationException(
                "An authorized confirmation outcome request resolved no authorization state.");
    }

    private static ApiResponse<LinkingLifecycleErrorData> LifecycleError(string code) => new()
    {
        IsSuccess = false,
        Message = ApiMessages.LinkingLifecycleMessage(code),
        Data = new LinkingLifecycleErrorData(code),
        Errors = [],
    };
}
