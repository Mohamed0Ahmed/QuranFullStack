using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QuranDashboard.Application.Abstractions.Linking.Preflight;

public static class LinkingPreflightToken
{
    public static string Compute(
        LinkingOperationRequest request,
        LinkingPreflightDoorComponent door,
        IReadOnlyList<LinkingPreflightContributionComponent> contributions)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(door);
        ArgumentNullException.ThrowIfNull(contributions);

        var canonical = new StringBuilder();

        AppendLine(canonical, "door", Number(door.DoorId), Number(door.Version));

        foreach (var contribution in contributions.OrderBy(candidate => candidate.Id))
        {
            AppendLine(canonical, "contribution", Number(contribution.Id), Number(contribution.Version));
        }

        AppendLine(canonical, "intent", Number(request.DoorId));

        var sources = request.Sources
            .OrderBy(source => source.OrderValue)
            .ThenBy(
                source => LinkingContributionIdentity.For(source.Descriptor, source.ContributionMode),
                StringComparer.Ordinal);

        foreach (var source in sources)
        {
            AppendLine(
                canonical,
                "source",
                Number(source.OrderValue),
                Text(LinkingContributionIdentity.For(source.Descriptor, source.ContributionMode)),
                LinkingOperationTokens.ToToken(source.ContributionMode),
                Flag(source.AutomaticWordMatchesEnabled));

            foreach (var unit in source.Units)
            {
                AppendLine(canonical, "unit", Number(unit.Ayahs.Count));

                foreach (var ayah in unit.Ayahs)
                {
                    AppendLine(
                        canonical,
                        "ayah",
                        Number(ayah.AyahId),
                        Numbers(ayah.SelectedWordIds.Distinct().Order()),
                        Texts(ayah.Descriptions));
                }
            }
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    public static IReadOnlyList<LinkingPreflightContributionComponent> AffectedContributionsOf(
        LinkingConfirmedDoorState state,
        LinkingOperationClassification classification)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(classification);

        var replaced = classification.Sources
            .Where(source => source.ExistingContributionId is not null)
            .Select(source => source.ExistingContributionId!.Value)
            .ToHashSet();

        var overlapped = classification.Sources
            .SelectMany(source => source.Ayahs)
            .SelectMany(ayah => ayah.OverlappingSources)
            .Select(source => source.SourceIdentity)
            .ToHashSet(StringComparer.Ordinal);

        return
        [
            .. state.Contributions
                .Where(contribution =>
                    replaced.Contains(contribution.Id) || overlapped.Contains(contribution.SourceIdentity))
                .Select(contribution => new LinkingPreflightContributionComponent(
                    contribution.Id, contribution.Version))
        ];
    }

    private static void AppendLine(StringBuilder canonical, params string[] parts) =>
        canonical.AppendLine(string.Join('|', parts));

    private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Number(uint value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Numbers(IEnumerable<int> values) =>
        string.Join(',', values.Select(value => Number(value)));

    private static string Flag(bool? value) => value switch
    {
        true => "1",
        false => "0",
        null => "-",
    };

    private static string Text(string value) =>
        $"{Number(Encoding.UTF8.GetByteCount(value))}:{value}";

    private static string Texts(IReadOnlyList<string> values) =>
        string.Join(',', values.Select(value => Text(value.Trim())));
}

public sealed record LinkingPreflightDoorComponent(int DoorId, uint Version);

public sealed record LinkingPreflightContributionComponent(long Id, uint Version);
