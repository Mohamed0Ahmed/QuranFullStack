using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

public sealed partial class EfWordTypesReader
{
    private static string TreeChildCountsSql()
    {
        var unscoped = WordTypeReadContext.Unscoped;
        return $"""
        WITH base AS (
            {BaseRowsSql(unscoped)}
        ), noun_children AS (
            SELECT '{NounType}' AS type, head_pos AS child_code, tashkeel_word_id
            FROM base
            WHERE pos_category = '{NounType}'
            GROUP BY head_pos, tashkeel_word_id
        ), verb_children AS (
            SELECT '{VerbType}' AS type, COALESCE(verb_tense, '{UnspecifiedContext}') AS child_code, tashkeel_word_id
            FROM base
            WHERE is_verb
            GROUP BY COALESCE(verb_tense, '{UnspecifiedContext}'), tashkeel_word_id
        ), particle_children AS (
            SELECT '{ParticleType}' AS type, head_pos AS child_code, tashkeel_word_id
            FROM base
            WHERE pos_category = '{ParticleType}' AND head_pos <> '{InlPos}'
            GROUP BY head_pos, tashkeel_word_id
        ), inl_rows AS (
            SELECT '{InlType}' AS type, '{InlPos}' AS child_code, tashkeel_word_id
            FROM base
            WHERE head_pos = '{InlPos}'
            GROUP BY tashkeel_word_id
        ), all_children AS (
            SELECT * FROM noun_children
            UNION ALL
            SELECT * FROM verb_children
            UNION ALL
            SELECT * FROM particle_children
            UNION ALL
            SELECT * FROM inl_rows
        )
        SELECT type AS "{nameof(TreeChildCountRow.Type)}", child_code AS "{nameof(TreeChildCountRow.ChildCode)}", COUNT(*)::int AS "{nameof(TreeChildCountRow.Count)}"
        FROM all_children
        GROUP BY type, child_code
        """;
    }

    // groupedDimension is null for the Words table/list reads and set only for grouped member-word
    // reads, where it adds an allowlisted numeric root_id|stem_id|lemma_id = @dimensionId predicate to
    // the shared base. Everything after the base — the (tashkeel_word_id, context_code) grouping — is
    // identical, guaranteeing row-for-row Words-table parity.
    private static string RowsCountSql(WordTypeReadContext context, WordTypeGroupedDimensionKind? groupedDimension = null) => $"""
        WITH base AS (
            {BaseRowsSql(context, groupedDimension)}
        ), grouped AS (
            SELECT tashkeel_word_id, {ContextExpression(context)} AS context_code
            FROM base
            GROUP BY tashkeel_word_id, {ContextExpression(context)}
        )
        SELECT COUNT(*)::int AS "{nameof(CountRow.Count)}"
        FROM grouped
        """;

