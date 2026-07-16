# Full-Stack Performance Review

**Workspace:** QuranFullStack monorepo
**Review date:** 2026-07-16
**Mode:** Read-only code and runtime audit
**Usage calibration:** Arabic-first administration dashboard, approximately three users

## Executive verdict

No Critical or High performance defect was found. The review found three Medium issues:

1. Word Types list/table reads repeat the same expensive scoped aggregation for `COUNT` and page data.
2. Lemma and Stem cold catalogue reads transfer and materialize every matching morphology occurrence before caching the whole summary.
3. Mushaf ayah/word HTTP subscriptions can outlive the route; token invalidation can then leave the same selection stuck in loading state on re-entry.

Everything else is Low severity at this product's actual scale. In particular, the 1.04M-row translation table and the combined approximately 0.9M-row tafsir mapping/entry set are not suffering from missing-index or sequential-scan problems on the real API lookup paths. Their equality lookups use the intended indexes and complete in sub-millisecond to low-single-digit-millisecond time locally.

The primary backend concern is therefore redundant work and first-hit latency, not throughput. The primary frontend concern is lifecycle correctness plus a few small repeated GETs, not change-detection collapse or large-list rendering failure.

### Severity summary

| Severity | Count | Summary |
|---|---:|---|
| Critical | 0 | None |
| High | 0 | None |
| Medium | 3 | Two measured backend cold paths; one frontend lifecycle/correctness path |
| Low | 7 | Bounded excess queries, small repeated GETs, finite off-route work, and one Quran-rendering safety issue |

## Scope, method, and safety

The audit covered the dashboard, Words/Word Types/Roots/Lemmas/Stems/Unique Words explorers, Mushaf pages, ayah study, word analysis, tafsir, translations, full i3rab, similarities, and mutashabihat.

Evidence came from four parallel workstreams and a final cross-stack merge:

- Static review of the full read paths, their callers, caches, contracts, and generated SQL shapes.
- The current existing Debug API binary, started without rebuilding, with console-only EF command logging. No scoped source file was newer than the binary; newer files found under `obj/` were generated build artifacts only.
- Read-only loopback HTTP `GET` requests. Quran payloads were discarded rather than printed.
- PostgreSQL 18.4 sessions guarded by `BEGIN READ ONLY`/rollback for exact row counts and `EXPLAIN (ANALYZE, BUFFERS)`.

No code, configuration, index, migration, database row, Quran source package, or persistent log was changed. The local API process was stopped after capture. The only workspace write is this report.

Single-machine timings are diagnostic samples, not production percentiles or load tests. The first request after process start included JIT/connection/model initialization (for example, the first ayah-study request was 3.785 s); that startup value is not attributed to the endpoint's steady cold-key cost. Findings use warmed-process, uncached-key timings where available.

## Runtime read matrix

`Cold SQL` means a previously uncached response key. Successful backend cache repeats produced zero SQL on the sampled cached paths.

| Read surface | Cold SQL commands | Sampled HTTP result | Cache/interpretation |
|---|---:|---|---|
| Dashboard info | 0 | 222 B | Assembly/environment metadata only |
| Mushaf page | 8 | 25 ms on a warmed process; 6-18.4 KB sampled | 0 SQL when cached |
| Ayah study, defaults/all sources | 12-13 | 37-104 ms on a warmed process; 2.6-70.8 KB depending on ayah/sources | 0 SQL on repeat; 30-minute sliding cache |
| Word analysis | Up to 12 | 31 ms on a warmed process; 1.6-2.4 KB | 0 SQL on repeat; 30-minute sliding cache |
| Similar ayahs detail | 4 | 967 B sampled | Batched; no query-in-loop |
| Ayah mutashabihat detail | 6 | 33.9 KB sampled | Batched; no query-in-loop |
| Roots list, page size 1000 | 1 | 174 ms; 165.6 KB | Whole summary cached |
| Lemmas list, page size 1000 | 2 | 991 ms; 189.9 KB | 7.6 ms/0 SQL on repeat |
| Stems list, page size 1000 | 2 | 1.251 s; 215.1 KB | 10.3 ms/0 SQL on repeat |
| Unique Words list, page size 1000 | 4 | 301 ms; approximately 238-248 KB | Bounded count/page/enrichment commands |
| Word Types words table, page size 1000 | 2 | 1.04-1.16 s; approximately 302-354 KB | 5.7 ms/0 SQL on repeat |
| Word Types roots table, page size 1000 | 2 | 455 ms; 109.9 KB | Same count/page pattern, cheaper grouping |
| Word Types scope counts | 1 | Approximately 294 ms; 167 B | Locked one-command invariant preserved |
| Flat Word Type ayahs | 5 | 152 ms; 189.8 KB | Includes redundant existence + count |
| Flat Word Type surahs | 4 | 93 ms; 7.0 KB | Occurrence materialization + two catalogue reads |
| Grouped Word Type ayahs/surahs | 3 / 2 | 182.7 KB / 7.3 KB sampled | Bounded reference implementation |

