namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

internal static class LinkingDescriptorCheckConstraints
{
    public const string KindReferenceCoherence =
        """
        (source_kind = 'root'
            AND root_id IS NOT NULL
            AND num_nonnulls(lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)
        OR (source_kind = 'lemma'
            AND lemma_id IS NOT NULL
            AND num_nonnulls(root_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)
        OR (source_kind = 'stem'
            AND stem_id IS NOT NULL
            AND num_nonnulls(root_id, lemma_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)
        OR (source_kind = 'unique_word'
            AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 1
            AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 0)
        OR (source_kind = 'word_type'
            AND num_nonnulls(root_id, lemma_id, stem_id, word_type_tashkeel_word_id) = 1
            AND num_nonnulls(unique_simple_word_id, unique_tashkeel_word_id) = 0)
        OR (source_kind = 'manual_mushaf_ayahs'
            AND num_nonnulls(root_id, lemma_id, stem_id, unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id) = 0)
        """;

    public static string JsonbSchemaVersion(string column) =>
        $"""
        jsonb_typeof({column}) = 'object'
        AND jsonb_exists({column}, 'schemaVersion')
        AND jsonb_typeof({column} -> 'schemaVersion') = 'number'
        AND ({column} ->> 'schemaVersion') ~ '^[1-9][0-9]*$'
        AND ({column} ->> 'schemaVersion')::numeric <= 2147483647
        """;

    public static string TokenIn(string column, IReadOnlyList<string> tokens) =>
        $"{column} IN ({string.Join(", ", tokens.Select(token => $"'{token}'"))})";
}
