# Lemmas Explorer Current-State Polish Report

Date: 2026-06-28

Branch: `017-lexical-explorers-polish`

Target page: `/dashboard/words/lemmas` (`الصيغ المعجمية`)

Scope: inspection/report only. No production code, tests, specs, migrations, frontend files, importers, or data were changed.

## Inspection Inputs

- Repository source inspected across Backend Lemmas reader/contracts/tests and Frontend Lemmas page/components/models.
- Live local database inspected with read-only `psql` queries against `quran_dashboard`.
- Current DB schema confirms `quran_word_morphology_segments.lemma_id`, `root_id`, and `pos` exist; `stem_id` does not exist on segments.
- `pg_isready` was blocked by the LeanCTX shell allowlist, so DB readiness was inferred from successful read-only `psql` queries.

## Live DB Findings

- `quran_word_morphology_segments` columns found: `lemma_id: integer NULL`, `root_id: integer NULL`, `pos: text NOT NULL`.
- `stem_id` returned no column row from `information_schema.columns`; this matches the intentional decision not to add segment `stem_id`.
- Segment indexes present: `IX_quran_word_morphology_segments_lemma_id`, `IX_quran_word_morphology_segments_root_id`, and filtered `IX_quran_word_morphology_segments_stem` on `quran_word_id WHERE kind = 'STEM'`.
- Live compound sample `quran_words.id = 4651`, text `أَلَّا`, word-level `head_pos = SUB`, word-level lemma `لَا`.
- Matching segments for `أَلَّا`: segment 1 `pos = SUB`, `lemma_id = 462`, lemma `أَن`; segment 2 `pos = NEG`, `lemma_id = 77`, lemma `لَا`.
- For lemma `لَا` (`id = 77`), current word-level distribution produces `ACC 1`, `INT 5`, `NEG 1364`, `PRO 327`, `SUB 40`; segment-matched distribution produces only `NEG 1406`, `PRO 332`.
- For lemma `أَن` (`id = 462`), current word-level distribution produces `INT 42`, `SUB 535`; segment-matched distribution produces `INT 47`, `SUB 578`.
- 48 words / 43 ayahs have a matching `أَن` segment while the word-level lemma is not `أَن`; current word-level Lemma reads miss those occurrences.

## 1. Main Lemmas Table

Current state:

- Current table renders nine headers: row number, lemma, root, occurrences, ayahs, surahs, simple words, tashkeel words, stems.
- No `نوع` column exists in `lemmas-table.component.html`.
- Current table headers come from `LEMMAS_COLUMN_HEADERS` in `lemmas.labels.ts`, not hardcoded in the table template.
- Current main-table labels are `كلمات بدون تشكيل` and `كلمات بالتشكيل`.
- Current words-tab labels are the shorter `بدون تشكيل` and `بالتشكيل`.
- Header text is centralized, but `LEMMAS_COLUMN_HEADERS` and `LEMMAS_COLUMN_COUNT_LABELS` duplicate the same count label strings.

Recommendation:

- No removal work needed for the main table type column; it is already absent.
- Keep the table type-free because one lemma can legitimately map to multiple segment POS values.
- Optional low-risk polish: if the desired final table headers are exactly `بدون تشكيل` and `بالتشكيل`, change only `LEMMAS_COLUMN_HEADERS.simpleWords` and `LEMMAS_COLUMN_HEADERS.tashkeelWords`; leave count chip aria labels as fuller `كلمات بدون تشكيل` / `كلمات بالتشكيل` if clarity is preferred.

Evidence:

- `Frontend/quran-dashboard-ui/src/app/features/words/components/lemmas-table/lemmas-table.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/models/lemmas.labels.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/lemmas-table/lemmas-table.component.spec.ts`

## 2. Lemma Details Panel Type Distribution

Current state:

- The details panel shell is pure chrome; type distribution data comes from `LemmaSummaryDto.TypeDistribution`.
- `EfLemmasReader.LoadWholeSummaryAsync` still calculates lemma type distribution from `quran_word_morphology.lemma_id` and `quran_word_morphology.head_pos`.
- Current type distribution does not use `quran_word_morphology_segments.lemma_id` or segment `pos`.
- Frontend no longer shows the old always-visible type distribution block in Lemmas details; it uses type filter chips in the Ayahs tab.

Required behavior:

- For a selected lemma, type distribution must be based on matching segment rows: `quran_word_morphology_segments.lemma_id = selectedLemmaId` and `quran_word_morphology_segments.pos`.
- For `أَلَّا = أَن + لَا`, the `أَن` occurrence must classify as `SUB`, and the `لَا` occurrence must classify as `NEG`.
- The word-level `head_pos = SUB` of `أَلَّا` must not make the `لَا` occurrence appear as `SUB`.

Recommendation:

- Replace Lemma summary type-distribution aggregation with a segment-based aggregation joined to `quran_pos_tags` on `segments.pos`.
- Preserve existing ordering contract: count descending, earliest Mushaf occurrence ascending, then POS code.
- Count matching segment occurrences, not whole-word head POS occurrences.
- Keep `quran_lemmas.root_id` as the owned-root source; do not infer root identity from segment joins for the main list/root field.

