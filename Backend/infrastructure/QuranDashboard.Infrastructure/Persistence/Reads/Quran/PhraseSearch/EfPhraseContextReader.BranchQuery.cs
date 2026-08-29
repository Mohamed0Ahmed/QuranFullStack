namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    private const string ContextBranchesWithAlternativesSql = """
        , side_settings AS MATERIALIZED (
          SELECT 1::smallint AS side,
                 CARDINALITY(@previous_exact_token_ids::integer[]) AS selected_count,
                 @previous_ends_at_boundary AS selected_ends_at_boundary,
                 @previous_alternative_exact_token_ids::integer[] AS alternative_token_ids,
                 @previous_offset::bigint AS page_offset,
                 @previous_page_size::integer AS page_size
          UNION ALL
          SELECT 2::smallint,
                 CARDINALITY(@following_exact_token_ids::integer[]),
                 @following_ends_at_boundary,
                 @following_alternative_exact_token_ids::integer[],
                 @following_offset::bigint,
                 @following_page_size::integer
        ), facet_occurrences AS MATERIALIZED (
          SELECT 1::smallint AS side,
                 occurrence.*
          FROM previous_facet_occurrences AS occurrence
          UNION ALL
          SELECT 2::smallint,
                 occurrence.*
          FROM following_facet_occurrences AS occurrence
        ), side_occurrences AS MATERIALIZED (
          SELECT setting.side,
                 setting.selected_count,
                 setting.selected_ends_at_boundary,
                 occurrence.occurrence_id,
                 occurrence.surah_number,
                 occurrence.ayah_number,
                 occurrence.start_word_number,
                 occurrence.end_word_number,
                 occurrence.readable_word_count,
                 CASE
                   WHEN setting.side = 1
                     THEN occurrence.start_word_number - 1 = setting.selected_count
                   ELSE occurrence.readable_word_count - occurrence.end_word_number = setting.selected_count
                 END AS is_at_boundary,
                 CASE
                   WHEN @simple_mode THEN next_word.unique_simple_word_id
                   ELSE next_word.unique_tashkeel_word_id
                 END AS next_exact_token_id,
                 next_word.text_uthmani AS next_display_text,
                 CASE
                   WHEN setting.side = 1
                     THEN occurrence.start_word_number - 1 = setting.selected_count + 1
                   ELSE occurrence.readable_word_count - occurrence.end_word_number = setting.selected_count + 1
                 END AS child_is_at_boundary
          FROM side_settings AS setting
          LEFT JOIN facet_occurrences AS occurrence
            ON occurrence.side = setting.side
          LEFT JOIN quran_words AS next_word
            ON NOT setting.selected_ends_at_boundary
           AND next_word.ayah_id = occurrence.ayah_id
           AND NOT next_word.is_ayah_marker
           AND next_word.word_number = CASE
                 WHEN setting.side = 1
                   THEN occurrence.start_word_number - setting.selected_count - 1
                 ELSE occurrence.end_word_number + setting.selected_count + 1
               END
        ), side_summaries AS (
          SELECT setting.side,
                 COUNT(occurrence.occurrence_id) FILTER (
                   WHERE occurrence.is_at_boundary
                 ) AS boundary_count
          FROM side_settings AS setting
          LEFT JOIN side_occurrences AS occurrence
            ON occurrence.side = setting.side
          GROUP BY setting.side
        ), token_options AS (
          SELECT occurrence.side,
                 occurrence.next_exact_token_id AS exact_token_id,
                 (ARRAY_AGG(
                   occurrence.next_display_text
                   ORDER BY occurrence.surah_number,
                            occurrence.ayah_number,
                            occurrence.start_word_number,
                            occurrence.occurrence_id
                 ))[1] AS display_text,
                 FALSE AS is_boundary,
                 COUNT(*) AS passes_through_count,
                 COUNT(*) FILTER (
                   WHERE occurrence.child_is_at_boundary
                 ) AS side_ends_here_count
          FROM side_occurrences AS occurrence
          WHERE occurrence.occurrence_id IS NOT NULL
            AND NOT occurrence.selected_ends_at_boundary
            AND NOT occurrence.is_at_boundary
          GROUP BY occurrence.side, occurrence.next_exact_token_id
        ), boundary_options AS (
          SELECT summary.side,
                 NULL::integer AS exact_token_id,
                 NULL::text AS display_text,
                 TRUE AS is_boundary,
                 summary.boundary_count AS passes_through_count,
                 summary.boundary_count AS side_ends_here_count
          FROM side_summaries AS summary
          JOIN side_settings AS setting
            ON setting.side = summary.side
          WHERE NOT setting.selected_ends_at_boundary
            AND summary.boundary_count > 0
        ), all_options AS MATERIALIZED (
          SELECT * FROM token_options
          UNION ALL
          SELECT * FROM boundary_options
        ), option_summaries AS (
          SELECT setting.side,
                 COUNT(option.side)::integer AS total_options
          FROM side_settings AS setting
          LEFT JOIN all_options AS option
            ON option.side = setting.side
          GROUP BY setting.side
        ), candidate_options AS MATERIALIZED (
          SELECT option.*
          FROM all_options AS option
          JOIN side_settings AS setting
            ON setting.side = option.side
          WHERE option.is_boundary
             OR NOT option.exact_token_id = ANY(setting.alternative_token_ids)
        ), candidate_summaries AS (
          SELECT setting.side,
                 COUNT(option.side)::integer AS candidate_page_count
          FROM side_settings AS setting
          LEFT JOIN candidate_options AS option
            ON option.side = setting.side
          GROUP BY setting.side
        ), ranked_candidate_options AS (
          SELECT option.*,
                 ROW_NUMBER() OVER (
                   PARTITION BY option.side
                   ORDER BY option.passes_through_count DESC,
                            option.exact_token_id NULLS FIRST
                 ) AS row_number
          FROM candidate_options AS option
        ), page_options AS (
          SELECT option.*
          FROM ranked_candidate_options AS option
          JOIN side_settings AS setting
            ON setting.side = option.side
          WHERE option.row_number > setting.page_offset
            AND option.row_number <= setting.page_offset + setting.page_size
        ), pinned_options AS (
          SELECT option.*
          FROM all_options AS option
          JOIN side_settings AS setting
            ON setting.side = option.side
          WHERE NOT option.is_boundary
            AND option.exact_token_id = ANY(setting.alternative_token_ids)
        ), response_options AS (
          SELECT option.side,
                 option.exact_token_id,
                 option.display_text,
                 option.is_boundary,
                 option.passes_through_count,
                 option.side_ends_here_count,
                 TRUE AS is_pinned,
                 NULL::bigint AS row_number
          FROM pinned_options AS option
          UNION ALL
          SELECT option.side,
                 option.exact_token_id,
                 option.display_text,
                 option.is_boundary,
                 option.passes_through_count,
                 option.side_ends_here_count,
                 FALSE AS is_pinned,
                 option.row_number
          FROM page_options AS option
        ), filtered_summary AS (
          SELECT COUNT(*)::integer AS total_count
          FROM filtered_occurrences
        ), overall_representative AS (
          SELECT occurrence.*
          FROM filtered_occurrences AS occurrence
          ORDER BY occurrence.surah_number,
                   occurrence.ayah_number,
                   occurrence.start_word_number,
                   occurrence.occurrence_id
          LIMIT 1
        )
        SELECT integrity.has_invalid_exact_identity,
               filtered.total_count,
               representative.occurrence_id,
               representative.ayah_id,
               representative.verse_key,
               representative.surah_number,
               representative.surah_name_arabic,
               representative.ayah_number,
               representative.page_from,
               representative.page_to,
               representative.start_word_number,
               representative.end_word_number,
               setting.side,
               summary.boundary_count,
               option_summary.total_options,
               candidate_summary.candidate_page_count,
               option.is_pinned,
               option.row_number,
               option.exact_token_id,
               option.display_text,
               option.is_boundary,
               option.passes_through_count,
               option.side_ends_here_count
        FROM population_integrity AS integrity
        CROSS JOIN filtered_summary AS filtered
        CROSS JOIN side_settings AS setting
        JOIN side_summaries AS summary
          ON summary.side = setting.side
        JOIN option_summaries AS option_summary
          ON option_summary.side = setting.side
        JOIN candidate_summaries AS candidate_summary
          ON candidate_summary.side = setting.side
        LEFT JOIN overall_representative AS representative
          ON TRUE
        LEFT JOIN response_options AS option
          ON option.side = setting.side
        ORDER BY setting.side,
                 option.is_pinned DESC NULLS LAST,
                 option.passes_through_count DESC,
                 option.exact_token_id NULLS FIRST,
                 option.row_number
        """;
}
