using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkAyahs;

public sealed class GetDoorLinkAyahsHandler(
    IDoorLinkRecordsReader reader,
    ILinkingDataRevisionReadScope revisionScope,
    ILinkingScalabilityPolicy policy)
{
    public async Task<GetDoorLinkAyahsOutcome> HandleAsync(
        GetDoorLinkAyahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.DoorId <= 0
            || query.UnitId <= 0
            || query.Page <= 0
            || query.PageSize <= 0
            || query.PageSize > policy.PageSizeMaximum
            || query.ExpectedDoorVersion is null or 0
            || query.ExpectedLinkingDataRevision is <= 0
            || (query.Page > 1 && query.ExpectedLinkingDataRevision is null))
        {
            return new GetDoorLinkAyahsOutcome.InvalidRequest();
        }

        try
        {
            return await revisionScope.ExecuteAsync<GetDoorLinkAyahsOutcome>(
                policy.MaximumAutomaticAttempts,
                async (revision, token) =>
                {
                    if (query.ExpectedLinkingDataRevision is long expectedRevision
                        && expectedRevision != revision)
                    {
                        return new GetDoorLinkAyahsOutcome.LinkingDataStale();
                    }

                    var result = await reader.ReadAyahsAsync(
                        query.DoorId,
                        query.UnitId,
                        query.ExpectedDoorVersion.Value,
                        revision,
                        query.Page,
                        query.PageSize,
                        token);
                    return Map(result);
                },
                cancellationToken);
        }
        catch (LinkingDataRevisionReadRetryExhaustedException)
        {
            return new GetDoorLinkAyahsOutcome.TransientFailure();
        }
    }

    private static GetDoorLinkAyahsOutcome Map(DoorLinkAyahsReadResult result) => result switch
    {
        DoorLinkAyahsReadResult.Success success =>
            new GetDoorLinkAyahsOutcome.Success(success.Page),
        DoorLinkAyahsReadResult.DoorNotFound =>
            new GetDoorLinkAyahsOutcome.DoorNotFound(),
        DoorLinkAyahsReadResult.DoorArchived =>
            new GetDoorLinkAyahsOutcome.DoorArchived(),
        DoorLinkAyahsReadResult.DoorVersionStale =>
            new GetDoorLinkAyahsOutcome.DoorVersionStale(),
        DoorLinkAyahsReadResult.UnitNotFound =>
            new GetDoorLinkAyahsOutcome.UnitNotFound(),
        _ => throw new InvalidOperationException(
            $"Unhandled {nameof(DoorLinkAyahsReadResult)} variant."),
    };
}