    private static string RowsSql(WordTypeReadContext context, string sortToken, WordTypeGroupedDimensionKind? groupedDimension = null) => $"""
        WITH base AS (
            {BaseRowsSql(context, groupedDimension)}
        ), grouped AS (
            SELECT
                tashkeel_word_id,
                {ContextExpression(context)} AS context_code,
                MIN(display_text) AS display_text,
                {TypeCodeExpression(context)} AS type_code,
                MIN({TypeLabelExpression(context)}) AS type_label,
                MIN(quran_word_id) AS first_word_order_in_mushaf,
                COUNT(*)::int AS occurrences_count,
                COUNT(DISTINCT ayah_id)::int AS ayahs_count,
                COUNT(DISTINCT surah_number)::int AS surahs_count
            FROM base
            GROUP BY tashkeel_word_id, {ContextExpression(context)}, {TypeCodeExpression(context)}
        ), root_candidates AS (
            SELECT
                tashkeel_word_id,
                {ContextExpression(context)} AS context_code,
                root_id,
                MIN(root_text) AS root_text,
                COUNT(*) AS occurrence_count,
                MIN(quran_word_id) AS first_word_order
            FROM base
            WHERE root_id IS NOT NULL
            GROUP BY tashkeel_word_id, {ContextExpression(context)}, root_id
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
                {ContextExpression(context)} AS context_code,
                lemma_id,
                MIN(lemma_text) AS lemma_text,
                COUNT(*) AS occurrence_count,
                MIN(quran_word_id) AS first_word_order
            FROM base
            WHERE lemma_id IS NOT NULL
            GROUP BY tashkeel_word_id, {ContextExpression(context)}, lemma_id
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
                {ContextExpression(context)} AS context_code,
                stem_id,
                MIN(stem_text) AS stem_text,
                COUNT(*) AS occurrence_count,
                MIN(quran_word_id) AS first_word_order
            FROM base
            WHERE stem_id IS NOT NULL
            GROUP BY tashkeel_word_id, {ContextExpression(context)}, stem_id
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
            '{ResolveBroadLabel(context.Type)}' AS "{nameof(WordTypeRowSqlResult.BroadLabel)}",
            {CaseOrFeatureSelect(context)} AS "{nameof(WordTypeRowSqlResult.CaseOrFeature)}",
            root_winners.root_text AS "{nameof(WordTypeRowSqlResult.RootText)}",
            lemma_winners.lemma_text AS "{nameof(WordTypeRowSqlResult.LemmaText)}",
            stem_winners.stem_text AS "{nameof(WordTypeRowSqlResult.StemText)}",
            g.occurrences_count AS "{nameof(WordTypeRowSqlResult.OccurrencesCount)}",
            g.ayahs_count AS "{nameof(WordTypeRowSqlResult.AyahsCount)}",
            g.surahs_count AS "{nameof(WordTypeRowSqlResult.SurahsCount)}",
            g.first_word_order_in_mushaf AS "{nameof(WordTypeRowSqlResult.FirstWordOrderInMushaf)}",
            -- Window count over the scoped grouped set (1:1 winner joins never fan out), so page + total
            -- come from ONE command; equals RowsCountSql's COUNT(*) FROM grouped for the identical scope.
            COUNT(*) OVER()::int AS "{nameof(WordTypeRowSqlResult.TotalCount)}"
        FROM grouped g
        LEFT JOIN root_winners ON root_winners.tashkeel_word_id = g.tashkeel_word_id AND root_winners.context_code = g.context_code
        LEFT JOIN lemma_winners ON lemma_winners.tashkeel_word_id = g.tashkeel_word_id AND lemma_winners.context_code = g.context_code
        LEFT JOIN stem_winners ON stem_winners.tashkeel_word_id = g.tashkeel_word_id AND stem_winners.context_code = g.context_code
        ORDER BY {OrderBy(sortToken)}
        OFFSET @skip LIMIT @take
        """;

    internal static string BaseRowsSql(WordTypeReadContext context, WordTypeGroupedDimensionKind? groupedDimension = null) => $"""
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
            {TypePredicate(context)}
            {SecondaryFilterPredicate(context)}
            {SearchPredicate(context)}
            {PresenceFilterPredicate(context)}
            {GroupedDimensionPredicate(groupedDimension)}
        """;

    // Word-identity search reuses the unique-tashkeel join the base already carries and matches the
    // same computed identity-search column Unique Words search uses (research R2 equivalence): the
    // GIN-trgm-indexed search_text_normalized (a fold of text_uthmani_simple + text_imlaei_simple).
    // Identity text only — never root/stem/lemma display text. The term is a parameter value, never
    // interpolated; the tree (Unscoped) and grouped-detail contexts carry no search so this is empty
    // for them, keeping the base byte-for-byte unchanged for existing callers.
    private static string SearchPredicate(WordTypeReadContext context) =>
        context.HasSearch
            ? "AND unique_word.search_text_normalized ILIKE @searchPattern"
            : string.Empty;

    // Tri-state presence flags (Feature 026, US6) narrow the shared base by whether the head morphology
    // row carries a root/stem/lemma. Only the allowlisted numeric id columns and the IS [NOT] NULL
    // operators appear — no user text, no parameters. Absent flags (list/table pre-feature callers,
    // tree Unscoped, grouped-detail contexts) emit nothing, keeping the base byte-for-byte unchanged.
    // Qualified with the morphology alias (m.) because the base FROM also joins quran_lemmas.
    private static string PresenceFilterPredicate(WordTypeReadContext context)
    {
        var fragments = new List<string>(3);

        if (context.HasRoot is { } hasRoot)
        {
            fragments.Add(hasRoot ? "m.root_id IS NOT NULL" : "m.root_id IS NULL");
        }

        if (context.HasStem is { } hasStem)
        {
            fragments.Add(hasStem ? "m.stem_id IS NOT NULL" : "m.stem_id IS NULL");
        }

        if (context.HasLemma is { } hasLemma)
        {
            fragments.Add(hasLemma ? "m.lemma_id IS NOT NULL" : "m.lemma_id IS NULL");
        }

        return fragments.Count == 0 ? string.Empty : "AND " + string.Join(" AND ", fragments);
    }

