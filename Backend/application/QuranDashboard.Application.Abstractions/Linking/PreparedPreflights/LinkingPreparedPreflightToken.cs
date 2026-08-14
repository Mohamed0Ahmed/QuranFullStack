using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;

public static class LinkingPreparedPreflightToken
{
    public static string Compute(
        Guid preflightId,
        int actorUserId,
        string requestHash,
        string intentHash,
        long linkingDataRevision,
        LinkingPreflightDoorComponent door,
        IReadOnlyList<LinkingPreflightContributionComponent> contributions)
    {
        var canonical = new StringBuilder();
        Append(canonical, "schema", "1");
        Append(canonical, "preflight", preflightId.ToString("D"));
        Append(canonical, "actor", Number(actorUserId));
        Append(canonical, "request", requestHash);
        Append(canonical, "intent", intentHash);
        Append(canonical, "revision", Number(linkingDataRevision));
        Append(canonical, "door", Number(door.DoorId), Number(door.Version));
        foreach (var contribution in contributions.OrderBy(value => value.Id))
        {
            Append(
                canonical,
                "contribution",
                Number(contribution.Id),
                Number(contribution.Version));
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    public static string IntentHash(LinkingOperationRequest request) =>
        LinkingPreparedPreflightRequestHasher.ComputeHash(
            JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }));

    private static void Append(StringBuilder builder, params string[] values) =>
        builder.AppendLine(string.Join('|', values));

    private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Number(uint value) => value.ToString(CultureInfo.InvariantCulture);
}
