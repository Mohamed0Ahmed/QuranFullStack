using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking.PreparedPreflights;

public sealed class LinkingPreparedPreflightInputBuilder(
    ILinkingSourcePreparationReader sourceReader,
    LinkingPreparedPreflightLeaseService leaseService,
    ILinkingScalabilityPolicy policy)
{
    internal async Task<LinkingPreparedInput> BuildAsync(
        LinkingPreparedPreflightLease lease,
        LinkingPreparedPreflightWork work,
        CancellationToken cancellationToken)
    {
        var sources = new List<LinkingOperationSourceRequest>(work.Sources.Count);
        var intentSources = new List<LinkingOperationSourceIntent>(work.Sources.Count);
        var processedSources = 0;
        var processedAyahs = 0;

        foreach (var source in work.Sources.OrderBy(source => source.OrderValue))
        {
            var resolvedAyahs = new List<LinkingResolvedAyahDto>();
            var totalAyahs = 0;
            await foreach (var batch in sourceReader.ReadBatchesAsync(
                source.Source.Descriptor,
                work.LinkingDataRevision,
                policy.PersistenceBatchSize,
                cancellationToken))
            {
                totalAyahs = batch.TotalAyahCount;
                resolvedAyahs.AddRange(batch.Ayahs);
                processedAyahs += batch.Ayahs.Count;
                await RequireProgressAsync(
                    lease,
                    LinkingPreparedPreflightStage.Resolving,
                    processedSources,
                    processedAyahs,
                    null,
                    cancellationToken);
            }

            var preparedSource = BuildSource(source, resolvedAyahs, totalAyahs);
            sources.Add(preparedSource.Request);
            intentSources.Add(preparedSource.Intent);
            processedSources++;
            await RequireProgressAsync(
                lease,
                LinkingPreparedPreflightStage.Resolving,
                processedSources,
                processedAyahs,
                null,
                cancellationToken);
        }

        await RequireProgressAsync(
            lease,
            LinkingPreparedPreflightStage.Classifying,
            processedSources,
            processedAyahs,
            processedAyahs,
            cancellationToken);

        return new LinkingPreparedInput(
            new LinkingOperationRequest(
                work.DoorId,
                work.LinkingDataRevision,
                null,
                null,
                sources),
            new LinkingOperationIntent(work.DoorId, false, intentSources));
    }

    private async Task RequireProgressAsync(
        LinkingPreparedPreflightLease lease,
        LinkingPreparedPreflightStage stage,
        int processedSources,
        int processedAyahs,
        int? totalAyahs,
        CancellationToken cancellationToken)
    {
        if (!await leaseService.PublishProgressAsync(
            lease,
            stage,
            processedSources,
            processedAyahs,
            totalAyahs,
            cancellationToken))
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private static LinkingPreparedSourceInput BuildSource(
        LinkingPreparedSourceWork source,
        IReadOnlyList<LinkingResolvedAyahDto> resolvedAyahs,
        int totalAyahs)
    {
        var inline = source.Source;
        var configuration = inline.Configuration;
        ValidateConfiguration(inline.Descriptor, configuration, resolvedAyahs);
        var overrides = configuration.AyahOverrides.ToHashSet();
        var selectedWords = configuration.SelectedWords
            .GroupBy(word => word.AyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)[.. group.Select(word => word.QuranWordId).Distinct().Order()]);
        var descriptions = configuration.Descriptions
            .GroupBy(description => description.AyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)[.. group
                    .OrderBy(description => description.OrderValue)
                    .Select(description => description.Body.Trim())]);
        var included = resolvedAyahs
            .Where(ayah => configuration.InclusionMode == LinkingInclusionMode.AllExcept
                ? !overrides.Contains(ayah.AyahId)
                : overrides.Contains(ayah.AyahId))
            .ToList();
        var grouped = configuration.ManualLinkShape == LinkingManualLinkShape.Grouped;
        var contributionMode = inline.Descriptor.Kind != LinkingSourceKind.ManualMushafAyahs
            ? LinkingContributionMode.Automatic
            : grouped
                ? LinkingContributionMode.ManualGrouped
                : LinkingContributionMode.ManualIndependent;
        var ayahRequests = included.Select(ayah => new LinkingOperationAyahRequest(
            ayah.AyahId,
            contributionMode == LinkingContributionMode.Automatic
                ? configuration.AutomaticWordMatchesEnabled == true
                    ? [.. ayah.MatchedQuranWordIds.Order()]
                    : []
                : selectedWords.GetValueOrDefault(ayah.AyahId, []),
            descriptions.GetValueOrDefault(ayah.AyahId, []))).ToList();
        var requestUnits = grouped
            ? [new LinkingOperationUnitRequest(ayahRequests)]
            : ayahRequests.Select(ayah => new LinkingOperationUnitRequest([ayah])).ToList();
        var ayahsById = resolvedAyahs.ToDictionary(ayah => ayah.AyahId);
        var intentUnits = requestUnits.Select(unit =>
        {
            var intentAyahs = unit.Ayahs.Select(ayah =>
            {
                var resolved = ayahsById[ayah.AyahId];
                return new LinkingOperationAyahIntent(
                    ayah.AyahId,
                    resolved.VerseKey,
                    resolved.SurahNumber,
                    resolved.AyahNumber,
                    ayah.SelectedWordIds,
                    ayah.Descriptions,
                    null,
                    resolved.MatchedQuranWordIds);
            }).ToList();
            return new LinkingOperationUnitIntent(
                LinkingUnitIdentity.For(grouped, intentAyahs),
                grouped,
                intentAyahs);
        }).ToList();
        var request = new LinkingOperationSourceRequest(
            inline.Descriptor,
            contributionMode,
            contributionMode == LinkingContributionMode.Automatic
                ? configuration.AutomaticWordMatchesEnabled
                : null,
            source.OrderValue,
            null,
            null,
            requestUnits);
        return new LinkingPreparedSourceInput(
            request,
            new LinkingOperationSourceIntent(
                LinkingContributionIdentity.For(inline.Descriptor, contributionMode),
                inline.Descriptor.Kind,
                inline.Descriptor.Label,
                contributionMode,
                request.AutomaticWordMatchesEnabled,
                source.OrderValue,
                totalAyahs,
                DateTimeOffset.UtcNow,
                intentUnits,
                null));
    }

    private static void ValidateConfiguration(
        LinkingSourceDescriptor descriptor,
        LinkingWorkspaceConfigurationInput configuration,
        IReadOnlyList<LinkingResolvedAyahDto> resolvedAyahs)
    {
        var isManual = descriptor.Kind == LinkingSourceKind.ManualMushafAyahs;
        if ((isManual
                && (configuration.ManualLinkShape is null
                    || configuration.AutomaticWordMatchesEnabled is not null))
            || (!isManual
                && (configuration.ManualLinkShape is not null
                    || configuration.AutomaticWordMatchesEnabled is null
                    || configuration.SelectedWords.Count != 0)))
        {
            throw new InvalidDataException("The prepared source configuration is incoherent.");
        }

        if (LinkingWorkspaceDescriptionValidation.TryNormalize(
                configuration.Descriptions,
                out _) is not null)
        {
            throw new InvalidDataException("The prepared source descriptions are invalid.");
        }

        var ayahsById = resolvedAyahs.ToDictionary(ayah => ayah.AyahId);
        var referencedAyahIds = configuration.AyahOverrides
            .Concat(configuration.SelectedWords.Select(word => word.AyahId))
            .Concat(configuration.Descriptions.Select(description => description.AyahId));
        if (referencedAyahIds.Any(ayahId => !ayahsById.ContainsKey(ayahId)))
        {
            throw new InvalidDataException("The prepared source references an ayah outside its membership.");
        }

        var wordOwners = new Dictionary<int, int>();
        foreach (var selected in configuration.SelectedWords)
        {
            if (!ayahsById[selected.AyahId].Words.Any(word => word.QuranWordId == selected.QuranWordId)
                || (wordOwners.TryGetValue(selected.QuranWordId, out var ownerAyahId)
                    && ownerAyahId != selected.AyahId))
            {
                throw new InvalidDataException("The prepared source contains an invalid Quran word reference.");
            }

            wordOwners[selected.QuranWordId] = selected.AyahId;
        }
    }
}

internal sealed record LinkingPreparedInput(
    LinkingOperationRequest Request,
    LinkingOperationIntent Intent);

internal sealed record LinkingPreparedSourceInput(
    LinkingOperationSourceRequest Request,
    LinkingOperationSourceIntent Intent);
