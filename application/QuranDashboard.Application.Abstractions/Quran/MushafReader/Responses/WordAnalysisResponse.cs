using System.Text.Json;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

public sealed record WordAnalysisResponse(
    WordOccurrenceDto Word,
    WordIdentityDto Identity,
    WordMorphologyDto Morphology,
    IReadOnlyList<RenderedSegmentDto> RenderedWordSegments);

public sealed record WordOccurrenceDto(
    int QuranWordId,
    string WordLocation,
    string VerseKey,
    int SurahNumber,
    int AyahNumber,
    int WordNumber,
    int PageNumber,
    int LineNumber,
    int LineWordOrder,
    string TextUthmani,
    string? TextUthmaniSimple,
    string? TextImlaeiSimple,
    string? QpcGlyph);

public sealed record WordIdentityDto(
    WordCountSummary OrderedTashkeel,
    WordCountSummary OrderedSimple,
    UniqueWordCountSummary UniqueTashkeel,
    UniqueSimpleWordCountSummary UniqueSimple);

public sealed record WordCountSummary(
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount);

public sealed record UniqueWordCountSummary(
    int Id,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount);

public sealed record UniqueSimpleWordCountSummary(
    int Id,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    string WordKeyImlaeiSimple);

public sealed record WordMorphologyDto(
    string HeadPos,
    LocalizedLabel HeadPosLabel,
    WordMorphologyRoot? Root,
    WordMorphologyLemma? Lemma,
    WordMorphologyStem? Stem,
    bool IsVerb,
    string? VerbTense,
    string? VerbVoice,
    string? CaseFeature);

public sealed record LocalizedLabel(
    string Ar,
    string En);

public sealed record WordMorphologyRoot(
    int Id,
    string? Text,
    string? Buckwalter);

public sealed record WordMorphologyLemma(
    string? Text,
    string? Buckwalter);

public sealed record WordMorphologyStem(
    string? Text);

public sealed record SegmentFeaturesDto(
    string? Raw,
    IReadOnlyList<JsonElement> Json);

public sealed record RenderedSegmentDto(
    string SegmentLocation,
    int SegmentNumber,
    int SegmentColorSlot,
    string? SegmentKind,
    string? SegmentDisplayText,
    string DisplayTextStatus,
    string? SegmentPos,
    LocalizedLabel? SegmentPosLabel,
    string? SegmentI3rabArabic,
    int? I3rabRuleId,
    string? I3rabRuleSignature,
    string? I3rabRuleFamily,
    string? I3rabStatus,
    SegmentFeaturesDto? SegmentFeatures);