Evidence:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs`
- `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/WordMorphologySegment.cs`
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Words/Morphology/WordMorphologySegmentConfiguration.cs`

## 3. Ayah Matches Type Filter

Current state:

- Frontend already renders compact Lemma type filter chips only in the Ayahs tab.
- Default chip is `عرض الكل`.
- URL state uses `typeCode` and clears stale `typeCode` when the loaded summary no longer contains the code.
- Backend `GetLemmaAyahMatchesAsync` still filters ayahs and matched words with `m.LemmaId == id` and optional `m.HeadPos == typeCode`.
- Filter options are based on `summary.typeDistribution`, which currently comes from word-level `head_pos`.

Required backend query behavior:

- The ayah set must come from segment matches: `segments.lemma_id = @lemmaId` and, when supplied, `segments.pos = @typeCode`.
- `totalCount` must count distinct ayahs from the segment-matched set.
- Highlighting must mark a word as matched when that word has at least one segment matching the selected lemma and selected POS filter.
- The visible words in each ayah should continue excluding ayah-marker words.
- Unknown/invalid `typeCode` can keep current behavior: successful empty page, not validation failure.
- Filter options must come from the same selected-lemma segment set, so `لَا` should offer `NEG` / `PRO`, not word-level `SUB` / `INT` / `ACC` artifacts.

Recommendation:

- Change `GetLemmaAyahMatchesAsync` to join `WordMorphologySegments` and `QuranWords`; do not use `WordMorphologies.HeadPos` for Lemma type filtering.
- Use `Distinct()` for ayah IDs and word IDs to avoid duplicate highlights if a future word has multiple matching segments.
- Keep `typeCode` in URL and API query string unchanged.

