using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Caching.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingWorkspaceWriter
{
    public async Task<LinkingWorkspaceDto> UpdateSourceTypesAsync(
        int userId,
        long sourceId,
        IReadOnlyList<string> typeCodes,
        uint expectedWorkspaceVersion,
        uint expectedSourceVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(typeCodes);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var linkingDataRevision = await LockLinkingDataForReadAsync(transaction, cancellationToken);
        var workspace = await LoadWorkspaceAsync(userId, cancellationToken)
            ?? throw new LinkingWorkspaceSourceNotFoundException(sourceId);
        ApplyWorkspaceVersion(workspace, expectedWorkspaceVersion);

        var source = await db.LinkingWorkspaceSources
            .FromSqlInterpolated(
                $"SELECT source.*, source.xmin FROM linking_workspace_sources source WHERE id = {sourceId} FOR UPDATE")
            .FirstOrDefaultAsync(
                candidate => candidate.Id == sourceId && candidate.WorkspaceId == workspace.Id,
                cancellationToken)
            ?? throw new LinkingWorkspaceSourceNotFoundException(sourceId);
        if (source.Version != expectedSourceVersion)
        {
            throw new LinkingStaleVersionException();
        }

        db.Entry(source).Property(entity => entity.Version).OriginalValue = expectedSourceVersion;
        var currentDescriptor = LinkingSourceStorage.Decode(source, []);
        if (!LinkingSourceTypeFilter.Supports(currentDescriptor))
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.ConfigurationIncoherent,
                "typeCodes",
                null));
        }

        var descriptor = LinkingSourceTypeFilter.Apply(currentDescriptor, typeCodes);
        var form = LinkingSourceStorage.Encode(descriptor);
        if (string.Equals(source.SourceIdentity, form.SourceIdentity, StringComparison.Ordinal))
        {
            await transaction.CommitAsync(cancellationToken);
            return await LinkingWorkspaceProjection.ProjectAsync(db, workspace, cancellationToken);
        }

        await EnsureSourceIdentityAvailableAsync(workspace.Id, source.Id, form, cancellationToken);
        var compact = await sourceCache.GetOrLoadAsync(
            LinkingSourceCacheKeys.For(form.Kind, form.SourceIdentity, linkingDataRevision),
            form.SourceIdentity,
            token => efResolution.ResolveCompactAsync(descriptor, token),
            cancellationToken);
        var retainedDescriptions = await db.LinkingWorkspaceSourceDescriptions
            .AsNoTracking()
            .Where(row => row.WorkspaceSourceId == source.Id
                && compact.AyahIds.Contains(row.AyahId))
            .OrderBy(row => row.AyahId)
            .ThenBy(row => row.OrderValue)
            .Select(row => new LinkingWorkspaceDescriptionInput(row.AyahId, row.OrderValue, row.Body))
            .ToListAsync(cancellationToken);
        if (!LinkingSourceConfiguration.TryCreate(
            form.Kind,
            LinkingInclusionMode.AllExcept,
            [],
            [],
            source.AutomaticWordMatchesEnabled,
            source.ManualLinkShape,
            retainedDescriptions,
            out var configuration,
            out var violation))
        {
            throw new LinkingWorkspaceViolationException(violation);
        }

        var now = DateTimeOffset.UtcNow;

        source.SourceIdentity = form.SourceIdentity;
        source.SourceIdentityHash = form.SourceIdentityHash;
        source.ScopeJson = form.ScopeJson;
        source.InclusionMode = configuration.InclusionMode;
        source.AutomaticWordMatchesEnabled = configuration.AutomaticWordMatchesEnabled;
        source.ManualLinkShape = configuration.ManualLinkShape;
        source.LastResolvedCount = compact.AyahCount;
        source.LastResolvedAtUtc = now;
        source.UpdatedAtUtc = now;
        source.UpdatedBy = userId;
        StampWorkspace(workspace, userId, now);

        await ResetSourceSelectionsAsync(source.Id, compact.AyahIds, cancellationToken);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await LinkingWorkspaceProjection.ProjectAsync(db, workspace, cancellationToken);
    }

    private async Task EnsureSourceIdentityAvailableAsync(
        long workspaceId,
        long sourceId,
        LinkingSourceStorageForm form,
        CancellationToken cancellationToken)
    {
        var identityUnavailable = await db.LinkingWorkspaceSources.AnyAsync(
            source => source.WorkspaceId == workspaceId
                && source.Id != sourceId
                && source.SourceIdentityHash == form.SourceIdentityHash,
            cancellationToken);
        if (identityUnavailable)
        {
            throw new LinkingDuplicateContributionException();
        }
    }

    private async Task ResetSourceSelectionsAsync(
        long sourceId,
        IReadOnlyList<int> retainedAyahIds,
        CancellationToken cancellationToken)
    {
        await db.LinkingWorkspaceSourceAyahOverrides
            .Where(row => row.WorkspaceSourceId == sourceId)
            .ExecuteDeleteAsync(cancellationToken);
        await db.LinkingWorkspaceSourceWords
            .Where(row => row.WorkspaceSourceId == sourceId)
            .ExecuteDeleteAsync(cancellationToken);
        var descriptions = db.LinkingWorkspaceSourceDescriptions
            .Where(row => row.WorkspaceSourceId == sourceId);
        if (retainedAyahIds.Count > 0)
        {
            descriptions = descriptions.Where(row => !retainedAyahIds.Contains(row.AyahId));
        }
        await descriptions.ExecuteDeleteAsync(cancellationToken);
    }
}