## Genuinely redundant or excess backend work

### B1 — Medium — Word Types count and page commands repeat the expensive scoped relation

**Classification:** Confirmed redundant database work and measured cold latency.

**Evidence**

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs:78-106` awaits `CountRowsAsync` before issuing `RowsSql` for the same scope.
- Grouped table views repeat the same pattern at `EfWordTypesReader.cs:127-160`.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.Sql.cs:55-65` builds the count from `BaseRowsSql`; lines 67-159 build the page from that same base and then add grouped winner CTEs.
- The front-end contract uses page size 1000: `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.models.ts:222-226`.

Representative actual SQL shape:

```sql
-- Command 1
WITH base AS (...), grouped AS (...)
SELECT COUNT(*) FROM grouped;

-- Command 2 repeats base/grouped, then computes winners and the page
WITH base AS (...), grouped AS (...),
     root_candidates AS (...), lemma_candidates AS (...), stem_candidates AS (...)
SELECT ...
FROM grouped
LEFT JOIN ...
ORDER BY ...
OFFSET @skip LIMIT @take;
```

For a noun/words page of 1000, the two EF commands took 193 ms and 934 ms in one capture; an independent capture of the frontend table route took approximately 250 ms and 693 ms. End-to-end samples were 1.04-1.16 s and 302-354 KB. A noun/roots table still spent 133 ms on the count and 239 ms on the page. Each filter/search/table-view permutation produces a different cold cache key; the 15-minute cache at `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/WordTypesCacheEntryOptions.cs:6-13` only removes exact repeats.

The separate scope-count endpoint is not the bug: Feature 026 deliberately requires its four values in one SQL command and with the same scope as the active table. The measured duplication is within the list/table `PagedResult` path.

**Impact for this dashboard**

This is user-visible first-load/filter latency, not a concurrency risk. At three users it is Medium because a normal cold filter can spend around one second while repeating a large scoped scan; cache hits are fast.

**Smallest safe remediation**

Return the page and total from one shared scoped relation, normally via a window count or equivalent single-statement projection. Preserve the current `PagedResult.TotalCount`, safe-skip, sort, search, presence filters, table-view parity, and zero-result behavior. If a plain `COUNT(*) OVER()` would lose `TotalCount` for an out-of-range empty page, retain a count-only fallback only for that exceptional page rather than for every successful page. Do not merge or alter the separate one-command scope-count contract.

### B2 — Medium — Lemma and Stem cold catalogues transfer occurrence-grain data to build summary-grain results

**Classification:** Confirmed over-fetch/materialization and measured cold latency.

**Evidence**

- Lemmas first execute a server aggregate at `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs:266-324`, then load every segment carrying a lemma at lines 331-367 to build type distributions in memory.
- That second Lemma query returned 74,608 rows in the live capture.
- Stems execute their aggregate at `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.Summary.cs:14-55`, then load every stem occurrence plus POS, lemma, and root fields at lines 62-90.
- That second Stem query returned 77,911 rows in the live capture.
- PostgreSQL statistics reported approximately 128,219 rows in `quran_word_morphology_segments` (81 MB total), 4,817 lemmas, and 11,843 stems.
- Lemmas measured 454 ms for the aggregate plus 46 ms for the raw occurrence query and 991 ms end to end. Stems measured 179 ms plus 50 ms and 1.251 s end to end. The remainder includes managed grouping/allocation and response mapping.
- `CachedLemmasReader.cs:118-127` and `CachedStemsReader.cs:129-138` cache the whole derived catalogue; `LemmasCacheEntryOptions.cs:4-14` and `StemsCacheEntryOptions.cs:4-14` give the whole-summary entry no expiration. Warm repeats were 7.6 ms and 10.3 ms with zero SQL.

