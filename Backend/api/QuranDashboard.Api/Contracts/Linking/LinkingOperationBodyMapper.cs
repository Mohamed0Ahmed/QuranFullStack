using System.Globalization;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Api.Contracts.Linking;

internal static class LinkingOperationBodyMapper
{
    internal static bool TryMap(
        LinkingPreflightBody? body,
        out LinkingOperationRequest request,
        out LinkingOperationViolation violation)
    {
        request = null!;
        violation = null!;

        if (body is null)
        {
            violation = Malformed("body");
            return false;
        }

        if (body.DoorId is null or <= 0)
        {
            violation = Malformed("doorId", Text(body.DoorId));
            return false;
        }

        var sources = new List<LinkingOperationSourceRequest>(body.Sources?.Count ?? 0);

        foreach (var source in body.Sources ?? [])
        {
            if (!TryMapSource(source, out var mapped, out violation))
            {
                return false;
            }

            sources.Add(mapped);
        }

        request = new LinkingOperationRequest(body.DoorId.Value, null, null, sources);

        return true;
    }

    internal static bool TryMap(
        LinkingConfirmationBody? body,
        out LinkingOperationRequest request,
        out LinkingOperationViolation violation)
    {
        request = null!;
        violation = null!;

        if (body is null)
        {
            violation = Malformed("body");
            return false;
        }

        if (body.DoorId is null or <= 0)
        {
            violation = Malformed("doorId", Text(body.DoorId));
            return false;
        }

        if (string.IsNullOrWhiteSpace(body.PreflightToken))
        {
            violation = new LinkingOperationViolation(
                LinkingOperationViolationCode.PreflightTokenRequired, "preflightToken", null);
            return false;
        }

        if (body.IdempotencyKey is null || body.IdempotencyKey == Guid.Empty)
        {
            violation = new LinkingOperationViolation(
                LinkingOperationViolationCode.IdempotencyKeyRequired, "idempotencyKey", null);
            return false;
        }

        var sources = new List<LinkingOperationSourceRequest>(body.Sources?.Count ?? 0);

        foreach (var source in body.Sources ?? [])
        {
            if (!TryMapSource(source, out var mapped, out violation))
            {
                return false;
            }

            sources.Add(mapped);
        }

        request = new LinkingOperationRequest(
            body.DoorId.Value,
            body.PreflightToken,
            body.IdempotencyKey,
            sources);

        return true;
    }

    private static bool TryMapSource(
        LinkingPreflightSourceBody? source,
        out LinkingOperationSourceRequest mapped,
        out LinkingOperationViolation violation)
    {
        mapped = null!;
        violation = null!;

        if (source is null)
        {
            violation = Malformed("sources");
            return false;
        }

        if (!LinkingSourceDescriptorBodyMapper.TryMap(
            source.Descriptor, out var descriptor, out var descriptorViolation))
        {
            violation = Malformed(
                $"sources.descriptor.{descriptorViolation.Field}", descriptorViolation.Value);
            return false;
        }

        if (!LinkingOperationTokens.TryParseContributionMode(source.ContributionMode, out var contributionMode))
        {
            violation = Malformed("sources.contributionMode", source.ContributionMode);
            return false;
        }

        if (source.OrderValue is null or <= 0)
        {
            violation = Malformed("sources.orderValue", Text(source.OrderValue));
            return false;
        }

        var units = new List<LinkingOperationUnitRequest>(source.Units?.Count ?? 0);

        foreach (var unit in source.Units ?? [])
        {
            if (!TryMapUnit(unit, out var mappedUnit, out violation))
            {
                return false;
            }

            units.Add(mappedUnit);
        }

        mapped = new LinkingOperationSourceRequest(
            descriptor,
            contributionMode,
            source.AutomaticWordMatchesEnabled,
            source.OrderValue.Value,
            null,
            null,
            units);

        return true;
    }

    private static bool TryMapSource(
        LinkingConfirmationSourceBody? source,
        out LinkingOperationSourceRequest mapped,
        out LinkingOperationViolation violation)
    {
        mapped = null!;
        violation = null!;

        if (source is null)
        {
            violation = Malformed("sources");
            return false;
        }

        if (!LinkingSourceDescriptorBodyMapper.TryMap(
            source.Descriptor, out var descriptor, out var descriptorViolation))
        {
            violation = Malformed(
                $"sources.descriptor.{descriptorViolation.Field}", descriptorViolation.Value);
            return false;
        }

        if (!LinkingOperationTokens.TryParseContributionMode(source.ContributionMode, out var contributionMode))
        {
            violation = Malformed("sources.contributionMode", source.ContributionMode);
            return false;
        }

        if (source.OrderValue is null or <= 0)
        {
            violation = Malformed("sources.orderValue", Text(source.OrderValue));
            return false;
        }

        if (source.ExistingContributionId is <= 0)
        {
            violation = new LinkingOperationViolation(
                LinkingOperationViolationCode.ExistingContributionIdInvalid,
                "sources.existingContributionId",
                source.ExistingContributionId.Value.ToString(CultureInfo.InvariantCulture));
            return false;
        }

        if (source.ExistingContributionVersion is 0)
        {
            violation = new LinkingOperationViolation(
                LinkingOperationViolationCode.ExistingContributionVersionInvalid,
                "sources.existingContributionVersion",
                source.ExistingContributionVersion.Value.ToString(CultureInfo.InvariantCulture));
            return false;
        }

        if (source.ExistingContributionId.HasValue != source.ExistingContributionVersion.HasValue)
        {
            violation = new LinkingOperationViolation(
                LinkingOperationViolationCode.ExistingContributionPairInvalid,
                "sources.existingContributionVersion",
                null);
            return false;
        }

        var units = new List<LinkingOperationUnitRequest>(source.Units?.Count ?? 0);

        foreach (var unit in source.Units ?? [])
        {
            if (!TryMapUnit(unit, out var mappedUnit, out violation))
            {
                return false;
            }

            units.Add(mappedUnit);
        }

        mapped = new LinkingOperationSourceRequest(
            descriptor,
            contributionMode,
            source.AutomaticWordMatchesEnabled,
            source.OrderValue.Value,
            source.ExistingContributionId,
            source.ExistingContributionVersion,
            units);

        return true;
    }

    private static bool TryMapUnit(
        LinkingOperationUnitBody? unit,
        out LinkingOperationUnitRequest mapped,
        out LinkingOperationViolation violation)
    {
        mapped = null!;
        violation = null!;

        if (unit is null)
        {
            violation = Malformed("sources.units");
            return false;
        }

        var ayahs = new List<LinkingOperationAyahRequest>(unit.Ayahs?.Count ?? 0);

        foreach (var ayah in unit.Ayahs ?? [])
        {
            if (ayah?.AyahId is null or <= 0)
            {
                violation = Malformed("sources.units.ayahs.ayahId", Text(ayah?.AyahId));
                return false;
            }

            if (ayah.SelectedWordIds?.Any(wordId => wordId <= 0) == true)
            {
                violation = Malformed("sources.units.ayahs.selectedWordIds", Text(ayah.AyahId));
                return false;
            }

            ayahs.Add(new LinkingOperationAyahRequest(
                ayah.AyahId.Value,
                [.. ayah.SelectedWordIds ?? []],
                [.. ayah.Descriptions ?? []]));
        }

        mapped = new LinkingOperationUnitRequest(ayahs);

        return true;
    }

    private static LinkingOperationViolation Malformed(string field, string? value = null) =>
        new(LinkingOperationViolationCode.MalformedBody, field, value);

    private static string? Text(int? value) => value?.ToString(CultureInfo.InvariantCulture);
}
