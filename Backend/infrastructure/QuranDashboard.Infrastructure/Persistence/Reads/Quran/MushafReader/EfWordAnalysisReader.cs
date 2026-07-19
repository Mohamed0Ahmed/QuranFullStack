using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

public sealed class EfWordAnalysisReader(QuranDashboardDbContext db, ILogger<EfWordAnalysisReader> logger) : IWordAnalysisReader
{
    private static readonly JsonSerializerOptions FeaturesJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<WordAnalysisOutcome> GetWordAnalysisAsync(string wordLocation, CancellationToken ct)
    {
        var core = await LoadCoreAsync(wordLocation, ct);

        if (core is null)
        {
            return new WordAnalysisOutcome.NotFound();
        }

        if (core.IsAyahMarker)
        {
            return new WordAnalysisOutcome.NotAnalyzable();
        }

        if (core.HeadPos is null
            || core.OrderedTashkeelOccurrencesCount is null
            || core.OrderedSimpleOccurrencesCount is null
            || core.WordUniqueTashkeelWordId is null
            || core.UniqueTashkeelOccurrencesCount is null
            || core.WordUniqueSimpleWordId is null
            || core.UniqueSimpleOccurrencesCount is null)
        {
            return new WordAnalysisOutcome.IncompleteData();
        }

        var headPosTag = await db.PosTags
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == core.HeadPos, ct);

        var segments = await LoadSegmentsAsync(core.WordId, ct);

        if (segments.Count == 0)
        {
            return new WordAnalysisOutcome.IncompleteData();
        }

        var response = new WordAnalysisResponse(
            MapWord(core),
            MapIdentity(core),
            MapMorphology(core, headPosTag),
            MapSegments(segments));

