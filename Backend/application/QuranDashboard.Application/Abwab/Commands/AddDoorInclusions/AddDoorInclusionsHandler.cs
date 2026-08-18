using QuranDashboard.Application.Abstractions.Abwab.Inclusions;

namespace QuranDashboard.Application.Abwab.Commands.AddDoorInclusions;

public sealed class AddDoorInclusionsHandler(
    ILogger<AddDoorInclusionsHandler> logger,
    IAbwabDoorInclusionsWriter writer)
{
    public async Task<AddDoorInclusionsOutcome> HandleAsync(
        AddDoorInclusionsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!IsValid(command))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {doorId} {sourceCount}",
                "AbwabInclusions",
                "AddDoorInclusions",
                "invalidRequest",
                command.TargetDoorId,
                command.SourceDoorIds.Count);
            return new AddDoorInclusionsOutcome.InvalidRequest();
        }

        var sourceDoorIds = command.SourceDoorIds.Order().ToArray();
        var result = await writer.AddAsync(
            command.TargetDoorId,
            command.ExpectedTargetDoorVersion,
            sourceDoorIds,
            command.ActorUserId,
            cancellationToken);

        AddDoorInclusionsOutcome outcome = result switch
        {
            AbwabDoorInclusionAddWriteResult.Success success =>
                new AddDoorInclusionsOutcome.Success(success.Result),
            AbwabDoorInclusionAddWriteResult.InvalidRequest => new AddDoorInclusionsOutcome.InvalidRequest(),
            AbwabDoorInclusionAddWriteResult.NotFound => new AddDoorInclusionsOutcome.NotFound(),
            AbwabDoorInclusionAddWriteResult.ArchivedDoor => new AddDoorInclusionsOutcome.ArchivedDoor(),
            AbwabDoorInclusionAddWriteResult.Duplicate => new AddDoorInclusionsOutcome.Duplicate(),
            AbwabDoorInclusionAddWriteResult.Cycle => new AddDoorInclusionsOutcome.Cycle(),
            AbwabDoorInclusionAddWriteResult.StaleTargetVersion =>
                new AddDoorInclusionsOutcome.StaleTargetVersion(),
            AbwabDoorInclusionAddWriteResult.SynchronizationUnavailable =>
                new AddDoorInclusionsOutcome.SynchronizationUnavailable(),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(AbwabDoorInclusionAddWriteResult)} variant."),
        };

        logger.LogInformation(
            "Completed {feature} {operation} {doorId} {sourceCount} {outcome}",
            "AbwabInclusions",
            "AddDoorInclusions",
            command.TargetDoorId,
            sourceDoorIds.Length,
            outcome.GetType().Name);

        return outcome;
    }

    private static bool IsValid(AddDoorInclusionsCommand command) =>
        command.TargetDoorId > 0
        && command.ActorUserId > 0
        && command.SourceDoorIds.Count > 0
        && command.SourceDoorIds.All(sourceDoorId => sourceDoorId > 0)
        && command.SourceDoorIds.Distinct().Count() == command.SourceDoorIds.Count
        && !command.SourceDoorIds.Contains(command.TargetDoorId);
}
