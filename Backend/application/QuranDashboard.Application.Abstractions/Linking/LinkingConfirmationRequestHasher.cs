using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Application.Abstractions.Linking;

public static class LinkingConfirmationRequestContracts
{
    public const string PreparedJob = "prepared_job";
    public const string LegacyExpanded = "legacy_expanded";
    public const int SchemaVersion = 1;
}

public static class LinkingConfirmationRequestHasher
{
    public static string ComputePrepared(
        Guid preflightId,
        string preflightToken,
        long linkingDataRevision)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, LinkingConfirmationRequestContracts.PreparedJob);
        Append(hash, LinkingConfirmationRequestContracts.SchemaVersion);
        Append(hash, preflightId);
        Append(hash, preflightToken);
        Append(hash, linkingDataRevision);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    public static string ComputeLegacy(LinkingOperationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, LinkingConfirmationRequestContracts.LegacyExpanded);
        Append(hash, LinkingConfirmationRequestContracts.SchemaVersion);
        Append(hash, request.DoorId);
        Append(hash, request.ExpectedLinkingDataRevision);
        Append(hash, request.PreflightToken ?? string.Empty);

        var sources = request.Sources.OrderBy(source => source.OrderValue).ToList();
        Append(hash, sources.Count);
        foreach (var source in sources)
        {
            Append(hash, LinkingSourceIdentity.For(source.Descriptor));
            Append(hash, source.Descriptor.Label);
            Append(hash, (int)source.ContributionMode);
            Append(hash, source.AutomaticWordMatchesEnabled);
            Append(hash, source.OrderValue);
            Append(hash, source.ExistingContributionId);
            Append(hash, source.ExistingContributionVersion);
            Append(hash, source.Units.Count);
            foreach (var unit in source.Units)
            {
                Append(hash, unit.Ayahs.Count);
                foreach (var ayah in unit.Ayahs)
                {
                    Append(hash, ayah.AyahId);
                    var wordIds = ayah.SelectedWordIds.Distinct().Order().ToList();
                    Append(hash, wordIds.Count);
                    foreach (var wordId in wordIds)
                    {
                        Append(hash, wordId);
                    }

                    Append(hash, ayah.Descriptions.Count);
                    foreach (var description in ayah.Descriptions)
                    {
                        Append(hash, description);
                    }
                }
            }
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void Append(IncrementalHash hash, string value) =>
        Append(hash, Encoding.UTF8.GetBytes(value));

    private static void Append(IncrementalHash hash, Guid value) => Append(hash, value.ToByteArray());

    private static void Append(IncrementalHash hash, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        Append(hash, bytes);
    }

    private static void Append(IncrementalHash hash, long value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        Append(hash, bytes);
    }

    private static void Append(IncrementalHash hash, bool? value) =>
        Append(hash, value is null ? -1 : value.Value ? 1 : 0);

    private static void Append(IncrementalHash hash, long? value)
    {
        Append(hash, value.HasValue);
        if (value.HasValue)
        {
            Append(hash, value.Value);
        }
    }

    private static void Append(IncrementalHash hash, uint? value)
    {
        Append(hash, value.HasValue);
        if (value.HasValue)
        {
            Append(hash, unchecked((long)value.Value));
        }
    }

    private static void Append(IncrementalHash hash, bool value) => Append(hash, value ? 1 : 0);

    private static void Append(IncrementalHash hash, ReadOnlySpan<byte> value)
    {
        Span<byte> length = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(length, value.Length);
        hash.AppendData(length);
        hash.AppendData(value);
    }
}
