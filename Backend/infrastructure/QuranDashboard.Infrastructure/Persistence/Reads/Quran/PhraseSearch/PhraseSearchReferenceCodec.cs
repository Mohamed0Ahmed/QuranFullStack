using System.Buffers.Binary;
using System.Security.Cryptography;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

internal sealed partial class PhraseSearchReferenceCodec : IPhraseSearchReferenceCodec
{
    private const byte FormatVersion = 1;
    private const byte ResolutionKind = 1;
    private const byte PathKind = 2;
    private const byte FullContextKind = 3;
    private const byte CursorKind = 4;
    private const byte AlternativeKind = 5;
    private const int ChecksumLength = 8;
    private const int MaximumEncodedReferenceLength = 4096;

    private static string Encode(Action<BinaryWriter> writePayload)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writePayload(writer);
        }

        var payload = stream.ToArray();
        var checksum = SHA256.HashData(payload);
        var result = new byte[payload.Length + ChecksumLength];
        payload.CopyTo(result, 0);
        checksum.AsSpan(0, ChecksumLength).CopyTo(result.AsSpan(payload.Length));
        return Convert.ToBase64String(result).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryOpen(
        string? value,
        byte expectedKind,
        out BinaryReader reader,
        out MemoryStream stream)
    {
        reader = null!;
        stream = null!;
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumEncodedReferenceLength)
        {
            return false;
        }

        byte[] bytes;
        try
        {
            bytes = DecodeBase64Url(value);
        }
        catch (FormatException)
        {
            return false;
        }

        if (bytes.Length < 2 + 16 + ChecksumLength)
        {
            return false;
        }

        var payloadLength = bytes.Length - ChecksumLength;
        var checksum = SHA256.HashData(bytes.AsSpan(0, payloadLength));
        if (!CryptographicOperations.FixedTimeEquals(
            checksum.AsSpan(0, ChecksumLength),
            bytes.AsSpan(payloadLength, ChecksumLength)))
        {
            return false;
        }

        stream = new MemoryStream(bytes, 0, payloadLength, writable: false, publiclyVisible: true);
        reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        if (reader.ReadByte() != FormatVersion || reader.ReadByte() != expectedKind)
        {
            reader.Dispose();
            stream.Dispose();
            reader = null!;
            stream = null!;
            return false;
        }

        return true;
    }

    private static byte[] DecodeBase64Url(string value)
    {
        if (value.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')))
        {
            throw new FormatException();
        }

        var paddingLength = (4 - value.Length % 4) % 4;
        if (paddingLength == 3)
        {
            throw new FormatException();
        }

        return Convert.FromBase64String(
            value.Replace('-', '+').Replace('_', '/') + new string('=', paddingLength));
    }

    private static void ValidateIdentity(
        Guid buildId,
        PhraseTextMode mode,
        IReadOnlyList<int> tokenIds)
    {
        if (buildId == Guid.Empty || !Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(buildId));
        }

        ValidateTokens(tokenIds, allowEmpty: false);
    }

    private static void ValidateTokens(IReadOnlyList<int> tokenIds, bool allowEmpty)
    {
        ArgumentNullException.ThrowIfNull(tokenIds);
        if ((!allowEmpty && tokenIds.Count == 0)
            || tokenIds.Count > PhraseSearchQueryLimits.MaximumResolvedTokens
            || tokenIds.Any(tokenId => tokenId <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(tokenIds));
        }
    }

    private static IReadOnlyList<int> CanonicalizeAlternativeTokenIds(IReadOnlyList<int> tokenIds)
    {
        ValidateTokens(tokenIds, allowEmpty: false);
        return tokenIds
            .Order()
            .Distinct()
            .ToArray();
    }

    private static bool IsCanonicalAlternativeTokenIds(IReadOnlyList<int> tokenIds)
    {
        for (var index = 1; index < tokenIds.Count; index++)
        {
            if (tokenIds[index - 1] >= tokenIds[index])
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteGuid(BinaryWriter writer, Guid value)
    {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes, bigEndian: true, out _);
        writer.Write(bytes);
    }

    private static Guid ReadGuid(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(16);
        if (bytes.Length != 16)
        {
            throw new EndOfStreamException();
        }

        var value = new Guid(bytes, bigEndian: true);
        if (value == Guid.Empty)
        {
            throw new InvalidDataException();
        }

        return value;
    }

    private static PhraseTextMode ReadMode(BinaryReader reader)
    {
        var mode = (PhraseTextMode)reader.ReadByte();
        return Enum.IsDefined(mode) ? mode : throw new InvalidDataException();
    }

    private static void WriteTokenIds(BinaryWriter writer, IReadOnlyList<int> tokenIds)
    {
        WriteUInt16(writer, checked((ushort)tokenIds.Count));
        foreach (var tokenId in tokenIds)
        {
            WriteInt32(writer, tokenId);
        }
    }

    private static IReadOnlyList<int> ReadTokenIds(
        BinaryReader reader,
        int maximum,
        bool allowEmpty = false)
    {
        var count = ReadUInt16(reader);
        if ((!allowEmpty && count == 0) || count > maximum)
        {
            throw new InvalidDataException();
        }

        var ids = new int[count];
        for (var index = 0; index < count; index++)
        {
            ids[index] = ReadInt32(reader);
            if (ids[index] <= 0)
            {
                throw new InvalidDataException();
            }
        }

        return ids;
    }

    private static void WriteUInt16(BinaryWriter writer, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static ushort ReadUInt16(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(2);
        return bytes.Length == 2
            ? BinaryPrimitives.ReadUInt16BigEndian(bytes)
            : throw new EndOfStreamException();
    }

    private static void WriteInt32(BinaryWriter writer, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static int ReadInt32(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        return bytes.Length == 4
            ? BinaryPrimitives.ReadInt32BigEndian(bytes)
            : throw new EndOfStreamException();
    }

    private static void WriteUInt64(BinaryWriter writer, ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static ulong ReadUInt64(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(8);
        return bytes.Length == 8
            ? BinaryPrimitives.ReadUInt64BigEndian(bytes)
            : throw new EndOfStreamException();
    }

    private static bool AtPayloadEnd(BinaryReader reader, MemoryStream stream) =>
        reader.BaseStream.Position == stream.Length;

    private static ulong HashScope(IEnumerable<string> values)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[4];
        foreach (var value in values)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        var digest = hash.GetHashAndReset();
        return BinaryPrimitives.ReadUInt64BigEndian(digest);
    }
}
