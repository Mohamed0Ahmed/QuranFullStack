using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Api.Contracts.Linking;

internal static class LinkingWorkspaceConfigurationBodyMapper
{
    internal static bool TryMapInitial(
        LinkingWorkspaceInitialConfigurationBody? body,
        LinkingSourceKind sourceKind,
        out LinkingSourceConfiguration configuration,
        out string errorMessage) =>
        TryMap(
            body is null
                ? null
                : new LinkingWorkspaceConfigurationBody
                {
                    InclusionMode = body.InclusionMode,
                    AyahOverrides = body.AyahOverrides,
                    SelectedWords = body.SelectedWords,
                    AutomaticWordMatchesEnabled = body.AutomaticWordMatchesEnabled,
                    ManualLinkShape = body.ManualLinkShape,
                    Descriptions = body.Descriptions,
                },
            sourceKind,
            out configuration,
            out errorMessage);

    internal static bool TryMap(
        LinkingWorkspaceConfigurationBody? body,
        LinkingSourceKind sourceKind,
        out LinkingSourceConfiguration configuration,
        out string errorMessage)
    {
        configuration = null!;
        errorMessage = string.Empty;

        if (body is null)
        {
            errorMessage = MalformedMessage(LinkingBodyViolations.Malformed("body"));
            return false;
        }

        if (!LinkingWorkspaceTokens.TryParseInclusionMode(body.InclusionMode, out var inclusionMode))
        {
            errorMessage = MalformedMessage(
                LinkingBodyViolations.Malformed("inclusionMode", body.InclusionMode));
            return false;
        }

        LinkingManualLinkShape? manualLinkShape = null;
        if (body.ManualLinkShape is not null)
        {
            if (!LinkingWorkspaceTokens.TryParseManualLinkShape(body.ManualLinkShape, out var parsedShape))
            {
                errorMessage = MalformedMessage(
                    LinkingBodyViolations.Malformed("manualLinkShape", body.ManualLinkShape));
                return false;
            }

            manualLinkShape = parsedShape;
        }

        var selectedWords = new List<LinkingWorkspaceSelectedWordInput>();
        foreach (var selectedWord in body.SelectedWords ?? [])
        {
            if (selectedWord?.AyahId is not > 0)
            {
                errorMessage = MalformedMessage(LinkingBodyViolations.Malformed(
                    "selectedWords.ayahId", LinkingBodyViolations.Text(selectedWord?.AyahId)));
                return false;
            }

            if (selectedWord.QuranWordId is not > 0)
            {
                errorMessage = MalformedMessage(LinkingBodyViolations.Malformed(
                    "selectedWords.quranWordId", LinkingBodyViolations.Text(selectedWord.QuranWordId)));
                return false;
            }

            selectedWords.Add(new LinkingWorkspaceSelectedWordInput(
                selectedWord.AyahId.Value, selectedWord.QuranWordId.Value));
        }

        foreach (var ayahId in body.AyahOverrides ?? [])
        {
            if (ayahId <= 0)
            {
                errorMessage = MalformedMessage(LinkingBodyViolations.Malformed(
                    "ayahOverrides", LinkingBodyViolations.Text(ayahId)));
                return false;
            }
        }

        var descriptions = new List<LinkingWorkspaceDescriptionInput>();
        foreach (var description in body.Descriptions ?? [])
        {
            if (description?.AyahId is not > 0)
            {
                errorMessage = MalformedMessage(LinkingBodyViolations.Malformed(
                    "descriptions.ayahId", LinkingBodyViolations.Text(description?.AyahId)));
                return false;
            }

            if (description.OrderValue is not > 0)
            {
                errorMessage = MalformedMessage(LinkingBodyViolations.Malformed(
                    "descriptions.orderValue", LinkingBodyViolations.Text(description.OrderValue)));
                return false;
            }

            descriptions.Add(new LinkingWorkspaceDescriptionInput(
                description.AyahId.Value, description.OrderValue.Value, description.Body ?? string.Empty));
        }

        if (!LinkingSourceConfiguration.TryCreate(
            sourceKind,
            inclusionMode,
            [.. body.AyahOverrides ?? []],
            selectedWords,
            body.AutomaticWordMatchesEnabled,
            manualLinkShape,
            descriptions,
            out configuration,
            out var configurationViolation))
        {
            errorMessage = ApiMessages.LinkingWorkspaceViolationMessage(configurationViolation);
            return false;
        }

        return true;
    }

    private static string MalformedMessage(LinkingDescriptorViolation violation) =>
        ApiMessages.LinkingDescriptorViolationMessage(violation);
}
