using System.Text.Json;
using QuranDashboard.Application.Abstractions.Quran.MushafReader;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.MushafReader;

public sealed class EfWordAnalysisReader(QuranDashboardDbContext db) : IWordAnalysisReader
{
    private static readonly JsonSerializerOptions FeaturesJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<WordAnalysisOutcome> GetWordAnalysisAsync(string wordLocation, CancellationToken ct)
    {
        var word = await db.QuranWords
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Location == wordLocation, ct);

        if (word is null)
        {
            return new WordAnalysisOutcome.NotFound();
        }

        if (word.IsAyahMarker)
        {
            return new WordAnalysisOutcome.NotAnalyzable();
        }

        var morphology = await db.WordMorphologies
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.QuranWordId == word.Id, ct);

        var orderedTashkeel = await db.QuranWordsOrderedTashkeel
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.QuranWordId == word.Id, ct);

        var orderedSimple = await db.QuranWordsOrderedSimple
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.QuranWordId == word.Id, ct);

        var uniqueTashkeel = word.UniqueTashkeelWordId is int tashkeelId
            ? await db.QuranWordsUniqueTashkeel
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == tashkeelId, ct)
            : null;

        var uniqueSimple = word.UniqueSimpleWordId is int simpleId
            ? await db.QuranWordsUniqueSimple
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == simpleId, ct)
            : null;

        if (morphology is null
            || orderedTashkeel is null
            || orderedSimple is null
            || word.UniqueTashkeelWordId is null
            || uniqueTashkeel is null
            || word.UniqueSimpleWordId is null
            || uniqueSimple is null)
        {
            return new WordAnalysisOutcome.IncompleteData();
        }

        var segments = await db.WordMorphologySegments
            .AsNoTracking()
            .Where(s => s.QuranWordId == word.Id)
            .OrderBy(s => s.SegmentNumber)
            .ToListAsync(ct);

        if (segments.Count == 0)
        {
            return new WordAnalysisOutcome.IncompleteData();
        }

        var posCodes = new HashSet<string>(StringComparer.Ordinal);
        posCodes.Add(morphology.HeadPos);

        foreach (var segment in segments)
        {
            posCodes.Add(segment.Pos);
        }

        var posTags = await db.PosTags
            .AsNoTracking()
            .Where(p => posCodes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code, ct);

        QuranDashboard.Domain.Quran.Words.Morphology.QuranRoot? root = null;
        QuranDashboard.Domain.Quran.Words.Morphology.QuranLemma? lemma = null;
        QuranDashboard.Domain.Quran.Words.Morphology.QuranStem? stem = null;

        if (morphology.RootId is int rootId)
        {
            root = await db.QuranRoots.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rootId, ct);
        }

        if (morphology.LemmaId is int lemmaId)
        {
            lemma = await db.QuranLemmas.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lemmaId, ct);
        }

        if (morphology.StemId is int stemId)
        {
            stem = await db.QuranStems.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stemId, ct);
        }

        var ruleIds = segments
            .Where(s => s.I3rabRuleId is not null)
            .Select(s => s.I3rabRuleId!.Value)
            .Distinct()
            .ToList();

        var i3rabRules = ruleIds.Count == 0
            ? new Dictionary<int, QuranDashboard.Domain.Quran.Words.Morphology.Irab.QuranI3rabRule>()
            : await db.QuranI3rabRules
                .AsNoTracking()
                .Where(r => ruleIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, ct);

        var response = new WordAnalysisResponse(
            MapWord(word),
            MapIdentity(orderedTashkeel, orderedSimple, uniqueTashkeel, uniqueSimple),
            MapMorphology(morphology, root, lemma, stem, posTags),
            MapSegments(segments, posTags, i3rabRules));

        return new WordAnalysisOutcome.Found(response);
    }

    private static WordOccurrenceDto MapWord(Domain.Quran.Words.QuranWord word) => new(
        word.Id,
        word.Location,
        $"{word.SurahNumber}:{word.AyahNumber}",
        word.SurahNumber,
        word.AyahNumber,
        word.WordNumber,
        word.PageNumber,
        word.LineNumber,
        word.LineWordOrder,
        word.TextUthmani,
        word.TextUthmaniSimple,
        word.TextImlaeiSimple,
        word.QpcGlyph);

    private static WordIdentityDto MapIdentity(
        Domain.Quran.Words.Display.OrderedTashkeelWord orderedTashkeel,
        Domain.Quran.Words.Display.OrderedSimpleWord orderedSimple,
        Domain.Quran.Words.Display.UniqueTashkeelWord uniqueTashkeel,
        Domain.Quran.Words.Display.UniqueSimpleWord uniqueSimple) => new(
        new WordCountSummary(
            orderedTashkeel.OccurrencesCount,
            orderedTashkeel.AyahsCount,
            orderedTashkeel.SurahsCount),
        new WordCountSummary(
            orderedSimple.OccurrencesCount,
            orderedSimple.AyahsCount,
            orderedSimple.SurahsCount),
        new UniqueWordCountSummary(
            uniqueTashkeel.Id,
            uniqueTashkeel.OccurrencesCount,
            uniqueTashkeel.AyahsCount,
            uniqueTashkeel.SurahsCount),
        new UniqueSimpleWordCountSummary(
            uniqueSimple.Id,
            uniqueSimple.OccurrencesCount,
            uniqueSimple.AyahsCount,
            uniqueSimple.SurahsCount,
            uniqueSimple.WordKeyImlaeiSimple));

    private static WordMorphologyDto MapMorphology(
        Domain.Quran.Words.Morphology.WordMorphology morphology,
        Domain.Quran.Words.Morphology.QuranRoot? root,
        Domain.Quran.Words.Morphology.QuranLemma? lemma,
        Domain.Quran.Words.Morphology.QuranStem? stem,
        IReadOnlyDictionary<string, Domain.Quran.Words.Morphology.PosTag> posTags)
    {
        posTags.TryGetValue(morphology.HeadPos, out var headPosTag);

        return new WordMorphologyDto(
            morphology.HeadPos,
            new LocalizedLabel(
                headPosTag?.ArabicLabel ?? morphology.HeadPos,
                headPosTag?.EnglishLabel ?? morphology.HeadPos),
            root is null
                ? null
                : new WordMorphologyRoot(root.Id, root.RootText, root.RootBuckwalter),
            lemma is null
                ? null
                : new WordMorphologyLemma(lemma.Id, lemma.LemmaText, lemma.LemmaBuckwalter),
            stem is null
                ? null
                : new WordMorphologyStem(stem.Id, stem.StemText),
            morphology.IsVerb,
            morphology.VerbTense,
            morphology.VerbVoice,
            morphology.CaseFeature);
    }

    private static IReadOnlyList<RenderedSegmentDto> MapSegments(
        IReadOnlyList<Domain.Quran.Words.Morphology.WordMorphologySegment> segments,
        IReadOnlyDictionary<string, Domain.Quran.Words.Morphology.PosTag> posTags,
        IReadOnlyDictionary<int, Domain.Quran.Words.Morphology.Irab.QuranI3rabRule> i3rabRules)
    {
        var rendered = new List<RenderedSegmentDto>(segments.Count);

        foreach (var segment in segments)
        {
            posTags.TryGetValue(segment.Pos, out var posTag);
            i3rabRules.TryGetValue(segment.I3rabRuleId ?? -1, out var rule);

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
                    posTag?.ArabicLabel ?? segment.Pos,
                    posTag?.EnglishLabel ?? segment.Pos),
                segment.I3rabArabic,
                segment.I3rabRuleId,
                rule?.SignatureKey,
                rule?.RuleFamily,
                segment.I3rabStatus,
                MapSegmentFeatures(segment)));
        }

        return rendered;
    }

    private static SegmentFeaturesDto? MapSegmentFeatures(
        Domain.Quran.Words.Morphology.WordMorphologySegment segment)
    {
        if (string.IsNullOrWhiteSpace(segment.FeaturesRaw) && string.IsNullOrWhiteSpace(segment.FeaturesJson))
        {
            return null;
        }

        return new SegmentFeaturesDto(segment.FeaturesRaw, ParseFeaturesJson(segment.FeaturesJson));
    }

    private static IReadOnlyList<JsonElement> ParseFeaturesJson(string? featuresJson)
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
        catch (JsonException)
        {
            return [];
        }
    }
}
