# Phase 5 similarity read measurements

Measured on 2026-08-26 against local database `quran_dashboard` and active build
`9c8559aa-ea1b-4a54-89ef-0dfa8cd24b82`. All database validation and plan capture was read-only.
This is temporary feature evidence and follows the feature-artifact deletion lifecycle.

## Runtime contract evidence

| Check | Result |
| --- | --- |
| Manual simple length 2, minimum 1 | HTTP 200; 555 matches; on-demand variant scan |
| Manual tashkil length 3, minimum 2 | HTTP 200; 1 match from the 53,993-variant partition |
| Manual simple length 4, minimum 2 | HTTP 200; 341 direct stored-edge matches |
| Global simple length 4 at 50% | HTTP 200; 42,300 anchors |
| Direct matches for anchor 117005 at 50% | HTTP 200; response total 341; database direct-edge total 341 |
| Nine-token manual minimum 4 | HTTP 400; `phrase_minimum_matched_words_invalid` |
| Nine-token manual minimum 5 | HTTP 200; 31 direct matches |
| Malformed resolution reference | HTTP 400; `phrase_reference_invalid` |
| Previous-build manual reference | HTTP 409; `phrase_index_changed` |
| Previous-build group reference | HTTP 409; `phrase_index_changed` |
| Non-preset global threshold 55 | HTTP 400; `phrase_similarity_threshold_invalid` |
| Migrated empty-schema routes | All similarity routes return HTTP 503 in the shared envelope |

Every sampled match returned one-based matched and differing positions, original Uthmani full-ayah
words, and server-computed phrase, matched, and differing Quran-word-ID roles for both phrases.
Development database command logs redacted EF parameters and contained no raw resolution reference or
query value. The similarity reader adds no raw-query or reference logging and no cache entry.

## Threshold totals and invariants

| Threshold | Stored length-4+ edge count | Directed anchor-stat total | Exact double |
| ---: | ---: | ---: | :---: |
| 50 | 1,115,977 | 2,231,954 | yes |
| 60 | 236,650 | 473,300 | yes |
| 70 | 100,789 | 201,578 | yes |
| 80 | 33,091 | 66,182 | yes |
| 90 | 1,682 | 3,364 | yes |

The active build had zero failures for all of these checks:

- length-2 or length-3 stored edges;
- self-edges;
- endpoint build, mode, or length mismatch;
- edge matched/difference arithmetic or difference-position cardinality mismatch.

The global match response total for anchor 117005 was exactly its 341 qualifying direct edge rows.
The query follows only those two edge directions and performs no transitive traversal.

## EXPLAIN (ANALYZE, BUFFERS)

### Worst length-2/3 manual partition

The measured partition was tashkil length 3, the largest length-2/3 partition at 53,993 variants.
The API executes a count and a stable page query. The count plan used the same bitmap partition index,
read 53,992 non-anchor rows, hit 2,055 shared buffers, and completed in 229.373 ms. The page plan was:

```text
Limit (actual time=271.250..271.255 rows=1 loops=1)
  Buffers: shared hit=8 read=2053
  -> Sort (actual time=118.450..118.453 rows=1 loops=1)
       Sort Key: matched_count DESC, id
       Sort Method: quicksort  Memory: 25kB
       -> Nested Loop (actual time=11.087..118.408 rows=1 loops=1)
            -> Bitmap Heap Scan on quran_phrase_variants (actual time=11.051..20.802 rows=53992 loops=1)
                 Recheck Cond: build_id = active AND mode = 2 AND word_count = 3
                 Filter: id <> 745419
                 Rows Removed by Filter: 1
                 Heap Blocks: exact=1362
                 Buffers: shared hit=2 read=2053
                 -> Bitmap Index Scan on IX_quran_phrase_variants_build_id_mode_word_count_occurrence_c~
                      (actual time=10.845..10.846 rows=53993 loops=1)
                      Index Cond: build_id = active AND mode = 2 AND word_count = 3
                      Index Searches: 1
                      Buffers: shared hit=1 read=692
            -> Aggregate (actual time=0.002..0.002 rows=0 loops=53992)
                 Filter: exact-array matched_count >= 2
                 -> Function Scan on generate_subscripts (actual rows=3 loops=53992)
Planning Time: 1.289 ms
Execution Time: 290.840 ms
```

