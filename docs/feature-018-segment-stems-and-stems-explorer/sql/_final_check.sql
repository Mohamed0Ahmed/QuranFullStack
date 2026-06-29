SELECT (SELECT COUNT(*) FROM seg_stem_candidates) AS rows_in_set,
       (SELECT COUNT(*) FILTER (WHERE segment_kind<>'STEM') FROM seg_stem_candidates) AS non_stem,
       (SELECT COUNT(*) FILTER (WHERE segment_number = primary_stem_segment_number) FROM seg_stem_candidates) AS primary_leak;
