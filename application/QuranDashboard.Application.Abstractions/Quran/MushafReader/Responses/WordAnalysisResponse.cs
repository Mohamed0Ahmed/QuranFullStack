using System.Text.Json;

namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

/// <summary>
/// Word-analysis read model (data-model.md §B3): occurrence identity, ordered
/// / unique identity counts, head morphology, and the ordered, color-linked
/// segments. The backend emits a stable <see cref="RenderedSegmentDto.SegmentColorSlot"/>
/// (visual-linking only); the frontend maps the slot to a palette color.
/// </summary>
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

/// <summary>
/// A localized POS/feature label. Shared by the word-morphology and rendered-
/// segment shapes.
/// </summary>
public sealed record LocalizedLabel(
    string Ar,
    string En);

public sealed record WordMorphologyRoot(
    string? Text,
    string? Buckwalter);

public sealed record WordMorphologyLemma(
    string? Text,
    string? Buckwalter);

public sealed record WordMorphologyStem(
    string? Text);

/// <summary>
/// Pass-through container for a segment's raw feature string and its parsed
/// JSON features array. The <see cref="Json"/> list mirrors the frontend
/// <c>object[]</c> shape (data-model.md §B3): one entry per parsed feature.
/// </summary>
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
