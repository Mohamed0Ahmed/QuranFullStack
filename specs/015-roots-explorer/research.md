# Phase 0 Research: Quran Roots Explorer

All decisions below are **pre-verified** by three prior reports and direct read-only database checks.
No `NEEDS CLARIFICATION` items remain.

Sources:
- `Backend/report/feature-015-roots-explorer/roots-explorer-capability-analysis-report.md`
- `Backend/report/feature-015-roots-explorer/roots-explorer-readonly-verification-report.md`
- `Frontend/report/feature-015-roots-explorer/roots-explorer-frontend-ux-contract-report.md`
- `docs/feature-015-roots-explorer/feature-015-roots-explorer-combined-implementation-plan.md`
- Feature 014 backend + frontend implementation.

---

## D1 — Feasibility from existing data (read-only)

- **Decision**: Build entirely from existing tables; no importer, no new pipeline, no Quran-text changes.
- **Rationale**: Roots → words via `quran_word_morphology.root_id` (indexed) joined to `quran_words` by PK gives every needed aggregate; `quran_roots` already precomputes `words_count` and `distinct_lemmas_count`.
- **Alternatives considered**: New denormalized count columns on `quran_roots` (rejected — needs migration + importer change; aggregation is cheap enough).

## D2 — List read strategy: compute-once + cache-whole-list

- **Decision**: One grouped aggregation produces all 1,642 roots with all 8 counts; cache the whole list once; apply **search → sort → page in memory** over the cached list.
- **Rationale**: Verified cost ~30–115 ms for the full aggregation (warm 31–36 ms); the dataset is small (1,642 rows) and immutable at runtime, so a single cached structure serves all list requests. In-memory search removes any unbounded free-text cache key while avoiding a DB round-trip.
- **Alternatives considered**: Per-request DB query with `ILIKE` + sort + page and cache-bypass on search (F014's exact pattern) — rejected as the primary approach because it re-aggregates per request; kept as a documented fallback if in-memory filtering is undesired.

## D3 — Count semantics (the 8 columns)

- **Decision**: occurrences = `quran_roots.words_count`; ayahs = `DISTINCT quran_words.ayah_id`; surahs = `DISTINCT surah_number`; simple words = `DISTINCT unique_simple_word_id`; tashkeel words = `DISTINCT unique_tashkeel_word_id`; **lemmas = `DISTINCT quran_word_morphology.lemma_id` (co-occurrence)**; stems = `DISTINCT stem_id`.
- **Rationale (verified)**: `words_count` reconciles exactly with morphology `COUNT(*)` for all 1,642 roots; `distinct_lemmas_count` equals morphology `COUNT(DISTINCT lemma_id)` for all 1,642 roots.
- **Alternatives considered & rejected**: lemmas via `COUNT(quran_lemmas WHERE root_id)` (dominant/ownership semantics) — differs for 49 roots (41 lemmas appear under >1 root; 153 lemmas have no root link). **Not used.** The table column and the lemmas tab MUST both use co-occurrence and MUST agree.

## D4 — Stems derivation

- **Decision**: Derive stems-per-root via `quran_word_morphology` (`DISTINCT stem_id WHERE root_id = X`), joined to `quran_stems` for text.
- **Rationale**: `quran_stems` has **no** `root_id` column (verified). Bounded (worst root ≈ 84 stems) and cheap; not a blocker.
- **Alternatives considered**: A direct stem→root link (does not exist).

## D5 — Unique-word links for navigation

- **Decision**: Use `quran_words.unique_simple_word_id` / `unique_tashkeel_word_id` for the words sub-views and to deep-link into the existing Unique Words detail flow.
- **Rationale (verified)**: 0 root-bearing words are missing either link (all 50,298 have both). Reliable for columns 5/6 and navigation.

## D6 — Verse highlighting

- **Decision**: Reuse F014's word-ID highlighting — response carries `matchedQuranWordIds` (exact `quran_words.id` values) plus the ordered ayah words; the frontend `highlighted-ayah` component marks a word iff its ID is in the set. Matched set for a root = `quran_words.id` where `root_id = X` in that ayah.
- **Rationale**: No string replacement, no Quran-text mutation; accessible (marker/label, not color-only); reuses a working component unchanged.

## D7 — No migration / no index

- **Decision**: Ship with no schema change and no new index.
- **Rationale (verified)**: Full aggregation uses optimal seq-scans (index-irrelevant for a whole-table aggregate); single-root detail already uses `IX_quran_word_morphology_root_id` (bitmap index scan ~0.24 ms; worst-case detail 27 ms). No measured evidence supports adding an index.

## D8 — Caching & logging conventions

- **Decision**: Reuse the F014 `Cached*Reader` decorator over the already-registered shared `IMemoryCache`; add a `roots:` key namespace; no global cache reconfiguration; cache for process lifetime (immutable data). Structured logs at the Application handler boundary: `feature`, `operation`, `rootId`, `view`, `subView`, `pageNumber`, `pageSize`, `sort`, `hasSearch`, `totalCount`, `itemCount`, `cacheResult`, `elapsedMs`; never log Quran/root/word/raw-search text.
- **Rationale**: Matches existing observability direction and safe-field rules; key space bounded.

## D9 — Frontend reuse & detail UX

- **Decision**: New `/dashboard/words/roots` page inside `features/words/`; persistent independent-scroll side panel (no modal; drawer on narrow screens). Reuse `highlighted-ayah`, surah/ayah list components, `word-count-chip`, the **shared `qd-pagination`**, `ApiResponseCache`, and `buildUniqueWordsDeepLink`. Model a `roots-detail.facade` on the F014 drilldown facade (lazy-per-view + cache + restore-from-URL) but as a panel, not a modal.
- **Rationale**: Maximizes reuse; respects `FRONTEND_STRUCTURE.md` (thin shell, URL-state tabs, shared primitives) and the user's locked UX decisions.
- **Alternatives considered**: Reusing the `word-drilldown-modal` shell — rejected (brief forbids modal as the primary desktop detail); old `unique-words-list-pagination` — rejected (shared `qd-pagination` now exists).

## D10 — Resolved open questions (documented defaults)

| Question | Decision |
|---|---|
| Sortable options | The three list-sort keys only: `mushaf-order` (default), `occurrences`, `alpha`. Individual numeric columns are not separately sortable. |
| Word-row count scope | Show in-root occurrence count; destination Unique Words detail shows global counts (expected). |
| Zero-count cells | Remain clickable; open the relevant tab in an empty state. |
| Panel placement | Inline-end (RTL-aware) on desktop; dismissible drawer on narrow screens. |
| Detail page sizes | Fixed sensible defaults (verses ≈100, words ≈50–100); not URL params or user settings in this version. |

## Open risks carried into design (mitigated)

- Lemma column/tab divergence → both locked to co-occurrence + a regression test on a known divergent root.
- N+1 on ayah reads → reuse F014 batched page-load; assert bounded command count via `SqlCommandCountInterceptor`.
- Eager detail in the list → table returns counts only; assert no detail API calls on list render.
- jsdom lacks `matchMedia`/`ResizeObserver` → guard and default desktop; keep CDK virtual-scroll observer fallback.
- Frontend test OOM → keep `VITEST_MAX_FORKS` cap.
