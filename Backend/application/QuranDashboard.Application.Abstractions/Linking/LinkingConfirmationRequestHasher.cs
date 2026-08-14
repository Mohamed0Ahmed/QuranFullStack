using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace QuranDashboard.Application.Abstractions.Linking;

public static class LinkingConfirmationRequestContracts
{
    public const string PreparedJob = "prepared_job";
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

    private static void Append(IncrementalHash hash, ReadOnlySpan<byte> value)
    {
        Span<byte> length = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(length, value.Length);
        hash.AppendData(length);
        hash.AppendData(value);
    }
}
