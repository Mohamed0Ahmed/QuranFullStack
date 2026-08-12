using System.Globalization;

namespace QuranDashboard.Application.Abstractions.Linking;

public sealed record LinkingWorkspaceAyahDescriptions(int AyahId, IReadOnlyList<string> Bodies);

public static class LinkingWorkspaceDescriptionValidation
{
    public const string DescriptionsField = "descriptions";

    public static LinkingWorkspaceViolation? TryNormalize(
        IReadOnlyList<LinkingWorkspaceDescriptionInput> descriptions,
        out IReadOnlyList<LinkingWorkspaceAyahDescriptions> normalized)
    {
        ArgumentNullException.ThrowIfNull(descriptions);

        normalized = [];

        var byAyah = new List<LinkingWorkspaceAyahDescriptions>();

        foreach (var group in descriptions.GroupBy(description => description.AyahId).OrderBy(group => group.Key))
        {
            var submitted = group.ToList();

            if (submitted.Count > LinkingLimits.MaxDescriptionsPerSourceAyah)
            {
                return Violation(LinkingWorkspaceViolationCode.DescriptionLimitExceeded, group.Key);
            }

            if (submitted.Any(description => description.OrderValue < 1)
                || submitted.Select(description => description.OrderValue).Distinct().Count() != submitted.Count)
            {
                return Violation(LinkingWorkspaceViolationCode.DescriptionOrderConflict, group.Key);
            }

            var bodies = new List<string>(submitted.Count);

            foreach (var description in submitted.OrderBy(description => description.OrderValue))
            {
                var body = description.Body?.Trim() ?? string.Empty;

                if (body.Length is 0 || body.Length > LinkingLimits.MaxDescriptionLength)
                {
                    return Violation(LinkingWorkspaceViolationCode.DescriptionBodyInvalid, group.Key);
                }

                bodies.Add(body);
            }

            byAyah.Add(new LinkingWorkspaceAyahDescriptions(group.Key, bodies));
        }

        normalized = byAyah;

        return null;
    }

    private static LinkingWorkspaceViolation Violation(LinkingWorkspaceViolationCode code, int ayahId) =>
        new(code, DescriptionsField, ayahId.ToString(CultureInfo.InvariantCulture));
}
