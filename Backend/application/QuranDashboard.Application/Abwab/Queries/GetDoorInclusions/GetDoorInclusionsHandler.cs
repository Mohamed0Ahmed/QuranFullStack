using QuranDashboard.Application.Abstractions.Abwab.Inclusions;

namespace QuranDashboard.Application.Abwab.Queries.GetDoorInclusions;

public sealed class GetDoorInclusionsHandler(
    ILogger<GetDoorInclusionsHandler> logger,
    IAbwabDoorInclusionsReader reader)
{
    public async Task<GetDoorInclusionsOutcome> HandleAsync(
        GetDoorInclusionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.DoorId <= 0)
        {
            return new GetDoorInclusionsOutcome.InvalidRequest();
        }

        var topology = await reader.GetAsync(query.DoorId, cancellationToken);
        if (topology is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {doorId}",
                "AbwabInclusions",
                "GetDoorInclusions",
                query.DoorId);
            return new GetDoorInclusionsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {doorId} {sourceCount} {consumerCount}",
            "AbwabInclusions",
            "GetDoorInclusions",
            query.DoorId,
            topology.Sources.Count,
            topology.Consumers.Count);

        return new GetDoorInclusionsOutcome.Success(topology);
    }
}
