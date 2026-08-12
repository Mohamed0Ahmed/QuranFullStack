using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Contracts.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Linking.Queries.PreflightLinkingOperation;

namespace QuranDashboard.Api.Controllers.Linking;

[ApiController]
[Route("api/linking/operations")]
public sealed class LinkingOperationsController(PreflightLinkingOperationHandler preflightHandler) : ControllerBase
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
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PreflightLinkingOperationOutcome)} variant."),
        };
    }
}
