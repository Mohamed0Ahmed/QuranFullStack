using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task<LinkingConfirmationResultDto> PersistOperationAsync(
        int actorUserId,
        LinkingOperationRequest request,
        LinkingOperationClassification classification,
        LockedConfirmationState loaded,
        LinkingConfirmationRequestContract requestContract,
        CancellationToken cancellationToken)
    {
        var now = await DatabaseNowAsync(cancellationToken);
        var operation = new LinkingOperation
        {
            DoorId = request.DoorId,
            ActorUserId = actorUserId,
            IdempotencyKey = request.IdempotencyKey!.Value,
            PreparedPreflightId = requestContract.PreparedPreflightId,
            PreparedPreflightReferenceId = requestContract.PreparedPreflightReferenceId,
            ConfirmationJobReferenceId = requestContract.ConfirmationJobReferenceId,
            RequestContractKind = requestContract.Kind,
            RequestSchemaVersion = requestContract.SchemaVersion,
            RequestHash = requestContract.RequestHash,
            LinkingDataRevision = requestContract.LinkingDataRevision,
            ConfirmedAtUtc = now,
            SourceCount = classification.Sources.Count,
            AyahCount = classification.Totals.Requested,
            OutcomeJson = EmptyOutcomeJson,
        };
        db.LinkingOperations.Add(operation);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        IReadOnlyDictionary<string, long?> contributionIds;
        if (classification.IsNoOp)
        {
            contributionIds = classification.Sources.ToDictionary(
                source => source.Source.SourceIdentity,
                source => source.ExistingContributionId,
                StringComparer.Ordinal);
        }
        else
        {
            var workset = BuildWorkset(request, classification, loaded.State);
            var unitIds = await EnsureUnitsAsync(
                request.DoorId,
                actorUserId,
                workset,
                now,
                cancellationToken);
            var changedContributionIds = await PersistContributionsAsync(
                operation.Id,
                request.DoorId,
                actorUserId,
                workset,
                loaded,
                now,
                cancellationToken);
            var mutableContributionIds = classification.Sources.ToDictionary(
                source => source.Source.SourceIdentity,
                source => source.ExistingContributionId,
                StringComparer.Ordinal);
            foreach (var (sourceIdentity, contributionId) in changedContributionIds)
            {
                mutableContributionIds[sourceIdentity] = contributionId;
            }

            var orphanCandidates = await SynchronizeContributionLinksAsync(
                workset,
                changedContributionIds,
                unitIds,
                loaded,
                cancellationToken);
            await RemoveNewlyOrphanedUnitsAsync(orphanCandidates, cancellationToken);
            await ApplyDoorStateAsync(
                actorUserId,
                request.DoorId,
                workset.AffectedAyahIds,
                loaded,
                now,
                cancellationToken);
            contributionIds = mutableContributionIds;
        }

        var result = CreateResult(request.DoorId, classification.IsNoOp, classification, contributionIds);
        operation.OutcomeJson = SerializeOutcome(result);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        return result;
    }
}
