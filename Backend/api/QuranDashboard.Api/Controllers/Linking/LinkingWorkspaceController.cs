using QuranDashboard.Api.Authorization;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Contracts.Linking;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Linking;
using QuranDashboard.Application.Linking.Commands.AddLinkingWorkspaceSource;
using QuranDashboard.Application.Linking.Commands.ClearLinkingWorkspaceSources;
using QuranDashboard.Application.Linking.Commands.RemoveLinkingWorkspaceSource;
using QuranDashboard.Application.Linking.Commands.ReorderLinkingWorkspaceSources;
using QuranDashboard.Application.Linking.Commands.ApplyLinkingWorkspaceSourceDelta;
using QuranDashboard.Application.Linking.Commands.UpdateLinkingWorkspaceSourceTypes;
using QuranDashboard.Application.Linking.Queries.GetLinkingWorkspace;

namespace QuranDashboard.Api.Controllers.Linking;

[ApiController]
[Route("api/linking/workspace")]
public sealed class LinkingWorkspaceController(
    AuthorizationStateAccessEvaluator stateEvaluator,
    GetLinkingWorkspaceHandler getHandler,
    AddLinkingWorkspaceSourceHandler addHandler,
    RemoveLinkingWorkspaceSourceHandler removeHandler,
    ReorderLinkingWorkspaceSourcesHandler reorderHandler,
    ApplyLinkingWorkspaceSourceDeltaHandler deltaHandler,
    UpdateLinkingWorkspaceSourceTypesHandler updateTypesHandler,
    ClearLinkingWorkspaceSourcesHandler clearHandler) : ControllerBase
{
    [HttpGet]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingWorkspaceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LinkingWorkspaceResponse>>> Load(
        CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync();

        var outcome = await getHandler.HandleAsync(new GetLinkingWorkspaceQuery(userId), cancellationToken);

        return Respond(outcome, ApiMessages.LinkingWorkspaceLoaded);
    }

    [HttpPost("sources")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingWorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LinkingWorkspaceResponse>>> AddSource(
        [FromBody] LinkingWorkspaceAddSourceBody body,
        CancellationToken cancellationToken)
    {
        if (!LinkingSourceDescriptorBodyMapper.TryMap(body?.Descriptor, out var descriptor, out var violation))
        {
            return BadRequest(ApiResponse<LinkingWorkspaceResponse>.Fail(
                ApiMessages.LinkingDescriptorViolationMessage(violation)));
        }

        var userId = await ResolveUserIdAsync();

        var outcome = await addHandler.HandleAsync(
            new AddLinkingWorkspaceSourceCommand(userId, descriptor, body!.WorkspaceVersion),
            cancellationToken);

        return Respond(outcome, ApiMessages.LinkingWorkspaceSourceAdded);
    }

    [HttpDelete("sources/{id:long}")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingWorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LinkingWorkspaceResponse>>> RemoveSource(
        long id,
        [FromQuery] uint? workspaceVersion,
        CancellationToken cancellationToken)
    {
        if (workspaceVersion is null)
        {
            return VersionRequired();
        }

        var userId = await ResolveUserIdAsync();

        var outcome = await removeHandler.HandleAsync(
            new RemoveLinkingWorkspaceSourceCommand(userId, id, workspaceVersion.Value),
            cancellationToken);

        return Respond(outcome, ApiMessages.LinkingWorkspaceSourceRemoved);
    }

    [HttpPatch("sources/{id:long}/types")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingWorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LinkingWorkspaceResponse>>> UpdateSourceTypes(
        long id,
        [FromBody] LinkingWorkspaceSourceTypesBody? body,
        CancellationToken cancellationToken)
    {
        if (body?.WorkspaceVersion is null or 0
            || body.SourceVersion is null or 0
            || body.TypeCodes is null
            || LinkingSourceDescriptorValidation.TypeCodesError(body.TypeCodes) is not null)
        {
            return BadRequest(ApiResponse<LinkingWorkspaceResponse>.Fail(
                ApiMessages.LinkingSourcePageInvalid));
        }

        var userId = await ResolveUserIdAsync();
        var outcome = await updateTypesHandler.HandleAsync(
            new UpdateLinkingWorkspaceSourceTypesCommand(
                userId,
                id,
                body.TypeCodes,
                body.WorkspaceVersion.Value,
                body.SourceVersion.Value),
            cancellationToken);

        return Respond(outcome, ApiMessages.LinkingWorkspaceConfigurationSaved);
    }

    [HttpPut("sources/order")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingWorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LinkingWorkspaceResponse>>> ReorderSources(
        [FromBody] LinkingWorkspaceReorderBody body,
        CancellationToken cancellationToken)
    {
        if (body?.WorkspaceVersion is null)
        {
            return VersionRequired();
        }

        var userId = await ResolveUserIdAsync();

        var outcome = await reorderHandler.HandleAsync(
            new ReorderLinkingWorkspaceSourcesCommand(
                userId, [.. body.SourceIds ?? []], body.WorkspaceVersion.Value),
            cancellationToken);

        return Respond(outcome, ApiMessages.LinkingWorkspaceSourcesReordered);
    }

    [HttpPatch("sources/{id:long}/configuration")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingWorkspaceDeltaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApplySourceConfigurationDelta(
        long id,
        [FromBody] LinkingWorkspaceDeltaBody? body,
        CancellationToken cancellationToken)
    {
        if (!LinkingWorkspaceDeltaBodyMapper.TryMap(body, out var delta))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiMessages.LinkingSourcePageInvalid));
        }

        var userId = await ResolveUserIdAsync();
        var outcome = await deltaHandler.HandleAsync(
            new ApplyLinkingWorkspaceSourceDeltaCommand(userId, id, delta),
            cancellationToken);

        return outcome switch
        {
            ApplyLinkingWorkspaceSourceDeltaOutcome.Success success =>
                Ok(ApiResponse<LinkingWorkspaceDeltaResponse>.Ok(
                    LinkingWorkspaceDeltaBodyMapper.ToResponse(success.Acknowledgement),
                    ApiMessages.LinkingWorkspaceConfigurationSaved)),
            ApplyLinkingWorkspaceSourceDeltaOutcome.InvalidRequest invalid =>
                BadRequest(ApiResponse<object>.Fail(
                    ApiMessages.LinkingWorkspaceViolationMessage(invalid.Violation))),
            ApplyLinkingWorkspaceSourceDeltaOutcome.SourceNotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.LinkingWorkspaceSourceNotFound)),
            ApplyLinkingWorkspaceSourceDeltaOutcome.StaleVersion =>
                Conflict(ApiResponse<object>.Fail(ApiMessages.LinkingWorkspaceStaleVersion)),
            ApplyLinkingWorkspaceSourceDeltaOutcome.LinkingDataStale =>
                Conflict(new ApiResponse<LinkingLifecycleErrorData>
                {
                    IsSuccess = false,
                    Message = ApiMessages.LinkingDataStale,
                    Data = new LinkingLifecycleErrorData("LINKING_DATA_STALE"),
                    Errors = [],
                }),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(ApplyLinkingWorkspaceSourceDeltaOutcome)} variant."),
        };
    }

    [HttpDelete("sources")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<LinkingWorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LinkingWorkspaceResponse>>> ClearSources(
        [FromQuery] uint? workspaceVersion,
        CancellationToken cancellationToken)
    {
        if (workspaceVersion is null)
        {
            return VersionRequired();
        }

        var userId = await ResolveUserIdAsync();

        var outcome = await clearHandler.HandleAsync(
            new ClearLinkingWorkspaceSourcesCommand(userId, workspaceVersion.Value),
            cancellationToken);

        return Respond(outcome, ApiMessages.LinkingWorkspaceCleared);
    }

    private async Task<int> ResolveUserIdAsync()
    {
        var state = await stateEvaluator.ResolveActiveStateAsync(User);

        return state?.UserId
            ?? throw new InvalidOperationException(
                "An authorized linking workspace request resolved no authorization state.");
    }

    private ActionResult<ApiResponse<LinkingWorkspaceResponse>> VersionRequired() =>
        BadRequest(ApiResponse<LinkingWorkspaceResponse>.Fail(
            ApiMessages.LinkingWorkspaceViolationMessage(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.VersionRequired, "version", null))));

    private ActionResult<ApiResponse<LinkingWorkspaceResponse>> Respond(
        LinkingWorkspaceOutcome outcome,
        string successMessage) =>
        outcome switch
        {
            LinkingWorkspaceOutcome.Success success =>
                Ok(ApiResponse<LinkingWorkspaceResponse>.Ok(
                    LinkingWorkspaceResponseMapper.ToResponse(success.Workspace), successMessage)),
            LinkingWorkspaceOutcome.InvalidRequest invalid =>
                BadRequest(ApiResponse<LinkingWorkspaceResponse>.Fail(
                    ApiMessages.LinkingWorkspaceViolationMessage(invalid.Violation))),
            LinkingWorkspaceOutcome.InvalidDescriptor invalid =>
                BadRequest(ApiResponse<LinkingWorkspaceResponse>.Fail(
                    ApiMessages.LinkingDescriptorViolationMessage(invalid.Violation))),
            LinkingWorkspaceOutcome.SourceNotFound =>
                NotFound(ApiResponse<LinkingWorkspaceResponse>.Fail(
                    ApiMessages.LinkingWorkspaceSourceNotFound)),
            LinkingWorkspaceOutcome.StaleVersion =>
                Conflict(ApiResponse<LinkingWorkspaceResponse>.Fail(
                    ApiMessages.LinkingWorkspaceStaleVersion)),
            LinkingWorkspaceOutcome.DuplicateSource =>
                Conflict(ApiResponse<LinkingWorkspaceResponse>.Fail(
                    ApiMessages.LinkingWorkspaceDuplicateSource)),
            LinkingWorkspaceOutcome.LinkingDataStale =>
                Conflict(new ApiResponse<LinkingLifecycleErrorData>
                {
                    IsSuccess = false,
                    Message = ApiMessages.LinkingDataStale,
                    Data = new LinkingLifecycleErrorData("LINKING_DATA_STALE"),
                    Errors = [],
                }),
            _ => throw new InvalidOperationException($"Unhandled {nameof(LinkingWorkspaceOutcome)} variant."),
        };
}
