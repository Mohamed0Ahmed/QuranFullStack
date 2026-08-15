using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Contracts.Linking;
using QuranDashboard.Application.Abstractions.Linking.DoorLinks;
using QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkAyahs;
using QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkRecords;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab/doors/{doorId:int}/links")]
public sealed class AbwabDoorLinksController(
    GetDoorLinkRecordsHandler getRecordsHandler,
    GetDoorLinkAyahsHandler getAyahsHandler) : ControllerBase
{
    [HttpGet]
    [RequireOwner]
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
    [RequireOwner]
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
