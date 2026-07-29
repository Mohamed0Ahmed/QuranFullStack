using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Queries.GetDoorRelations;

public sealed class GetDoorRelationsHandler(
    ILogger<GetDoorRelationsHandler> logger,
    IAbwabRelationsReader reader)
{
    private const string FeatureName = "Abwab";
    private const string OperationName = "GetDoorRelations";

    public async Task<GetDoorRelationsOutcome> HandleAsync(
        GetDoorRelationsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var relations = await reader.GetForDoorAsync(query.DoorId, cancellationToken);
        if (relations is null)
        {
            return new GetDoorRelationsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {doorId} {relationCount}",
            FeatureName, OperationName, query.DoorId, relations.Count);

        return new GetDoorRelationsOutcome.Success(relations);
    }
}
