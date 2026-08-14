using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationJobStore
{
    public async Task<bool> PrepareExecutionAsync(
        LinkingConfirmationJobLease lease,
        Func<int, int, CancellationToken, Task<bool>> publishProgress,
        CancellationToken cancellationToken)
    {
        var jobExists = await db.LinkingConfirmationJobs.AsNoTracking().AnyAsync(
            job => job.Id == lease.JobId
                && job.LeaseOwner == lease.LeaseOwner
                && job.AttemptCount == lease.AttemptCount
                && job.LeaseExpiresAtUtc > DateTimeOffset.UtcNow
                && (job.Status == LinkingConfirmationJobStatus.Running
                    || job.Status == LinkingConfirmationJobStatus.Finalizing),
            cancellationToken);
        if (!jobExists)
        {
            return false;
        }

        var preflight = await db.LinkingPreparedPreflights.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Id == lease.PreflightId
                && candidate.ActorUserId == lease.ActorUserId
                && candidate.DoorId == lease.DoorId,
            cancellationToken);
        if (preflight is null
            || preflight.Status != LinkingPreparedPreflightStatus.Ready
            || preflight.ConfirmationAcceptedAtUtc is null
            || preflight.IsBlocked != false
            || string.IsNullOrWhiteSpace(preflight.PreflightToken))
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.PreflightStale,
                LinkingPreparedPreflightFailureCode.PreflightStale);
        }

        var expectedHash = LinkingConfirmationRequestHasher.ComputePrepared(
            preflight.Id,
            preflight.PreflightToken,
            preflight.LinkingDataRevision);
        if (!string.Equals(expectedHash, lease.RequestHash, StringComparison.Ordinal))
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.IdempotencyConflict,
                LinkingPreparedPreflightFailureCode.IdempotencyConflict);
        }

        var totalItems = await db.LinkingPreparedAyahs.AsNoTracking()
            .CountAsync(
                ayah => ayah.PreflightId == preflight.Id && ayah.IsRequested,
                cancellationToken);
        PreparedExecutionAyahRow? cursor = null;
        var processedItems = 0;
        while (true)
        {
            var query = db.LinkingPreparedAyahs.AsNoTracking()
                .Where(ayah => ayah.PreflightId == preflight.Id && ayah.IsRequested);
            if (cursor is not null)
            {
                query = query.Where(ayah =>
                    ayah.SourceOrder > cursor.SourceOrder
                    || (ayah.SourceOrder == cursor.SourceOrder && ayah.UnitOrder > cursor.UnitOrder)
                    || (ayah.SourceOrder == cursor.SourceOrder
                        && ayah.UnitOrder == cursor.UnitOrder
                        && ayah.AyahOrder > cursor.AyahOrder)
                    || (ayah.SourceOrder == cursor.SourceOrder
                        && ayah.UnitOrder == cursor.UnitOrder
                        && ayah.AyahOrder == cursor.AyahOrder
                        && ayah.Id > cursor.Id));
            }

            var batch = await query
                .OrderBy(ayah => ayah.SourceOrder)
                .ThenBy(ayah => ayah.UnitOrder)
                .ThenBy(ayah => ayah.AyahOrder)
                .ThenBy(ayah => ayah.Id)
                .Take(policy.PersistenceBatchSize)
                .Select(ayah => new PreparedExecutionAyahRow(
                    ayah.Id,
                    ayah.UnitId!.Value,
                    ayah.SourceOrder,
                    ayah.UnitOrder,
                    ayah.AyahOrder))
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            await ReadPreparedExecutionBatchAsync(batch, cancellationToken);
            processedItems += batch.Count;
            if (!await publishProgress(processedItems, totalItems, cancellationToken))
            {
                return false;
            }

            cursor = batch[^1];
        }

        if (processedItems == 0)
        {
            return await publishProgress(0, totalItems, cancellationToken);
        }

        return processedItems == totalItems;
    }

    private async Task ReadPreparedExecutionBatchAsync(
        IReadOnlyList<PreparedExecutionAyahRow> ayahs,
        CancellationToken cancellationToken)
    {
        var unitIds = ayahs.Select(ayah => ayah.UnitId).Distinct().ToList();
        var persistedUnitCount = await db.LinkingPreparedUnits.AsNoTracking()
            .CountAsync(unit => unitIds.Contains(unit.Id), cancellationToken);
        if (persistedUnitCount != unitIds.Count)
        {
            throw Conflict(
                LinkingConfirmationJobConflictKind.PreflightStale,
                LinkingPreparedPreflightFailureCode.PreflightStale);
        }

        var ayahIds = ayahs.Select(ayah => ayah.Id).ToList();
        long? wordAyahCursor = null;
        int? wordCursor = null;
        while (true)
        {
            var words = db.LinkingPreparedAyahWords.AsNoTracking()
                .Where(word => ayahIds.Contains(word.PreparedAyahId));
            if (wordAyahCursor is not null && wordCursor is not null)
            {
                words = words.Where(word =>
                    word.PreparedAyahId > wordAyahCursor
                    || (word.PreparedAyahId == wordAyahCursor && word.QuranWordId > wordCursor));
            }

            var batch = await words
                .OrderBy(word => word.PreparedAyahId)
                .ThenBy(word => word.QuranWordId)
                .Take(policy.PersistenceBatchSize)
                .Select(word => new PreparedExecutionWordRow(
                    word.PreparedAyahId,
                    word.QuranWordId))
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            wordAyahCursor = batch[^1].PreparedAyahId;
            wordCursor = batch[^1].QuranWordId;
        }

        long? descriptionCursor = null;
        while (true)
        {
            var descriptions = db.LinkingPreparedAyahDescriptions.AsNoTracking()
                .Where(description => ayahIds.Contains(description.PreparedAyahId));
            if (descriptionCursor is not null)
            {
                descriptions = descriptions.Where(description => description.Id > descriptionCursor);
            }

            var batch = await descriptions
                .OrderBy(description => description.Id)
                .Take(policy.PersistenceBatchSize)
                .Select(description => description.Id)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            descriptionCursor = batch[^1];
        }
    }

    private sealed record PreparedExecutionAyahRow(
        long Id,
        long UnitId,
        int SourceOrder,
        int UnitOrder,
        int AyahOrder);

    private sealed record PreparedExecutionWordRow(long PreparedAyahId, int QuranWordId);
}
