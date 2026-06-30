using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

public sealed class EfWordTypesReader(QuranDashboardDbContext dbContext) : IWordTypesReader
{
    private const string NounType = "noun";
    private const string VerbType = "verb";
    private const string ParticleType = "particle";
    private const string InlType = "inl";
    private const string InlPos = "INL";
    private const string UnspecifiedContext = WordTypeRowContext.Unspecified;

    private readonly QuranDashboardDbContext _dbContext = dbContext;

    public async Task<WordTypeTreeDto> GetTreeAsync(CancellationToken cancellationToken)
    {
        var counts = await _dbContext.Database.SqlQueryRaw<TreeCountRow>(TreeCountsSql())
            .ToDictionaryAsync(row => row.Type, row => row.Count, cancellationToken);

        return new WordTypeTreeDto([
            MainNode(NounType, "اسم", counts.GetValueOrDefault(NounType), "case"),
            MainNode(VerbType, "فعل", counts.GetValueOrDefault(VerbType), "tense+voice"),
            MainNode(ParticleType, "حرف وأداة", counts.GetValueOrDefault(ParticleType), "none"),
            MainNode(InlType, "حروف مقطّعة", counts.GetValueOrDefault(InlType), "none"),
        ]);
    }

    public async Task<PagedResult<WordTypeRowDto>> GetRowsAsync(
        WordTypeFilter filter,
        WordTypeSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var type = NormalizeType(filter.Type);
        var totalCount = await CountRowsAsync(type, cancellationToken);
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<WordTypeRowDto>(page, pageSize, totalCount, []);
        }

        var rows = await _dbContext.Database.SqlQueryRaw<WordTypeRowSqlResult>(
            RowsSql(type, sort),
            new NpgsqlParameter<int>("skip", skip.Value),
            new NpgsqlParameter<int>("take", pageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<WordTypeRowDto>(
            page,
            pageSize,
            totalCount,
            rows.Select(row => row.ToDto()).ToList());
    }

    public async Task<WordTypeSummaryDto?> GetSummaryAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken)
    {
        if (!identity.IsValid)
        {
            return null;
        }

        var rows = await (
                from morphology in MatchedMorphologyQuery(identity)
                join word in _dbContext.QuranWords.AsNoTracking() on morphology.QuranWordId equals word.Id
                join uniqueWord in _dbContext.QuranWordsUniqueTashkeel.AsNoTracking()
                    on word.UniqueTashkeelWordId equals uniqueWord.Id
                join pos in _dbContext.PosTags.AsNoTracking() on morphology.HeadPos equals pos.Code
                join root in _dbContext.QuranRoots.AsNoTracking() on morphology.RootId equals root.Id into roots
                from root in roots.DefaultIfEmpty()
                join lemma in _dbContext.QuranLemmas.AsNoTracking() on morphology.LemmaId equals lemma.Id into lemmas
                from lemma in lemmas.DefaultIfEmpty()
                join stem in _dbContext.QuranStems.AsNoTracking() on morphology.StemId equals stem.Id into stems
                from stem in stems.DefaultIfEmpty()
                select new SummarySourceRow(
                    word.Id,
                    word.AyahId,
                    word.SurahNumber,
                    uniqueWord.TextUthmani,
                    morphology.HeadPos,
                    pos.ArabicLabel,
                    pos.Category,
                    morphology.VerbTense,
                    morphology.CaseFeature,
                    root != null ? root.RootText : null,
                    lemma != null ? lemma.LemmaText : null,
                    stem != null ? stem.StemText : null,
                    morphology.RootId,
                    morphology.LemmaId,
                    morphology.StemId))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return null;
        }

        var first = rows.OrderBy(row => row.QuranWordId).First();
        var typeLabel = ResolveTypeLabel(first, identity.ContextCode);
        var broadLabel = ResolveBroadLabelFromCategory(first.PosCategory, first.HeadPos);
        var caseOrFeature = WordTypeIdentityMatcher.IsVerbContextCode(identity.ContextCode)
            ? identity.ContextCode
            : first.CaseFeature;

