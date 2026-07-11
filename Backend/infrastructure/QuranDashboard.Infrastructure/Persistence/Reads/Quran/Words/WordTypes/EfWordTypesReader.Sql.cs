using Npgsql;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

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
        ), all_children AS (
            SELECT * FROM noun_children
            UNION ALL
            SELECT * FROM verb_children
            UNION ALL
            SELECT * FROM particle_children
        )
        SELECT type AS "{nameof(TreeChildCountRow.Type)}", child_code AS "{nameof(TreeChildCountRow.ChildCode)}", COUNT(*)::int AS "{nameof(TreeChildCountRow.Count)}"
        FROM all_children
        GROUP BY type, child_code
        """;
    }

    private static string RowsCountSql(WordTypeReadContext context) => $"""
        WITH base AS (
            {BaseRowsSql(context)}
        ), grouped AS (
            SELECT tashkeel_word_id, {ContextExpression(context)} AS context_code
            FROM base
            GROUP BY tashkeel_word_id, {ContextExpression(context)}
        )
        SELECT COUNT(*)::int AS "{nameof(CountRow.Count)}"
        FROM grouped
        """;

    private static string RowsSql(WordTypeReadContext context, WordTypeSort sort) => $"""
        WITH base AS (
            {BaseRowsSql(context)}
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
            g.first_word_order_in_mushaf AS "{nameof(WordTypeRowSqlResult.FirstWordOrderInMushaf)}"
        FROM grouped g
        LEFT JOIN root_winners ON root_winners.tashkeel_word_id = g.tashkeel_word_id AND root_winners.context_code = g.context_code
        LEFT JOIN lemma_winners ON lemma_winners.tashkeel_word_id = g.tashkeel_word_id AND lemma_winners.context_code = g.context_code
        LEFT JOIN stem_winners ON stem_winners.tashkeel_word_id = g.tashkeel_word_id AND stem_winners.context_code = g.context_code
        ORDER BY {OrderBy(sort)}
        OFFSET @skip LIMIT @take
        """;

    private static string BaseRowsSql(WordTypeReadContext context) => $"""
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
        """;

    // Grouped table views (roots/stems/lemmas) reuse BaseRowsSql verbatim and group by the numeric
    // dimension ID, excluding nulls. Grouping and total counting happen before pagination.
    private static string GroupedRowsSql(WordTypeReadContext context, WordTypeTableView view, WordTypeSort sort)
    {
        var (idColumn, textColumn) = DimensionColumns(view);
        var needsFold = sort == WordTypeSort.Alpha;
        var normTextColumn = needsFold
            ? $", replace(translate(lower(MIN({textColumn})), @foldFrom, @foldTo), ' ', '') AS norm_text"
            : string.Empty;

        return $"""
        WITH base AS (
            {BaseRowsSql(context)}
        ), grouped AS (
            SELECT
                {idColumn} AS dimension_id,
                MIN({textColumn}) AS display_text,
                MIN(quran_word_id) AS first_word_order_in_mushaf,
                COUNT(*)::int AS occurrences_count,
                COUNT(DISTINCT ayah_id)::int AS ayahs_count,
                COUNT(DISTINCT surah_number)::int AS surahs_count
                {normTextColumn}
            FROM base
            WHERE {idColumn} IS NOT NULL
            GROUP BY {idColumn}
        )
        SELECT
            dimension_id AS "{nameof(GroupedRowSqlResult.DimensionId)}",
            display_text AS "{nameof(GroupedRowSqlResult.DisplayText)}",
            occurrences_count AS "{nameof(GroupedRowSqlResult.OccurrencesCount)}",
            ayahs_count AS "{nameof(GroupedRowSqlResult.AyahsCount)}",
            surahs_count AS "{nameof(GroupedRowSqlResult.SurahsCount)}",
            first_word_order_in_mushaf AS "{nameof(GroupedRowSqlResult.FirstWordOrderInMushaf)}"
        FROM grouped
        ORDER BY {GroupedOrderBy(sort)}
        OFFSET @skip LIMIT @take
        """;
    }

    // Grouped totalCount = distinct non-null dimension IDs over the scoped base, measured before paging.
    private static string GroupedRowsCountSql(WordTypeReadContext context, WordTypeTableView view)
    {
        var (idColumn, _) = DimensionColumns(view);
        return $"""
        WITH base AS (
            {BaseRowsSql(context)}
        )
        SELECT COUNT(DISTINCT {idColumn})::int AS "{nameof(CountRow.Count)}"
        FROM base
        WHERE {idColumn} IS NOT NULL
        """;
    }

    private static string GroupedOrderBy(WordTypeSort sort) => sort switch
    {
        WordTypeSort.Ayahs => "ayahs_count DESC, first_word_order_in_mushaf, dimension_id",
        WordTypeSort.Surahs => "surahs_count DESC, first_word_order_in_mushaf, dimension_id",
        WordTypeSort.MushafOrder => "first_word_order_in_mushaf, dimension_id",
        WordTypeSort.Alpha => "norm_text COLLATE \"C\", dimension_id",
        _ => "occurrences_count DESC, first_word_order_in_mushaf, dimension_id",
    };

    private static (string IdColumn, string TextColumn) DimensionColumns(WordTypeTableView view) => view switch
    {
        WordTypeTableView.Roots => ("root_id", "root_text"),
        WordTypeTableView.Stems => ("stem_id", "stem_text"),
        WordTypeTableView.Lemmas => ("lemma_id", "lemma_text"),
        _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Grouped dimension columns are only defined for roots/stems/lemmas."),
    };

    private static object[] BuildGroupedRowsParameters(WordTypeReadContext context, WordTypeSort sort, int skip, int take)
    {
        var parameters = new List<object>
        {
            new NpgsqlParameter<int>("skip", skip),
            new NpgsqlParameter<int>("take", take),
        };
        AddChildCodeParameter(context, parameters);
        AddSecondaryFilterParameters(context, parameters);

        if (sort == WordTypeSort.Alpha)
        {
            parameters.Add(new NpgsqlParameter<string>("foldFrom", RootsListDerivation.ArabicFoldFrom));
            parameters.Add(new NpgsqlParameter<string>("foldTo", RootsListDerivation.ArabicFoldTo));
        }

        return [.. parameters];
    }

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
        // Noun and particle children pin the head POS; verb children pin the tense.
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

    private static object[] BuildRowsParameters(WordTypeReadContext context, int skip, int take)
    {
        var parameters = new List<object>
        {
            new NpgsqlParameter<int>("skip", skip),
            new NpgsqlParameter<int>("take", take),
        };
        AddChildCodeParameter(context, parameters);
        AddSecondaryFilterParameters(context, parameters);
        return [.. parameters];
    }

    private static object[] BuildCountParameters(WordTypeReadContext context)
    {
        var parameters = new List<object>();
        AddChildCodeParameter(context, parameters);
        AddSecondaryFilterParameters(context, parameters);
        return [.. parameters];
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

    private static string OrderBy(WordTypeSort sort) => sort switch
    {
        WordTypeSort.Ayahs => "g.ayahs_count DESC, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        WordTypeSort.Surahs => "g.surahs_count DESC, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        WordTypeSort.MushafOrder => "g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        WordTypeSort.Alpha => "g.display_text, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
        _ => "g.occurrences_count DESC, g.first_word_order_in_mushaf, g.tashkeel_word_id, g.context_code",
    };

    private static string ResolveBroadLabel(string type) => type switch
    {
        VerbType => "فعل",
        ParticleType => "حرف وأداة",
        InlType => "حروف مقطّعة",
        _ => "اسم",
    };

    /// <summary>
    /// Bundles the normalized main type with the optional selected child code and active secondary
    /// filters so the row/tree SQL builders thread every dimension consistently. <see cref="Unscoped"/>
    /// is the no-child/no-secondary-filter baseline used by the tree reads (E1 counts stay unscoped).
    /// </summary>
    private sealed record WordTypeReadContext(string Type, string? ChildCode, string? Case, string? Tense, string? Voice)
    {
        public static WordTypeReadContext Unscoped { get; } = new(string.Empty, null, null, null, null);

        public bool HasChildCode => !string.IsNullOrWhiteSpace(ChildCode);

        // "all" is the frontend default meaning "no secondary filter applied"; only a concrete value
        // (or "null" for case) narrows the rows. "null" is a real noun-case filter meaning غير محدد.
        public bool HasCaseFilter => !string.IsNullOrWhiteSpace(Case) && Case != "all";
        public bool HasTenseFilter => !string.IsNullOrWhiteSpace(Tense) && Tense != "all";
        public bool HasVoiceFilter => !string.IsNullOrWhiteSpace(Voice) && Voice != "all";
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

    private sealed record GroupedRowSqlResult(
        int DimensionId,
        string DisplayText,
        int OccurrencesCount,
        int AyahsCount,
        int SurahsCount,
        int FirstWordOrderInMushaf)
    {
        public WordTypeTableRowDto ToDto(WordTypeTableView view) => view switch
        {
            WordTypeTableView.Roots => new RootTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            WordTypeTableView.Stems => new StemTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            WordTypeTableView.Lemmas => new LemmaTableRowDto(DimensionId, DisplayText, OccurrencesCount, AyahsCount, SurahsCount),
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Grouped mapping is only defined for roots/stems/lemmas."),
        };
    }
}
