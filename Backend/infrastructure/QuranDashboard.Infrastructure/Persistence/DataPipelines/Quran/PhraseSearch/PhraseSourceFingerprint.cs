namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal static class PhraseSourceFingerprint
{
    internal static string Compute(IReadOnlyList<PhraseSourceToken> tokens)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> lengthPrefix = stackalloc byte[sizeof(int)];
        Append(
            hash,
            $"phrase-source-v{PhraseIndexBuildConstants.SourceFingerprintVersion.ToString(CultureInfo.InvariantCulture)}",
            lengthPrefix);

        foreach (var token in tokens)
        {
            Append(hash, token.Id.ToString(CultureInfo.InvariantCulture), lengthPrefix);
            Append(hash, token.AyahId.ToString(CultureInfo.InvariantCulture), lengthPrefix);
            Append(hash, token.SurahNumber.ToString(CultureInfo.InvariantCulture), lengthPrefix);
            Append(hash, token.WordNumber.ToString(CultureInfo.InvariantCulture), lengthPrefix);
            Append(hash, token.TextUthmani, lengthPrefix);
            Append(hash, token.WordKeyImlaeiSimple, lengthPrefix);
            Append(hash, token.TashkilIdentity, lengthPrefix);
            Append(hash, token.UniqueSimpleWordId.ToString(CultureInfo.InvariantCulture), lengthPrefix);
            Append(hash, token.UniqueTashkeelWordId.ToString(CultureInfo.InvariantCulture), lengthPrefix);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void Append(IncrementalHash hash, string value, Span<byte> lengthPrefix)
    {
        var bytes = Encoding.UTF8.GetBytes(value.Normalize(NormalizationForm.FormC));
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, bytes.Length);
        hash.AppendData(lengthPrefix);
        hash.AppendData(bytes);
    }
}