    // Grouped member/detail reads restrict the shared base to a single numeric head dimension. Only the
    // allowlisted id column may appear; the *_text columns are projection-only and never a membership
    // predicate. Null (list/table reads) emits nothing, so the base stays byte-for-byte semantically
    // unchanged for existing callers.
    // Qualified with the morphology alias (m.) because the base FROM also joins quran_lemmas, which
    // carries its own root_id; an unqualified column would be ambiguous.
    private static string GroupedDimensionPredicate(WordTypeGroupedDimensionKind? groupedDimension) =>
        groupedDimension is null
            ? string.Empty
            : $"AND m.{GroupedDimensionColumns(groupedDimension).IdColumn} = @dimensionId";

    private static string TypePredicate(WordTypeReadContext context)
    {
        var typePredicate = context.Type switch
        {
            NounType => $"AND pos.category = '{NounType}'",
            VerbType => "AND m.is_verb",
            ParticleType => $"AND pos.category = '{ParticleType}' AND m.head_pos <> '{InlPos}'",
            InlType => $"AND m.head_pos = '{InlPos}'",
            _ => string.Empty,
        };

        return context.HasChildCode
            ? $"{typePredicate} AND {ChildCodePredicate(context)}"
            : typePredicate;
    }

    private static string ChildCodePredicate(WordTypeReadContext context) => context.Type switch
    {
        NounType or ParticleType => "m.head_pos = @childCode",
        VerbType => $"COALESCE(m.verb_tense, '{UnspecifiedContext}') = @childCode",
        _ => "FALSE",
    };

    // Secondary filters narrow base rows in place. Case applies only to nouns; tense/voice only to
    // verbs. "null" is a real case value meaning غير محدد (NULL case_feature). The "all" sentinel is
    // treated as no filter by the context and never reaches this predicate.
    private static string SecondaryFilterPredicate(WordTypeReadContext context)
    {
        var fragments = new List<string>(3);

        if (context.HasCaseFilter)
        {
            fragments.Add(context.Case == "null"
                ? "m.case_feature IS NULL"
                : "m.case_feature = @caseFilter");
        }

        if (context.HasTenseFilter)
        {
            fragments.Add("m.verb_tense = @tenseFilter");
        }

        if (context.HasVoiceFilter)
        {
            fragments.Add("m.verb_voice = @voiceFilter");
        }

        return fragments.Count == 0 ? string.Empty : "AND " + string.Join(" AND ", fragments);
    }

    // dimensionId is bound only for grouped member-word reads (where BaseRowsSql emits the numeric
    // predicate). List/table callers pass null and the @dimensionId parameter is never added.
    private static object[] BuildRowsParameters(WordTypeReadContext context, int skip, int take, int? dimensionId = null)
    {
        var parameters = new List<object>
        {
            new NpgsqlParameter<int>("skip", skip),
            new NpgsqlParameter<int>("take", take),
        };
        AddChildCodeParameter(context, parameters);
        AddSecondaryFilterParameters(context, parameters);
        AddSearchParameter(context, parameters);
        AddDimensionParameter(dimensionId, parameters);
        return [.. parameters];
    }

    private static object[] BuildCountParameters(WordTypeReadContext context, int? dimensionId = null)
    {
        var parameters = new List<object>();
        AddChildCodeParameter(context, parameters);
        AddSecondaryFilterParameters(context, parameters);
        AddSearchParameter(context, parameters);
        AddDimensionParameter(dimensionId, parameters);
        return [.. parameters];
    }

    private static void AddDimensionParameter(int? dimensionId, List<object> parameters)
    {
        if (dimensionId is not null)
        {
            parameters.Add(new NpgsqlParameter<int>("dimensionId", dimensionId.Value));
        }
    }

    private static void AddChildCodeParameter(WordTypeReadContext context, List<object> parameters)
    {
        if (context.HasChildCode)
        {
            parameters.Add(new NpgsqlParameter<string>("childCode", context.ChildCode!));
        }
    }

    // Each secondary filter parameter is added only when the corresponding predicate is emitted so the
    // raw SQL and the parameter list stay in sync (Npgsql rejects unused/unbound names).
    private static void AddSecondaryFilterParameters(WordTypeReadContext context, List<object> parameters)
    {
        if (context.HasCaseFilter && context.Case != "null")
        {
            parameters.Add(new NpgsqlParameter<string>("caseFilter", context.Case!));
        }

        if (context.HasTenseFilter)
        {
            parameters.Add(new NpgsqlParameter<string>("tenseFilter", context.Tense!));
        }

        if (context.HasVoiceFilter)
        {
            parameters.Add(new NpgsqlParameter<string>("voiceFilter", context.Voice!));
        }
    }

