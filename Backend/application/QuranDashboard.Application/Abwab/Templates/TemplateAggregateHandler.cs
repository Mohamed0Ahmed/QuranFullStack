using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Tree;

namespace QuranDashboard.Application.Abwab.Templates;

public sealed class TemplateAggregateHandler(
    IAbwabWriteExecutor executor,
    IDoorTemplateStore templates,
    ITemplateNodeStore nodes,
    ITemplateNodeAliasStore aliases,
    IServerClock clock)
{
    private readonly TemplateHistory _history = new(nodes, aliases);

    public async Task<Guid> AddAsync(AddDoorTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var doorTemplateId = Guid.NewGuid();

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            (_, _) =>
            {
                var template = new DoorTemplate
                {
                    DoorTemplateId = doorTemplateId,
                    Name = command.Name,
                    NormalizedName = ArabicNameNormalizer.Normalize(command.Name),
                    Description = command.Description,
                };

                templates.Add(template);

                var working = new TemplateAggregateWorkingSet(template, [], []);
                return Task.FromResult(TemplateHistory.Draft(
                    TemplateAuditActions.Created,
                    new TemplateHistoryAuditView(doorTemplateId, null, working.Snapshot(), [])));
            });

        await executor.ExecuteAsync(request, cancellationToken);
        return doorTemplateId;
    }

    public Task EditAsync(EditDoorTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return MutateAsync(command.ExpectedTimelineGeneration, command.ActorSubject, command.DoorTemplateId, command.ExpectedVersion, TemplateAuditActions.Edited, template =>
        {
            if (template.IsDeleted)
            {
                throw TemplateTreeGuards.Stale();
            }

            template.Name = command.Name;
            template.NormalizedName = ArabicNameNormalizer.Normalize(command.Name);
            template.Description = command.Description;
        }, cancellationToken);
    }

    public Task DeleteAsync(DeleteDoorTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return MutateAsync(command.ExpectedTimelineGeneration, command.ActorSubject, command.DoorTemplateId, command.ExpectedVersion, TemplateAuditActions.Deleted, template =>
        {
            if (template.IsDeleted)
            {
                throw TemplateTreeGuards.Stale();
            }

            template.IsDeleted = true;
            template.DeletedAtUtc = clock.UtcNow;
        }, cancellationToken);
    }

    public Task RestoreAsync(RestoreDoorTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return MutateAsync(command.ExpectedTimelineGeneration, command.ActorSubject, command.DoorTemplateId, command.ExpectedVersion, TemplateAuditActions.Restored, template =>
        {
            if (!template.IsDeleted)
            {
                throw TemplateTreeGuards.Stale();
            }

            template.IsDeleted = false;
            template.DeletedAtUtc = null;
        }, cancellationToken);
    }

    private async Task MutateAsync(
        ExpectedTimelineGeneration expectedGeneration,
        string actorSubject,
        Guid doorTemplateId,
        uint expectedVersion,
        string action,
        Action<DoorTemplate> mutate,
        CancellationToken cancellationToken)
    {
        var request = new AbwabAuditedOperationRequest(
            expectedGeneration,
            actorSubject,
            async (_, operationToken) =>
            {
                var template = await templates.FindTrackedAsync(doorTemplateId, operationToken) ?? throw TemplateTreeGuards.Stale();
                AbwabRevisionGuards.GuardRowVersion(template.Version, expectedVersion);

                var working = await _history.LoadAsync(template, operationToken);
                var before = working.Snapshot();
                mutate(template);

                return TemplateHistory.Draft(
                    action,
                    new TemplateHistoryAuditView(doorTemplateId, before, working.Snapshot(), []));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }
}
