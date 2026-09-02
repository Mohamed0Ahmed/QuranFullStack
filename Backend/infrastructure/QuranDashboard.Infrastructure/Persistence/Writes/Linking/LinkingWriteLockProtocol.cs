using System.Data;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Storage;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed class LinkingWriteLockProtocol(QuranDashboardDbContext db)
{
    private const int PreparedActorEnqueueNamespace = 193648317;
    private const int ConfirmationActorEnqueueNamespace = PreparedActorEnqueueNamespace;
    private const int PreparedEnqueueNamespace = 193648318;
    private const int PreparedProcessingNamespace = 193648319;
    private const int ConfirmationIdempotencyNamespace = PreparedProcessingNamespace;
    private const int PreparedWorkerClaimNamespace = 193648320;
    private const int ConfirmationWorkerClaimNamespace = PreparedWorkerClaimNamespace;
    private const int WorkerClaimKey = 1;
    private const int ConfirmationJobNamespace = 193648321;
    private const int DoorInclusionGraphMutationNamespace = 193648322;
    private const int DoorInclusionGraphMutationKey = 1;

    public async Task AcquirePreparedEnqueueAsync(
        int actorUserId,
        Guid preparationKey,
        CancellationToken cancellationToken)
    {
        RequireActiveTransaction();
        await AcquireTransactionLockAsync(
            PreparedActorEnqueueNamespace,
            ActorKey(actorUserId),
            cancellationToken);
        await AcquireTransactionLockAsync(
            PreparedEnqueueNamespace,
            HashedGuidKey(preparationKey),
            cancellationToken);
    }

    public async Task AcquireConfirmationEnqueueAsync(
        int actorUserId,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireActiveTransaction();
        await AcquireTransactionLockAsync(
            ConfirmationActorEnqueueNamespace,
            ActorKey(actorUserId),
            cancellationToken);
        await AcquireTransactionLockAsync(
            ConfirmationIdempotencyNamespace,
            HashedGuidKey(idempotencyKey),
            cancellationToken);
    }

    public Task AcquirePreparedWorkerClaimAsync(CancellationToken cancellationToken) =>
        AcquireRequiredTransactionLockAsync(
            PreparedWorkerClaimNamespace,
            WorkerClaimKey,
            cancellationToken);

    public Task<bool> TryAcquirePreparedProcessingForWorkerClaimAsync(
        Guid preflightId,
        CancellationToken cancellationToken) =>
        TryAcquireRequiredTransactionLockAsync(
            PreparedProcessingNamespace,
            RawGuidKey(preflightId),
            cancellationToken);

    public Task AcquireConfirmationWorkerClaimAsync(CancellationToken cancellationToken) =>
        AcquireRequiredTransactionLockAsync(
            ConfirmationWorkerClaimNamespace,
            WorkerClaimKey,
            cancellationToken);

    public Task AcquirePreparedProcessingMutationAsync(
        Guid preflightId,
        CancellationToken cancellationToken) =>
        AcquireRequiredTransactionLockAsync(
            PreparedProcessingNamespace,
            RawGuidKey(preflightId),
            cancellationToken);

    public async Task<IAsyncDisposable?> TryAcquirePreparedProcessingSessionAsync(
        Guid preflightId,
        CancellationToken cancellationToken)
    {
        RequireOpenConnection();
        var key = RawGuidKey(preflightId);
        var acquired = await db.Database.SqlQueryRaw<bool>(
                "SELECT pg_try_advisory_lock({0}, {1}) AS \"Value\"",
                PreparedProcessingNamespace,
                key)
            .SingleAsync(cancellationToken);
        return acquired ? new PreparedProcessingSessionLock(this, key) : null;
    }

    public Task AcquireConfirmationJobMutationAsync(
        Guid jobId,
        CancellationToken cancellationToken) =>
        AcquireRequiredTransactionLockAsync(
            ConfirmationJobNamespace,
            HashedGuidKey(jobId),
            cancellationToken);

    public async Task AcquireConfirmationFinalizingAsync(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        RequireActiveTransaction();
        await AcquireTransactionLockAsync(
            ConfirmationJobNamespace,
            HashedGuidKey(jobId),
            cancellationToken);
        await AcquireTransactionLockAsync(
            DoorInclusionGraphMutationNamespace,
            DoorInclusionGraphMutationKey,
            cancellationToken);
    }

    public async Task<IConfirmationJobPhase> BeginConfirmationCommitAsync(
        IDbContextTransaction transaction,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        RequireActiveTransaction(transaction);
        await AcquireTransactionLockAsync(
            ConfirmationJobNamespace,
            HashedGuidKey(jobId),
            cancellationToken);
        return new ConfirmationJobPhase(this, transaction);
    }

    public async Task<IConfirmationIdempotencyPhase> AcquireConfirmationIdempotencyAsync(
        IConfirmationJobPhase phase,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        var transaction = RequirePhase(phase);
        await AcquireTransactionLockAsync(
            ConfirmationIdempotencyNamespace,
            HashedGuidKey(idempotencyKey),
            cancellationToken);
        return new ConfirmationIdempotencyPhase(this, transaction);
    }

    public async Task AcquireConfirmationGraphMutationAsync(
        IConfirmationIdempotencyPhase phase,
        CancellationToken cancellationToken)
    {
        _ = RequirePhase(phase);
        await AcquireTransactionLockAsync(
            DoorInclusionGraphMutationNamespace,
            DoorInclusionGraphMutationKey,
            cancellationToken);
    }

    public Task AcquireDoorInclusionGraphMutationAsync(CancellationToken cancellationToken) =>
        AcquireRequiredTransactionLockAsync(
            DoorInclusionGraphMutationNamespace,
            DoorInclusionGraphMutationKey,
            cancellationToken);

    private async Task AcquireRequiredTransactionLockAsync(
        int lockNamespace,
        int key,
        CancellationToken cancellationToken)
    {
        RequireActiveTransaction();
        await AcquireTransactionLockAsync(lockNamespace, key, cancellationToken);
    }

    private async Task<bool> TryAcquireRequiredTransactionLockAsync(
        int lockNamespace,
        int key,
        CancellationToken cancellationToken)
    {
        RequireActiveTransaction();
        return await db.Database.SqlQueryRaw<bool>(
                "SELECT pg_try_advisory_xact_lock({0}, {1}) AS \"Value\"",
                lockNamespace,
                key)
            .SingleAsync(cancellationToken);
    }

    private async Task AcquireTransactionLockAsync(
        int lockNamespace,
        int key,
        CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({lockNamespace}, {key})",
            cancellationToken);

    private async ValueTask ReleasePreparedProcessingSessionAsync(int key)
    {
        RequireOpenConnection();
        _ = await db.Database.SqlQueryRaw<bool>(
                "SELECT pg_advisory_unlock({0}, {1}) AS \"Value\"",
                PreparedProcessingNamespace,
                key)
            .SingleAsync();
    }

    private IDbContextTransaction RequireActiveTransaction() =>
        db.Database.CurrentTransaction
        ?? throw new InvalidOperationException("The Linking write lock requires an active database transaction.");

    private void RequireActiveTransaction(IDbContextTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        if (!ReferenceEquals(db.Database.CurrentTransaction, transaction))
        {
            throw new InvalidOperationException(
                "The Linking write lock phase requires the active transaction that created it.");
        }
    }

    private IDbContextTransaction RequirePhase(IConfirmationJobPhase phase)
    {
        ArgumentNullException.ThrowIfNull(phase);
        return phase is ConfirmationJobPhase created
            ? created.Require(this, db.Database.CurrentTransaction)
            : throw new InvalidOperationException("The confirmation lock phase was not created by this protocol.");
    }

    private IDbContextTransaction RequirePhase(IConfirmationIdempotencyPhase phase)
    {
        ArgumentNullException.ThrowIfNull(phase);
        return phase is ConfirmationIdempotencyPhase created
            ? created.Require(this, db.Database.CurrentTransaction)
            : throw new InvalidOperationException("The confirmation lock phase was not created by this protocol.");
    }

    private void RequireOpenConnection()
    {
        if (db.Database.GetDbConnection() is not NpgsqlConnection { State: ConnectionState.Open })
        {
            throw new InvalidOperationException(
                "The prepared-processing lock requires an open Npgsql connection.");
        }
    }

    private static int ActorKey(int actorUserId) => actorUserId > 0
        ? actorUserId
        : throw new ArgumentOutOfRangeException(nameof(actorUserId));

    private static int HashedGuidKey(Guid key) =>
        BitConverter.ToInt32(SHA256.HashData(key.ToByteArray()), 0);

    private static int RawGuidKey(Guid key) => BitConverter.ToInt32(key.ToByteArray(), 0);

    internal interface IConfirmationJobPhase
    {
    }

    internal interface IConfirmationIdempotencyPhase
    {
    }

    private abstract class ConfirmationPhase(
        LinkingWriteLockProtocol protocol,
        IDbContextTransaction transaction)
    {
        private readonly LinkingWriteLockProtocol protocol = protocol;
        private readonly IDbContextTransaction transaction = transaction;

        internal IDbContextTransaction Require(
            LinkingWriteLockProtocol expectedProtocol,
            IDbContextTransaction? currentTransaction)
        {
            if (!ReferenceEquals(protocol, expectedProtocol))
            {
                throw new InvalidOperationException(
                    "The confirmation lock phase belongs to another protocol instance.");
            }

            if (!ReferenceEquals(transaction, currentTransaction))
            {
                throw new InvalidOperationException(
                    "The confirmation lock phase requires the active transaction that created it.");
            }

            return transaction;
        }
    }

    private sealed class ConfirmationJobPhase(
        LinkingWriteLockProtocol protocol,
        IDbContextTransaction transaction)
        : ConfirmationPhase(protocol, transaction), IConfirmationJobPhase
    {
    }

    private sealed class ConfirmationIdempotencyPhase(
        LinkingWriteLockProtocol protocol,
        IDbContextTransaction transaction)
        : ConfirmationPhase(protocol, transaction), IConfirmationIdempotencyPhase
    {
    }

    private sealed class PreparedProcessingSessionLock(LinkingWriteLockProtocol protocol, int key)
        : IAsyncDisposable
    {
        private bool disposed;

        public async ValueTask DisposeAsync()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            await protocol.ReleasePreparedProcessingSessionAsync(key);
        }
    }
}