The scan is bounded to one active-build, mode, and length variant partition. It does not read Quran
words, occurrences, another build, another mode, or another length while scoring.

### Global anchor page

The measured default global page was simple length 4 at 50%, ordered by neighbor count descending and
stable variant ID.

```text
Limit (actual time=0.475..7.565 rows=25 loops=1)
  Buffers: shared hit=99 read=54
  -> Nested Loop (actual time=0.475..7.559 rows=25 loops=1)
       -> Index Scan using IX_quran_phrase_similarity_anchor_stats_build_id_mode_word_cou~
            (actual time=0.026..6.621 rows=25 loops=1)
            Index Cond: build_id = active AND mode = 1 AND word_count = 4 AND threshold = 50
            Index Searches: 1
            Buffers: shared hit=10 read=18
       -> Index Scan using PK_quran_phrase_variants
            (actual time=0.017..0.017 rows=1 loops=25)
            Index Cond: build_id = active AND id = stat.variant_id
            Index Searches: 25
            Buffers: shared hit=89 read=36
Planning Time: 1.114 ms
Execution Time: 7.788 ms
```

The page streams directly from the locked anchor-stat ordering index; it does not group the edge table.

### Direct-neighbor lookup

The measured anchor was variant 117005 at the 50% floor. Both directions are ordered independently,
merged, limited, and then hydrated. The final score and all position lists are recomputed from the two
exact token arrays; the edge supplies only candidate identity and matched-count filtering/order.

```text
Limit (actual time=0.122..0.321 rows=25 loops=1)
  Buffers: shared hit=138
  -> Nested Loop (actual time=0.121..0.318 rows=25 loops=1)
       -> Merge Append (actual time=0.081..0.089 rows=25 loops=1)
            Sort Key: matched_count DESC, variant_id
            -> Index Only Scan using IX_quran_phrase_similarity_edges_build_id_left_variant_id_matc~
                 (actual time=0.046..0.049 rows=24 loops=1)
                 Index Cond: build_id = active AND left_variant_id = 117005 AND matched_count >= 2
                 Heap Fetches: 0
                 Index Searches: 1
                 Buffers: shared hit=8
            -> Index Only Scan using IX_quran_phrase_similarity_edges_build_id_right_variant_id_mat~
                 (actual time=0.033..0.034 rows=2 loops=1)
                 Index Cond: build_id = active AND right_variant_id = 117005 AND matched_count >= 2
                 Heap Fetches: 0
                 Index Searches: 1
                 Buffers: shared hit=5
       -> Index Scan using AK_quran_phrase_variants_build_id_id_mode_word_count
            (actual time=0.009..0.009 rows=1 loops=25)
            Index Cond: build_id = active AND id = neighbor.variant_id
            Index Searches: 25
            Buffers: shared hit=125
Planning Time: 0.770 ms
Execution Time: 0.352 ms
```

Both locked neighbor indexes are used without a corpus scan or transitive component traversal.

## Build, OpenAPI, and retained gates

- `Backend/scripts/qd-build`: passed with 0 errors; only the pre-existing SSH.NET NU1903 warning.
- OpenAPI exported twice with identical SHA-256
  `2e4cce23ba3f9ea9cc05dd4e24ba7a659bb88df4a427a0d8d2e6c8fac4dbf3dc`.
- `SmokeRouteBaselineTests`: 2/2 passed.
- `SmokeRoutePipelineTests`: 59/59 passed, including all 10 PhraseSearch routes and the three new
  unavailable-index responses.
- `SmokeCoverageParityTests`: 4/5 passed. The only failure is the pre-existing unrelated missing
  catalog entry for `GET api/mushaf/pages/{pageNumber}/door-highlights`.
- Full retained smoke lane: 98/100 passed. Its two failures are the same unrelated Mushaf parity
  entry and the pre-existing `SmokeBootGuardTests` expectation that omits the already registered
  `ApplicationAuthentication` and `DeviceSession` schemes.
- Test Guard: no new test class, method, or file. The only retained-test changes are the three route
  catalog entries and the existing baseline count from 118 to 121.
