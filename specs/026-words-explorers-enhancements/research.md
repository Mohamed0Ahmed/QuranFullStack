# Research: Words Explorers Enhancements (Feature 026)

**Date**: 2026-07-14 · **Inputs**: read-only inspection of the five Words explorers
(2026-07-14), the authoritative decision record
(`docs/feature-026-words-explorers-enhancements/plan.md`), and the spec's
`## Clarifications` session. No `NEEDS CLARIFICATION` items remain.

## R1 — Where the Word Types search predicate lives

- **Decision**: one parameterized predicate on the shared scoped occurrence base
  (`BaseRowsSql` in `EfWordTypesReader.Sql.cs`), matching
  `quran_words_unique_tashkeel.search_text_normalized` via normalized `ILIKE @searchPattern`.
- **Rationale**: all four tableViews and the new scope-counts read derive from that
  base verbatim (reads README invariant), so a single predicate keeps words view,
  grouped views, and the four counts mutually consistent — which the spec's equality
  criterion (FR-016) requires. Identity rule (clean imlaei-simple) is respected;
  dimension display text is never searched (Locked A1 + reconciliation A1×C2).
- **Alternatives considered**: (a) search only the words view — rejected: grouped
  totals would disagree with the four counts under search, violating FR-016;
  (b) search dimension display text on grouped views — rejected: breaks A1 and mixes
  match grains; (c) post-filter in memory — rejected: 77k-row base, pagination happens
  in SQL.

## R2 — Arabic normalization reuse

- **Decision**: extract `EfUniqueWordsReader.NormalizeArabicQuery` into a shared
  internal helper in `Reads/Quran/Words/`; both readers consume it; Unique Words
  behavior pinned by existing tests before reuse.
- **Rationale**: the two search boxes must treat diacritics/orthography identically;
  copy-paste would drift.
- **Alternatives**: duplicate the method (drift risk); PostgreSQL-side normalization
  function (schema/DB object change — forbidden by B3/non-goals).

## R3 — Page-size caps (Word Types)

- **Decision**: split `WordTypesHandlerValidation.MaxPageSize = 100` into
  `MaxListPageSize = 1000` and `MaxDetailPageSize = 100`; list default 25→1000; detail
  defaults 25→100 in both controllers; frontend constants aligned.
- **Rationale**: the single constant currently gates list AND detail reads; raising it
  wholesale would silently raise the documented grouped-detail `pageSize 1..100`
  contract (reads README). Split preserves the documented detail cap while unlocking
  list parity.
- **Alternatives**: raise the single cap to 1000 (contract drift on detail reads —
  rejected); keep list cap 100 and page client-side (violates Locked A2).

## R4 — 1000-row rendering

- **Decision**: adopt `CdkVirtualScrollViewport` in `word-types-table`, mirroring
  `roots-table` (`useVirtualScroll = HAS_RESIZE_OBSERVER` guard, shared
  `explorer-table-scroll.ts` wiring).
- **Rationale**: the four 1000-row explorers already use exactly this pattern and are
  smooth; Word Types rows carry interactive chip buttons — 1000 static rows without
  virtualization is a known DOM cost.
- **Alternatives**: plain rendering (jank risk, SC-003 fails); infinite scroll /
  incremental fetch (new paging contract — out of scope).

## R5 — Range-filter URL grammar

- **Decision**: canonical `min..max` per metric key, either side omissible; the URL
  stores the actual range; bucket chips are presentation only; malformed values parse
  fail-closed (filter absent).
- **Rationale**: shareable links stay stable when bucket thresholds are tuned later
  (clarified buckets are v1 presets, not contracts); fail-closed matches the words
  feature's existing URL discipline.
- **Alternatives**: bucket IDs in URL (threshold changes break links); separate
  `<k>Min`/`<k>Max` URL params per metric (2× key count, noisier URLs — still used at
  the HTTP layer where nullable ints are idiomatic).

## R6 — Preset bucket thresholds (clarified)

- **Decision** (spec Clarifications, disjoint boundaries): occurrences
  1 · 2–10 · 11–100 · 101–1000 · 1001+; ayahs/surahs 1 · 2–10 · 11–50 · 51+
  (surahs ≤ 114); word/lemma/stem sub-counts 1 · 2–5 · 6–20 · 21+; every metric row
  also offers مخصّص (custom min/max).
- **Rationale**: spans the real distributions (occurrences reach tens of thousands;
  surahs cap at 114); "1" isolates hapax-style singletons researchers care about.
- **Alternatives**: uniform coarse buckets (less tailored); data-driven quartiles
  (arbitrary-looking boundaries, extra analysis step) — both offered and declined in
  clarification.

## R7 — Where each filter predicate executes

- **Decision**: Unique Words → SQL predicates inside the existing raw list queries;
  Roots/Lemmas/Stems → in-memory predicates in their `*ListDerivation` (whole-summary
  cached, derivation per request); Word Types has-flags → allowlisted
  `IS [NOT] NULL` predicates on `m.root_id|stem_id|lemma_id` in `BaseRowsSql`.
