using QuranDashboard.Api.Authorization;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Contracts.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Application.Linking.Commands.ConfirmLinkingOperation;
using QuranDashboard.Application.Linking.Queries.PreflightLinkingOperation;

namespace QuranDashboard.Api.Controllers.Linking;

[ApiController]
[Route("api/linking/operations")]
public sealed class LinkingOperationsController(
    AuthorizationStateAccessEvaluator stateEvaluator,
    PreflightLinkingOperationHandler preflightHandler,
    ConfirmLinkingOperationHandler confirmHandler) : ControllerBase
{
    [HttpPost("preflight")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingPreflightResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LinkingPreflightResultDto>>> Preflight(
        [FromBody] LinkingPreflightBody body,
        CancellationToken cancellationToken)
    {
        if (!LinkingOperationBodyMapper.TryMap(body, out var request, out var bodyViolation))
        {
            return BadRequest(ApiResponse<LinkingPreflightResultDto>.Fail(
                ApiMessages.LinkingOperationViolationMessage(bodyViolation)));
        }

        var outcome = await preflightHandler.HandleAsync(
            new PreflightLinkingOperationQuery(request),
            cancellationToken);

        return outcome switch
        {
            PreflightLinkingOperationOutcome.Success success =>
                Ok(ApiResponse<LinkingPreflightResultDto>.Ok(
                    success.Result, ApiMessages.LinkingOperationPreflighted)),
            PreflightLinkingOperationOutcome.InvalidRequest invalid =>
                BadRequest(ApiResponse<LinkingPreflightResultDto>.Fail(
                    ApiMessages.LinkingOperationViolationMessage(invalid.Violation))),
            PreflightLinkingOperationOutcome.InvalidDescriptor invalid =>
                BadRequest(ApiResponse<LinkingPreflightResultDto>.Fail(
                    ApiMessages.LinkingDescriptorViolationMessage(invalid.Violation))),
            PreflightLinkingOperationOutcome.DoorNotFound =>
                NotFound(ApiResponse<LinkingPreflightResultDto>.Fail(
                    ApiMessages.LinkingOperationDoorNotFound)),
            PreflightLinkingOperationOutcome.SourceNotFound notFound =>
                NotFound(ApiResponse<LinkingPreflightResultDto>.Fail(
                    ApiMessages.LinkingSourceNotFoundMessage(notFound.Reference))),
            PreflightLinkingOperationOutcome.LinkingDataStale =>
                Conflict(LinkingDataStaleResponse()),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PreflightLinkingOperationOutcome)} variant."),
        };
    }

    [HttpPost]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingConfirmationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingPreflightResultDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(
        [FromBody] LinkingConfirmationBody body,
        CancellationToken cancellationToken)
    {
        if (!LinkingOperationBodyMapper.TryMap(body, out var request, out var bodyViolation))
        {
            return BadRequest(ApiResponse<LinkingConfirmationResultDto>.Fail(
                ApiMessages.LinkingOperationViolationMessage(bodyViolation)));
        }

        var actor = await stateEvaluator.ResolveActiveStateAsync(User)
            ?? throw new InvalidOperationException(
                "An authorized linking confirmation request resolved no authorization state.");
        var outcome = await confirmHandler.HandleAsync(
            new ConfirmLinkingOperationCommand(actor, request),
            cancellationToken);

        return outcome switch
        {
            ConfirmLinkingOperationOutcome.Success success =>
                Ok(ApiResponse<LinkingConfirmationResultDto>.Ok(
                    success.Result,
                    success.Result.IsNoOp
                        ? ApiMessages.LinkingOperationNoChanges
                        : ApiMessages.LinkingOperationConfirmed)),
            ConfirmLinkingOperationOutcome.InvalidRequest invalid =>
                BadRequest(ApiResponse<LinkingConfirmationResultDto>.Fail(
                    ApiMessages.LinkingOperationViolationMessage(invalid.Violation))),
            ConfirmLinkingOperationOutcome.InvalidDescriptor invalid =>
                BadRequest(ApiResponse<LinkingConfirmationResultDto>.Fail(
                    ApiMessages.LinkingDescriptorViolationMessage(invalid.Violation))),
            ConfirmLinkingOperationOutcome.InvalidClassification =>
                BadRequest(ApiResponse<LinkingConfirmationResultDto>.Fail(
                    ApiMessages.LinkingOperationInvalidClassification)),
            ConfirmLinkingOperationOutcome.DoorNotFound =>
                NotFound(ApiResponse<LinkingConfirmationResultDto>.Fail(
                    ApiMessages.LinkingOperationDoorNotFound)),
            ConfirmLinkingOperationOutcome.SourceNotFound notFound =>
                NotFound(ApiResponse<LinkingConfirmationResultDto>.Fail(
                    ApiMessages.LinkingSourceNotFoundMessage(notFound.Reference))),
            ConfirmLinkingOperationOutcome.StaleVersion =>
                Conflict(ApiResponse<LinkingConfirmationResultDto>.Fail(
                    ApiMessages.LinkingOperationStaleVersion)),
            ConfirmLinkingOperationOutcome.StalePreflight stale =>
                Conflict(new ApiResponse<LinkingPreflightResultDto>
                {
                    IsSuccess = false,
                    Message = ApiMessages.LinkingOperationStalePreflight,
                    Data = stale.FreshClassification,
                    Errors = [],
                }),
            ConfirmLinkingOperationOutcome.DuplicateContribution =>
                Conflict(ApiResponse<LinkingConfirmationResultDto>.Fail(
                    ApiMessages.LinkingOperationDuplicateContribution)),
            ConfirmLinkingOperationOutcome.IdempotencyConflict =>
                Conflict(ApiResponse<LinkingConfirmationResultDto>.Fail(
                    ApiMessages.LinkingOperationIdempotencyConflict)),
            ConfirmLinkingOperationOutcome.LinkingDataStale =>
                Conflict(LinkingDataStaleResponse()),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(ConfirmLinkingOperationOutcome)} variant."),
        };
    }

    private static ApiResponse<LinkingLifecycleErrorData> LinkingDataStaleResponse() => new()
    {
        IsSuccess = false,
        Message = ApiMessages.LinkingDataStale,
        Data = new LinkingLifecycleErrorData("LINKING_DATA_STALE"),
        Errors = [],
    };
}
