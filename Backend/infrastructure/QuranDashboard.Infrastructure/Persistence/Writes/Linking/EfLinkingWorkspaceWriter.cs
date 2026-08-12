using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingWorkspaceWriter(
    QuranDashboardDbContext db,
    ILinkingSourceResolutionReader resolution) : ILinkingWorkspaceWriter
{
    private const string SelectionFieldPrefix = "selection.";
    private const string RootIdField = "rootId";
    private const string LemmaIdField = "lemmaId";
    private const string StemIdField = "stemId";
    private const string WordIdField = "wordId";
    private const string SelectionTashkeelWordIdField = SelectionFieldPrefix + "tashkeelWordId";

    public async Task<LinkingWorkspaceDto> AddSourceAsync(
        int userId,
        LinkingSourceDescriptor descriptor,
        uint? expectedWorkspaceVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var form = LinkingSourceStorage.Encode(descriptor);
        await EnsureDimensionReferencesExistAsync(form, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var workspace = await LoadWorkspaceAsync(userId, cancellationToken);
        if (workspace is null)
        {
            if (expectedWorkspaceVersion.HasValue)
            {
                throw new LinkingStaleVersionException();
            }

            workspace = new LinkingWorkspace
            {
                UserId = userId,
                CreatedAtUtc = now,
                CreatedBy = userId,
                UpdatedAtUtc = now,
                UpdatedBy = userId,
            };

            db.LinkingWorkspaces.Add(workspace);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        }
        else
        {
            ApplyWorkspaceVersion(workspace, expectedWorkspaceVersion);
        }

        var existing = await FindEquivalentSourceAsync(workspace.Id, form, cancellationToken);
        if (existing is not null)
        {
            existing.Label = form.Label;
            existing.UpdatedAtUtc = now;
            existing.UpdatedBy = userId;

            StampWorkspace(workspace, userId, now);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await LinkingWorkspaceProjection.ProjectAsync(db, workspace, cancellationToken);
        }

        var sourceCount = await db.LinkingWorkspaceSources
            .CountAsync(source => source.WorkspaceId == workspace.Id, cancellationToken);
        if (sourceCount >= LinkingLimits.MaxPreparedSources)
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.PreparedSourceLimitExceeded,
                "sources",
                LinkingLimits.MaxPreparedSources.ToString(CultureInfo.InvariantCulture)));
        }

        var isManual = form.Kind == LinkingSourceKind.ManualMushafAyahs;

        var source = new LinkingWorkspaceSource
        {
            WorkspaceId = workspace.Id,
            OrderValue = sourceCount + 1,
            SourceKind = form.Kind,
            SourceIdentity = form.SourceIdentity,
            SourceIdentityHash = form.SourceIdentityHash,
            Label = form.Label,
            ScopeJson = form.ScopeJson,
            RootId = form.RootId,
            LemmaId = form.LemmaId,
            StemId = form.StemId,
            UniqueSimpleWordId = form.UniqueSimpleWordId,
            UniqueTashkeelWordId = form.UniqueTashkeelWordId,
            WordTypeTashkeelWordId = form.WordTypeTashkeelWordId,
            InclusionMode = LinkingInclusionMode.AllExcept,
            AutomaticWordMatchesEnabled = isManual ? null : true,
            ManualLinkShape = isManual ? LinkingManualLinkShape.Independent : null,
            CreatedAtUtc = now,
            CreatedBy = userId,
            UpdatedAtUtc = now,
            UpdatedBy = userId,
        };

        db.LinkingWorkspaceSources.Add(source);
        StampWorkspace(workspace, userId, now);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        if (descriptor is LinkingSourceDescriptor.ManualMushafAyahs manual)
        {
            await AddManualAyahsAsync(source.Id, manual, cancellationToken);
            await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return await LinkingWorkspaceProjection.ProjectAsync(db, workspace, cancellationToken);
    }

    public async Task<LinkingWorkspaceDto> RemoveSourceAsync(
        int userId,
        long sourceId,
        uint expectedWorkspaceVersion,
        CancellationToken cancellationToken)
    {
        var workspace = await LoadWorkspaceAsync(userId, cancellationToken)
            ?? throw new LinkingWorkspaceSourceNotFoundException(sourceId);

        ApplyWorkspaceVersion(workspace, expectedWorkspaceVersion);

        var source = await db.LinkingWorkspaceSources
            .FirstOrDefaultAsync(
                candidate => candidate.Id == sourceId && candidate.WorkspaceId == workspace.Id,
                cancellationToken)
            ?? throw new LinkingWorkspaceSourceNotFoundException(sourceId);

        db.LinkingWorkspaceSources.Remove(source);

        var survivors = await db.LinkingWorkspaceSources
            .Where(candidate => candidate.WorkspaceId == workspace.Id && candidate.Id != sourceId)
            .OrderBy(candidate => candidate.OrderValue)
            .ThenBy(candidate => candidate.Id)
            .ToListAsync(cancellationToken);

        Resequence(survivors);
        StampWorkspace(workspace, userId, DateTimeOffset.UtcNow);

        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        return await LinkingWorkspaceProjection.ProjectAsync(db, workspace, cancellationToken);
    }

    public async Task<LinkingWorkspaceDto> ReorderSourcesAsync(
        int userId,
        IReadOnlyList<long> orderedSourceIds,
        uint expectedWorkspaceVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(orderedSourceIds);

        var workspace = await LoadWorkspaceAsync(userId, cancellationToken)
            ?? throw new LinkingStaleVersionException();

        ApplyWorkspaceVersion(workspace, expectedWorkspaceVersion);

        var sources = await db.LinkingWorkspaceSources
            .Where(source => source.WorkspaceId == workspace.Id)
            .ToListAsync(cancellationToken);

        var requested = orderedSourceIds.ToList();
        if (requested.Count != sources.Count
            || requested.Distinct().Count() != requested.Count
            || !requested.ToHashSet().SetEquals(sources.Select(source => source.Id)))
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.ReorderSetMismatch, "sourceIds", null));
        }

        var byId = sources.ToDictionary(source => source.Id);
        Resequence(requested.Select(id => byId[id]));

        StampWorkspace(workspace, userId, DateTimeOffset.UtcNow);

        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        return await LinkingWorkspaceProjection.ProjectAsync(db, workspace, cancellationToken);
    }

    public async Task<LinkingWorkspaceDto> ClearSourcesAsync(
        int userId,
        uint expectedWorkspaceVersion,
        CancellationToken cancellationToken)
    {
        var workspace = await LoadWorkspaceAsync(userId, cancellationToken)
            ?? throw new LinkingStaleVersionException();

        ApplyWorkspaceVersion(workspace, expectedWorkspaceVersion);

        var sources = await db.LinkingWorkspaceSources
            .Where(source => source.WorkspaceId == workspace.Id)
            .ToListAsync(cancellationToken);

        db.LinkingWorkspaceSources.RemoveRange(sources);
        StampWorkspace(workspace, userId, DateTimeOffset.UtcNow);

        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        return await LinkingWorkspaceProjection.ProjectAsync(db, workspace, cancellationToken);
    }

    private async Task<LinkingWorkspace?> LoadWorkspaceAsync(int userId, CancellationToken cancellationToken) =>
        await db.LinkingWorkspaces.FirstOrDefaultAsync(
            workspace => workspace.UserId == userId, cancellationToken);

    private async Task<LinkingWorkspaceSource?> FindEquivalentSourceAsync(
        long workspaceId,
        LinkingSourceStorageForm form,
        CancellationToken cancellationToken)
    {
        var candidate = await db.LinkingWorkspaceSources.FirstOrDefaultAsync(
            source => source.WorkspaceId == workspaceId && source.SourceIdentityHash == form.SourceIdentityHash,
            cancellationToken);

        if (candidate is null)
        {
            return null;
        }

        return string.Equals(candidate.SourceIdentity, form.SourceIdentity, StringComparison.Ordinal)
            ? candidate
            : throw new LinkingDuplicateContributionException();
    }

    private async Task EnsureDimensionReferencesExistAsync(
        LinkingSourceStorageForm form,
        CancellationToken cancellationToken)
    {
        var prefix = form.Kind == LinkingSourceKind.WordType ? SelectionFieldPrefix : string.Empty;

        await EnsureReferenceExistsAsync(
            form.RootId,
            prefix + RootIdField,
            db.QuranRoots.AsNoTracking().Select(root => root.Id),
            cancellationToken);

        await EnsureReferenceExistsAsync(
            form.LemmaId,
            prefix + LemmaIdField,
            db.QuranLemmas.AsNoTracking().Select(lemma => lemma.Id),
            cancellationToken);

        await EnsureReferenceExistsAsync(
            form.StemId,
            prefix + StemIdField,
            db.QuranStems.AsNoTracking().Select(stem => stem.Id),
            cancellationToken);

        await EnsureReferenceExistsAsync(
            form.UniqueSimpleWordId,
            WordIdField,
            db.QuranWordsUniqueSimple.AsNoTracking().Select(word => word.Id),
            cancellationToken);

        await EnsureReferenceExistsAsync(
            form.UniqueTashkeelWordId,
            WordIdField,
            db.QuranWordsUniqueTashkeel.AsNoTracking().Select(word => word.Id),
            cancellationToken);

        await EnsureReferenceExistsAsync(
            form.WordTypeTashkeelWordId,
            SelectionTashkeelWordIdField,
            db.QuranWordsUniqueTashkeel.AsNoTracking().Select(word => word.Id),
            cancellationToken);
    }

    private static async Task EnsureReferenceExistsAsync(
        int? referenceId,
        string field,
        IQueryable<int> existingIds,
        CancellationToken cancellationToken)
    {
        if (referenceId is not int id)
        {
            return;
        }

        if (!await existingIds.AnyAsync(candidate => candidate == id, cancellationToken))
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.ReferenceUnknown,
                field,
                id.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private async Task AddManualAyahsAsync(
        long sourceId,
        LinkingSourceDescriptor.ManualMushafAyahs manual,
        CancellationToken cancellationToken)
    {
        var verseKeys = manual.VerseKeys.Select(verseKey => verseKey.Value).ToList();

        var ayahs = await db.QuranAyahs
            .AsNoTracking()
            .Where(ayah => verseKeys.Contains(ayah.VerseKey))
            .Select(ayah => new { ayah.Id, ayah.VerseKey, ayah.PageFrom })
            .ToListAsync(cancellationToken);

        var ayahsByVerseKey = ayahs.ToDictionary(ayah => ayah.VerseKey, StringComparer.Ordinal);

        var orderValue = 1;
        foreach (var verseKey in verseKeys)
        {
            if (!ayahsByVerseKey.TryGetValue(verseKey, out var ayah))
            {
                throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                    LinkingWorkspaceViolationCode.AyahReferenceUnknown, "manualAyahs.verseKey", verseKey));
            }

            db.LinkingWorkspaceSourceManualAyahs.Add(new LinkingWorkspaceSourceManualAyah
            {
                WorkspaceSourceId = sourceId,
                AyahId = ayah.Id,
                OrderValue = orderValue++,
                PageHint = ayah.PageFrom,
            });
        }
    }

    private void ApplyWorkspaceVersion(LinkingWorkspace workspace, uint? expectedVersion)
    {
        if (!expectedVersion.HasValue)
        {
            throw new LinkingStaleVersionException();
        }

        db.Entry(workspace).Property(entity => entity.Version).OriginalValue = expectedVersion.Value;
    }

    private static void StampWorkspace(LinkingWorkspace workspace, int userId, DateTimeOffset now)
    {
        workspace.UpdatedAtUtc = now;
        workspace.UpdatedBy = userId;
    }

    private static void Resequence(IEnumerable<LinkingWorkspaceSource> orderedSources)
    {
        var position = 1;
        foreach (var source in orderedSources)
        {
            source.OrderValue = position++;
        }
    }

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
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new LinkingDuplicateContributionException();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23503" })
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.ReferenceUnknown, null, null));
        }
    }
}