Representative excess query shape:

```sql
SELECT segment.lemma_id, segment.quran_word_id, pos.code, ...
FROM quran_word_morphology_segments segment
JOIN quran_words word ON ...
JOIN quran_pos_tags pos ON ...
WHERE segment.lemma_id IS NOT NULL;
```

The Stem variant additionally joins and transfers lemma/root identity columns for every occurrence.

**Impact for this dashboard**

The impact is a one-time-per-process/restart allocation and latency spike, plus the possibility of duplicate cold construction if users arrive concurrently before the cache is filled. Indefinite caching and only three users keep this at Medium rather than High.

**Smallest safe remediation**

Change the second read to return summary-grain rows: group type distribution by `(lemma_id, POS)` and group Stem winner/distribution inputs by `(stem_id, POS/lemma/root)` as needed. Keep the exact deterministic winner order and the real earliest coordinate tuple; do not replace it with independent minima that could invent a coordinate. Returning compact grouped rows in a second command is a smaller first step than forcing every result into one very complex SQL statement. Lemma and Stem implementations can be changed independently.

### B3 — Low — Flat Word Type detail reads repeat existence/catalogue work

**Classification:** Confirmed redundant queries; bounded measured impact.

**Evidence**

- Flat ayahs run `AnyAsync` and then count the same identity scope at `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs:245-270`, followed by page metadata, matched rows, and full ayah words at lines 276-327.
- Generated SQL joins `quran_words` twice on the same morphology key in the count/page subqueries.
- The live flat ayah request executed 5 commands. The redundant `EXISTS` took 1 ms and the following distinct `COUNT` 4 ms; total HTTP time was 152 ms for a 189.8 KB response.
- Flat surahs at `EfWordTypesReader.cs:357-404` run `AnyAsync`, materialize one surah number per matching occurrence, load full Surah entities, and query missing surahs again. The live request executed 4 commands and completed in 93 ms.
- The grouped siblings already show the safer shapes: grouped ayahs use 3 bounded commands at `EfWordTypesReader.GroupedDetails.cs:75-136`; grouped surahs aggregate in SQL and read one projected catalogue at lines 166-215.

**Impact for this dashboard**

This is real excess work, but the sampled flat detail requests remained below 200 ms and are cached for 15 minutes. It is Low at three users.

**Smallest safe remediation**

Use the distinct count as the existence result and remove the preliminary `Any`. Reuse one matched-word projection rather than identical word joins. For surahs, mirror the grouped implementation: one server-side occurrence aggregate and one projected 114-row catalogue, deriving mentioned and missing lists in memory. Preserve the flat identity and count scopes exactly.

### B4 — Low — Mushaf ayah study and word analysis fan out into many sequential point queries

**Classification:** Confirmed excess query count; local plans are efficient and caches are effective.

**Evidence: ayah study**

- Defaults resolve all three source families on an ordinary request: `Backend/api/QuranDashboard.Api/appsettings.json:16-20` and `Backend/application/QuranDashboard.Application/Quran/MushafReader/Queries/GetAyahStudy/GetAyahStudyHandler.cs:19-28`.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfAyahStudyReader.cs:23-48` loads the ayah, sajda, all three selected resource families, and summary sequentially.
- Tafsir is source -> mapping -> text (lines 128-171); translation is source -> mapping (174-204); full i3rab is source -> mapping -> text (207-248); similarity/mutashabihat add 2-3 count commands (62-107).
- EF logging confirmed 13 commands for an ayah with mutashabihat groups and 12 without them. A warmed-process uncached 13-command request completed in 37 ms locally and returned 70.8 KB for the sampled source combination; another uncached key completed in 104 ms. An exact repeat executed zero SQL.
- The frontend sends the complete selected source trio on each study load at `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-ayah-study-load.runner.ts:79-99` and `.../data-access/mushaf-ayah-study.api.ts:19-37`; the response contains tafsir, translation, and full i3rab even when only one study tab is visible.

Representative actual command chain:

```sql
SELECT ... FROM quran_tafsir_sources WHERE source_key = @sourceKey LIMIT 1;
SELECT ... FROM quran_tafsir_ayah_entries
WHERE source_id = @sourceId AND ayah_id = @ayahId LIMIT 1;
SELECT ... FROM quran_tafsir_entries WHERE id = @entryId LIMIT 1;
```

Translation uses the first two equivalents; full i3rab uses all three.

**Evidence: word analysis**

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs:14-52` performs six core/identity reads, lines 65-87 load segments and POS, lines 93-106 load up to three dimensions, and lines 108-119 load i3rab rules.
- EF logging confirmed 12 commands for a fully populated word. A warmed-process uncached word took 31 ms and returned 2.4 KB; an exact repeat took 2.8 ms with zero SQL.