Evidence:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-ayah-type-filters/lemma-ayah-type-filters.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.ts`

## 4. Word Rows Inside Lemma Details

Current state:

- Backend simple word projection already uses `w.UniqueSimpleWordId` and `w.TextUthmaniSimple`.
- Backend tashkeel word projection already uses `w.UniqueTashkeelWordId` and `w.TextUthmani`.
- Frontend displays `LemmaWordItemDto.displayText` for both tabs and builds Unique Words deep links with `word={uniqueWordId}`.
- Existing backend tests already assert simple display text without tashkeel and tashkeel display text with tashkeel.

Recommendation:

- No display-field backend change is needed for the simple/tashkeel word rows.
- Keep simple view backed by `TextUthmaniSimple`; do not regress to `TextUthmani`.
- Note: `GetLemmaWordsAsync`, mentioned/missing surahs, and related stems still filter by word-level `m.LemmaId`. If the product decision is that all Lemma detail tabs must be segment-complete, those methods need a follow-up segment-aware pass. If this polish is limited to type correctness, keep that broader semantic pass separate.

Evidence:

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/LemmasWordsReadTests.cs`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-words-list/lemma-words-list.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/lemma-words-list/lemma-words-list.component.ts`

## 5. `السائد` Label

Current state:

- Visible `السائد` is not rendered by the current shared type distribution component.
- Current shared component keeps non-visible dominant semantics via `qd-is-selected`, `aria-current="true"`, and `data-testid="type-distribution-dominant"`.
- The component test explicitly asserts that rendered text does not contain `السائد`.
- Lemmas page does not render `qd-type-distribution-list`; Stems page still uses it.

Recommendation:

- No visible-label removal is needed; current UI already satisfies the requirement.
- Keep non-visible dominant semantics for accessibility and test targeting.
- Shared Stems side effect is already in place and acceptable: Stems no longer receives visible `السائد` from the shared component either.

Evidence:

- `Frontend/quran-dashboard-ui/src/app/features/words/components/type-distribution-list/type-distribution-list.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/type-distribution-list/type-distribution-list.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/type-distribution-list/type-distribution-list.component.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html`

## 6. Response Shape Cleanup

Current state:

- Lemma list DTO is already compact: ids, lemma/root display fields, and counts.
- Lemma summary DTO adds `typeDistribution` only.
- Lemma word item DTO returns `uniqueWordId`, `displayText`, and `occurrencesCount`.
- Lemma ayah match DTO returns ayah metadata plus word text and `isMatched`; it does not expose raw Buckwalter or raw morphology fields.
- Frontend does not render `TypeSummaryDto.firstSurahNumber`, `firstAyahNumber`, or `firstWordNumber`; they are used as backend ordering evidence and remain part of the shared Lemmas/Stems type summary contract.

Recommendation:

- Do not perform response-shape cleanup in this polish slice unless broad churn is explicitly accepted.
- Defer any removal of `firstSurahNumber`, `firstAyahNumber`, and `firstWordNumber` because `TypeSummaryDto` is shared by Lemmas and Stems and test fixtures currently model those fields.
- Do not change canonical URL identity: keep `lemma={lemmaId}`, `root={rootId}`, and `word={uniqueWordId}`.
- Do not add text-based URL identity or Buckwalter fields to frontend models for this work.

Evidence:

- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/Responses/LemmaListItemDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/Responses/LemmaSummaryDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/Responses/LemmaWordItemDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Lemmas/Responses/LemmaAyahMatchDto.cs`
- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Morphology/Responses/TypeSummaryDto.cs`
- `Frontend/quran-dashboard-ui/src/app/features/words/models/lemmas.models.ts`

## 7. Stems Separation

Must not include:

- No Stems Explorer redesign.
- No segment `stem_id` addition.
- No Stems-specific query rewrites in this Lemmas-only polish.
- No Roots changes.
- No shared DTO/component cleanup if it causes Stems churn.

Allowed shared cleanup:

- Shared `type-distribution-list` cleanup is already complete and low-risk.
- If future work touches `TypeSummaryDto`, treat it as shared Lemmas/Stems API work, not this Lemmas-only polish.

## 8. Testing Impact

Backend tests to add/update:

- Add fixture coverage for a compound word like `أَلَّا = أَن + لَا` with segment `lemma_id` and `pos` rows.
- Add `GetLemmaSummary` test proving type distribution comes from matching segment POS, not word-level `head_pos`.
- Add `GetLemmaAyahs` test proving selected lemma `لَا` filters `أَلَّا` as `NEG`, not `SUB`.
- Add `GetLemmaAyahs` test proving selected lemma `أَن` includes `أَلَّا` occurrences even when word-level lemma is `لَا`.
- Add type-filter empty test for a word-level-only false POS such as `SUB` on lemma `لَا`.
- Keep/update bounded query count test because segment joins may add one query but must not introduce per-ayah N+1.
- Keep existing simple/tashkeel word display tests; they already assert `TextUthmaniSimple` vs `TextUthmani` behavior.

Frontend tests to add/update:

- Keep existing main table tests that assert no type column; strengthen with an explicit visible-header negative assertion for `نوع` if not brittle.
- Keep existing `LemmaAyahTypeFiltersComponent` tests for default `عرض الكل`, emitted `typeCode`, and loading state.
- Add/keep Lemmas page test that filter chips are populated only from `summary.typeDistribution` for the selected lemma.
- Keep existing stale `typeCode` clearing test.
- Keep existing `TypeDistributionListComponent` test asserting no visible `السائد`.
- Add/keep word list/page test that simple and tashkeel tabs render the API `displayText` and preserve `word={uniqueWordId}` links.

## 9. Risk Assessment

Migration risk: low.

- Required columns and indexes already exist in current DB.
- No migration is needed for the minimal segment-matched reader fix.
- Do not add `stem_id` to segments.

API contract risk: low if DTO shapes stay unchanged.

- TypeDistribution values will change scientifically for compound/multi-segment cases, but the JSON shape can remain the same.
- Removing frontend-unused `TypeSummaryDto` fields would raise contract/test churn and should be deferred.

Frontend regression risk: low.

- Main table, word tabs, ayah chips, URL `typeCode`, and `السائد` removal are already implemented.
- Main frontend dependency is corrected backend `summary.typeDistribution` and ayah match results.

Stems shared-component risk: low.

- Shared `type-distribution-list` already has no visible `السائد`.
- Avoid shared DTO cleanup in this slice to prevent Stems churn.

Performance risk: medium.

- Segment joins add query cost to summary and ayah reads.
- Existing `lemma_id` and `pos` indexes reduce risk, but there is no observed composite `(lemma_id, pos)` index.
- Keep query shape bounded, avoid per-ayah queries, and validate with existing command-count tests plus real local endpoint/DB checks.
- If future performance data shows need for a composite index, handle it as a separate migration-backed performance task, not this polish.

## 10. Final Recommendation

Verdict: READY_WITH_NOTES

Implementation slice order:

1. Backend test fixture slice: add/extend Morphology Explorer seed cases for `أَلَّا`/compound segment lemma+POS matching.
2. Backend reader slice: update Lemma summary type distribution and Lemma ayah matches/type filter to use `quran_word_morphology_segments.lemma_id` + `pos`.
3. Backend verification slice: update affected Lemmas reader tests, especially compound inclusion/type correctness and bounded query count.
4. Frontend test slice: keep current UI behavior, strengthen tests for no main-table `نوع`, no visible `السائد`, chip options from selected summary, and simple/tashkeel display.
5. Optional follow-up decision: decide whether `GetLemmaWords`, mentioned/missing surahs, and related stems should become segment-complete for selected lemma IDs; keep out of the minimal type-filter polish unless explicitly approved.

Minimal implementation plan:

- Do not touch migrations, importers, Roots, or Stems-specific UI.
- Do not redesign the page.
- Do not change URL identity.
- Do not clean response shapes in this slice.
- Do change backend Lemmas type semantics from word-level `head_pos` to matching segment `pos`.