- **Rationale**: follow each page's existing read mechanism — Unique Words already
  filters in SQL (21k rows), the trio already derives in memory over a cached whole
  summary (1.6k–12k rows), Word Types already composes scope predicates in
  `BaseRowsSql`. No mechanism changes, so no cache-architecture changes: only Unique
  Words and Word Types backend cache keys gain components.
- **Alternatives**: unify all filtering into SQL (forces trio cache redesign for no
  user-visible gain); unify in memory (Unique Words/WordTypes datasets too large).

## R8 — Association-filter semantics

- **Decision**: Unique Words `primaryType`/`rootId` predicates must reproduce exactly
  the primary-selection rules the page's display enrichment uses
  (`LoadPrimaryWordTypesAsync` / `LoadPrimaryRootsAsync`); Lemmas `rootId` uses the
  real FK; Stems `rootId`/`lemmaId` use the derived *primary* association surfaced on
  their list rows, labeled "الجذر الأساسي" / "الصيغة المعجمية الأساسية".
- **Rationale**: the filter must never contradict the chip the row displays (spec
  FR-009); stems have no FK to roots/lemmas — the honest, existing-data option is the
  primary association, stated as such (Locked B2).
- **Alternatives**: stems filtered by ALL co-occurring roots/lemmas (needs new
  derivation over morphology co-occurrence — heavier, and the row's displayed
  association could then disagree with the filter); adding FK columns (schema —
  forbidden).

## R9 — Scope-counts read shape

- **Decision**: one new read `GET api/words/word-types/scope-counts` returning
  `WordTypeScopeCountsDto(WordsCount, RootsCount, StemsCount, LemmasCount)`; ONE SQL
  command — CTE over the scoped `BaseRowsSql` base + four aggregates byte-consistent
  with `RowsCountSql` / the three `GroupedRowsCountSql`; cached with every scope input
  in the key.
- **Rationale**: Locked C2 prefers one round-trip; equality with the four tableView
  totals (FR-016) is guaranteed by reusing the same SQL fragments instead of
  re-deriving; single-command budget is testable via the existing
  `SqlCommandCountInterceptor` pattern.
- **Alternatives**: four separate count calls (4 round-trips, race-prone consistency);
  frontend computing counts from loaded pages (wrong — pages are subsets);
  piggybacking counts on the table response (couples scope-level data to per-page
  cache entries and reloads counts on page changes).

## R10 — Result-count stat (normal explorers)

- **Decision**: surface the existing `PagedResult<T>.TotalCount` from list state; no
  new backend read. Phrasing (clarified): label-prefix "عدد الـ…: N" — عدد الكلمات /
  عدد الجذور / عدد الصيغ المعجمية / عدد الأصول الصرفية.
- **Rationale**: the number already exists and already reflects search+filters (it is
  the filtered query's total); label-prefix sidesteps Arabic تمييز number-agreement
  with dynamic digits and reads scholarly-plain.
- **Alternatives**: bare `N + noun` (grammatically loose); inflected تمييز (agreement
  varies by number class — error-prone with dynamic values); new count endpoint
  (pointless duplication — violates C1).

## R11 — Four-count strip placement (clarified)

- **Decision**: between the type-filter strip and the table-view tabs
  (filters → scope summary → tabs → table); strip is non-interactive; own
  loading/error/zero states; must not break the mounted-shell invariant.
- **Rationale**: counts summarize the scope configured directly above and label the
  views switchable directly below; RTL top-down reading order matches cause → summary
  → views.
- **Alternatives**: below tabs (separates tabs from table); page-header KPI band
  (detaches counts from the filters that produce them) — offered and declined.

## R12 — Terminology (post-clarification correction)

- **Decision**: user-facing labels use the app's live terms — stem = "الأصل الصرفي"
  (pl. "الأصول الصرفية"), lemma = "الصيغة المعجمية" (pl. "الصيغ المعجمية"), root =
  "الجذر" (pl. "الجذور"); "الجذع"/"اللمّة" are internal reference terms only. The
  four-count strip reuses the existing tabs' short labels verbatim
  (كلمات | جذور | أصول | صيغ) — tabs are not renamed; full terms stay canonical for
  standalone labels (stat lines, filter labels).
- **Rationale**: verified against the words feature's live label files; introducing a
  second user-facing synonym pair would violate the feature's own "label names the
  dimension" rule and confuse existing users.
- **Alternatives**: adopt "الجذع/اللمّة" everywhere (would rename existing UI —
  unrelated churn, rejected).

## R13 — Performance risk envelope

- **Decision**: mandatory P1 perf gate — time `RowsSql`/`GroupedRowsSql` at
  `pageSize=1000` on worst scopes (unscoped verb; stems grouped ≈ 12,108 groups
  counted before pagination) and measure cache-entry growth; P4 gate — scope-counts
  SQL timed on the widest scopes. Hard failure at default 1000 → stop condition
  (user sign-off to reduce the default while keeping the 1000 cap).
- **Rationale**: grouping-before-pagination is the only query shape whose cost scales
  with the page-size change; dataset ceilings are known (12,108 stems worst case), so
  the gate is cheap to run and decisive.
- **Alternatives**: skip measurement (risk lands in production); pre-aggregations or
  indexes (schema — forbidden).