**Database evidence and calibration**

The large translation/tafsir mappings use the correct indexes (see the database section below), so this is round-trip cleanliness and potential remote-DB latency, not a missing-index issue. The 30-minute sliding cache is defined at `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/MushafReader/MushafReaderCacheEntryOptions.cs:3-12`.

**Impact for this dashboard**

Local warmed-process cost was small and exact repeats are cached. The remaining concern is avoidable latency sensitivity when the database is remote and when users explore new ayahs, source combinations, or word locations. That makes both paths Low at three users despite their high command counts.

**Smallest safe remediation**

For ayah study, collapse each source/mapping/text family into one projection and combine independent summary counts only where null/not-found semantics stay identical. Do not weaken provenance or missing-entry handling. For word analysis, use one projected core/morphology/identity/dimension query plus one projected segment/POS/rule query. Do not replace the current incomplete-data checks with permissive nulls. Treat lazy per-tab resource endpoints as a later contract decision, not the first fix.

### B5 — Low — A cold Mushaf page uses eight bounded commands

**Classification:** Confirmed excess command count; negligible measured warmed-process cost.

**Evidence**

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfMushafPageReader.cs:12-62` executes page existence, lines, words/ayahs, and Surah-name reads.
- Lines 132-153 execute four more reads for juz, hizb, rub, and sajda markers.
- EF logging confirmed 8 commands. A warmed-process uncached page completed in 25 ms and returned 18.4 KB; repeats execute zero SQL.
- The frontend intentionally prefetches adjacent pages at `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader-page.helpers.ts:5-21`, but `ApiResponseCache.prefetch` skips cached and in-flight keys. On a completely cold middle page this can produce the current page plus two bounded background reads; it is deliberate navigation work, not an identical duplicate.

**Impact for this dashboard**

The query count looks high, but local execution is fast and data is bounded by one Mushaf page. This is Low and not a priority.

**Smallest safe remediation**

Let the line read establish page existence and project only the needed Surah fields. Combine marker reads only if a simple `UNION ALL` projection preserves marker type, order, and sajda semantics. Benchmark before accepting extra raw-SQL complexity.

### B6 — Low — Lemma/Stem word pages repeat full identity-detail materialization across pages

**Classification:** Confirmed repeat work across distinct page keys.

**Evidence**

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs:422-482` checks existence, loads all matching occurrence rows, groups them, and only then applies `Skip/Take`; the full occurrence query is at lines 485-514.
- Stems follow the same pattern at `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.cs:354-415` and 417 onward.
- `CachedLemmasReader.cs:147-161` and `CachedStemsReader.cs:158-172` cache the already-sliced page key. A sampled Lemma identity issued the same full occurrence query again for page 2 after page 1.

**Impact for this dashboard**

The sampled identity cardinality and query time were modest, so this is Low. It becomes visible only while paging a relatively common lemma/stem.

**Smallest safe remediation**

Either cache the complete grouped identity result once and slice it for page keys, as the catalogue pattern does, or group/count/page on the server. Prefer server-side paging for identities whose grouped set can grow; preserve first-Mushaf-order and both simple/tashkeel identity semantics.

## Database and index review — no index finding

### Current cardinality and storage

| Table | Exact rows | Heap | Indexes | Total |
|---|---:|---:|---:|---:|
| `quran_translation_ayah_entries` | 1,041,412 | 351 MB | 76 MB | 432 MB |
| `quran_tafsir_ayah_entries` | 523,824 | 51 MB | 72 MB | 123 MB |
| `quran_tafsir_entries` | 382,704 | 306 MB | 38 MB | 611 MB |

