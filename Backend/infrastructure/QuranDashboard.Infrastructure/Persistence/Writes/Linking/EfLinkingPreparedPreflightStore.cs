using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Background;
using QuranDashboard.Infrastructure.Persistence.Linking;
using Microsoft.EntityFrameworkCore.Storage;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingPreparedPreflightStore(
    QuranDashboardDbContext db,
    ILinkingDataRevisionWriterStore revisionStore,
    ILinkingScalabilityPolicy policy,
    LinkingJobQueueSignal queueSignal,
    LinkingWriteLockProtocol lockProtocol) : ILinkingPreparedPreflightStore
{
    private const int RequestSchemaVersion = 1;
    private const int SnapshotSchemaVersion = 1;

    public async Task<LinkingPreparedPreflightReceipt> EnqueueAsync(
        int actorUserId,
        CreateLinkingPreparedPreflightRequest request,
        CancellationToken cancellationToken)
    {
        var requestDocument = LinkingPreparedPreflightRequestHasher.ComputeCanonicalDocument(request);
        var requestHash = LinkingPreparedPreflightRequestHasher.ComputeHash(requestDocument);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await lockProtocol.AcquirePreparedEnqueueAsync(
            actorUserId,
            request.PreparationKey,
            cancellationToken);

        var existing = await db.LinkingPreparedPreflights
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.ActorUserId == actorUserId
                    && candidate.PreparationKey == request.PreparationKey,
                cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new LinkingPreparedPreflightLifecycleException(
                    LinkingPreparedPreflightFailureCode.IdempotencyConflict);
            }

            var status = await ProjectStatusAsync(existing, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            if (existing.Status == LinkingPreparedPreflightStatus.Queued)
            {
                queueSignal.NotifyPreparedPreflightQueued();
            }

            return new LinkingPreparedPreflightReceipt(status, false);
        }

        var activePreflightCount = await db.LinkingPreparedPreflights.CountAsync(
            candidate => candidate.ActorUserId == actorUserId
                && candidate.ConfirmationAcceptedAtUtc == null
                && (candidate.Status == LinkingPreparedPreflightStatus.Queued
                    || candidate.Status == LinkingPreparedPreflightStatus.Preparing
                    || candidate.Status == LinkingPreparedPreflightStatus.Ready),
            cancellationToken);
        var activeJobCount = await db.LinkingConfirmationJobs.CountAsync(
            candidate => candidate.ActorUserId == actorUserId
                && (candidate.Status == LinkingConfirmationJobStatus.Queued
                    || candidate.Status == LinkingConfirmationJobStatus.Running
                    || candidate.Status == LinkingConfirmationJobStatus.Finalizing),
            cancellationToken);
        if (activePreflightCount + activeJobCount >= policy.ActiveWorkflowsPerActor)
        {
            throw new LinkingPreparedPreflightLifecycleException(
                LinkingPreparedPreflightFailureCode.ActiveLinkingWorkflowLimit);
        }

        var revision = await LockRevisionAsync(transaction, cancellationToken);
        if (request.ExpectedLinkingDataRevision is { } expectedRevision
            && revision != expectedRevision)
        {
            throw new LinkingDataStaleException(expectedRevision, revision);
        }

        if (!await db.AbwabDoors.AsNoTracking().AnyAsync(
                door => door.Id == request.DoorId,
                cancellationToken))
        {
            throw new LinkingSourceNotFoundException($"doorId={request.DoorId}");
        }

        var snapshots = await BuildSnapshotsAsync(actorUserId, request.Sources, cancellationToken);
        EnsureContributionIdentitiesAreUnique(snapshots);
        var now = await DatabaseNowAsync(cancellationToken);
        var preflight = new LinkingPreparedPreflight
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            DoorId = request.DoorId,
            PreparationKey = request.PreparationKey,
            Status = LinkingPreparedPreflightStatus.Queued,
            Stage = LinkingPreparedPreflightStage.Resolving,
            RequestSchemaVersion = RequestSchemaVersion,
            RequestDocumentJson = requestDocument,
            RequestHash = requestHash,
            LinkingDataRevision = revision,
            ProcessedSources = 0,
            TotalSources = snapshots.Count,
            ProcessedAyahs = 0,
            AttemptCount = 0,
            CleanupAttemptCount = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.LinkingPreparedPreflights.Add(preflight);
        db.LinkingPreparedSources.AddRange(snapshots.Select(snapshot => new LinkingPreparedSource
        {
            PreflightId = preflight.Id,
            OrderValue = snapshot.OrderValue,
            ResolutionIdentity = snapshot.ResolutionIdentity,
            ResolutionIdentityHash = LinkingSourceIdentity.HashOf(snapshot.ResolutionIdentity),
            ContributionIdentity = snapshot.ContributionIdentity,
            ContributionIdentityHash = LinkingSourceIdentity.HashOf(snapshot.ContributionIdentity),
            Label = snapshot.Source.Descriptor.Label,
            SourceKind = snapshot.Source.Descriptor.Kind,
            ContributionMode = snapshot.ContributionMode,
            DescriptorSchemaVersion = SnapshotSchemaVersion,
            DescriptorDocumentJson = LinkingPreparedSnapshotCodec.EncodeDescriptor(
                snapshot.Source.Descriptor,
                snapshot.ManualVerseKeys),
            ConfigurationSchemaVersion = SnapshotSchemaVersion,
            ConfigurationDocumentJson = LinkingPreparedSnapshotCodec.EncodeConfiguration(
                snapshot.Source.Configuration),
            WorkspaceSourceId = snapshot.WorkspaceSourceId,
            SourceVersion = snapshot.SourceVersion,
            AutomaticWordMatchesEnabled = snapshot.Source.Configuration.AutomaticWordMatchesEnabled,
        }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        queueSignal.NotifyPreparedPreflightQueued();
        var createdStatus = await ProjectStatusAsync(preflight, cancellationToken);
        return new LinkingPreparedPreflightReceipt(createdStatus, true);
    }

    public async Task<LinkingPreparedPreflightStatusDto?> GetStatusAsync(
        int actorUserId,
        Guid preflightId,
        CancellationToken cancellationToken)
    {
        var preflight = await db.LinkingPreparedPreflights
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == preflightId
                    && candidate.ActorUserId == actorUserId
                    && candidate.CleanupStartedAtUtc == null,
                cancellationToken);
        return preflight is null ? null : await ProjectStatusAsync(preflight, cancellationToken);
    }

    public async Task<LinkingPreparedPreflightStatusDto?> CancelAsync(
        int actorUserId,
        Guid preflightId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var preflight = await LockOwnedPreflightAsync(actorUserId, preflightId, cancellationToken);
        if (preflight is null)
        {
            return null;
        }

        var now = await DatabaseNowAsync(cancellationToken);
        if (preflight.ConfirmationAcceptedAtUtc is not null
            || await db.LinkingConfirmationJobs.AsNoTracking().AnyAsync(
                job => job.PreflightId == preflight.Id,
                cancellationToken))
        {
            throw new LinkingPreparedPreflightLifecycleException(
                LinkingPreparedPreflightFailureCode.CancellationTooLate);
        }

        if (preflight.Status == LinkingPreparedPreflightStatus.Ready
            && preflight.ExpiresAtUtc <= now)
        {
            ApplyTerminalState(
                preflight,
                LinkingPreparedPreflightStatus.Expired,
                LinkingPreparedPreflightFailureCode.PreflightExpired,
                now);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new LinkingPreparedPreflightLifecycleException(
                LinkingPreparedPreflightFailureCode.PreflightExpired,
                true);
        }

        switch (preflight.Status)
        {
            case LinkingPreparedPreflightStatus.Queued:
            case LinkingPreparedPreflightStatus.Ready:
                preflight.Status = LinkingPreparedPreflightStatus.Cancelled;
                preflight.FailureCode = LinkingPreparedPreflightFailureCode.PreflightCancelled;
                preflight.CompletedAtUtc = now;
                preflight.CancellationRequestedAtUtc = now;
                preflight.UpdatedAtUtc = now;
                break;
            case LinkingPreparedPreflightStatus.Preparing:
                preflight.CancellationRequestedAtUtc ??= now;
                preflight.UpdatedAtUtc = now;
                break;
            case LinkingPreparedPreflightStatus.Cancelled:
                break;
            case LinkingPreparedPreflightStatus.Expired:
                throw new LinkingPreparedPreflightLifecycleException(
                    LinkingPreparedPreflightFailureCode.PreflightExpired,
                    true);
            case LinkingPreparedPreflightStatus.Confirmed:
                throw new LinkingPreparedPreflightLifecycleException(
                    LinkingPreparedPreflightFailureCode.PreflightAlreadyConfirmed);
            default:
                throw new LinkingPreparedPreflightLifecycleException(
                    preflight.FailureCode ?? LinkingPreparedPreflightFailureCode.PreparationFailed);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ProjectStatusAsync(preflight, cancellationToken);
    }

    private async Task<LinkingPreparedPreflightStatusDto> ProjectStatusAsync(
        LinkingPreparedPreflight preflight,
        CancellationToken cancellationToken)
    {
        var ready = preflight.Status is LinkingPreparedPreflightStatus.Ready
            or LinkingPreparedPreflightStatus.Confirmed;
        var sourceRows = ready
            ? await db.LinkingPreparedSources
                .AsNoTracking()
                .Where(source => source.PreflightId == preflight.Id)
                .OrderBy(source => source.OrderValue)
                .ToListAsync(cancellationToken)
            : [];
        var linkCountsBySourceId = ready
            ? await db.LinkingPreparedUnits
                .AsNoTracking()
                .Where(unit => unit.PreflightId == preflight.Id)
                .GroupBy(unit => unit.SourceId)
                .Select(group => new { SourceId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.SourceId, item => item.Count, cancellationToken)
            : [];
        var sources = sourceRows
            .Select(source => new LinkingPreparedSourceSummaryDto(
                    source.Id,
                    source.OrderValue,
                    source.ResolutionIdentity,
                    source.Label,
                    LinkingSourceTokens.ToToken(source.SourceKind),
                    LinkingOperationTokens.ToToken(source.ContributionMode),
                    source.AutomaticWordMatchesEnabled,
                    source.Classification,
                    source.RequestedCount == null ? null : CountsOf(source),
                    source.ExistingContributionId,
                    source.ExpectedContributionVersion,
                    source.TotalAyahCount,
                    linkCountsBySourceId.GetValueOrDefault(source.Id)))
            .ToList();

        return new LinkingPreparedPreflightStatusDto(
            preflight.Id,
            LinkingPreparedPreflightLifecycleTokens.ToToken(preflight.Status),
            LinkingPreparedPreflightLifecycleTokens.ToToken(preflight.Stage),
            preflight.ProcessedSources,
            preflight.TotalSources,
            preflight.ProcessedAyahs,
            preflight.TotalAyahs,
            ready ? linkCountsBySourceId.Values.Sum() : null,
            policy.PollAfterMilliseconds,
            preflight.LinkingDataRevision,
            preflight.CreatedAtUtc,
            preflight.ExpiresAtUtc,
            preflight.IsNoOp,
            preflight.IsBlocked,
            ready ? preflight.PreflightToken : null,
            preflight.RequestedCount == null ? null : CountsOf(preflight),
            sources,
            LinkingPreparedPreflightLifecycleTokens.ToToken(preflight.FailureCode));
    }

    private static LinkingPreflightCountsDto CountsOf(LinkingPreparedPreflight preflight) =>
        new(
            preflight.RequestedCount!.Value,
            preflight.NewCount!.Value,
            preflight.OverlappingCount!.Value,
            preflight.UnchangedCount!.Value,
            preflight.UpdatedCount!.Value,
            preflight.RemovedCount!.Value,
            preflight.InvalidCount!.Value);

    private static LinkingPreflightCountsDto CountsOf(LinkingPreparedSource source) =>
        new(
            source.RequestedCount!.Value,
            source.NewCount!.Value,
            source.OverlappingCount!.Value,
            source.UnchangedCount!.Value,
            source.UpdatedCount!.Value,
            source.RemovedCount!.Value,
            source.InvalidCount!.Value);

    private async Task<IReadOnlyList<PreparedSnapshot>> BuildSnapshotsAsync(
        int actorUserId,
        IReadOnlyList<LinkingPreparedSourceRequest> requests,
        CancellationToken cancellationToken)
    {
        var workspaceIds = requests
            .Where(request => request.WorkspaceSource is not null)
            .Select(request => request.WorkspaceSource!.SourceId)
            .Distinct()
            .Order()
            .ToList();
        var workspaceSources = new Dictionary<long, WorkspaceSnapshot>();
        foreach (var sourceId in workspaceIds)
        {
            workspaceSources.Add(
                sourceId,
                await LoadWorkspaceSnapshotAsync(actorUserId, sourceId, cancellationToken));
        }

        return requests.OrderBy(request => request.OrderValue).Select(request =>
        {
            if (request.InlineSource is not null)
            {
                return PreparedSnapshot.Create(request.OrderValue, request.InlineSource, null, null, []);
            }

            var reference = request.WorkspaceSource!;
            var workspace = workspaceSources[reference.SourceId];
            if (workspace.Version != reference.SourceVersion)
            {
                throw new LinkingPreparedPreflightLifecycleException(
                    LinkingPreparedPreflightFailureCode.WorkspaceSourceStale);
            }

            return PreparedSnapshot.Create(
                request.OrderValue,
                workspace.Source,
                reference.SourceId,
                reference.SourceVersion,
                workspace.ManualVerseKeys);
        }).ToList();
    }

    private async Task<WorkspaceSnapshot> LoadWorkspaceSnapshotAsync(
        int actorUserId,
        long sourceId,
        CancellationToken cancellationToken)
    {
        var rows = await db.LinkingWorkspaceSources
            .FromSqlInterpolated(
                $"""
                SELECT source.*, source.xmin
                FROM linking_workspace_sources source
                JOIN linking_workspaces workspace ON workspace.id = source.workspace_id
                WHERE source.id = {sourceId} AND workspace.user_id = {actorUserId}
                FOR UPDATE OF source
                """)
            .ToListAsync(cancellationToken);
        var source = rows.SingleOrDefault()
            ?? throw new LinkingPreparedPreflightLifecycleException(
                LinkingPreparedPreflightFailureCode.WorkspaceSourceStale);
        var manualRows = await (
            from manual in db.LinkingWorkspaceSourceManualAyahs.AsNoTracking()
            join ayah in db.QuranAyahs.AsNoTracking() on manual.AyahId equals ayah.Id
            where manual.WorkspaceSourceId == source.Id
            orderby manual.OrderValue
            select new { manual.AyahId, ayah.VerseKey })
            .ToListAsync(cancellationToken);
        var overrides = await db.LinkingWorkspaceSourceAyahOverrides
            .AsNoTracking()
            .Where(row => row.WorkspaceSourceId == source.Id)
            .Select(row => row.AyahId)
            .ToListAsync(cancellationToken);
        var words = await db.LinkingWorkspaceSourceWords
            .AsNoTracking()
            .Where(row => row.WorkspaceSourceId == source.Id)
            .Select(row => new LinkingWorkspaceSelectedWordInput(row.AyahId, row.QuranWordId))
            .ToListAsync(cancellationToken);
        var descriptions = await db.LinkingWorkspaceSourceDescriptions
            .AsNoTracking()
            .Where(row => row.WorkspaceSourceId == source.Id)
            .OrderBy(row => row.AyahId)
            .ThenBy(row => row.OrderValue)
            .Select(row => new LinkingWorkspaceDescriptionInput(row.AyahId, row.OrderValue, row.Body))
            .ToListAsync(cancellationToken);
        var descriptor = LinkingSourceStorage.Decode(source, [.. manualRows.Select(row => row.VerseKey)]);
        if (!LinkingSourceConfiguration.TryCreate(
            descriptor.Kind,
            source.InclusionMode,
            overrides,
            words,
            source.AutomaticWordMatchesEnabled,
            source.ManualLinkShape,
            descriptions,
            out var configuration,
            out _))
        {
            throw new InvalidDataException("The stored linking workspace configuration is incoherent.");
        }

        return new WorkspaceSnapshot(
            source.Version,
            new LinkingPreparedInlineSource(descriptor, configuration),
            [.. manualRows.Select(row => row.VerseKey)]);
    }

    private static void EnsureContributionIdentitiesAreUnique(IReadOnlyList<PreparedSnapshot> snapshots)
    {
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var snapshot in snapshots)
        {
            if (!identities.Add(snapshot.ContributionIdentity))
            {
                throw new LinkingDuplicateContributionException();
            }
        }
    }

    private async Task<long> LockRevisionAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException("Expected an Npgsql connection.");
        var npgsqlTransaction = transaction.GetDbTransaction() as NpgsqlTransaction
            ?? throw new InvalidOperationException("Expected an Npgsql transaction.");
        return await revisionStore.LockForReadAsync(connection, npgsqlTransaction, cancellationToken);
    }

    private async Task<LinkingPreparedPreflight?> LockOwnedPreflightAsync(
        int actorUserId,
        Guid preflightId,
        CancellationToken cancellationToken) =>
        (await db.LinkingPreparedPreflights
            .FromSqlInterpolated(
                $"""
                SELECT preflight.*, preflight.xmin
                FROM linking_prepared_preflights preflight
                WHERE id = {preflightId}
                  AND actor_user_id = {actorUserId}
                  AND cleanup_started_at_utc IS NULL
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken))
        .SingleOrDefault();

    private sealed record WorkspaceSnapshot(
        uint Version,
        LinkingPreparedInlineSource Source,
        IReadOnlyList<string> ManualVerseKeys);

    private sealed record PreparedSnapshot(
        int OrderValue,
        LinkingPreparedInlineSource Source,
        long? WorkspaceSourceId,
        uint? SourceVersion,
        IReadOnlyList<string> ManualVerseKeys,
        LinkingContributionMode ContributionMode,
        string ResolutionIdentity,
        string ContributionIdentity)
    {
        public static PreparedSnapshot Create(
            int orderValue,
            LinkingPreparedInlineSource source,
            long? workspaceSourceId,
            uint? sourceVersion,
            IReadOnlyList<string> manualVerseKeys)
        {
            if (source.Descriptor.Kind != source.Configuration.SourceKind)
            {
                throw new InvalidDataException("The prepared linking source configuration is incoherent.");
            }

            var mode = source.Configuration.ContributionMode;
            var descriptorVerseKeys = source.Descriptor is LinkingSourceDescriptor.ManualMushafAyahs manual
                ? manual.VerseKeys.Select(verseKey => verseKey.Value).ToList()
                : manualVerseKeys;
            return new PreparedSnapshot(
                orderValue,
                source,
                workspaceSourceId,
                sourceVersion,
                descriptorVerseKeys,
                mode,
                LinkingSourceIdentity.For(source.Descriptor),
                LinkingContributionIdentity.For(source.Descriptor, mode));
        }
    }
}
