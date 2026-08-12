using System.Globalization;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking;

public static class LinkingOperationValidation
{
    public static LinkingOperationViolation? Validate(LinkingOperationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Sources.Count == 0)
        {
            return new LinkingOperationViolation(LinkingOperationViolationCode.SourcesRequired, "sources", null);
        }

        return request.Sources
            .Select(ValidateSource)
            .FirstOrDefault(violation => violation is not null);
    }

    private static LinkingOperationViolation? ValidateSource(LinkingOperationSourceRequest source)
    {
        var ayahs = source.Units.SelectMany(unit => unit.Ayahs).ToList();

        if (ayahs.Count == 0)
        {
            return new LinkingOperationViolation(LinkingOperationViolationCode.AyahRequired, "units", null);
        }

        var duplicate = ayahs
            .GroupBy(ayah => ayah.AyahId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            return new LinkingOperationViolation(
                LinkingOperationViolationCode.DuplicateAyah, "units.ayahs.ayahId", Text(duplicate.Key));
        }

        return ValidateMode(source)
            ?? ValidateGrouping(source)
            ?? ValidateWords(source, ayahs)
            ?? ValidateDescriptions(ayahs);
    }

    private static LinkingOperationViolation? ValidateMode(LinkingOperationSourceRequest source)
    {
        var isManualDescriptor = source.Descriptor.Kind == LinkingSourceKind.ManualMushafAyahs;

        if (isManualDescriptor != LinkingOperationTokens.IsManual(source.ContributionMode))
        {
            return new LinkingOperationViolation(
                LinkingOperationViolationCode.ContributionModeInvalid,
                "contributionMode",
                LinkingOperationTokens.ToToken(source.ContributionMode));
        }

        if (source.ContributionMode == LinkingContributionMode.Automatic
            && source.AutomaticWordMatchesEnabled is null)
        {
            return new LinkingOperationViolation(
                LinkingOperationViolationCode.AutomaticWordMatchesRequired,
                "automaticWordMatchesEnabled",
                null);
        }

        if (source.ContributionMode != LinkingContributionMode.Automatic
            && source.AutomaticWordMatchesEnabled is not null)
        {
            return new LinkingOperationViolation(
                LinkingOperationViolationCode.AutomaticWordMatchesNotAllowed,
                "automaticWordMatchesEnabled",
                null);
        }

        return null;
    }

    private static LinkingOperationViolation? ValidateGrouping(LinkingOperationSourceRequest source)
    {
        var grouped = source.ContributionMode == LinkingContributionMode.ManualGrouped;

        var coherent = grouped
            ? source.Units.Count == 1
            : source.Units.All(unit => unit.Ayahs.Count == 1);

        return coherent
            ? null
            : new LinkingOperationViolation(
                LinkingOperationViolationCode.GroupingInvalid,
                "units",
                LinkingOperationTokens.ToToken(source.ContributionMode));
    }

    private static LinkingOperationViolation? ValidateWords(
        LinkingOperationSourceRequest source,
        IReadOnlyList<LinkingOperationAyahRequest> ayahs)
    {
        if (source.ContributionMode != LinkingContributionMode.Automatic)
        {
            return null;
        }

        var authored = ayahs.FirstOrDefault(ayah => ayah.SelectedWordIds.Count > 0);

        return authored is null
            ? null
            : new LinkingOperationViolation(
                LinkingOperationViolationCode.WordsNotAllowedOnAutomaticSource,
                "units.ayahs.selectedWordIds",
                Text(authored.AyahId));
    }

    private static LinkingOperationViolation? ValidateDescriptions(IReadOnlyList<LinkingOperationAyahRequest> ayahs)
    {
        foreach (var ayah in ayahs)
        {
            if (ayah.Descriptions.Count > LinkingLimits.MaxDescriptionsPerSourceAyah)
            {
                return new LinkingOperationViolation(
                    LinkingOperationViolationCode.DescriptionLimitExceeded,
                    "units.ayahs.descriptions",
                    Text(ayah.AyahId));
            }

            var invalid = ayah.Descriptions.Any(body =>
                string.IsNullOrWhiteSpace(body) || body.Trim().Length > LinkingLimits.MaxDescriptionLength);

            if (invalid)
            {
                return new LinkingOperationViolation(
                    LinkingOperationViolationCode.DescriptionBodyInvalid,
                    "units.ayahs.descriptions",
                    Text(ayah.AyahId));
            }
        }

        return null;
    }

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
}
