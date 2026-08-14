using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter(
    QuranDashboardDbContext db,
    ILinkingDataRevisionWriterStore revisionStore) : ILinkingConfirmationWriter
{
    private const int IdempotencyLockNamespace = 193648319;

    public async Task<LinkingConfirmationResultDto?> FindLegacyReplayAsync(
        int actorUserId,
        int doorId,
        Guid idempotencyKey,
        LinkingConfirmationRequestContract requestContract,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await TakeIdempotencyLockAsync(idempotencyKey, cancellationToken);
        var replay = await FindLegacyReplayUnderLockAsync(
            actorUserId,
            doorId,
            idempotencyKey,
            requestContract,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return replay;
    }

    public async Task<LinkingConfirmationWriteResult> ConfirmAsync(
        int actorUserId,
        LinkingOperationRequest request,
        LinkingOperationIntent intent,
        LinkingConfirmationRequestContract requestContract,
        Func<LinkingOperationIntent, LinkingConfirmedDoorState, LinkingOperationClassification> classify,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(requestContract);
        ArgumentNullException.ThrowIfNull(classify);

        var idempotencyKey = request.IdempotencyKey
            ?? throw new ArgumentException("A confirmation idempotency key is required.", nameof(request));
        var suppliedToken = request.PreflightToken
            ?? throw new ArgumentException("A confirmation preflight token is required.", nameof(request));
        RequireLegacyContract(request, requestContract);

        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await TakeIdempotencyLockAsync(idempotencyKey, cancellationToken);
        var replay = await FindLegacyReplayUnderLockAsync(
            actorUserId,
            request.DoorId,
            idempotencyKey,
            requestContract,
            cancellationToken);
        if (replay is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new LinkingConfirmationWriteResult.Success(replay, true);
        }

        var revision = await LockRevisionAsync(transaction, cancellationToken);
        if (revision != request.ExpectedLinkingDataRevision)
        {
            throw new LinkingDataStaleException(request.ExpectedLinkingDataRevision, revision);
        }

        var loaded = await LoadLockedStateAsync(request.DoorId, cancellationToken);
        if (loaded is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new LinkingConfirmationWriteResult.DoorNotFound(request.DoorId);
        }

        var versionChecks = BuildExpectedVersionChecks(request, loaded.State);
        intent = intent with { IsDoorArchived = loaded.State.IsArchived };
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
            await transaction.RollbackAsync(cancellationToken);
            return new LinkingConfirmationWriteResult.InvalidClassification(classification);
        }

        var result = await PersistOperationAsync(
            actorUserId,
            request,
            classification,
            loaded,
            requestContract,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new LinkingConfirmationWriteResult.Success(result, false);
    }

    private async Task<LinkingConfirmationResultDto?> FindLegacyReplayUnderLockAsync(
        int actorUserId,
        int doorId,
        Guid idempotencyKey,
        LinkingConfirmationRequestContract requestContract,
        CancellationToken cancellationToken)
    {
        if (await db.LinkingConfirmationJobs.AsNoTracking().AnyAsync(
                job => job.IdempotencyKey == idempotencyKey,
                cancellationToken))
        {
            throw new LinkingIdempotencyConflictException();
        }

        var operation = await db.LinkingOperations.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.IdempotencyKey == idempotencyKey,
            cancellationToken);
        if (operation is null)
        {
            return null;
        }

        if (operation.ActorUserId != actorUserId
            || operation.DoorId != doorId
            || !string.Equals(operation.RequestContractKind, requestContract.Kind, StringComparison.Ordinal)
            || operation.RequestSchemaVersion != requestContract.SchemaVersion
            || !string.Equals(operation.RequestHash, requestContract.RequestHash, StringComparison.Ordinal)
            || operation.LinkingDataRevision != requestContract.LinkingDataRevision)
        {
            throw new LinkingIdempotencyConflictException();
        }

        return DeserializeOutcome(operation.OutcomeJson);
    }

    private static void RequireLegacyContract(
        LinkingOperationRequest request,
        LinkingConfirmationRequestContract requestContract)
    {
        if (!string.Equals(
                requestContract.Kind,
                LinkingConfirmationRequestContracts.LegacyExpanded,
                StringComparison.Ordinal)
            || requestContract.SchemaVersion != LinkingConfirmationRequestContracts.SchemaVersion
            || requestContract.LinkingDataRevision != request.ExpectedLinkingDataRevision
            || !string.Equals(
                requestContract.RequestHash,
                LinkingConfirmationRequestHasher.ComputeLegacy(request),
                StringComparison.Ordinal))
        {
            throw new LinkingIdempotencyConflictException();
        }
    }

    private async Task TakeIdempotencyLockAsync(Guid idempotencyKey, CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({IdempotencyLockNamespace}, {LockKey(idempotencyKey)})",
            cancellationToken);

    private async Task<long> LockRevisionAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException("Expected an Npgsql linking confirmation connection.");
        var npgsqlTransaction = transaction.GetDbTransaction() as NpgsqlTransaction
            ?? throw new InvalidOperationException("Expected an Npgsql linking confirmation transaction.");
        return await revisionStore.LockForReadAsync(connection, npgsqlTransaction, cancellationToken);
    }

    private async Task<DateTimeOffset> DatabaseNowAsync(CancellationToken cancellationToken) =>
        await db.Database.SqlQuery<DateTimeOffset>($"SELECT CURRENT_TIMESTAMP AS \"Value\"")
            .SingleAsync(cancellationToken);

    private static int LockKey(Guid key) =>
        BitConverter.ToInt32(SHA256.HashData(key.ToByteArray()), 0);

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
