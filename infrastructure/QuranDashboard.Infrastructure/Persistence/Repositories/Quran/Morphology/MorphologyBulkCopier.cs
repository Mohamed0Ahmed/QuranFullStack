using NpgsqlTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;
using QuranDashboard.Infrastructure.Files.Quran.Morphology;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Morphology;

// Owns the FK-safe binary COPY write path: POS controlled vocabulary first, then the deduped
// dimensions, then morphology, then segments. Each method streams the assembled in-memory graph into
// Postgres via the Npgsql binary importer using the connection's ambient transaction.
internal static class MorphologyBulkCopier
{
    public static async Task CopyPosTagsAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_pos_tags (code, arabic_label, english_label, category, sort_order, description)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var tag in PosTagSeed.GetAll())
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(tag.Code, ct);
            await importer.WriteAsync(tag.ArabicLabel, ct);
            await importer.WriteAsync(tag.EnglishLabel, ct);
            await importer.WriteAsync(tag.Category, ct);
            await importer.WriteAsync(tag.SortOrder, ct);
            await importer.WriteAsync(tag.Description, NpgsqlDbType.Text, ct);
        }

        await importer.CompleteAsync(ct);
    }

    public static async Task CopyRootsAsync(
        NpgsqlConnection connection, MorphologySourceData source, CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_roots (id, root_text, root_buckwalter, words_count, distinct_lemmas_count, first_word_order_in_mushaf)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var root in source.ResolvedRoots)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(root.AssignedId, ct);
            await importer.WriteAsync(root.RootText, ct);
            await importer.WriteAsync(root.RootBuckwalter, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(root.WordsCount, ct);
            await importer.WriteAsync(root.DistinctLemmasCount, ct);
            await importer.WriteAsync(root.FirstWordOrderInMushaf, ct);
        }

        await importer.CompleteAsync(ct);
    }

    public static async Task CopyLemmasAsync(
        NpgsqlConnection connection, MorphologySourceData source, CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_lemmas (id, lemma_text, lemma_buckwalter, root_id, words_count, first_word_order_in_mushaf)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var lemma in source.ResolvedLemmas)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(lemma.AssignedId, ct);
            await importer.WriteAsync(lemma.LemmaText, ct);
            await importer.WriteAsync(lemma.LemmaBuckwalter, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(lemma.RootId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(lemma.WordsCount, ct);
            await importer.WriteAsync(lemma.FirstWordOrderInMushaf, ct);
        }

        await importer.CompleteAsync(ct);
    }

    public static async Task CopyStemsAsync(
        NpgsqlConnection connection, MorphologySourceData source, CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_stems (id, stem_text, words_count, first_word_order_in_mushaf)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var stem in source.ResolvedStems)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(stem.AssignedId, ct);
            await importer.WriteAsync(stem.StemText, ct);
            await importer.WriteAsync(stem.WordsCount, ct);
            await importer.WriteAsync(stem.FirstWordOrderInMushaf, ct);
        }

        await importer.CompleteAsync(ct);
    }

    public static async Task CopyMorphologyAsync(
        NpgsqlConnection connection,
        MorphologySourceData source,
        IReadOnlyDictionary<string, int> wordIdsByLocation,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_word_morphology (
                quran_word_id, location, head_pos, segment_count,
                root_id, lemma_id, stem_id, is_verb, verb_tense, verb_voice,
                case_feature, head_features_json)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var word in source.Words)
        {
            if (!wordIdsByLocation.TryGetValue(word.Location, out var quranWordId))
            {
                throw new InvalidDataException($"Readable word location '{word.Location}' was not found in quran_words.");
            }

            await importer.StartRowAsync(ct);
            await importer.WriteAsync(quranWordId, ct);
            await importer.WriteAsync(word.Location, ct);
            await importer.WriteAsync(word.HeadPos, ct);
            await importer.WriteAsync((short)word.Segments.Count, ct);
            await importer.WriteAsync(word.RootId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(word.LemmaId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(word.StemId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(word.IsVerb, ct);
            await importer.WriteAsync(word.VerbTense, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(word.VerbVoice, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(word.CaseFeature, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(word.HeadFeaturesJson, NpgsqlDbType.Jsonb, ct);
        }

        await importer.CompleteAsync(ct);
    }

    public static async Task CopySegmentsAsync(
        NpgsqlConnection connection,
        MorphologySourceData source,
        IReadOnlyDictionary<string, int> wordIdsByLocation,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_word_morphology_segments (
                quran_word_id, segment_location, segment_number, kind, pos,
                form_buckwalter, form_arabic_normalized, arabic_render_tier, arabic_render_source,
                root_buckwalter, lemma_buckwalter, features_raw, features_json)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var word in source.Words)
        {
            if (!wordIdsByLocation.TryGetValue(word.Location, out var quranWordId))
            {
                throw new InvalidDataException($"Readable word location '{word.Location}' was not found in quran_words.");
            }

            foreach (var segment in word.Segments)
            {
                var segmentLocation = $"{word.Location}:{segment.SegmentNumber}";

                await importer.StartRowAsync(ct);
                await importer.WriteAsync(quranWordId, ct);
                await importer.WriteAsync(segmentLocation, ct);
                await importer.WriteAsync(segment.SegmentNumber, ct);
                await importer.WriteAsync(segment.Kind, ct);
                await importer.WriteAsync(segment.Pos, ct);
                await importer.WriteAsync(segment.FormBuckwalter, ct);
                await importer.WriteAsync(segment.FormArabicNormalized, NpgsqlDbType.Text, ct);
                await importer.WriteAsync(segment.RenderTier, NpgsqlDbType.Text, ct);
                await importer.WriteAsync(segment.RenderSource, ct);
                await importer.WriteAsync(segment.RootBuckwalter, NpgsqlDbType.Text, ct);
                await importer.WriteAsync(segment.LemmaBuckwalter, NpgsqlDbType.Text, ct);
                await importer.WriteAsync(segment.FeaturesRaw, ct);
                await importer.WriteAsync(segment.FeaturesJson, NpgsqlDbType.Jsonb, ct);
            }
        }

        await importer.CompleteAsync(ct);
    }
}
