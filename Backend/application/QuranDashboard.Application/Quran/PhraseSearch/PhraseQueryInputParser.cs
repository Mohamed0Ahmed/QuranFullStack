using System.Text;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Application.Quran.PhraseSearch;

internal static class PhraseQueryInputParser
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static PhraseQueryParseResult Parse(string? q64, PhraseTextMode mode)
    {
        if (string.IsNullOrWhiteSpace(q64))
        {
            return new PhraseQueryParseResult.Failure(PhraseRequestInvalidKind.Query);
        }

        if (q64.Length > PhraseSearchQueryLimits.MaximumEncodedQueryLength)
        {
            return new PhraseQueryParseResult.Failure(PhraseRequestInvalidKind.QueryTooLong);
        }

        byte[] bytes;
        try
        {
            bytes = DecodeBase64Url(q64);
        }
        catch (FormatException)
        {
            return new PhraseQueryParseResult.Failure(PhraseRequestInvalidKind.QueryEncoding);
        }

        if (bytes.Length > PhraseSearchQueryLimits.MaximumDecodedQueryBytes)
        {
            return new PhraseQueryParseResult.Failure(PhraseRequestInvalidKind.QueryTooLong);
        }

        string raw;
        try
        {
            raw = StrictUtf8.GetString(bytes).Normalize(NormalizationForm.FormC);
        }
        catch (DecoderFallbackException)
        {
            return new PhraseQueryParseResult.Failure(PhraseRequestInvalidKind.QueryEncoding);
        }

        var segments = raw
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(segment => PhraseSearchInputNormalizer.NormalizeSegment(segment, mode))
            .ToArray();

        if (segments.Length == 0)
        {
            return new PhraseQueryParseResult.Failure(PhraseRequestInvalidKind.Query);
        }

        if (segments.Length > PhraseSearchQueryLimits.MaximumResolvedTokens)
        {
            return new PhraseQueryParseResult.Failure(PhraseRequestInvalidKind.QueryTooLong);
        }

        return new PhraseQueryParseResult.Success(segments);
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

        var base64 = value.Replace('-', '+').Replace('_', '/') + new string('=', paddingLength);
        return Convert.FromBase64String(base64);
    }
}

internal abstract record PhraseQueryParseResult
{
    private PhraseQueryParseResult() { }

    internal sealed record Success(IReadOnlyList<string> Segments) : PhraseQueryParseResult;
    internal sealed record Failure(PhraseRequestInvalidKind Kind) : PhraseQueryParseResult;
}
