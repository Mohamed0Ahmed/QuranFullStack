using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Contracts.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Application.Linking.Queries.ResolveLinkingSource;

namespace QuranDashboard.Api.Controllers.Linking;

[ApiController]
[Route("api/linking/sources")]
public sealed class LinkingSourcesController(ResolveLinkingSourceHandler resolveHandler) : ControllerBase
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
}
