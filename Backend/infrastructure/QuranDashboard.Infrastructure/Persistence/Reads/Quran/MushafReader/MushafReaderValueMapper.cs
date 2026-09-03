using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

internal sealed record WordAnalysisSegmentValue(
    string SegmentLocation,
    short SegmentNumber,
    string Kind,
    string? FormArabicNormalized,
    string Pos,
    string? PosArabicLabel,
    string? PosEnglishLabel,
    string? I3rabArabic,
    int? I3rabRuleId,
    string? RuleSignatureKey,
    string? RuleFamily,
    string? I3rabStatus,
    string FeaturesRaw,
    string? FeaturesJson);

internal static class MushafReaderValueMapper
{
    private static readonly JsonSerializerOptions FeaturesJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static IReadOnlyList<string> ParseCoveredAyahKeys(
        string json,
        string verseKey,
        string sourceKey,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Corrupt covered_ayah_keys JSON for ayah {verseKey} source {sourceKey}; treating as empty",
                verseKey,
                sourceKey);
            return [];
        }
    }

    internal static IReadOnlyList<RenderedSegmentDto> MapSegments(
        IReadOnlyList<WordAnalysisSegmentValue> segments,
        ILogger logger)
    {
        return segments.Select(segment =>
        {
            var hasDisplayText = !string.IsNullOrWhiteSpace(segment.FormArabicNormalized);
            return new RenderedSegmentDto(
                segment.SegmentLocation,
                segment.SegmentNumber,
                segment.SegmentNumber,
                segment.Kind,
                hasDisplayText ? segment.FormArabicNormalized : null,
                hasDisplayText ? "available" : "missing",
                segment.Pos,
                new LocalizedLabel(
                    segment.PosArabicLabel ?? segment.Pos,
                    segment.PosEnglishLabel ?? segment.Pos),
                segment.I3rabArabic,
                segment.I3rabRuleId,
                segment.RuleSignatureKey,
                segment.RuleFamily,
                segment.I3rabStatus,
                MapSegmentFeatures(segment, logger));
        }).ToArray();
    }

    private static SegmentFeaturesDto? MapSegmentFeatures(
        WordAnalysisSegmentValue segment,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(segment.FeaturesRaw) && string.IsNullOrWhiteSpace(segment.FeaturesJson))
        {
            return null;
        }

        return new SegmentFeaturesDto(
            segment.FeaturesRaw,
            ParseFeaturesJson(segment.FeaturesJson, segment.SegmentLocation, logger));
    }

    private static IReadOnlyList<JsonElement> ParseFeaturesJson(
        string? featuresJson,
        string segmentLocation,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(featuresJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<JsonElement>>(featuresJson, FeaturesJsonOptions) ?? [];
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Corrupt features_json JSON for segment {segmentLocation}; treating as empty",
                segmentLocation);
            return [];
        }
    }
}
