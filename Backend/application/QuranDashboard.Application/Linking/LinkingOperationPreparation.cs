using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking;

public sealed class LinkingOperationPreparation(ILinkingSourceResolutionReader resolution)
{
    private const string DescriptorField = "descriptor";

    public async Task<LinkingOperationIntent> PrepareAsync(
        LinkingOperationRequest request,
        LinkingConfirmedDoorState state,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(state);

        return await PrepareCoreAsync(request, state, cancellationToken);
    }

    public Task<LinkingOperationIntent> PrepareConfirmationAsync(
        LinkingOperationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return PrepareCoreAsync(request, null, cancellationToken);
    }

    private async Task<LinkingOperationIntent> PrepareCoreAsync(
        LinkingOperationRequest request,
        LinkingConfirmedDoorState? state,
        CancellationToken cancellationToken)
    {
        var sources = new List<LinkingOperationSourceIntent>(request.Sources.Count);

        foreach (var source in request.Sources)
        {
            sources.Add(await PrepareSourceAsync(source, state, cancellationToken));
        }

        return new LinkingOperationIntent(request.DoorId, state?.IsArchived ?? false, sources);
    }

    private async Task<LinkingOperationSourceIntent> PrepareSourceAsync(
        LinkingOperationSourceRequest source,
        LinkingConfirmedDoorState? state,
        CancellationToken cancellationToken)
    {
        if (!LinkingSourceDescriptorValidation.TryValidate(source.Descriptor, out _))
        {
            throw new LinkingInvalidDescriptorException(new LinkingDescriptorViolation(
                LinkingDescriptorViolationCode.MalformedDescriptor, DescriptorField, null));
        }

        var identity = LinkingSourceIdentity.For(source.Descriptor);
        var resolved = await resolution.ResolveAsync(source.Descriptor, cancellationToken);
        var members = resolved.Ayahs.ToDictionary(ayah => ayah.AyahId);
        var confirmed = state is null ? [] : ConfirmedAyahMetadataOf(state);

        var units = source.Units
            .Select(unit => new LinkingOperationUnitIntent(
                [
                    .. unit.Ayahs.Select(ayah =>
                        PrepareAyah(source, ayah, members.GetValueOrDefault(ayah.AyahId), confirmed))
                ]))
            .ToList();

        return new LinkingOperationSourceIntent(
            identity,
            source.Descriptor.Kind,
            source.Descriptor.Label,
            source.ContributionMode,
            source.AutomaticWordMatchesEnabled,
            source.OrderValue,
            resolved.TotalAyahCount,
            resolved.ResolvedAtUtc,
            units,
            state?.IsArchived == true ? LinkingPreflightInvalidReason.DoorArchived : null);
    }

    private static LinkingOperationAyahIntent PrepareAyah(
        LinkingOperationSourceRequest source,
        LinkingOperationAyahRequest ayah,
        LinkingResolvedAyahDto? member,
        IReadOnlyDictionary<int, AyahMetadata> confirmed)
    {
        var descriptions = ayah.Descriptions.Select(body => body.Trim()).ToList();

        if (member is null)
        {
            var fallback = confirmed.GetValueOrDefault(ayah.AyahId);

            return new LinkingOperationAyahIntent(
                ayah.AyahId,
                fallback?.VerseKey ?? string.Empty,
                fallback?.SurahNumber ?? 0,
                fallback?.AyahNumber ?? 0,
                [.. ayah.SelectedWordIds.Distinct().Order()],
                descriptions,
                LinkingPreflightInvalidReason.AyahOutsideSource);
        }

        var (wordIds, invalidReason) = EffectiveWordsOf(source, ayah, member);

        return new LinkingOperationAyahIntent(
            ayah.AyahId,
            member.VerseKey,
            member.SurahNumber,
            member.AyahNumber,
            wordIds,
            descriptions,
            invalidReason);
    }

    private static (IReadOnlyList<int> WordIds, LinkingPreflightInvalidReason? InvalidReason) EffectiveWordsOf(
        LinkingOperationSourceRequest source,
        LinkingOperationAyahRequest ayah,
        LinkingResolvedAyahDto member)
    {
        if (source.ContributionMode == LinkingContributionMode.Automatic)
        {
            return (
                source.AutomaticWordMatchesEnabled == true ? [.. member.MatchedQuranWordIds.Order()] : [],
                null);
        }

        var authored = ayah.SelectedWordIds.Distinct().Order().ToList();
        var wordsById = member.Words.ToDictionary(word => word.QuranWordId);

        foreach (var wordId in authored)
        {
            if (!wordsById.TryGetValue(wordId, out var word))
            {
                return (authored, LinkingPreflightInvalidReason.WordOutsideAyah);
            }

            if (word.IsAyahMarker)
            {
                return (authored, LinkingPreflightInvalidReason.WordIsAyahMarker);
            }
        }

        return (authored, null);
    }

    private static Dictionary<int, AyahMetadata> ConfirmedAyahMetadataOf(
        LinkingConfirmedDoorState state) =>
        state.Ayahs
            .ToDictionary(
                ayah => ayah.AyahId,
                ayah => new AyahMetadata(ayah.VerseKey, ayah.SurahNumber, ayah.AyahNumber));

    private sealed record AyahMetadata(string VerseKey, int SurahNumber, int AyahNumber);
}
