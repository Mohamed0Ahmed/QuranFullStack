using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter(QuranDashboardDbContext db) : ILinkingConfirmationWriter
{
    public async Task<LinkingConfirmationWriteResult> ConfirmAsync(
        int actorUserId,
        LinkingOperationRequest request,
        LinkingOperationIntent intent,
        Func<LinkingOperationIntent, LinkingConfirmedDoorState, LinkingOperationClassification> classify,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(classify);

        var idempotencyKey = request.IdempotencyKey
            ?? throw new ArgumentException("A confirmation idempotency key is required.", nameof(request));
        var suppliedToken = request.PreflightToken
            ?? throw new ArgumentException("A confirmation preflight token is required.", nameof(request));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var replay = await FindReplayAsync(
            idempotencyKey, actorUserId, request.DoorId, cancellationToken);
        if (replay is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new LinkingConfirmationWriteResult.Success(replay, true);
        }

        var loaded = await LoadLockedStateAsync(request.DoorId, cancellationToken);
        if (loaded is null)
        {
            return new LinkingConfirmationWriteResult.DoorNotFound(request.DoorId);
        }

        replay = await FindReplayAsync(idempotencyKey, actorUserId, request.DoorId, cancellationToken);
        if (replay is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new LinkingConfirmationWriteResult.Success(replay, true);
        }

        var versionChecks = BuildExpectedVersionChecks(request, loaded.State);
        ApplyExpectedVersions(versionChecks, loaded);

        var classification = classify(intent, loaded.State);
        var freshToken = LinkingPreflightToken.Compute(
            request,
            new LinkingPreflightDoorComponent(loaded.State.DoorId, loaded.State.DoorVersion),
            LinkingPreflightToken.AffectedContributionsOf(loaded.State, classification));

        if (!string.Equals(suppliedToken, freshToken, StringComparison.Ordinal))
        {
            throw new LinkingPreflightStaleException(loaded.State, classification, freshToken);
        }

        EnsureExpectedVersions(versionChecks);

        if (classification.IsBlocked)
        {
            return new LinkingConfirmationWriteResult.InvalidClassification(classification);
        }

        if (classification.IsNoOp)
        {
            var noOp = CreateResult(
                request.DoorId,
                true,
                classification,
                classification.Sources.ToDictionary(
                    source => source.Source.SourceIdentity,
                    source => source.ExistingContributionId,
                    StringComparer.Ordinal));

            await transaction.CommitAsync(cancellationToken);
            return new LinkingConfirmationWriteResult.Success(noOp, false);
        }

        var now = DateTimeOffset.UtcNow;
        var operation = new LinkingOperation
        {
            DoorId = request.DoorId,
            ActorUserId = actorUserId,
            IdempotencyKey = idempotencyKey,
            ConfirmedAtUtc = now,
            SourceCount = classification.Sources.Count,
            AyahCount = classification.Totals.Requested,
            OutcomeJson = EmptyOutcomeJson,
        };

        db.LinkingOperations.Add(operation);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        var contributionIds = new Dictionary<string, long?>(StringComparer.Ordinal);

        for (var index = 0; index < classification.Sources.Count; index++)
        {
            var source = classification.Sources[index];
            var sourceRequest = request.Sources[index];

            switch (source.Classification)
            {
                case LinkingPreflightClassification.Unchanged:
                    contributionIds.Add(source.Source.SourceIdentity, source.ExistingContributionId);
                    break;
                case LinkingPreflightClassification.NewSource:
                    var created = await InsertContributionAsync(
                        operation.Id,
                        request.DoorId,
                        actorUserId,
                        sourceRequest,
                        source.Source,
                        now,
                        cancellationToken);
                    contributionIds.Add(source.Source.SourceIdentity, created.Id);
                    break;
                case LinkingPreflightClassification.Update:
                    var existing = loaded.ContributionsById[source.ExistingContributionId!.Value];
                    await ReplaceContributionAsync(
                        operation.Id,
                        actorUserId,
                        existing,
                        sourceRequest,
                        source.Source,
                        loaded,
                        now,
                        cancellationToken);
                    contributionIds.Add(source.Source.SourceIdentity, existing.Id);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Confirmation cannot apply source classification '{source.Classification}'.");
            }
        }

        await RemoveOrphanUnitsAsync(request.DoorId, cancellationToken);
        await ApplyDoorStateAsync(actorUserId, request.DoorId, loaded, now, cancellationToken);

        var result = CreateResult(request.DoorId, false, classification, contributionIds);
        operation.OutcomeJson = SerializeOutcome(result);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new LinkingConfirmationWriteResult.Success(result, false);
    }

    private async Task<LinkingConfirmationResultDto?> FindReplayAsync(
        Guid idempotencyKey,
        int actorUserId,
        int doorId,
        CancellationToken cancellationToken)
    {
        var operation = await db.LinkingOperations
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.IdempotencyKey == idempotencyKey, cancellationToken);

        if (operation is null)
        {
            return null;
        }

        if (operation.ActorUserId != actorUserId || operation.DoorId != doorId)
        {
            throw new LinkingIdempotencyConflictException();
        }

        return DeserializeOutcome(operation.OutcomeJson);
    }

    private static IReadOnlyList<ExpectedVersionCheck> BuildExpectedVersionChecks(
        LinkingOperationRequest request,
        LinkingConfirmedDoorState state)
    {
        var liveByIdentity = state.Contributions.ToDictionary(
            contribution => contribution.SourceIdentity,
            StringComparer.Ordinal);

        return
        [
            .. request.Sources.Select(source => new ExpectedVersionCheck(
                source,
                liveByIdentity.GetValueOrDefault(
                    LinkingContributionIdentity.For(source.Descriptor, source.ContributionMode))))
        ];
    }

    private void ApplyExpectedVersions(
        IReadOnlyList<ExpectedVersionCheck> versionChecks,
        LockedConfirmationState loaded)
    {
        foreach (var check in versionChecks)
        {
            var source = check.Source;
            var live = check.Live;

            if (live is null
                || source.ExistingContributionId != live.Id
                || source.ExistingContributionVersion is not { } expectedVersion)
            {
                continue;
            }

            db.Entry(loaded.ContributionsById[live.Id])
                .Property(contribution => contribution.Version)
                .OriginalValue = expectedVersion;
        }
    }

    private static void EnsureExpectedVersions(IReadOnlyList<ExpectedVersionCheck> versionChecks)
    {
        foreach (var check in versionChecks)
        {
            var source = check.Source;
            var live = check.Live;

            if (live is null)
            {
                if (source.ExistingContributionId is not null || source.ExistingContributionVersion is not null)
                {
                    throw new LinkingStaleVersionException();
                }

                continue;
            }

            if (source.ExistingContributionId != live.Id
                || source.ExistingContributionVersion != live.Version)
            {
                throw new LinkingStaleVersionException();
            }
        }
    }

    private sealed record ExpectedVersionCheck(
        LinkingOperationSourceRequest Source,
        LinkingConfirmedContribution? Live);

    private async Task SaveTranslatingWriteExceptionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LinkingStaleVersionException();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new LinkingDuplicateContributionException();
        }
    }
}