    // Context.Search is already normalized; the parameter is the escaped %contains% pattern, matching
    // the Unique Words reader's `%EscapeLikePattern(normalized)%` + ILIKE shape exactly. Added only when
    // the SearchPredicate is emitted so the SQL and parameter list stay in sync.
    private static void AddSearchParameter(WordTypeReadContext context, List<object> parameters)
    {
        if (context.HasSearch)
        {
            var pattern = $"%{ArabicSearchQueryNormalizer.EscapeLikePattern(context.Search!)}%";
            parameters.Add(new NpgsqlParameter<string>("searchPattern", pattern));
        }
    }

    private static string ContextExpression(WordTypeReadContext context) => context.Type == VerbType
        ? $"COALESCE(verb_tense, '{UnspecifiedContext}')"
        : "head_pos";

    private static string TypeCodeExpression(WordTypeReadContext context) => context.Type == VerbType
        ? $"COALESCE(verb_tense, '{UnspecifiedContext}')"
        : "head_pos";

    private static string TypeLabelExpression(WordTypeReadContext context) => context.Type == VerbType
        ? $"CASE verb_tense WHEN 'past' THEN 'ماض' WHEN 'present' THEN 'مضارع' WHEN 'imperative' THEN 'أمر' ELSE pos_label END"
        : "pos_label";

    private static string CaseOrFeatureSelect(WordTypeReadContext context)
    {
        // Verb rows always resolve to their tense context code. Noun rows only carry a case value
        // when a secondary case filter pins the whole row to it (or to NULL for غير محدد); under the
        // unfiltered noun parent a row aggregates multiple cases, so it stays null rather than unioning.
        if (context.Type == VerbType)
        {
            return "g.context_code";
        }

        if (context.Type == NounType && context.HasCaseFilter)
        {
            // context.Case is allowlist-validated before SQL emission (same as type/child discriminators
            // interpolated elsewhere in this file); the WHERE clause parameterizes @caseFilter instead.
            return context.Case == "null" ? "NULL::text" : $"'{context.Case}'::text";
        }

        return "NULL::text";
    }

    // Words-view ORDER BY. Every arm returns a compiler-known constant selected by the canonical sort
    // token, so no request text ever reaches the SQL string. The
    // per-view tie chain (Mushaf order, then the identity pair) is identical in BOTH directions, so
    // reversing a column never reshuffles its ties.
    private static string OrderBy(string sortToken) => sortToken switch
    {
        "occurrences" => "g.occurrences_count DESC, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        "occurrences-asc" => "g.occurrences_count, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        "ayahs" => "g.ayahs_count DESC, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        "ayahs-asc" => "g.ayahs_count, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        "surahs" => "g.surahs_count DESC, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        "surahs-asc" => "g.surahs_count, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        "alpha" => "g.display_text, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        "alpha-desc" => "g.display_text DESC, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        // mushaf-order is ascending-only by contract (the parser rejects any suffix on it).
        "mushaf-order" => "g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeSortSpec)} value."),
    };

    private static string ResolveBroadLabel(string type) => type switch
    {
        VerbType => "فعل",
        ParticleType => "حرف وأداة",
        InlType => "حروف مقطّعة",
        _ => "اسم",
    };

    internal sealed record WordTypeReadContext(
        string Type,
        string? ChildCode,
        string? Case,
        string? Tense,
        string? Voice,
        string? Search = null,
        bool? HasRoot = null,
        bool? HasStem = null,
        bool? HasLemma = null)
    {
        public static WordTypeReadContext Unscoped { get; } = new(string.Empty, null, null, null, null);

        public bool HasChildCode => !string.IsNullOrWhiteSpace(ChildCode);

        // "all" is the frontend default meaning "no secondary filter applied"; only a concrete value
        // (or "null" for case) narrows the rows. "null" is a real noun-case filter meaning غير محدد.
        public bool HasCaseFilter => !string.IsNullOrWhiteSpace(Case) && Case != "all";
        public bool HasTenseFilter => !string.IsNullOrWhiteSpace(Tense) && Tense != "all";
        public bool HasVoiceFilter => !string.IsNullOrWhiteSpace(Voice) && Voice != "all";

        // Search holds the already-normalized identity fragment (empty/whitespace/diacritics-only
        // collapsed to null in the reader), so a non-empty value always narrows the base.
        public bool HasSearch => !string.IsNullOrEmpty(Search);
    }

    private sealed record TreeChildCountRow(string Type, string ChildCode, int Count);

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
        int FirstWordOrderInMushaf,
        int TotalCount)
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
