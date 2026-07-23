using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abwab.Protection;

public sealed class ManualProtectionWriterHandler(
    IAbwabWriteExecutor executor,
    ICategoryTreeStore categories,
    IManualProtectionWriteStore protections,
    IServerClock clock)
{
    public async Task ApplyAsync(ApplyManualProtectionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                if (!await categories.ExistsAsync(command.CategoryId, operationToken))
                {
                    throw new AbwabWriteConflictException(AbwabConflictCodes.CategoryUnavailable, "Category is unavailable.");
                }

                var existing = await protections.FindActiveAsync(command.CategoryId, command.ProtectionType, operationToken);

                if (existing is null)
                {
                    protections.Add(new ManualProtection
                    {
                        ManualProtectionId = Guid.NewGuid(),
                        CategoryId = command.CategoryId,
                        ProtectionType = command.ProtectionType,
                        ProtectionScope = command.Scope,
                        AppliedByActorSubject = command.ActorSubject,
                        AppliedAtUtc = clock.UtcNow,
                    });

                    return AbwabAuditedOperationOutcome.Audited(
                        new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("protection.applied", new { command.CategoryId, command.ProtectionType, command.Scope })));
                }

                if (existing.ProtectionScope == command.Scope)
                {
                    return AbwabAuditedOperationOutcome.NoOp;
                }

                if (command.ExpectedVersion is not { } expectedVersion || existing.Version != expectedVersion)
                {
                    throw new AbwabWriteConflictException(
                        AbwabConflictCodes.ManualProtectionScopeConflict,
                        "The protection scope changed since it was last read.");
                }

                var oldScope = existing.ProtectionScope;
                existing.ProtectionScope = command.Scope;
                existing.AppliedByActorSubject = command.ActorSubject;
                existing.AppliedAtUtc = clock.UtcNow;

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("protection.scope_changed", new { command.CategoryId, command.ProtectionType, oldScope, newScope = command.Scope })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }

    public async Task LiftAsync(LiftManualProtectionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new AbwabAuditedOperationRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async (_, operationToken) =>
            {
                var existing = await protections.FindActiveAsync(command.CategoryId, command.ProtectionType, operationToken);

                if (existing is null)
                {
                    return AbwabAuditedOperationOutcome.NoOp;
                }

                if (existing.Version != command.ExpectedVersion)
                {
                    throw new AbwabWriteConflictException(AbwabConflictCodes.RowStale, "The protection record changed since it was last read.");
                }

                existing.IsDeleted = true;
                existing.DeletedAtUtc = clock.UtcNow;
                existing.LiftedByActorSubject = command.ActorSubject;
                existing.LiftedAtUtc = clock.UtcNow;

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("protection.lifted", new { command.CategoryId, command.ProtectionType })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }
}
