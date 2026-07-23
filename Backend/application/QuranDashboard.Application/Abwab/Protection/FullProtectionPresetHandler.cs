using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Application.Abwab.Protection;

// T058: one selected scope idempotently upserts all five typed ManualProtection records. Same-scope
// records are left untouched; each different-scope record requires its own Expected Version; missing
// types are inserted; all-matching is an idempotent no-ChangeSet success; any stale/conflict failure
// rolls back all five because every mutation shares the one operation delegate / one transaction.
public sealed class FullProtectionPresetHandler(
    IAbwabWriteExecutor executor,
    ICategoryTreeStore categories,
    IManualProtectionWriteStore protections,
    IServerClock clock)
{
    public async Task ApplyAsync(ApplyFullProtectionPresetCommand command, CancellationToken cancellationToken)
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

                var existing = await protections.FindActiveByCategoryAsync(command.CategoryId, operationToken);
                var existingByType = existing.ToDictionary(p => p.ProtectionType);

                var changed = new List<(ManualProtectionType Type, string Kind)>();
                var now = clock.UtcNow;

                foreach (var type in Enum.GetValues<ManualProtectionType>())
                {
                    if (existingByType.TryGetValue(type, out var record))
                    {
                        if (record.ProtectionScope == command.Scope)
                        {
                            continue;
                        }

                        if (!command.ExpectedVersions.TryGetValue(type, out var expectedVersion) || record.Version != expectedVersion)
                        {
                            throw new AbwabWriteConflictException(
                                AbwabConflictCodes.ManualProtectionScopeConflict,
                                $"The {type} protection scope changed since it was last read.");
                        }

                        record.ProtectionScope = command.Scope;
                        record.AppliedByActorSubject = command.ActorSubject;
                        record.AppliedAtUtc = now;
                        changed.Add((type, "scope_changed"));
                    }
                    else
                    {
                        protections.Add(new ManualProtection
                        {
                            ManualProtectionId = Guid.NewGuid(),
                            CategoryId = command.CategoryId,
                            ProtectionType = type,
                            ProtectionScope = command.Scope,
                            AppliedByActorSubject = command.ActorSubject,
                            AppliedAtUtc = now,
                        });
                        changed.Add((type, "added"));
                    }
                }

                if (changed.Count == 0)
                {
                    return AbwabAuditedOperationOutcome.NoOp;
                }

                return AbwabAuditedOperationOutcome.Audited(
                    new AbwabAuditEventDraft(0, AbwabAuditPayload.Serialize("protection.full_preset_applied", new { command.CategoryId, command.Scope, changed })));
            });

        await executor.ExecuteAsync(request, cancellationToken);
    }
}