        return new WordTypeSummaryDto(
            identity.TashkeelWordId,
            identity.ContextCode,
            first.DisplayText,
            new WordTypeLabelDto(typeLabel),
            new WordTypeLabelDto(broadLabel),
            caseOrFeature,
            SelectWinner(rows, row => row.RootText, row => row.RootId),
            SelectWinner(rows, row => row.LemmaText, row => row.LemmaId),
            SelectWinner(rows, row => row.StemText, row => row.StemId),
            rows.Count,
            rows.Select(row => row.AyahId).Distinct().Count(),
            rows.Select(row => row.SurahNumber).Distinct().Count());
    }

    public async Task<PagedResult<WordTypeAyahMatchDto>?> GetAyahMatchesAsync(
        WordTypeRowIdentity identity,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!identity.IsValid)
        {
            return null;
        }

        if (!await MatchedMorphologyQuery(identity).AnyAsync(cancellationToken))
        {
            return null;
        }

        var matchedAyahIds = MatchedMorphologyQuery(identity)
            .Join(
                _dbContext.QuranWords.AsNoTracking(),
                morphology => morphology.QuranWordId,
                word => word.Id,
                (_, word) => word.AyahId)
            .Distinct();

        var totalCount = await matchedAyahIds.CountAsync(cancellationToken);
        var skip = ReadPaging.CalculateSafeSkip(page, pageSize, totalCount);
        if (skip is null)
        {
            return new PagedResult<WordTypeAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var pageAyahs = await (
                from ayah in _dbContext.QuranAyahs.AsNoTracking()
                where matchedAyahIds.Contains(ayah.Id)
                orderby ayah.SurahNumber, ayah.AyahNumber
                select new AyahMetaRow(
                    ayah.Id,
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.AyahNumber,
                    ayah.TextUthmani))
            .Skip(skip.Value)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (pageAyahs.Count == 0)
        {
            return new PagedResult<WordTypeAyahMatchDto>(page, pageSize, totalCount, []);
        }

        var ayahIds = pageAyahs.Select(ayah => ayah.AyahId).ToList();

        var matchedRows = await MatchedMorphologyQuery(identity)
            .Join(
                _dbContext.QuranWords.AsNoTracking(),
                morphology => morphology.QuranWordId,
                word => word.Id,
                (_, word) => word)
            .Where(word => ayahIds.Contains(word.AyahId))
            .OrderBy(word => word.SurahNumber)
            .ThenBy(word => word.AyahNumber)
            .ThenBy(word => word.WordNumber)
            .ThenBy(word => word.Id)
            .Select(word => new MatchedWordRow(word.AyahId, word.Id, word.WordNumber))
            .ToListAsync(cancellationToken);

        var matchedIdsByAyah = matchedRows
            .GroupBy(row => row.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var wordsByAyah = await _dbContext.QuranWords
            .AsNoTracking()
            .Where(word => ayahIds.Contains(word.AyahId) && !word.IsAyahMarker)
            .OrderBy(word => word.SurahNumber)
            .ThenBy(word => word.AyahNumber)
            .ThenBy(word => word.WordNumber)
            .Select(word => new AyahWordRow(
                word.AyahId,
                word.Id,
                word.WordNumber,
                word.TextUthmani,
                word.IsAyahMarker))
            .ToListAsync(cancellationToken);

        var wordsGrouped = wordsByAyah
            .GroupBy(word => word.AyahId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var items = pageAyahs
            .Select(ayah =>
            {
                var words = wordsGrouped.GetValueOrDefault(ayah.AyahId, []);
                var matched = matchedIdsByAyah.GetValueOrDefault(ayah.AyahId, []);
                var matchedPositions = matched.Select(row => row.WordNumber).Distinct().OrderBy(number => number).ToList();

                return new WordTypeAyahMatchDto(
                    ayah.VerseKey,
                    ayah.SurahNumber,
                    ayah.AyahNumber,
                    ayah.AyahText,
                    matchedPositions,
                    matched.Select(row => row.QuranWordId).ToList(),
                    words.Select(word => new AyahWordForHighlightDto(
                        word.QuranWordId,
                        word.TextUthmani,
                        word.IsAyahMarker)).ToList());
            })
            .ToList();

        return new PagedResult<WordTypeAyahMatchDto>(page, pageSize, totalCount, items);
    }

    public async Task<WordTypeSurahsResponse?> GetSurahsAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken)
    {
        if (!identity.IsValid)
        {
            return null;
        }

        if (!await MatchedMorphologyQuery(identity).AnyAsync(cancellationToken))
        {
            return null;
        }

        var matchedWords = await MatchedMorphologyQuery(identity)
            .Join(
                _dbContext.QuranWords.AsNoTracking(),
                morphology => morphology.QuranWordId,
                word => word.Id,
                (_, word) => word.SurahNumber)
            .ToListAsync(cancellationToken);

        var surahGroups = matchedWords
            .GroupBy(surahNumber => surahNumber)
            .Select(group => new SurahOccurrenceRow(group.Key, group.Count()))
            .OrderBy(row => row.SurahNumber)
            .ToList();

        var mentionedNumbers = surahGroups.Select(row => row.SurahNumber).ToList();
        var surahNames = await _dbContext.QuranSurahs
            .AsNoTracking()
            .ToDictionaryAsync(surah => surah.SurahNumber, surah => surah.NameArabic, cancellationToken);

        var surahs = surahGroups
            .Select(row => new WordTypeSurahOccurrenceDto(
                (int)row.SurahNumber,
                surahNames[row.SurahNumber],
                row.OccurrencesCount))
            .ToList();

        var missingSurahs = await _dbContext.QuranSurahs
            .AsNoTracking()
            .Where(surah => !mentionedNumbers.Contains(surah.SurahNumber))
            .OrderBy(surah => surah.SurahNumber)
            .Select(surah => new WordTypeMissingSurahDto((int)surah.SurahNumber, surah.NameArabic))
            .ToListAsync(cancellationToken);

        return new WordTypeSurahsResponse(surahs, missingSurahs);
    }

    private IQueryable<Domain.Quran.Words.Morphology.WordMorphology> MatchedMorphologyQuery(WordTypeRowIdentity identity) =>
        from morphology in _dbContext.WordMorphologies.AsNoTracking()
        join word in _dbContext.QuranWords.AsNoTracking() on morphology.QuranWordId equals word.Id
        where !word.IsAyahMarker
            && word.UniqueTashkeelWordId == identity.TashkeelWordId
            && (
                (identity.ContextCode == "past"
                    || identity.ContextCode == "present"
                    || identity.ContextCode == "imperative"
                    || identity.ContextCode == UnspecifiedContext)
                    ? morphology.IsVerb
                        && (morphology.VerbTense ?? UnspecifiedContext) == identity.ContextCode
                    : morphology.HeadPos == identity.ContextCode)
            && (identity.Case == null
                || identity.Case == "all"
                || (identity.Case == "null" ? morphology.CaseFeature == null : morphology.CaseFeature == identity.Case))
            && (identity.Tense == null
                || identity.Tense == "all"
                || morphology.VerbTense == identity.Tense)
            && (identity.Voice == null
                || identity.Voice == "all"
                || morphology.VerbVoice == identity.Voice)
        select morphology;

    private async Task<int> CountRowsAsync(string type, CancellationToken cancellationToken)
    {
        var result = await _dbContext.Database.SqlQueryRaw<CountRow>(RowsCountSql(type))
            .SingleAsync(cancellationToken);

        return result.Count;
    }

    private static string TreeCountsSql() => $"""
        WITH base AS (
            {BaseRowsSql(null)}
        ), grouped AS (
            SELECT '{NounType}' AS type, tashkeel_word_id, head_pos AS context_code
            FROM base
            WHERE pos_category = '{NounType}'
            GROUP BY tashkeel_word_id, head_pos

            UNION ALL

            SELECT '{VerbType}' AS type, tashkeel_word_id, COALESCE(verb_tense, '{UnspecifiedContext}') AS context_code
            FROM base
            WHERE is_verb
            GROUP BY tashkeel_word_id, COALESCE(verb_tense, '{UnspecifiedContext}')

            UNION ALL

            SELECT '{ParticleType}' AS type, tashkeel_word_id, head_pos AS context_code
            FROM base
            WHERE pos_category = '{ParticleType}' AND head_pos <> '{InlPos}'
            GROUP BY tashkeel_word_id, head_pos

            UNION ALL

            SELECT '{InlType}' AS type, tashkeel_word_id, head_pos AS context_code
            FROM base
            WHERE head_pos = '{InlPos}'
            GROUP BY tashkeel_word_id, head_pos
        )
        SELECT type AS "{nameof(TreeCountRow.Type)}", COUNT(*)::int AS "{nameof(TreeCountRow.Count)}"
        FROM grouped
        GROUP BY type
        """;

    private static string RowsCountSql(string type) => $"""
        WITH base AS (
            {BaseRowsSql(type)}
        ), grouped AS (
            SELECT tashkeel_word_id, {ContextExpression(type)} AS context_code
            FROM base
            GROUP BY tashkeel_word_id, {ContextExpression(type)}
        )
        SELECT COUNT(*)::int AS "{nameof(CountRow.Count)}"
        FROM grouped
        """;

    private static string RowsSql(string type, WordTypeSort sort) => $"""
        WITH base AS (
            {BaseRowsSql(type)}
        ), grouped AS (
            SELECT
                tashkeel_word_id,
                {ContextExpression(type)} AS context_code,
                MIN(display_text) AS display_text,
                {TypeCodeExpression(type)} AS type_code,
                MIN({TypeLabelExpression(type)}) AS type_label,
                MIN(quran_word_id) AS first_word_order_in_mushaf,
                COUNT(*)::int AS occurrences_count,
                COUNT(DISTINCT ayah_id)::int AS ayahs_count,
                COUNT(DISTINCT surah_number)::int AS surahs_count
            FROM base
            GROUP BY tashkeel_word_id, {ContextExpression(type)}, {TypeCodeExpression(type)}
        ), root_candidates AS (
            SELECT
                tashkeel_word_id,
                {ContextExpression(type)} AS context_code,
                root_id,
                MIN(root_text) AS root_text,
                COUNT(*) AS occurrence_count,
                MIN(quran_word_id) AS first_word_order
            FROM base
            WHERE root_id IS NOT NULL
            GROUP BY tashkeel_word_id, {ContextExpression(type)}, root_id
        ), root_winners AS (
            SELECT DISTINCT ON (tashkeel_word_id, context_code)
                tashkeel_word_id,
                context_code,
                root_text
            FROM root_candidates
            ORDER BY tashkeel_word_id, context_code, occurrence_count DESC, first_word_order, root_id
        ), lemma_candidates AS (
            SELECT
                tashkeel_word_id,
                {ContextExpression(type)} AS context_code,
                lemma_id,
                MIN(lemma_text) AS lemma_text,
                COUNT(*) AS occurrence_count,
                MIN(quran_word_id) AS first_word_order
            FROM base
            WHERE lemma_id IS NOT NULL
            GROUP BY tashkeel_word_id, {ContextExpression(type)}, lemma_id
        ), lemma_winners AS (
            SELECT DISTINCT ON (tashkeel_word_id, context_code)
                tashkeel_word_id,
                context_code,
                lemma_text
            FROM lemma_candidates
            ORDER BY tashkeel_word_id, context_code, occurrence_count DESC, first_word_order, lemma_id
        ), stem_candidates AS (
            SELECT
                tashkeel_word_id,
                {ContextExpression(type)} AS context_code,
                stem_id,
                MIN(stem_text) AS stem_text,
                COUNT(*) AS occurrence_count,
                MIN(quran_word_id) AS first_word_order
            FROM base
            WHERE stem_id IS NOT NULL
            GROUP BY tashkeel_word_id, {ContextExpression(type)}, stem_id
        ), stem_winners AS (
            SELECT DISTINCT ON (tashkeel_word_id, context_code)
                tashkeel_word_id,
                context_code,
                stem_text
            FROM stem_candidates
            ORDER BY tashkeel_word_id, context_code, occurrence_count DESC, first_word_order, stem_id
        )
        SELECT
            g.tashkeel_word_id AS "{nameof(WordTypeRowSqlResult.TashkeelWordId)}",
            g.context_code AS "{nameof(WordTypeRowSqlResult.ContextCode)}",
            g.display_text AS "{nameof(WordTypeRowSqlResult.DisplayText)}",
            g.type_code AS "{nameof(WordTypeRowSqlResult.TypeCode)}",
            g.type_label AS "{nameof(WordTypeRowSqlResult.TypeLabel)}",
            '{ResolveBroadLabel(type)}' AS "{nameof(WordTypeRowSqlResult.BroadLabel)}",
            {CaseOrFeatureSelect(type)} AS "{nameof(WordTypeRowSqlResult.CaseOrFeature)}",
            root_winners.root_text AS "{nameof(WordTypeRowSqlResult.RootText)}",
            lemma_winners.lemma_text AS "{nameof(WordTypeRowSqlResult.LemmaText)}",
            stem_winners.stem_text AS "{nameof(WordTypeRowSqlResult.StemText)}",
            g.occurrences_count AS "{nameof(WordTypeRowSqlResult.OccurrencesCount)}",
            g.ayahs_count AS "{nameof(WordTypeRowSqlResult.AyahsCount)}",
            g.surahs_count AS "{nameof(WordTypeRowSqlResult.SurahsCount)}",
            g.first_word_order_in_mushaf AS "{nameof(WordTypeRowSqlResult.FirstWordOrderInMushaf)}"
        FROM grouped g
        LEFT JOIN root_winners ON root_winners.tashkeel_word_id = g.tashkeel_word_id AND root_winners.context_code = g.context_code
        LEFT JOIN lemma_winners ON lemma_winners.tashkeel_word_id = g.tashkeel_word_id AND lemma_winners.context_code = g.context_code
        LEFT JOIN stem_winners ON stem_winners.tashkeel_word_id = g.tashkeel_word_id AND stem_winners.context_code = g.context_code
        ORDER BY {OrderBy(sort)}
        OFFSET @skip LIMIT @take
        """;

    private static string BaseRowsSql(string? type) => $"""
        SELECT
            w.id AS quran_word_id,
            w.ayah_id,
            w.surah_number,
            w.ayah_number,
            w.word_number,
            w.unique_tashkeel_word_id AS tashkeel_word_id,
            unique_word.text_uthmani AS display_text,
            m.head_pos,
            m.is_verb,
            m.verb_tense,
            m.verb_voice,
            m.case_feature,
            m.root_id,
            root.root_text,
            m.lemma_id,
            lemma.lemma_text,
            m.stem_id,
            stem.stem_text,
            pos.arabic_label AS pos_label,
            pos.category AS pos_category
        FROM quran_word_morphology m
        JOIN quran_words w ON w.id = m.quran_word_id
        JOIN quran_words_unique_tashkeel unique_word ON unique_word.id = w.unique_tashkeel_word_id
        JOIN quran_pos_tags pos ON pos.code = m.head_pos
        LEFT JOIN quran_roots root ON root.id = m.root_id
        LEFT JOIN quran_lemmas lemma ON lemma.id = m.lemma_id
        LEFT JOIN quran_stems stem ON stem.id = m.stem_id
        WHERE NOT w.is_ayah_marker
            AND w.unique_tashkeel_word_id IS NOT NULL
            {TypePredicate(type)}
        """;

    private static string TypePredicate(string? type) => type switch
    {
        NounType => $"AND pos.category = '{NounType}'",
        VerbType => "AND m.is_verb",
        ParticleType => $"AND pos.category = '{ParticleType}' AND m.head_pos <> '{InlPos}'",
        InlType => $"AND m.head_pos = '{InlPos}'",
        _ => string.Empty,
    };

    private static string ContextExpression(string type) => type == VerbType
        ? $"COALESCE(verb_tense, '{UnspecifiedContext}')"
        : "head_pos";

    private static string TypeCodeExpression(string type) => type == VerbType
        ? $"COALESCE(verb_tense, '{UnspecifiedContext}')"
        : "head_pos";

    private static string TypeLabelExpression(string type) => type == VerbType
        ? $"CASE verb_tense WHEN 'past' THEN 'ماض' WHEN 'present' THEN 'مضارع' WHEN 'imperative' THEN 'أمر' ELSE pos_label END"
        : "pos_label";

    private static string CaseOrFeatureSelect(string type) => type == VerbType
        ? "g.context_code"
        : "NULL::text";

    private static string OrderBy(WordTypeSort sort) => sort switch
    {
        WordTypeSort.Ayahs => "g.ayahs_count DESC, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        WordTypeSort.Surahs => "g.surahs_count DESC, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        WordTypeSort.MushafOrder => "g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        WordTypeSort.Alpha => "g.display_text, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        _ => "g.occurrences_count DESC, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
    };

    private static WordTypeTreeNodeDto MainNode(string code, string label, int count, string secondaryKind) =>
        new(
            code,
            new WordTypeLabelDto(label),
            count,
            new WordTypeSecondaryFilterDto(secondaryKind, SecondaryOptions(secondaryKind), VoiceOptions(secondaryKind)),
            []);

    private static IReadOnlyList<WordTypeFilterOptionDto> SecondaryOptions(string secondaryKind) => secondaryKind switch
    {
        "case" => [
            Option("nominative", "مرفوع"),
            Option("accusative", "منصوب"),
            Option("genitive", "مجرور"),
            Option("null", "غير محدد"),
        ],
        "tense+voice" => [
            Option("past", "ماض"),
            Option("present", "مضارع"),
            Option("imperative", "أمر"),
        ],
        _ => [],
    };

    private static IReadOnlyList<WordTypeFilterOptionDto> VoiceOptions(string secondaryKind) =>
        secondaryKind == "tense+voice"
            ? [Option("active", "معلوم"), Option("passive", "مجهول")]
            : [];

    private static WordTypeFilterOptionDto Option(string code, string label) =>
        new(code, new WordTypeLabelDto(label));

    private static string? SelectWinner<TId>(
        IReadOnlyList<SummarySourceRow> rows,
        Func<SummarySourceRow, string?> textSelector,
        Func<SummarySourceRow, TId?> idSelector)
        where TId : struct
    {
        var candidates = rows
            .Where(row => idSelector(row) is not null && textSelector(row) is not null)
            .GroupBy(row => idSelector(row)!.Value)
            .Select(group => new
            {
                Text = group.Min(row => textSelector(row))!,
                Count = group.Count(),
                FirstWordOrder = group.Min(row => row.QuranWordId),
                Id = group.Key,
            })
            .OrderByDescending(candidate => candidate.Count)
            .ThenBy(candidate => candidate.FirstWordOrder)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefault();

        return candidates?.Text;
    }

    private static string ResolveTypeLabel(SummarySourceRow row, string contextCode) =>
        WordTypeIdentityMatcher.IsVerbContextCode(contextCode)
            ? contextCode switch
            {
                "past" => "ماض",
                "present" => "مضارع",
                "imperative" => "أمر",
                _ => row.PosLabel,
            }
            : row.PosLabel;

    private static string ResolveBroadLabelFromCategory(string category, string headPos) => category switch
    {
        VerbType => "فعل",
        ParticleType when headPos == InlPos => "حروف مقطّعة",
        ParticleType => "حرف وأداة",
        _ => "اسم",
    };

    private static string NormalizeType(string? type) => string.IsNullOrWhiteSpace(type) ? NounType : type.Trim().ToLowerInvariant();

    private static string ResolveBroadLabel(string type) => type switch
    {
        VerbType => "فعل",
        ParticleType => "حرف وأداة",
        InlType => "حروف مقطّعة",
        _ => "اسم",
    };

    private sealed record SummarySourceRow(
        int QuranWordId,
        int AyahId,
        int SurahNumber,
        string DisplayText,
        string HeadPos,
        string PosLabel,
        string PosCategory,
        string? VerbTense,
        string? CaseFeature,
        string? RootText,
        string? LemmaText,
        string? StemText,
        int? RootId,
        int? LemmaId,
        int? StemId);

    private sealed record AyahMetaRow(int AyahId, string VerseKey, int SurahNumber, int AyahNumber, string AyahText);

    private sealed record MatchedWordRow(int AyahId, int QuranWordId, int WordNumber);

    private sealed record AyahWordRow(int AyahId, int QuranWordId, int WordNumber, string TextUthmani, bool IsAyahMarker);

    private sealed record SurahOccurrenceRow(short SurahNumber, int OccurrencesCount);

    private sealed record TreeCountRow(string Type, int Count);

    private sealed record CountRow(int Count);

    private sealed record WordTypeRowSqlResult(
        int TashkeelWordId,
        string ContextCode,
        string DisplayText,
        string TypeCode,
        string TypeLabel,
        string BroadLabel,
        string? CaseOrFeature,
        string? RootText,
        string? LemmaText,
        string? StemText,
        int OccurrencesCount,
        int AyahsCount,
        int SurahsCount,
        int FirstWordOrderInMushaf)
    {
        public WordTypeRowDto ToDto() => new(
            TashkeelWordId,
            ContextCode,
            DisplayText,
            TypeCode,
            new WordTypeLabelDto(TypeLabel),
            new WordTypeLabelDto(BroadLabel),
            CaseOrFeature,
            RootText,
            LemmaText,
            StemText,
            OccurrencesCount,
            AyahsCount,
            SurahsCount);
    }
}
