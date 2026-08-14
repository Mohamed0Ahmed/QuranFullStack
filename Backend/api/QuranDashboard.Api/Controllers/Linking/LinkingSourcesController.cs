using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Contracts.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Application.Linking.Queries.ResolveLinkingSource;
using QuranDashboard.Application.Linking.Queries.ResolveLinkingSourcePage;

namespace QuranDashboard.Api.Controllers.Linking;

[ApiController]
[Route("api/linking/sources")]
public sealed class LinkingSourcesController(
    ResolveLinkingSourceHandler resolveHandler,
    ResolveLinkingSourcePageHandler resolvePageHandler) : ControllerBase
{
    [HttpPost("resolve")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingResolvedSourceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LinkingResolvedSourceDto>>> Resolve(
        [FromBody] LinkingSourceDescriptorBody body,
        CancellationToken cancellationToken)
    {
        if (!LinkingSourceDescriptorBodyMapper.TryMap(body, out var descriptor, out var bodyViolation))
        {
            return BadRequest(ApiResponse<LinkingResolvedSourceDto>.Fail(
                ApiMessages.LinkingDescriptorViolationMessage(bodyViolation)));
        }

        var outcome = await resolveHandler.HandleAsync(
            new ResolveLinkingSourceQuery(descriptor),
            cancellationToken);

        return outcome switch
        {
            ResolveLinkingSourceOutcome.Success success =>
                Ok(ApiResponse<LinkingResolvedSourceDto>.Ok(success.Source, ApiMessages.LinkingSourceResolved)),
            ResolveLinkingSourceOutcome.InvalidDescriptor invalid =>
                BadRequest(ApiResponse<LinkingResolvedSourceDto>.Fail(
                    ApiMessages.LinkingDescriptorViolationMessage(invalid.Violation))),
            ResolveLinkingSourceOutcome.NotFound notFound =>
                NotFound(ApiResponse<LinkingResolvedSourceDto>.Fail(
                    ApiMessages.LinkingSourceNotFoundMessage(notFound.Reference))),
            _ => throw new InvalidOperationException($"Unhandled {nameof(ResolveLinkingSourceOutcome)} variant."),
        };
    }

    [HttpPost("resolve-page")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingResolvedSourcePageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolvePage(
        [FromBody] LinkingSourcePageBody? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiMessages.LinkingSourcePageInvalid));
        }

        if (!LinkingSourceDescriptorBodyMapper.TryMap(
                body.Descriptor,
                out var descriptor,
                out var descriptorViolation))
        {
            return BadRequest(ApiResponse<object>.Fail(
                ApiMessages.LinkingDescriptorViolationMessage(descriptorViolation)));
        }

        if (!LinkingSourcePageBodyMapper.TryMapView(body.View, out var view)
            || body.Page is null
            || body.PageSize is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiMessages.LinkingSourcePageInvalid));
        }

        var outcome = await resolvePageHandler.HandleAsync(
            new ResolveLinkingSourcePageQuery(
                descriptor,
                body.ExpectedLinkingDataRevision,
                body.ExpectedSourceViewIdentity,
                view,
                body.Page.Value,
                body.PageSize.Value),
            cancellationToken);

        return outcome switch
        {
            ResolveLinkingSourcePageOutcome.Success success =>
                Ok(ApiResponse<LinkingResolvedSourcePageDto>.Ok(
                    success.Page,
                    ApiMessages.LinkingSourceResolved)),
            ResolveLinkingSourcePageOutcome.InvalidRequest =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.LinkingSourcePageInvalid)),
            ResolveLinkingSourcePageOutcome.InvalidDescriptor invalid =>
                BadRequest(ApiResponse<object>.Fail(
                    ApiMessages.LinkingDescriptorViolationMessage(invalid.Violation))),
            ResolveLinkingSourcePageOutcome.NotFound notFound =>
                NotFound(ApiResponse<object>.Fail(
                    ApiMessages.LinkingSourceNotFoundMessage(notFound.Reference))),
            ResolveLinkingSourcePageOutcome.LinkingDataStale =>
                Conflict(LifecycleError("LINKING_DATA_STALE", ApiMessages.LinkingDataStale)),
            ResolveLinkingSourcePageOutcome.SourceViewStale =>
                Conflict(LifecycleError("SOURCE_VIEW_STALE", ApiMessages.LinkingSourceViewStale)),
            ResolveLinkingSourcePageOutcome.TransientFailure =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.Fail(ApiMessages.LinkingSourceReadTransientFailure)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(ResolveLinkingSourcePageOutcome)} variant."),
        };
    }

    private static ApiResponse<LinkingLifecycleErrorData> LifecycleError(string code, string message) => new()
    {
        IsSuccess = false,
        Message = message,
        Data = new LinkingLifecycleErrorData(code),
        Errors = [],
    };
}
