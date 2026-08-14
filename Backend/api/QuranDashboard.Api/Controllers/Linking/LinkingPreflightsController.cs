using QuranDashboard.Api.Authorization;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Contracts.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Application.Linking.PreparedPreflights;

namespace QuranDashboard.Api.Controllers.Linking;

[ApiController]
[Route("api/linking/preflights")]
public sealed class LinkingPreflightsController(
    AuthorizationStateAccessEvaluator stateEvaluator,
    CreateLinkingPreparedPreflightHandler createHandler,
    GetLinkingPreparedPreflightHandler getHandler,
    CancelLinkingPreparedPreflightHandler cancelHandler,
    GetLinkingPreparedDetailPageHandler detailHandler) : ControllerBase
{
    [HttpPost]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingPreparedPreflightStatusDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<LinkingPreparedPreflightStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLinkingPreparedPreflightBody? body,
        CancellationToken cancellationToken)
    {
        if (!LinkingPreparedPreflightBodyMapper.TryMap(body, out var request))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiMessages.LinkingPreparedPreflightInvalid));
        }

        var outcome = await createHandler.HandleAsync(
            await ResolveUserIdAsync(),
            request,
            cancellationToken);
        return outcome switch
        {
            CreateLinkingPreparedPreflightOutcome.Success success when success.Accepted =>
                AcceptedAtAction(
                    nameof(Get),
                    new { preflightId = success.Receipt.Status.PreflightId },
                    ApiResponse<LinkingPreparedPreflightStatusDto>.Ok(
                        success.Receipt.Status,
                        ApiMessages.LinkingPreparedPreflightAccepted)),
            CreateLinkingPreparedPreflightOutcome.Success success =>
                Ok(ApiResponse<LinkingPreparedPreflightStatusDto>.Ok(
                    success.Receipt.Status,
                    ApiMessages.LinkingPreparedPreflightLoaded)),
            CreateLinkingPreparedPreflightOutcome.InvalidRequest =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.LinkingPreparedPreflightInvalid)),
            CreateLinkingPreparedPreflightOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.LinkingSourceNotFound)),
            CreateLinkingPreparedPreflightOutcome.Conflict conflict =>
                Conflict(LifecycleError(conflict.FailureCode)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(CreateLinkingPreparedPreflightOutcome)} variant."),
        };
    }

    [HttpGet("{preflightId:guid}")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingPreparedPreflightStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid preflightId, CancellationToken cancellationToken)
    {
        var status = await getHandler.HandleAsync(
            await ResolveUserIdAsync(),
            preflightId,
            cancellationToken);
        return status is null
            ? NotFound(ApiResponse<object>.Fail(ApiMessages.LinkingPreparedPreflightNotFound))
            : Ok(ApiResponse<LinkingPreparedPreflightStatusDto>.Ok(
                status,
                ApiMessages.LinkingPreparedPreflightLoaded));
    }

    [HttpDelete("{preflightId:guid}")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingPreparedPreflightStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status410Gone)]
    public async Task<IActionResult> Cancel(Guid preflightId, CancellationToken cancellationToken)
    {
        var outcome = await cancelHandler.HandleAsync(
            await ResolveUserIdAsync(),
            preflightId,
            cancellationToken);
        return outcome switch
        {
            CancelLinkingPreparedPreflightOutcome.Success success =>
                Ok(ApiResponse<LinkingPreparedPreflightStatusDto>.Ok(
                    success.Status,
                    ApiMessages.LinkingPreparedPreflightCancelled)),
            CancelLinkingPreparedPreflightOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.LinkingPreparedPreflightNotFound)),
            CancelLinkingPreparedPreflightOutcome.Conflict conflict when conflict.Expired =>
                StatusCode(StatusCodes.Status410Gone, LifecycleError(conflict.FailureCode)),
            CancelLinkingPreparedPreflightOutcome.Conflict conflict =>
                Conflict(LifecycleError(conflict.FailureCode)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(CancelLinkingPreparedPreflightOutcome)} variant."),
        };
    }

    [HttpGet("{preflightId:guid}/sources/{preparedSourceId:long}/ayahs")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingPreparedDetailPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status410Gone)]
    public Task<IActionResult> GetSourceDetails(
        Guid preflightId,
        long preparedSourceId,
        [FromQuery] string filter,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken) =>
        GetDetails(preflightId, preparedSourceId, filter, page, pageSize, cancellationToken);

    [HttpGet("{preflightId:guid}/merged-ayahs")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingPreparedDetailPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status410Gone)]
    public Task<IActionResult> GetMergedDetails(
        Guid preflightId,
        [FromQuery] string filter,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken) =>
        GetDetails(preflightId, null, filter, page, pageSize, cancellationToken);

    private async Task<IActionResult> GetDetails(
        Guid preflightId,
        long? preparedSourceId,
        string filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await detailHandler.HandleAsync(
            await ResolveUserIdAsync(),
            preflightId,
            preparedSourceId,
            filter,
            page,
            pageSize,
            cancellationToken);
        return outcome switch
        {
            GetLinkingPreparedDetailPageOutcome.Success success =>
                Ok(ApiResponse<LinkingPreparedDetailPageDto>.Ok(
                    success.Page,
                    ApiMessages.LinkingPreparedPreflightLoaded)),
            GetLinkingPreparedDetailPageOutcome.InvalidRequest =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.LinkingPreparedPreflightInvalid)),
            GetLinkingPreparedDetailPageOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.LinkingPreparedPreflightNotFound)),
            GetLinkingPreparedDetailPageOutcome.Conflict conflict when conflict.Expired =>
                StatusCode(StatusCodes.Status410Gone, LifecycleError(conflict.FailureCode)),
            GetLinkingPreparedDetailPageOutcome.Conflict conflict =>
                Conflict(LifecycleError(conflict.FailureCode)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(GetLinkingPreparedDetailPageOutcome)} variant."),
        };
    }

    private async Task<int> ResolveUserIdAsync()
    {
        var state = await stateEvaluator.ResolveActiveStateAsync(User);
        return state?.UserId
            ?? throw new InvalidOperationException(
                "An authorized prepared linking request resolved no authorization state.");
    }

    private static ApiResponse<LinkingLifecycleErrorData> LifecycleError(string code) => new()
    {
        IsSuccess = false,
        Message = ApiMessages.LinkingLifecycleMessage(code),
        Data = new LinkingLifecycleErrorData(code),
        Errors = [],
    };
}