        return new WordAnalysisOutcome.Found(response);
    }

    // Word, morphology, its root/lemma/stem dimensions, and the four identity summary rows are
    // collapsed into one LEFT-JOIN projection instead of up to six sequential round trips; a missing
    // morphology/identity row surfaces as null joined columns, preserving the incomplete-data checks.
    private async Task<WordCoreProjection?> LoadCoreAsync(string wordLocation, CancellationToken ct)
    {
        return await (
            from word in db.QuranWords.AsNoTracking()
            where word.Location == wordLocation
            join m in db.WordMorphologies.AsNoTracking()
                on word.Id equals m.QuranWordId into morphologyGroup
            from morphology in morphologyGroup.DefaultIfEmpty()
            join r in db.QuranRoots.AsNoTracking()
                on morphology.RootId equals (int?)r.Id into rootGroup
            from root in rootGroup.DefaultIfEmpty()
            join l in db.QuranLemmas.AsNoTracking()
                on morphology.LemmaId equals (int?)l.Id into lemmaGroup
            from lemma in lemmaGroup.DefaultIfEmpty()
            join st in db.QuranStems.AsNoTracking()
                on morphology.StemId equals (int?)st.Id into stemGroup
            from stem in stemGroup.DefaultIfEmpty()
            join ot in db.QuranWordsOrderedTashkeel.AsNoTracking()
                on word.Id equals ot.QuranWordId into orderedTashkeelGroup
            from orderedTashkeel in orderedTashkeelGroup.DefaultIfEmpty()
            join os in db.QuranWordsOrderedSimple.AsNoTracking()
                on word.Id equals os.QuranWordId into orderedSimpleGroup
            from orderedSimple in orderedSimpleGroup.DefaultIfEmpty()
            join ut in db.QuranWordsUniqueTashkeel.AsNoTracking()
                on word.UniqueTashkeelWordId equals (int?)ut.Id into uniqueTashkeelGroup
            from uniqueTashkeel in uniqueTashkeelGroup.DefaultIfEmpty()
            join us in db.QuranWordsUniqueSimple.AsNoTracking()
                on word.UniqueSimpleWordId equals (int?)us.Id into uniqueSimpleGroup
            from uniqueSimple in uniqueSimpleGroup.DefaultIfEmpty()
            select new WordCoreProjection(
                word.Id,
                word.Location,
                word.SurahNumber,
                word.AyahNumber,
                word.WordNumber,
                word.PageNumber,
                word.LineNumber,
                word.LineWordOrder,
                word.TextUthmani,
                word.TextUthmaniSimple,
                word.TextImlaeiSimple,
                word.QpcGlyph,
                word.IsAyahMarker,
                word.UniqueTashkeelWordId,
                word.UniqueSimpleWordId,
                morphology != null ? morphology.HeadPos : null,
                morphology != null ? (bool?)morphology.IsVerb : null,
                morphology != null ? morphology.VerbTense : null,
                morphology != null ? morphology.VerbVoice : null,
                morphology != null ? morphology.CaseFeature : null,
                root != null ? (int?)root.Id : null,
                root != null ? root.RootText : null,
                root != null ? root.RootBuckwalter : null,
                lemma != null ? (int?)lemma.Id : null,
                lemma != null ? lemma.LemmaText : null,
                lemma != null ? lemma.LemmaBuckwalter : null,
                stem != null ? (int?)stem.Id : null,
                stem != null ? stem.StemText : null,
                orderedTashkeel != null ? (int?)orderedTashkeel.OccurrencesCount : null,
                orderedTashkeel != null ? (short?)orderedTashkeel.AyahsCount : null,
                orderedTashkeel != null ? (short?)orderedTashkeel.SurahsCount : null,
                orderedSimple != null ? (int?)orderedSimple.OccurrencesCount : null,
                orderedSimple != null ? (short?)orderedSimple.AyahsCount : null,
                orderedSimple != null ? (short?)orderedSimple.SurahsCount : null,
                uniqueTashkeel != null ? (int?)uniqueTashkeel.Id : null,
                uniqueTashkeel != null ? (int?)uniqueTashkeel.OccurrencesCount : null,
                uniqueTashkeel != null ? (short?)uniqueTashkeel.AyahsCount : null,
                uniqueTashkeel != null ? (short?)uniqueTashkeel.SurahsCount : null,
                uniqueSimple != null ? (int?)uniqueSimple.Id : null,
                uniqueSimple != null ? (int?)uniqueSimple.OccurrencesCount : null,
                uniqueSimple != null ? (short?)uniqueSimple.AyahsCount : null,
                uniqueSimple != null ? (short?)uniqueSimple.SurahsCount : null,
                uniqueSimple != null ? uniqueSimple.WordKeyImlaeiSimple : null))
            .FirstOrDefaultAsync(ct);
    }

    // Ordered segments joined to their POS tag and i3rab rule in one projection, replacing the prior
    // segment fetch plus separate POS-code and rule-id lookups.
    private async Task<IReadOnlyList<SegmentProjection>> LoadSegmentsAsync(int wordId, CancellationToken ct)
    {
        return await (
            from segment in db.WordMorphologySegments.AsNoTracking()
            where segment.QuranWordId == wordId
            join pt in db.PosTags.AsNoTracking()
                on segment.Pos equals pt.Code into posGroup
            from posTag in posGroup.DefaultIfEmpty()
            join rule in db.QuranI3rabRules.AsNoTracking()
                on segment.I3rabRuleId equals (int?)rule.Id into ruleGroup
            from i3rabRule in ruleGroup.DefaultIfEmpty()
            orderby segment.SegmentNumber
            select new SegmentProjection(
                segment.SegmentLocation,
                segment.SegmentNumber,
                segment.Kind,
                segment.FormArabicNormalized,
                segment.Pos,
                posTag != null ? posTag.ArabicLabel : null,
                posTag != null ? posTag.EnglishLabel : null,
                segment.I3rabArabic,
                segment.I3rabRuleId,
                i3rabRule != null ? i3rabRule.SignatureKey : null,
                i3rabRule != null ? i3rabRule.RuleFamily : null,
                segment.I3rabStatus,
                segment.FeaturesRaw,
                segment.FeaturesJson))
            .ToListAsync(ct);
    }

    private static WordOccurrenceDto MapWord(WordCoreProjection core) => new(
        core.WordId,
        core.Location,
        $"{core.SurahNumber}:{core.AyahNumber}",
        core.SurahNumber,
        core.AyahNumber,
        core.WordNumber,
        core.PageNumber,
        core.LineNumber,
        core.LineWordOrder,
        core.TextUthmani,
        core.TextUthmaniSimple,
        core.TextImlaeiSimple,
        core.QpcGlyph);

    private static WordIdentityDto MapIdentity(WordCoreProjection core) => new(
        new WordCountSummary(
            core.OrderedTashkeelOccurrencesCount!.Value,
            core.OrderedTashkeelAyahsCount!.Value,
            core.OrderedTashkeelSurahsCount!.Value),
        new WordCountSummary(
            core.OrderedSimpleOccurrencesCount!.Value,
            core.OrderedSimpleAyahsCount!.Value,
            core.OrderedSimpleSurahsCount!.Value),
        new UniqueWordCountSummary(
            core.UniqueTashkeelRowId!.Value,
            core.UniqueTashkeelOccurrencesCount!.Value,
            core.UniqueTashkeelAyahsCount!.Value,
            core.UniqueTashkeelSurahsCount!.Value),
        new UniqueSimpleWordCountSummary(
            core.UniqueSimpleRowId!.Value,
            core.UniqueSimpleOccurrencesCount!.Value,
            core.UniqueSimpleAyahsCount!.Value,
            core.UniqueSimpleSurahsCount!.Value,
            core.UniqueSimpleWordKeyImlaeiSimple!));

    private static WordMorphologyDto MapMorphology(
        WordCoreProjection core,
        Domain.Quran.Words.Morphology.PosTag? headPosTag) => new(
        core.HeadPos!,
        new LocalizedLabel(
            headPosTag?.ArabicLabel ?? core.HeadPos!,
            headPosTag?.EnglishLabel ?? core.HeadPos!),
        core.RootId is null
            ? null
            : new WordMorphologyRoot(core.RootId.Value, core.RootText!, core.RootBuckwalter),
        core.LemmaId is null
            ? null
            : new WordMorphologyLemma(core.LemmaId.Value, core.LemmaText!, core.LemmaBuckwalter),
        core.StemId is null
            ? null
            : new WordMorphologyStem(core.StemId.Value, core.StemText!),
        core.IsVerb!.Value,
        core.VerbTense,
        core.VerbVoice,
        core.CaseFeature);

    private IReadOnlyList<RenderedSegmentDto> MapSegments(IReadOnlyList<SegmentProjection> segments)
    {
        var rendered = new List<RenderedSegmentDto>(segments.Count);

        foreach (var segment in segments)
        {
            var hasDisplayText = !string.IsNullOrWhiteSpace(segment.FormArabicNormalized);

            rendered.Add(new RenderedSegmentDto(
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
                MapSegmentFeatures(segment)));
        }

        return rendered;
    }

    private SegmentFeaturesDto? MapSegmentFeatures(SegmentProjection segment)
    {
        if (string.IsNullOrWhiteSpace(segment.FeaturesRaw) && string.IsNullOrWhiteSpace(segment.FeaturesJson))
        {
            return null;
        }

        return new SegmentFeaturesDto(
            segment.FeaturesRaw,
            ParseFeaturesJson(segment.FeaturesJson, segment.SegmentLocation));
    }

    // quran-safety rule 3: corrupt features_json must not be swallowed silently. The return contract
    // is unchanged (still empty on failure), but a corrupt row logs a Warning naming the segment so
    // the underlying data issue stays visible.
    private IReadOnlyList<JsonElement> ParseFeaturesJson(string? featuresJson, string segmentLocation)
    {
        if (string.IsNullOrWhiteSpace(featuresJson))
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<JsonElement>>(featuresJson, FeaturesJsonOptions);
            return parsed ?? (IReadOnlyList<JsonElement>)[];
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Corrupt features_json JSON for segment {segmentLocation}; treating as empty",
                segmentLocation);
            return [];
        }
    }

    private sealed record WordCoreProjection(
        int WordId,
        string Location,
        short SurahNumber,
        short AyahNumber,
        short WordNumber,
        short PageNumber,
        short LineNumber,
        short LineWordOrder,
        string TextUthmani,
        string TextUthmaniSimple,
        string TextImlaeiSimple,
        string QpcGlyph,
        bool IsAyahMarker,
        int? WordUniqueTashkeelWordId,
        int? WordUniqueSimpleWordId,
        string? HeadPos,
        bool? IsVerb,
        string? VerbTense,
        string? VerbVoice,
        string? CaseFeature,
        int? RootId,
        string? RootText,
        string? RootBuckwalter,
        int? LemmaId,
        string? LemmaText,
        string? LemmaBuckwalter,
        int? StemId,
        string? StemText,
        int? OrderedTashkeelOccurrencesCount,
        short? OrderedTashkeelAyahsCount,
        short? OrderedTashkeelSurahsCount,
        int? OrderedSimpleOccurrencesCount,
        short? OrderedSimpleAyahsCount,
        short? OrderedSimpleSurahsCount,
        int? UniqueTashkeelRowId,
        int? UniqueTashkeelOccurrencesCount,
        short? UniqueTashkeelAyahsCount,
        short? UniqueTashkeelSurahsCount,
        int? UniqueSimpleRowId,
        int? UniqueSimpleOccurrencesCount,
        short? UniqueSimpleAyahsCount,
        short? UniqueSimpleSurahsCount,
        string? UniqueSimpleWordKeyImlaeiSimple);

    private sealed record SegmentProjection(
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
}