`quran_tafsir_entries` also has approximately 267 MB of TOAST storage. All three reported zero dead tuples and recent auto-analysis. The tested equality predicates had a one-row planner estimate.

### Live plans for the actual hot predicates

Translation source `en-sahih-international`, ayah `2:255`:

```text
Index Scan using "IX_quran_translation_ayah_entries_source_id_ayah_id"
  Index Cond: ((source_id = 40) AND (ayah_id = 262))
  actual time=0.007..0.007 rows=1 loops=1
  Buffers: shared hit=4
Execution Time: 0.014 ms
Serialization: 0.004 ms, output=2 kB; total 0.027 ms
```

Tafsir source `ar-muyassar`, ayah `2:255` mapping:

```text
Index Scan using "IX_quran_tafsir_ayah_entries_source_id_ayah_id"
  Index Cond: ((source_id = 33) AND (ayah_id = 262))
  actual time=0.077..0.077 rows=1 loops=1
  Buffers: shared hit=4
Execution Time: 0.126 ms
```

Tafsir text row:

```text
Index Scan using "PK_quran_tafsir_entries"
  Index Cond: (id = 109541)
  Buffers: shared hit=6 read=1
Execution Time: 3.931 ms first read; 0.062 ms warm with serialization
```

Source-key lookup plans chose sequential scans over only 84 tafsir-source and 167 translation-source rows: 0.033 ms and 0.021 ms. Catalogue scan/sort was 0.817 ms and 0.326 ms. Those are appropriate tiny-table plans, not missing indexes.

