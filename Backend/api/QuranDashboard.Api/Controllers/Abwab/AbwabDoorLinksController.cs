using QuranDashboard.Api.Authorization;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Contracts.Linking;
using QuranDashboard.Application.Abstractions.Linking.DoorLinks;
using QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkAyahs;
using QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkRecords;
using QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkSnapshot;
using QuranDashboard.Application.Linking.DoorLinks.Commands.DeleteDoorLinks;
using QuranDashboard.Application.Linking.DoorLinks.Commands.ReplaceDoorLinkWords;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab/doors/{doorId:int}/links")]
public sealed class AbwabDoorLinksController(
    AuthorizationStateAccessEvaluator stateEvaluator,
    GetDoorLinkRecordsHandler getRecordsHandler,
    GetDoorLinkSnapshotHandler getSnapshotHandler,
    GetDoorLinkAyahsHandler getAyahsHandler,
    ReplaceDoorLinkWordsHandler replaceWordsHandler,
    DeleteDoorLinksHandler deleteLinksHandler) : ControllerBase
{
    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(ApiResponse<DoorLinkSnapshotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetSnapshot(
        int doorId,
        CancellationToken cancellationToken)
    {
        var outcome = await getSnapshotHandler.HandleAsync(
            new GetDoorLinkSnapshotQuery(doorId),
            cancellationToken);

        return outcome switch
        {
            GetDoorLinkSnapshotOutcome.Success success =>
                Ok(ApiResponse<DoorLinkSnapshotDto>.Ok(
                    success.Snapshot,
                    ApiMessages.AbwabDoorLinkRecordsLoaded)),
            GetDoorLinkSnapshotOutcome.InvalidRequest =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorLinksInvalidRequest)),
            GetDoorLinkSnapshotOutcome.DoorNotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorNotFound)),
            GetDoorLinkSnapshotOutcome.DoorArchived =>
                Conflict(LifecycleError(
                    AbwabDoorLinkConflictCodes.DoorArchived,
                    ApiMessages.AbwabDoorLinksArchived)),
            GetDoorLinkSnapshotOutcome.TransientFailure =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.Fail(ApiMessages.LinkingSourceReadTransientFailure)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(GetDoorLinkSnapshotOutcome)} variant."),
        };
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DoorLinkRecordsPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetRecords(
        int doorId,
        [FromQuery] AbwabDoorLinkRecordsQueryBody? query,
        CancellationToken cancellationToken)
    {
        var outcome = await getRecordsHandler.HandleAsync(
            new GetDoorLinkRecordsQuery(
                doorId,
                query?.ExpectedDoorVersion,
                query?.Page ?? 0,
                query?.PageSize ?? 0),
            cancellationToken);

        return outcome switch
        {
            GetDoorLinkRecordsOutcome.Success success =>
                Ok(ApiResponse<DoorLinkRecordsPageDto>.Ok(
                    success.Page,
                    ApiMessages.AbwabDoorLinkRecordsLoaded)),
            GetDoorLinkRecordsOutcome.InvalidRequest =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorLinksInvalidRequest)),
            GetDoorLinkRecordsOutcome.DoorNotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorNotFound)),
            GetDoorLinkRecordsOutcome.DoorArchived =>
                Conflict(LifecycleError(
                    AbwabDoorLinkConflictCodes.DoorArchived,
                    ApiMessages.AbwabDoorLinksArchived)),
            GetDoorLinkRecordsOutcome.DoorVersionStale =>
                Conflict(LifecycleError(
                    AbwabDoorLinkConflictCodes.DoorLinksStale,
                    ApiMessages.AbwabDoorLinksStale)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(GetDoorLinkRecordsOutcome)} variant."),
        };
    }

    [HttpGet("{unitId:long}/ayahs")]
    [ProducesResponseType(typeof(ApiResponse<DoorLinkAyahsPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAyahs(
        int doorId,
        long unitId,
        [FromQuery] AbwabDoorLinkAyahsQueryBody? query,
        CancellationToken cancellationToken)
    {
        var outcome = await getAyahsHandler.HandleAsync(
            new GetDoorLinkAyahsQuery(
                doorId,
                unitId,
                query?.ExpectedDoorVersion,
                query?.ExpectedLinkingDataRevision,
                query?.Page ?? 0,
                query?.PageSize ?? 0),
            cancellationToken);

        return outcome switch
        {
            GetDoorLinkAyahsOutcome.Success success =>
                Ok(ApiResponse<DoorLinkAyahsPageDto>.Ok(
                    success.Page,
                    ApiMessages.AbwabDoorLinkAyahsLoaded)),
            GetDoorLinkAyahsOutcome.InvalidRequest =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorLinksInvalidRequest)),
            GetDoorLinkAyahsOutcome.DoorNotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorNotFound)),
            GetDoorLinkAyahsOutcome.UnitNotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorLinkNotFound)),
            GetDoorLinkAyahsOutcome.DoorArchived =>
                Conflict(LifecycleError(
                    AbwabDoorLinkConflictCodes.DoorArchived,
                    ApiMessages.AbwabDoorLinksArchived)),
            GetDoorLinkAyahsOutcome.DoorVersionStale =>
                Conflict(LifecycleError(
                    AbwabDoorLinkConflictCodes.DoorLinksStale,
                    ApiMessages.AbwabDoorLinksStale)),
            GetDoorLinkAyahsOutcome.LinkingDataStale =>
                Conflict(LifecycleError(
                    AbwabDoorLinkConflictCodes.LinkingDataStale,
                    ApiMessages.LinkingDataStale)),
            GetDoorLinkAyahsOutcome.TransientFailure =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.Fail(ApiMessages.LinkingSourceReadTransientFailure)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(GetDoorLinkAyahsOutcome)} variant."),
        };
    }

    [HttpPatch("{unitId:long}/words")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<DoorLinkMutationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReplaceWords(
        int doorId,
        long unitId,
        [FromBody] ReplaceAbwabDoorLinkWordsBody? body,
        CancellationToken cancellationToken)
    {
        if (body?.ExpectedDoorVersion is not uint expectedDoorVersion
            || body.SelectedWords is null
            || body.SelectedWords.Any(word => word is null))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorLinksInvalidRequest));
        }

        var actorUserId = await ResolveUserIdAsync();
        var outcome = await replaceWordsHandler.HandleAsync(
            new ReplaceDoorLinkWordsCommand(
                doorId,
                unitId,
                expectedDoorVersion,
                [.. body.SelectedWords.Select(word => new DoorLinkSelectedWord(word!.AyahId, word.QuranWordId))],
                actorUserId),
            cancellationToken);

        return outcome switch
        {
            ReplaceDoorLinkWordsOutcome.Success success =>
                Ok(ApiResponse<DoorLinkMutationDto>.Ok(
                    success.Result,
                    ApiMessages.AbwabDoorLinkWordsReplaced)),
            ReplaceDoorLinkWordsOutcome.InvalidRequest =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorLinksInvalidRequest)),
            ReplaceDoorLinkWordsOutcome.DoorNotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorNotFound)),
            ReplaceDoorLinkWordsOutcome.UnitNotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorLinkNotFound)),
            ReplaceDoorLinkWordsOutcome.DoorArchived =>
                Conflict(LifecycleError(
                    AbwabDoorLinkConflictCodes.DoorArchived,
                    ApiMessages.AbwabDoorLinksArchived)),
            ReplaceDoorLinkWordsOutcome.DoorVersionStale =>
                Conflict(LifecycleError(
                    AbwabDoorLinkConflictCodes.DoorLinksStale,
                    ApiMessages.AbwabDoorLinksStale)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(ReplaceDoorLinkWordsOutcome)} variant."),
        };
    }

    [HttpPost("bulk-delete")]
    [RequireOwner]
    [ProducesResponseType(typeof(ApiResponse<DoorLinkMutationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<LinkingLifecycleErrorData>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteLinks(
        int doorId,
        [FromBody] DeleteAbwabDoorLinksBody? body,
        CancellationToken cancellationToken)
    {
        if (body?.ExpectedDoorVersion is not uint expectedDoorVersion
            || body.UnitIds is null
            || !DoorLinkSelectionModeTokens.TryParse(body.SelectionMode, out var selectionMode))
        {
            return BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorLinksInvalidRequest));
        }

        var actorUserId = await ResolveUserIdAsync();
        var outcome = await deleteLinksHandler.HandleAsync(
            new DeleteDoorLinksCommand(
                doorId,
                expectedDoorVersion,
                new DoorLinkSelection(selectionMode, body.UnitIds),
                actorUserId),
            cancellationToken);

        return outcome switch
        {
            DeleteDoorLinksOutcome.Success success =>
                Ok(ApiResponse<DoorLinkMutationDto>.Ok(
                    success.Result,
                    ApiMessages.AbwabDoorLinksDeleted)),
            DeleteDoorLinksOutcome.InvalidRequest =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorLinksInvalidRequest)),
            DeleteDoorLinksOutcome.DoorNotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorNotFound)),
            DeleteDoorLinksOutcome.UnitNotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorLinkNotFound)),
            DeleteDoorLinksOutcome.DoorArchived =>
                Conflict(LifecycleError(
                    AbwabDoorLinkConflictCodes.DoorArchived,
                    ApiMessages.AbwabDoorLinksArchived)),
            DeleteDoorLinksOutcome.DoorVersionStale =>
                Conflict(LifecycleError(
                    AbwabDoorLinkConflictCodes.DoorLinksStale,
                    ApiMessages.AbwabDoorLinksStale)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(DeleteDoorLinksOutcome)} variant."),
        };
    }

    private async Task<int> ResolveUserIdAsync()
    {
        var state = await stateEvaluator.ResolveActiveStateAsync(User);
        return state?.UserId
            ?? throw new InvalidOperationException(
                "An authorized Abwab door-link request resolved no authorization state.");
    }

    private static ApiResponse<LinkingLifecycleErrorData> LifecycleError(
        string code,
        string message) => new()
    {
        IsSuccess = false,
        Message = message,
        Data = new LinkingLifecycleErrorData(code),
        Errors = [],
    };
}
