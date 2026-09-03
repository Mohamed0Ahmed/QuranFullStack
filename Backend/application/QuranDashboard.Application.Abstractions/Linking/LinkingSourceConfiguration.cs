using System.Globalization;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public sealed class LinkingSourceConfiguration
{
    private LinkingSourceConfiguration(
        LinkingSourceKind sourceKind,
        LinkingInclusionMode inclusionMode,
        IReadOnlyList<int> ayahOverrides,
        IReadOnlyList<LinkingWorkspaceSelectedWordInput> selectedWords,
        bool? automaticWordMatchesEnabled,
        LinkingManualLinkShape? manualLinkShape,
        IReadOnlyList<LinkingWorkspaceDescriptionInput> descriptions,
        IReadOnlyList<LinkingWorkspaceAyahDescriptions> normalizedDescriptions,
        LinkingContributionMode contributionMode)
    {
        SourceKind = sourceKind;
        InclusionMode = inclusionMode;
        AyahOverrides = ayahOverrides;
        SelectedWords = selectedWords;
        AutomaticWordMatchesEnabled = automaticWordMatchesEnabled;
        ManualLinkShape = manualLinkShape;
        Descriptions = descriptions;
        NormalizedDescriptions = normalizedDescriptions;
        ContributionMode = contributionMode;
    }

    public LinkingSourceKind SourceKind { get; }
    public LinkingInclusionMode InclusionMode { get; }
    public IReadOnlyList<int> AyahOverrides { get; }
    public IReadOnlyList<LinkingWorkspaceSelectedWordInput> SelectedWords { get; }
    public bool? AutomaticWordMatchesEnabled { get; }
    public LinkingManualLinkShape? ManualLinkShape { get; }
    public IReadOnlyList<LinkingWorkspaceDescriptionInput> Descriptions { get; }
    public IReadOnlyList<LinkingWorkspaceAyahDescriptions> NormalizedDescriptions { get; }
    public LinkingContributionMode ContributionMode { get; }

    public static bool TryCreate(
        LinkingSourceKind sourceKind,
        LinkingInclusionMode inclusionMode,
        IReadOnlyList<int>? ayahOverrides,
        IReadOnlyList<LinkingWorkspaceSelectedWordInput>? selectedWords,
        bool? automaticWordMatchesEnabled,
        LinkingManualLinkShape? manualLinkShape,
        IReadOnlyList<LinkingWorkspaceDescriptionInput>? descriptions,
        out LinkingSourceConfiguration configuration,
        out LinkingWorkspaceViolation violation)
    {
        configuration = null!;
        violation = null!;
        ayahOverrides ??= [];
        selectedWords ??= [];
        descriptions ??= [];

        if (!Enum.IsDefined(sourceKind)
            || !Enum.IsDefined(inclusionMode)
            || manualLinkShape is { } shape && !Enum.IsDefined(shape)
            || selectedWords.Any(word => word is null)
            || descriptions.Any(description => description is null))
        {
            violation = Incoherent("configuration");
            return false;
        }

        var isManual = sourceKind == LinkingSourceKind.ManualMushafAyahs;
        if (isManual
                ? manualLinkShape is null || automaticWordMatchesEnabled is not null
                : manualLinkShape is not null || automaticWordMatchesEnabled is null)
        {
            violation = Incoherent(isManual ? "manualLinkShape" : "automaticWordMatchesEnabled");
            return false;
        }

        if (!isManual && selectedWords.Count != 0)
        {
            violation = new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.WordsNotAllowedOnAutomaticSource,
                "selectedWords",
                null);
            return false;
        }

        var selectedByWordId = new Dictionary<int, LinkingWorkspaceSelectedWordInput>();
        foreach (var selectedWord in selectedWords)
        {
            if (selectedByWordId.TryGetValue(selectedWord.QuranWordId, out var existing)
                && existing.AyahId != selectedWord.AyahId)
            {
                violation = new LinkingWorkspaceViolation(
                    LinkingWorkspaceViolationCode.SelectedWordAyahConflict,
                    "selectedWords.quranWordId",
                    selectedWord.QuranWordId.ToString(CultureInfo.InvariantCulture));
                return false;
            }

            selectedByWordId[selectedWord.QuranWordId] = selectedWord;
        }

        var descriptionViolation = TryNormalizeDescriptions(descriptions, out var normalizedDescriptions);
        if (descriptionViolation is not null)
        {
            violation = descriptionViolation;
            return false;
        }

        var orderedDescriptions = descriptions
            .OrderBy(description => description.AyahId)
            .ThenBy(description => description.OrderValue)
            .ToArray();
        var contributionMode = !isManual
            ? LinkingContributionMode.Automatic
            : manualLinkShape == LinkingManualLinkShape.Grouped
                ? LinkingContributionMode.ManualGrouped
                : LinkingContributionMode.ManualIndependent;
        configuration = new LinkingSourceConfiguration(
            sourceKind,
            inclusionMode,
            Array.AsReadOnly(ayahOverrides.Distinct().Order().ToArray()),
            Array.AsReadOnly(selectedByWordId.Values
                .OrderBy(word => word.AyahId)
                .ThenBy(word => word.QuranWordId)
                .ToArray()),
            automaticWordMatchesEnabled,
            manualLinkShape,
            Array.AsReadOnly(orderedDescriptions),
            Array.AsReadOnly(normalizedDescriptions.ToArray()),
            contributionMode);
        return true;
    }

    private static LinkingWorkspaceViolation Incoherent(string field) =>
        new(LinkingWorkspaceViolationCode.ConfigurationIncoherent, field, null);

    private static LinkingWorkspaceViolation? TryNormalizeDescriptions(
        IReadOnlyList<LinkingWorkspaceDescriptionInput> descriptions,
        out IReadOnlyList<LinkingWorkspaceAyahDescriptions> normalized)
    {
        var byAyah = new List<LinkingWorkspaceAyahDescriptions>();
        foreach (var group in descriptions.GroupBy(description => description.AyahId).OrderBy(group => group.Key))
        {
            var submitted = group.ToList();
            if (submitted.Count > LinkingLimits.MaxDescriptionsPerSourceAyah)
            {
                normalized = [];
                return DescriptionViolation(LinkingWorkspaceViolationCode.DescriptionLimitExceeded, group.Key);
            }

            if (submitted.Any(description => description.OrderValue < 1)
                || submitted.Select(description => description.OrderValue).Distinct().Count() != submitted.Count)
            {
                normalized = [];
                return DescriptionViolation(LinkingWorkspaceViolationCode.DescriptionOrderConflict, group.Key);
            }

            var bodies = new List<string>(submitted.Count);
            foreach (var description in submitted.OrderBy(description => description.OrderValue))
            {
                var body = description.Body?.Trim() ?? string.Empty;
                if (body.Length is 0 or > LinkingLimits.MaxDescriptionLength)
                {
                    normalized = [];
                    return DescriptionViolation(LinkingWorkspaceViolationCode.DescriptionBodyInvalid, group.Key);
                }

                bodies.Add(body);
            }

            byAyah.Add(new LinkingWorkspaceAyahDescriptions(
                group.Key,
                Array.AsReadOnly(bodies.ToArray())));
        }

        normalized = byAyah;
        return null;
    }

    private static LinkingWorkspaceViolation DescriptionViolation(
        LinkingWorkspaceViolationCode code,
        int ayahId) =>
        new(code, "descriptions", ayahId.ToString(CultureInfo.InvariantCulture));
}

public sealed record LinkingWorkspaceSelectedWordInput(int AyahId, int QuranWordId);

public sealed record LinkingWorkspaceDescriptionInput(int AyahId, int OrderValue, string Body);

public sealed record LinkingWorkspaceAyahDescriptions(int AyahId, IReadOnlyList<string> Bodies);
