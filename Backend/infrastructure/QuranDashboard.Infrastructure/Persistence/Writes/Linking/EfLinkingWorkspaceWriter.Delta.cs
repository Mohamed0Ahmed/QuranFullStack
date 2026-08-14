using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Caching.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingWorkspaceWriter
{
    public async Task<LinkingWorkspaceDeltaAcknowledgement> ApplyDeltaAsync(
        int userId,
        long sourceId,
        LinkingWorkspaceDeltaInput delta,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delta);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var revision = await LockLinkingDataForReadAsync(transaction, cancellationToken);
        if (revision != delta.ExpectedLinkingDataRevision)
        {
            throw new LinkingDataStaleException(delta.ExpectedLinkingDataRevision, revision);
        }

        var workspace = await LoadWorkspaceAsync(userId, cancellationToken)
            ?? throw new LinkingWorkspaceSourceNotFoundException(sourceId);
        var source = await db.LinkingWorkspaceSources
            .FromSqlInterpolated(
                $"SELECT source.*, source.xmin FROM linking_workspace_sources source WHERE id = {sourceId} FOR UPDATE")
            .FirstOrDefaultAsync(
                candidate => candidate.Id == sourceId && candidate.WorkspaceId == workspace.Id,
                cancellationToken)
            ?? throw new LinkingWorkspaceSourceNotFoundException(sourceId);
        if (source.Version != delta.SourceVersion)
        {
            throw new LinkingStaleVersionException();
        }

        db.Entry(source).Property(entity => entity.Version).OriginalValue = delta.SourceVersion;

        var current = await LoadDeltaStateAsync(source, cancellationToken);
        var normalized = ApplyDelta(current, delta.Changes);
        var compact = await LoadCompactSourceAsync(source, revision, cancellationToken);
        ValidateDeltaState(source, current, compact);
        await EnsureSelectedWordsAsync([.. current.SelectedWords.Values], cancellationToken);

        var now = DateTimeOffset.UtcNow;
        source.Label = current.Label;
        source.InclusionMode = current.InclusionMode;
        source.AutomaticWordMatchesEnabled = current.AutomaticWordMatchesEnabled;
        source.ManualLinkShape = current.ManualLinkShape;
        source.UpdatedAtUtc = now;
        source.UpdatedBy = userId;
        StampWorkspace(workspace, userId, now);

        await ReplaceOverridesAsync(source.Id, [.. current.AyahOverrides], cancellationToken);
        await ReplaceSelectedWordsAsync(source.Id, [.. current.SelectedWords.Values], cancellationToken);
        await ReplaceDescriptionsAsync(
            source.Id,
            [.. current.Descriptions
                .OrderBy(entry => entry.Key)
                .Select(entry => new LinkingWorkspaceAyahDescriptions(entry.Key, entry.Value))],
            userId,
            now,
            cancellationToken);
        await SaveTranslatingWriteExceptionsAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new LinkingWorkspaceDeltaAcknowledgement(
            workspace.Version,
            source.Id,
            source.Version,
            revision,
            normalized);
    }

    private async Task<DeltaState> LoadDeltaStateAsync(
        LinkingWorkspaceSource source,
        CancellationToken cancellationToken)
    {
        var overrides = await db.LinkingWorkspaceSourceAyahOverrides
            .Where(row => row.WorkspaceSourceId == source.Id)
            .Select(row => row.AyahId)
            .ToListAsync(cancellationToken);
        var words = await db.LinkingWorkspaceSourceWords
            .Where(row => row.WorkspaceSourceId == source.Id)
            .Select(row => new LinkingWorkspaceSelectedWordInput(row.AyahId, row.QuranWordId))
            .ToListAsync(cancellationToken);
        var descriptions = await db.LinkingWorkspaceSourceDescriptions
            .Where(row => row.WorkspaceSourceId == source.Id)
            .OrderBy(row => row.AyahId)
            .ThenBy(row => row.OrderValue)
            .Select(row => new { row.AyahId, row.Body })
            .ToListAsync(cancellationToken);

        return new DeltaState(
            source.Label,
            source.InclusionMode,
            [.. overrides],
            words.ToDictionary(word => word.QuranWordId),
            source.AutomaticWordMatchesEnabled,
            source.ManualLinkShape,
            descriptions
                .GroupBy(row => row.AyahId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)[.. group.Select(row => row.Body)]));
    }

    private static IReadOnlyList<LinkingWorkspaceDeltaChange> ApplyDelta(
        DeltaState state,
        IReadOnlyList<LinkingWorkspaceDeltaChange> changes)
    {
        var labelChanged = false;
        var inclusionChanged = false;
        LinkingWorkspaceDeltaChange.SetAutomaticWordMatches? automatic = null;
        LinkingWorkspaceDeltaChange.SetManualLinkShape? shape = null;
        var wordChanges = new Dictionary<int, LinkingWorkspaceDeltaChange.SetWordSelected>();
        var descriptionChanges = new Dictionary<int, LinkingWorkspaceDeltaChange.ReplaceAyahDescriptions>();

        foreach (var change in changes)
        {
            switch (change)
            {
                case LinkingWorkspaceDeltaChange.SetLabel label:
                    state.Label = label.Label.Trim();
                    labelChanged = true;
                    break;
                case LinkingWorkspaceDeltaChange.SetAyahIncluded ayah:
                    SetIncluded(state, ayah.AyahId, ayah.Included);
                    inclusionChanged = true;
                    break;
                case LinkingWorkspaceDeltaChange.ReplaceInclusion inclusion:
                    state.InclusionMode = inclusion.Mode;
                    state.AyahOverrides = [.. inclusion.AyahOverrideIds.Distinct()];
                    inclusionChanged = true;
                    break;
                case LinkingWorkspaceDeltaChange.SetWordSelected word:
                    if (word.Selected)
                    {
                        state.SelectedWords[word.QuranWordId] =
                            new LinkingWorkspaceSelectedWordInput(word.AyahId, word.QuranWordId);
                    }
                    else
                    {
                        state.SelectedWords.Remove(word.QuranWordId);
                    }

                    wordChanges[word.QuranWordId] = word;
                    break;
                case LinkingWorkspaceDeltaChange.SetAutomaticWordMatches value:
                    state.AutomaticWordMatchesEnabled = value.Enabled;
                    automatic = value;
                    break;
                case LinkingWorkspaceDeltaChange.SetManualLinkShape value:
                    state.ManualLinkShape = value.Shape;
                    shape = value;
                    break;
                case LinkingWorkspaceDeltaChange.ReplaceAyahDescriptions value:
                    state.Descriptions[value.AyahId] =
                        [.. value.Descriptions.Select(body => body.Trim())];
                    descriptionChanges[value.AyahId] = value with
                    {
                        Descriptions = state.Descriptions[value.AyahId],
                    };
                    break;
                default:
                    throw new InvalidOperationException("Unknown linking workspace delta change.");
            }
        }

        var normalized = new List<LinkingWorkspaceDeltaChange>();
        if (labelChanged)
        {
            normalized.Add(new LinkingWorkspaceDeltaChange.SetLabel(state.Label));
        }

        if (inclusionChanged)
        {
            normalized.Add(new LinkingWorkspaceDeltaChange.ReplaceInclusion(
                state.InclusionMode,
                [.. state.AyahOverrides.Order()]));
        }

        normalized.AddRange(wordChanges.Values.OrderBy(change => change.QuranWordId));
        if (automatic is not null)
        {
            normalized.Add(automatic);
        }

        if (shape is not null)
        {
            normalized.Add(shape);
        }

        normalized.AddRange(descriptionChanges.Values.OrderBy(change => change.AyahId));
        return normalized;
    }

    private static void SetIncluded(DeltaState state, int ayahId, bool included)
    {
        var shouldOverride = state.InclusionMode == LinkingInclusionMode.AllExcept
            ? !included
            : included;
        if (shouldOverride)
        {
            state.AyahOverrides.Add(ayahId);
        }
        else
        {
            state.AyahOverrides.Remove(ayahId);
        }
    }

    private async Task<LinkingResolvedSourceCompact> LoadCompactSourceAsync(
        LinkingWorkspaceSource source,
        long revision,
        CancellationToken cancellationToken)
    {
        var manualVerseKeys = source.SourceKind == LinkingSourceKind.ManualMushafAyahs
            ? await (
                from manual in db.LinkingWorkspaceSourceManualAyahs.AsNoTracking()
                join ayah in db.QuranAyahs.AsNoTracking() on manual.AyahId equals ayah.Id
                where manual.WorkspaceSourceId == source.Id
                orderby manual.OrderValue
                select ayah.VerseKey)
                .ToListAsync(cancellationToken)
            : [];
        var descriptor = LinkingSourceStorage.Decode(source, manualVerseKeys);
        var key = LinkingSourceCacheKeys.For(source.SourceKind, source.SourceIdentity, revision);
        return await sourceCache.GetOrLoadAsync(
            key,
            source.SourceIdentity,
            token => efResolution.ResolveCompactAsync(descriptor, token),
            cancellationToken);
    }

    private static void ValidateDeltaState(
        LinkingWorkspaceSource source,
        DeltaState state,
        LinkingResolvedSourceCompact compact)
    {
        EnsureLabel(state.Label);
        var configuration = new LinkingWorkspaceConfigurationInput(
            state.Label,
            state.InclusionMode,
            [.. state.AyahOverrides],
            [.. state.SelectedWords.Values],
            state.AutomaticWordMatchesEnabled,
            state.ManualLinkShape,
            [.. state.Descriptions.SelectMany(entry =>
                entry.Value.Select((body, index) =>
                    new LinkingWorkspaceDescriptionInput(entry.Key, index + 1, body)))]);
        EnsureConfigurationCoherence(
            source.SourceKind == LinkingSourceKind.ManualMushafAyahs,
            configuration);
        var memberIds = compact.AyahIds.ToHashSet();
        var invalidAyah = state.AyahOverrides
            .Concat(state.SelectedWords.Values.Select(word => word.AyahId))
            .Concat(state.Descriptions.Keys)
            .FirstOrDefault(ayahId => !memberIds.Contains(ayahId));
        if (invalidAyah != 0)
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.AyahReferenceUnknown,
                "changes.ayahId",
                invalidAyah.ToString(CultureInfo.InvariantCulture)));
        }

        if (source.SourceKind != LinkingSourceKind.ManualMushafAyahs && state.SelectedWords.Count > 0)
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.WordsNotAllowedOnAutomaticSource,
                "changes.quranWordId",
                null));
        }

        var wordsByAyah = compact.Ayahs.ToDictionary(
            ayah => ayah.AyahId,
            ayah => ayah.QuranWordIds.ToHashSet());
        var invalidWord = state.SelectedWords.Values.FirstOrDefault(word =>
            !wordsByAyah.GetValueOrDefault(word.AyahId, []).Contains(word.QuranWordId));
        if (invalidWord is not null)
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.SelectedWordInvalid,
                "changes.quranWordId",
                invalidWord.QuranWordId.ToString(CultureInfo.InvariantCulture)));
        }

        _ = NormalizeDescriptions(configuration.Descriptions);
    }

    private sealed class DeltaState(
        string label,
        LinkingInclusionMode inclusionMode,
        HashSet<int> ayahOverrides,
        Dictionary<int, LinkingWorkspaceSelectedWordInput> selectedWords,
        bool? automaticWordMatchesEnabled,
        LinkingManualLinkShape? manualLinkShape,
        Dictionary<int, IReadOnlyList<string>> descriptions)
    {
        public string Label { get; set; } = label;
        public LinkingInclusionMode InclusionMode { get; set; } = inclusionMode;
        public HashSet<int> AyahOverrides { get; set; } = ayahOverrides;
        public Dictionary<int, LinkingWorkspaceSelectedWordInput> SelectedWords { get; } = selectedWords;
        public bool? AutomaticWordMatchesEnabled { get; set; } = automaticWordMatchesEnabled;
        public LinkingManualLinkShape? ManualLinkShape { get; set; } = manualLinkShape;
        public Dictionary<int, IReadOnlyList<string>> Descriptions { get; } = descriptions;
    }
}
