using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Api.Contracts.Linking;

internal static class LinkingWorkspaceConfigurationBodyMapper
{
    internal static bool TryMap(
        LinkingWorkspaceConfigurationBody? body,
        out LinkingWorkspaceConfigurationInput configuration,
        out LinkingDescriptorViolation violation)
    {
        configuration = null!;
        violation = null!;

        if (body is null)
        {
            violation = LinkingBodyViolations.Malformed("body");
            return false;
        }

        if (!LinkingWorkspaceTokens.TryParseInclusionMode(body.InclusionMode, out var inclusionMode))
        {
            violation = LinkingBodyViolations.Malformed("inclusionMode", body.InclusionMode);
            return false;
        }

        LinkingManualLinkShape? manualLinkShape = null;
        if (body.ManualLinkShape is not null)
        {
            if (!LinkingWorkspaceTokens.TryParseManualLinkShape(body.ManualLinkShape, out var parsedShape))
            {
                violation = LinkingBodyViolations.Malformed("manualLinkShape", body.ManualLinkShape);
                return false;
            }

            manualLinkShape = parsedShape;
        }

        var selectedWords = new List<LinkingWorkspaceSelectedWordInput>();
        foreach (var selectedWord in body.SelectedWords ?? [])
        {
            if (selectedWord?.AyahId is not > 0)
            {
                violation = LinkingBodyViolations.Malformed(
                    "selectedWords.ayahId", LinkingBodyViolations.Text(selectedWord?.AyahId));
                return false;
            }

            if (selectedWord.QuranWordId is not > 0)
            {
                violation = LinkingBodyViolations.Malformed(
                    "selectedWords.quranWordId", LinkingBodyViolations.Text(selectedWord.QuranWordId));
                return false;
            }

            selectedWords.Add(new LinkingWorkspaceSelectedWordInput(
                selectedWord.AyahId.Value, selectedWord.QuranWordId.Value));
        }

        foreach (var ayahId in body.AyahOverrides ?? [])
        {
            if (ayahId <= 0)
            {
                violation = LinkingBodyViolations.Malformed(
                    "ayahOverrides", LinkingBodyViolations.Text(ayahId));
                return false;
            }
        }

        configuration = new LinkingWorkspaceConfigurationInput(
            body.Label ?? string.Empty,
            inclusionMode,
            [.. body.AyahOverrides ?? []],
            selectedWords,
            body.AutomaticWordMatchesEnabled,
            manualLinkShape);

        return true;
    }
}