The relevant indexes are explicitly configured at:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Translations/TranslationAyahEntryConfiguration.cs:37-38`
- `.../Tafsirs/TafsirAyahEntryConfiguration.cs:54-57`
- `.../Tafsirs/TafsirEntryConfiguration.cs:62-64`
- `.../Translations/TranslationSourceConfiguration.cs:96-98`
- `.../Tafsirs/TafsirSourceConfiguration.cs:122-125`

Several reverse/provenance/FK indexes had zero scans in the local counters. That is not enough evidence to drop them: some enforce uniqueness or support foreign keys/import/audit access, and the counters are not production workload history. No index should be added or removed from this review.

### Similarity/mutashabihat cardinality

The live data also rules out a hidden large-response problem in these detail endpoints:

| Family | P50 | P95 | P99 | Maximum |
|---|---:|---:|---:|---:|
| Occurrences per mutashabihat group (814 groups) | 2 | 11 | 35 | 71 |
| Similar ayahs, conditional on a nonempty result | 1 | 9 | 31 | 31 |

The 71-row group lookup used the leading `group_id` index and completed in 0.109 ms. The maximum similar result used indexed outgoing/incoming reads. No N+1 or missing-index finding was confirmed in `EfAyahSimilaritiesReader` or `EfAyahMutashabihatReader`.

## Frontend findings

### F1 — Medium — Mushaf requests can outlive route teardown and strand loading state on same-selection re-entry

**Classification:** Confirmed subscription lifecycle/correctness issue with wasted finite HTTP work.

**Evidence**

- Page teardown only calls facade unbind at `Frontend/quran-dashboard-ui/src/app/features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.ts:41-48`.
- `MushafReaderFacade.unbindFromRoute` clears timers and bumps runner tokens at `.../state/mushaf-reader.facade.ts:231-240`, but it has no active HTTP subscription to cancel.
- `subscribeToApiLoad` subscribes and discards the handle at `.../state/mushaf-api-load.helpers.ts:55-80`.
- The ayah runner's `clearPending` only clears a timer and invalidates its token at `.../state/mushaf-ayah-study-load.runner.ts:51-58`; the request subscription created at lines 79-115 remains active.
- Word analysis has the same pattern at `.../state/mushaf-word-analysis-load.runner.ts:47-54,73-114`.
- Rehydration reloads only if the word/ayah/source identity changed: `.../state/mushaf-url-hydration.ts:41-64`.

Trigger sequence:

1. Start `GET /api/mushaf/ayahs/{verseKey}/study` or `GET /api/mushaf/words/{wordLocation}/analysis`.
2. Navigate away before completion; teardown invalidates the token but does not unsubscribe.
3. The late response is deliberately ignored because the token no longer matches.
4. Return to the same URL. The persisted selection has not changed, so hydration does not reload it; the root facade can remain in its prior loading state.

The affected frontend calls map to `GET /api/mushaf/ayahs/{verseKey}/study` (`Backend/api/QuranDashboard.Api/Controllers/MushafReader/Ayahs/MushafAyahStudyController.cs:22-32`) and `GET /api/mushaf/words/{wordLocation}/analysis` (`Backend/api/QuranDashboard.Api/Controllers/MushafReader/Words/MushafWordAnalysisController.cs:19-26`).

**Impact for this dashboard**

This is a finite request, not a permanent RxJS memory leak. It is Medium because it can turn performance cleanup into visible correctness failure even with one user.

**Smallest safe remediation**

Have the load helper return a `Subscription`, retain it in each runner, unsubscribe in `clearPending`, and reset/reload unresolved state on rebind. A lifecycle-bound `switchMap` is also valid if it keeps the current debounce, request-token race protection, source-sensitive cache keys, and explicit retry behavior. Add a leave-while-loading/same-URL-return test for both ayah and word selections.

### F2 — Low — Three small reference/metadata calls lack one shared successful-response cache

**Classification:** Confirmed repeated GETs; trivial backend work.

**Evidence and endpoint mapping**

| Frontend behavior | Repeated endpoint | Backend effect |
|---|---|---|
| Every Mushaf mount calls `loadStudySourceCatalog` at `mushaf-reader-page.component.ts:41-43`; `mushaf-reader.facade.ts:372-385` has no loaded/in-flight guard | `GET /api/mushaf/study-sources` | Three tiny catalogue reads, then a 12-hour backend cache |
| Unique Words builds Word Type options with its own `shareReplay` stream at `features/words/data-access/words-association-options.service.ts:63-78`, while Word Types uses `WordTypesCache` at `features/words/state/word-types-explorer.facade.ts:240-243` | `GET /api/words/word-types/tree` | Two identical browser-session GETs if both features are visited; backend tree is cached 30 minutes |
| Every dashboard mount calls `SystemApi.getDashboardInfo` at `dashboard-home.component.ts:27-51` and `core/data-access/system.api.ts:17-32` | `GET /api/dashboard/info` | Zero SQL; tiny assembly/environment response |

Backend mappings are `MushafStudySourceCatalogController.cs:15-19`, `WordTypesController.cs:33-42`, and `DashboardController.cs:13-21` under `Backend/api/QuranDashboard.Api/Controllers/`.

**Impact for this dashboard**

At three users these are Low. The catalog/tree cases are genuine network duplicates but normally hit server memory; dashboard metadata is cleanliness and one avoidable round trip, not a meaningful database win.

**Smallest safe remediation**

Use one success/in-flight-aware cache per resource. Reuse `WordTypesCacheKeys.tree` rather than maintaining a second tree cache. For the study catalog, keep an explicit loaded/in-flight state so an empty successful catalogue is distinguishable from “not loaded” and failures stay retryable. Cache dashboard info only for the browser application lifetime, so a reload/deployment naturally refreshes it.

### F3 — Low — Unique Words drilldown requests can continue after page destruction

**Classification:** Confirmed finite off-route work; not a permanent leak.

**Evidence**

- `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.ts:222-230` unbinds the list facade but does not close/reset the root drilldown facade.
- The list facade only unsubscribes its own route stream at `.../state/unique-words.facade.ts:99-102`.
- Drilldown subscriptions are cancelled by explicit `closeDrilldown` at `.../state/unique-words-drilldown.facade.ts:131-138`, while summary and detail HTTP subscriptions are created at lines 191-226 and 265-328.

The calls are `GET /api/words/unique/{kind}/{id}` plus its `/surahs`, `/missing-surahs`, or `/ayahs` detail routes, mapped at `Backend/api/QuranDashboard.Api/Controllers/Words/UniqueWordsController.cs:80-184`.

**Impact for this dashboard**

At most one summary or detail request continues per leave-while-loading event, and detail responses are cached. The cost is Low, but the root singleton can update offscreen state.

**Smallest safe remediation**

Cancel/reset drilldown work as part of page/facade unbind while preserving the URL selection required to reload on return. Add a navigation-away test that proves the HTTP subscription is disposed and the same URL can restore cleanly.

### F4 — Low — Two Quran text buttons animate their text color

**Classification:** Quran-rendering safety violation found during the rendering-performance sweep; not a latency win.

**Evidence**

- `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/similar-ayahs-card/similar-ayahs-card.component.html:32-41` renders `item.displayText` inside the navigable text button; its SCSS applies `transition: color 120ms ease` at `similar-ayahs-card.component.scss:46-66`.
- `.../mutashabihat-groups-card/mutashabihat-groups-card.component.html:83-92` renders Quran display text; `mutashabihat-groups-card.component.scss:109-129` applies the same transition.

The installed frontend performance rules prohibit transitions/animations on Quran text. Removing them has no material application-performance benefit; this is a hard rendering-safety cleanup.

**Impact for this dashboard**

There is no meaningful latency impact at three users. The severity is Low because this is a narrow but authoritative Quran-rendering rule violation discovered inside the rendering audit.

**Smallest safe remediation**

Remove only the color transition from these Quran text selectors. Keep the immediate hover color, focus-visible outline, semantic button, RTL direction, and navigation behavior.

## Theoretical or measurement-only frontend notes

These are intentionally not promoted to confirmed performance findings.

### T1 — Unique Words mode navigation may start one quickly-cancelled obsolete request

`Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.ts:264-270` changes path mode and query state together. `.../state/unique-words.facade.ts:85-96` combines independently emitting `paramMap` and `queryParamMap`; each distinct intermediate key reaches `switchMap` at lines 168-195. Installed Angular Router ordering can therefore begin an intermediate `GET /api/words/unique/{kind}` before the final state arrives.

`switchMap`/FetchBackend should abort it promptly, and whether PostgreSQL sees it is timing-dependent. No browser HAR was captured, so this is a Low-priority measurement target. If reproduced, coalesce same-navigation emissions (`auditTime(0)` or an atomic `NavigationEnd` snapshot) and add a mode-plus-query transition test.

### T2 — Ayah detail lists render up to 100 cards without virtualization

All explorer detail page sizes are 100 (`unique-words.models.ts:87`, `roots.models.ts:135`, `lemmas.models.ts:129`, `stems.models.ts:130`, `word-types.models.ts:226`). `ayah-matches-list.component.html:23-63` creates every card inside a fixed scroll viewport (`ayah-matches-list.component.scss:11-15`), and each card renders per-word spans at `highlighted-ayah.component.html:1-10`.

The implementation already has `OnPush`, stable ayah/word keys, and only a 100-ayah upper bound. No browser long-task, frame, or memory trace was captured. Profile a worst-case 100-ayah result before introducing windowing; any change must preserve Quran text, matched-word highlighting, keyboard access, deep links, and the locked 100-result detail contract.

## Audited safe and intentional patterns

- No query-in-loop N+1 was found in the reviewed backend read paths.
- No multi-collection `Include` or cartesian-explosion path was found.
- No count/detail scope drift was confirmed against the locked explorer contracts.
- Main read queries consistently use `AsNoTracking`, scalar SQL, or DTO projections.
- The large translation/tafsir tables use indexed equality predicates; no large-table sequential scan occurred on the API paths.
- Similarity and mutashabihat reads are batched and have bounded live cardinality.
- Dashboard info performs no SQL.
- `ApiResponseCache` deduplicates in-flight GETs, caches only successful responses, and bounds browser entries to 48 at `Frontend/quran-dashboard-ui/src/app/core/caching/api-response-cache.ts:5-38,51-79`.
- Explorer cache keys include result-affecting filter, sort, page, association, and source values. Ayah-study keys include all three selected source keys at `mushaf-reader-cache.ts:6-28`.
- Search inputs use approximately 300 ms debounce and request streams use `switchMap`; no raw HTTP-per-keystroke loop was found.
- Feature routes are lazy loaded.
- The five high-volume Words tables (Unique Words, Roots, Lemmas, Stems, Word Types) use CDK virtual scroll on desktop and stable row tracking.
- Mushaf page/line/word components use `OnPush` and stable keys; no untracked data-row loop was found.
- The Unique Words `COUNT` + page split was measured at 8 ms + 1 ms for its simple base, so it was not treated as equivalent to the expensive repeated Word Types CTE. `COUNT(*) OVER()` is not a blanket recommendation.
- Adjacent Mushaf page prefetch is intentional, bounded, and in-flight/cache-aware.
- Locked Quran count, identity, source, read-only, `ApiResponse`, and `PagedResult` invariants remain requirements for every suggested remediation.

## Recommended fix order

1. **B1 Word Types count/page duplication.** Highest measured repeat cost and isolated to Word Types SQL/read mapping. First add a query-count/timing regression test, then implement the one-statement normal-page path. **Sequential with B3** because both edit `EfWordTypesReader` and its tests.
2. **B2 Lemma/Stem cold summary over-fetch.** High user-visible first-hit value. Lemma and Stem implementations are **independent and safely parallelizable**, provided both preserve deterministic first-occurrence/winner rules and are verified against existing count baselines.
3. **F1 Mushaf request lifecycle.** Independent of backend work and important for correctness. Implement before other Mushaf frontend caching changes so cancellation/rebind semantics are settled.
4. **B3 flat Word Type detail cleanup.** Do after or in the same coordinated branch as B1; reuse the grouped-detail bounded patterns and reassert the locked command ceilings.
5. **B4 ayah-study and word-analysis query consolidation.** The two backend readers are **independent and parallelizable**. Start with joined projections; defer any endpoint split/lazy-resource contract change until after measuring deployment-equivalent database RTT.
6. **F2 small shared client caches.** The Word Types tree and dashboard cache fixes are **independent**. Sequence the Mushaf study-catalog cache after F1 because both touch Mushaf load lifecycle/state.
7. **B6 detail pagination, B5 Mushaf page, and F3 off-route drilldown cleanup.** Low-value, independent cleanup after the measured paths above.
8. **F4 Quran text transition removal.** Tiny, independent safety fix; it can land at any time.

No database index/migration task belongs in the fix queue based on this evidence.

## Verification required after any future fix

- Re-run the exact endpoint with EF command capture and verify the intended cold/warm command budgets.
- Re-run `EXPLAIN (ANALYZE, BUFFERS)` for any changed SQL, in a read-only transaction.
- Assert Word Types `TotalCount` equals the active table scope and keep scope counts at one SQL command.
- Reassert grouped Word Type ceilings (ayahs no more than 3 commands; surahs no more than 2).
- Reassert Quran word/segment/lemma/stem count and completeness invariants; no performance change may weaken an integrity gate.
- For frontend lifecycle fixes, test leave-while-loading, same-URL re-entry, cancellation, retry, cache hit, and stale-response suppression.
- For rendering changes, verify RTL, keyboard/focus behavior, Quran font/text integrity, highlighting, and no Quran text animation.

## Evidence limitations

- No production telemetry, deployment-equivalent network RTT, concurrency/load test, browser HAR, Angular profiler trace, or Core Web Vitals capture was available.
- HTTP timings are local loopback samples; PostgreSQL plan timings are mostly warm-buffer local samples.
- `pg_stat_statements` is not installed. Historical sequential-scan counters cannot be attributed precisely and were not used as evidence of an API missing index.
- Frontend network frequency is derived from deterministic route/component code, not analytics.
- Theoretical notes T1/T2 require a browser trace before implementation.

## Instruction and contract sources consulted

- Root, Backend, and Frontend `CLAUDE.md`/`AGENTS.md`
- `CODING_PRINCIPLES.md`, `PRODUCT.md`, and `DESIGN.md`
- Backend and Frontend root READMEs plus the nearest API, Controllers, Words, MushafReader, core, shared, Words, and Mushaf READMEs
- Backend API and structure guidelines; frontend structure and API-integration guidelines
- Backend and frontend installed performance-review skills and Quran data-safety reference
- `docs/contracts/` index, HTTP API, response envelope, Mushaf Reader, and Words explorers pointers
- Feature 026 locked decision record, active specification, plan, contracts, and quickstart
- Current database tables/relationships report and relevant EF configurations/migrations
