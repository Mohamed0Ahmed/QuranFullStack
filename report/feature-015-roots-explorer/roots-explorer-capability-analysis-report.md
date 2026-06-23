# Feature 015 — Quran Roots Explorer — Capability / Feasibility Analysis

> **Review-only.** No code, migrations, importers, or schema changes were produced. This report
> inspects the current data model and the Feature 014 (Unique Words Explorer) implementation to
> decide whether the requested Roots Explorer can be built, and on what terms. It does **not**
> constitute a plan; it is the input to a later Spec Kit `plan` step.

| Item | Value |
| --- | --- |
| Feature | 015 — Quran Roots Explorer |
| Proposed route | `/dashboard/words/roots` |
| Reference feature | 014 — Unique Words Explorer (`/dashboard/words/unique`) |
| Data posture | Read-only over existing morphology + words tables |
| Evidence basis | DB baseline report, domain entities, morphology importer, F014 backend + frontend, architecture docs |
| **Verdict** | **READY_WITH_NOTES** |

---

## 0. Method and evidence

Inspected, read-only:

- `Backend/report/database/current-database-tables-and-relationships-report.md` — table inventory, row counts, indexes, FKs.
- Domain entities: `QuranRoot.cs`, `QuranLemma.cs`, `QuranStem.cs`, `WordMorphology.cs`, `QuranWord.cs`, `UniqueSimpleWord.cs`, `UniqueTashkeelWord.cs` (`Backend/domain/.../Quran/Words/...`).
- Morphology importer count semantics: `MorphologyAssembler.cs` (`DimensionEntry.AddWord`) and `MorphologySourceData.cs`.
- Feature 014 backend: `UniqueWordsController.cs`, `IUniqueWordsReader.cs`, `EfUniqueWordsReader.cs`, `CachedUniqueWordsReader.cs`, `UniqueWordsCacheKeys.cs`, `UniqueWordsDependencyInjection.cs`, the five `Get*` handlers, the response DTOs.
- Feature 014 frontend: feature folder under `src/app/features/words/`, `unique-words-url-sync.ts`, `frontend-routing-state.md` contract.
- Architecture docs: `API_GUIDELINES.md`, `LOGGING_GUIDELINES.md`, `CLAUDE.md`/`Backend/CLAUDE.md`.

No live database queries were run; numeric latency claims below are **estimates** grounded in row
counts and existing indexes, with exact read-only verification SQL provided in §8.4 for the planning step.

---

## 1. Can we build the requested Roots Explorer from current data?

**Yes.** Every requested capability maps onto data that already exists, and every requested
behavior has a working precedent in Feature 014. No importer, no new pipeline, and no migration is
required for the core feature.

Key facts that make this true:

- `quran_roots` (1,642 rows) already carries two of the requested counts **precomputed**:
  `words_count` and `distinct_lemmas_count` (see `QuranRoot.cs` and §2).
- The root → word relationship exists through `quran_word_morphology.root_id`
  (indexed; 77,432 rows, one per readable word) joined to `quran_words` by primary key
  (`quran_word_morphology.quran_word_id = quran_words.id`).
- Each readable word already carries `unique_simple_word_id` and `unique_tashkeel_word_id`
  (`QuranWord.cs`), which are exactly the identifiers Feature 014 uses for its word-detail flow.
  This is what makes the "click a simple/tashkeel word → open the existing word detail" behavior a
  data lookup, not a new join to design.
- `quran_lemmas.root_id` is indexed, so lemmas-per-root is a direct filtered read.
- Feature 014 already proves the hard parts: ID-based ayah highlighting, paged ayah matches,
  mentioned/missing surahs, a caching decorator, and URL-driven drill-down state. Roots reuse the
  same shapes with a different filter key.

**The single genuine caveat** (hence READY_WITH_*NOTES*, not READY_FOR_PLAN): five of the eight
requested table columns are **not** precomputed and must be produced by aggregation. They are all
cheaply derivable, but the planning step must pick how/when to compute them (§2, §4) and confirm a
small set of data assumptions (§8.4).

---

## 2. Which desired counts are directly available, and which need queries/aggregation?

The table requests eight columns. Status of each:

| # | Requested column | Source | Status |
| --- | --- | --- | --- |
| 1 | root text | `quran_roots.root_text` | **Direct** |
| 2 | occurrences / المواضع | `quran_roots.words_count` (precomputed) | **Direct** |
| 3 | ayahs count | `COUNT(DISTINCT quran_words.ayah_id)` over root's words | **Aggregate** |
| 4 | surahs count | `COUNT(DISTINCT quran_words.surah_number)` over root's words | **Aggregate** |
| 5 | simple words count | `COUNT(DISTINCT quran_words.unique_simple_word_id)` over root's words | **Aggregate** |
| 6 | tashkeel words count | `COUNT(DISTINCT quran_words.unique_tashkeel_word_id)` over root's words | **Aggregate** |
| 7 | lemmas count / الصيغ المعجمية | `quran_roots.distinct_lemmas_count` (precomputed); equivalently `COUNT(quran_lemmas WHERE root_id = X)` | **Direct** |
| 8 | stems count / الأصول الصرفية | `COUNT(DISTINCT quran_word_morphology.stem_id)` where `root_id = X` | **Aggregate** |

### 2.1 Why `words_count` == occurrences (verified from importer)

In `MorphologyAssembler.cs`, `DimensionEntry.AddWord` is called once per readable word that carries
the root and simply does `WordsCount++`. So `quran_roots.words_count` is the **total occurrence
count** of the root across the mushaf — exactly the المواضع column. It is **not** a distinct-word
count, which is why columns 5/6 (distinct simple/tashkeel words) are separate aggregations.

### 2.2 The asymmetry to flag

- **Lemmas-per-root is precomputed** (`distinct_lemmas_count`) and also independently available via
  the indexed `quran_lemmas.root_id`. Either source is fine; they should agree.
- **Stems-per-root is NOT precomputed and has no shortcut**: `quran_stems` has no `root_id` column
  (confirmed in `QuranStem.cs` and the DB report). Stems-per-root must be aggregated through
  `quran_word_morphology` (`COUNT(DISTINCT stem_id) WHERE root_id = X`). This is the one place where
  the lemma and stem code paths diverge; planning should not assume symmetry.

### 2.3 Cost shape of the aggregates

All aggregates (columns 3–6, 8) come from the same driving set: morphology rows where
`root_id = X`, joined to `quran_words` by PK. For a single root this is small and index-served
(`IX...root_id`). For the **whole table at once** it is one grouped pass over 77,432 morphology
rows joined to words — bounded, and the result set is only 1,642 rows. This bounded-ness is what
makes the caching strategy in §6 safe and cheap.

---

## 3. What APIs / read models are recommended?

Mirror the Feature 014 shape exactly: a thin controller → outcome-returning handlers → an
`IRootsReader` abstraction → an `EfRootsReader` implementation wrapped by a `CachedRootsReader`
decorator. Keep all EF/LINQ in infrastructure; keep `ApiResponse<T>` + Arabic messages at the
boundary (`API_GUIDELINES.md`).

Proposed endpoints under a new `RootsController` (`api/words/roots`), all `GET`:

| Endpoint | Returns | Notes |
| --- | --- | --- |
| `GET /api/words/roots` | `PagedResult<RootListItemDto>` | List with `search` (root text), `sort`, `page`, `pageSize`. Carries the 8 summary numbers per row (§2, §4). |
| `GET /api/words/roots/{id}` | `RootSummaryDto` | Header + counts for deep-link/state restore (mirrors `GetUniqueWordSummary`). |
| `GET /api/words/roots/{id}/words/simple` | `PagedResult<RootWordItemDto>` | الكلمات → بدون تشكيل. Each item carries `uniqueSimpleWordId` for the F014 deep link. |
| `GET /api/words/roots/{id}/words/tashkeel` | `PagedResult<RootWordItemDto>` | الكلمات → بالتشكيل. Each item carries `uniqueTashkeelWordId`. |
| `GET /api/words/roots/{id}/ayahs` | `PagedResult<RootAyahMatchDto>` | الآيات, paginated. Reuse F014's `AyahWordForHighlightDto` + `matchedQuranWordIds` (§5). |
| `GET /api/words/roots/{id}/surahs` | `RootSurahsResponse` | السور → ورد فيها (mentioned + per-surah occurrences). |
| `GET /api/words/roots/{id}/missing-surahs` | `RootMissingSurahsResponse` | السور → لم يذكر فيها (114 − mentioned). |
| `GET /api/words/roots/{id}/lemmas` | `RootLemmasResponse` | الصيغ المعجمية. Direct from `quran_lemmas.root_id`. Returns `lemmaId` + text + `wordsCount` (link-ready). |
| `GET /api/words/roots/{id}/stems` | `RootStemsResponse` | الأصول الصرفية. Aggregated via morphology. Returns `stemId` + text + per-root occurrence count (link-ready). |

Notes:

- The `RootWordItemDto` for the simple/tashkeel sub-views is the **navigation bridge**: it must
  carry the unique word's ID and display text so the frontend can build a Feature 014 deep link
  (`buildUniqueWordsDeepLink(kind, { wordId })`) without inventing new state. Per-root occurrence
  count should be computed **in context** (a unique simple spelling can, in principle, map to more
  than one root for homographs), while the word's global counts can come from the unique tables.
- Lemmas and stems responses return IDs now so they are link-ready; the detail pages for them stay
  out of scope (§9).
- Single-resource reads return `null` from the reader on a missing ID; the handler maps that to a
  controlled `404` with an Arabic message — identical to F014's `NotFound` outcome variant.
- Surahs and missing-surahs can be returned **whole** (≤ 114 rows) — verified small; no pagination
  needed, matching the F014 surahs/missing endpoints.

---

## 4. What should be lazy-loaded vs included in the table summary?

**Principle (from the brief and from F014):** the table shows summary numbers only; details load on
demand into the side panel. The numbers themselves must be in the list response because they are
columns and because clicking a number opens the matching detail sub-view.

### 4.1 Table summary (list endpoint)

Must include all 8 columns per row. Two of them are free (`words_count`,
`distinct_lemmas_count`); the other five are aggregates. Recommended approach, in priority order:

1. **Recommended — compute-once, cache-whole-list.** Because the roots set is only 1,642 rows and
   immutable, run a single grouped aggregation producing the full per-root summary (all 8 numbers),
   cache it under one stable key, then page/sort/search over the cached structure. First
   uncached request pays one bounded grouped scan; every later request is a memory hit. This is the
   cleanest fit for read-only data and keeps per-row latency flat.
2. **Alternative — page-scoped aggregation.** Compute the five aggregates only for the current
   page's root IDs (e.g. 25–50 IDs) on each request, joined back to the precomputed columns. Lower
   first-request cost, slightly more per-request work; still index-served via `root_id`.

Either avoids "load all details eagerly." Option 1 is preferred unless planning's latency
verification (§8.4) shows the one-time full aggregation is unexpectedly heavy.

> Explicitly **out** of the table summary and the list query: ayah word lists, surah lists, lemma
> lists, stem lists, and any highlight payloads. Those are detail-panel reads only.

### 4.2 Detail panel (lazy, per active tab/sub-view)

| Tab / sub-view | Load trigger | Pagination |
| --- | --- | --- |
| الكلمات / بدون تشكيل | tab+subview active | Paginated (`page`/`pageSize`); largest roots have many distinct words. |
| الكلمات / بالتشكيل | tab+subview active | Paginated. |
| الآيات | tab active | **Paginated / lazy — mandatory.** A high-frequency root can match thousands of ayahs. |
| السور / ورد فيها | tab active | Whole (≤114). |
| السور / لم يذكر فيها | tab active | Whole (≤114). |
| الصيغ المعجمية | tab active | Whole or paginated; bounded by `distinct_lemmas_count` (small). |
| الأصول الصرفية | tab active | Whole or paginated; bounded, small. |

The independent-scroll split panel is a **frontend concern** (a side panel with its own scroll
container), not a data concern. It does not change the API surface: each tab/sub-view is its own
lazy read keyed by `rootId` + view + sub-view + detail page, exactly like F014's modal views.

---

## 5. How should ayah highlighting be implemented safely?

**Reuse Feature 014's word-ID highlighting verbatim in shape; only the match filter changes.**

In `EfUniqueWordsReader.GetAyahMatchesAsync`, the "matched words" come from
`ReadableMatchesQuery` (`quran_words` filtered by `unique_*_word_id == id`). For roots, the matched
set is instead:

```
quran_word_morphology m  (m.root_id == X)
  → join quran_words w on w.id == m.quran_word_id
  → w.id are the matched quran_words.id values
```

(Morphology rows are one-per-readable-word — 77,432 == readable word count — so ayah markers never
enter the set; the `!IsAyahMarker` guard is harmless/redundant.)

Then the response uses the **same DTOs**: `RootAyahMatchDto` carrying
`IReadOnlyList<int> MatchedQuranWordIds` plus the ordered `IReadOnlyList<AyahWordForHighlightDto>`
(each word with its `QuranWordId`, `WordNumber`, `TextUthmani`, `IsAyahMarker`). The frontend
`highlighted-ayah` component highlights a word **iff** its `quranWordId` ∈ `matchedQuranWordIds`.

This satisfies every constraint:

- **No string replacement / no text-fragment matching** — matching is by exact `quran_words.id`.
- **No Quran text mutation** — words are rendered as stored; highlight is a CSS class/marker, not a
  text edit.
- **Accessibility** — keep F014's rule: highlight must not rely on color alone (class/marker/label),
  per `frontend-routing-state.md` "Highlighting Rules".
- **Paginated** — ayah matches are paged (`page`/`pageSize`), with the matched-IDs map built only for
  the current page's ayahs, exactly as F014 does.

---

## 6. What cache strategy should be used?

The project already has a proven, safe pattern; extend it, do not reinvent it.

### 6.1 Mechanism (reuse exactly)

- A `CachedRootsReader` decorator wraps `EfRootsReader`, injected with the shared `IMemoryCache`
  (same pattern as `CachedUniqueWordsReader` + `UniqueWordsDependencyInjection`).
- `IMemoryCache` is **already registered once** (`services.AddMemoryCache()` in
  `MushafReaderDependencyInjection`) and shared across features via **namespaced string keys**.
  Roots must **not** call any global cache reconfiguration and must **not** add size limits or
  default expirations that would change behavior for existing `words:*` / mushaf keys. This is the
  "avoid unsafe global cache settings" requirement — honored by adding a new key namespace only.

### 6.2 What to cache, and keys

Add a `RootsCacheKeys` helper (mirror `UniqueWordsCacheKeys`), namespace prefix `roots:`:

| Read | Cache? | Key shape |
| --- | --- | --- |
| List, **no search** | Yes | `roots:list:{sort}:p{page}:s{pageSize}` (or `roots:summary:all` for the cache-whole-list option in §4.1) |
| List, **with search** | **No — bypass** | Free-text keys are unbounded; bypass exactly as F014 does for searched lists |
| Root summary | Yes | `roots:{id}:summary` |
| Root surahs / missing | Yes | `roots:{id}:surahs` / `roots:{id}:missing` |
| Root words (simple/tashkeel), paged | Yes | `roots:{id}:words:{kind}:p{page}:s{pageSize}` |
| Root ayahs, paged | Yes | `roots:{id}:ayahs:p{page}:s{pageSize}` |
| Root lemmas / stems | Yes | `roots:{id}:lemmas` / `roots:{id}:stems` |

### 6.3 Expiration and invalidation

- **Expiration: none (cache for process lifetime).** F014's `cache.Set(key, value)` uses no
  expiration because the underlying Quran data is immutable at runtime. Roots data is equally
  immutable, so the same applies. This is intentional and safe.
- **Invalidation: none at runtime; restart-bounded.** The morphology lexicons only change via an
  offline reseed/import. There is no write path in this feature (or anywhere at runtime) that
  mutates `quran_roots` / `quran_word_morphology`, so there is nothing to invalidate. A reseed is
  followed by an app restart, which clears the in-memory cache.
- **Why this is safe for read-only Quran data:** cached values are pure functions of immutable
  tables; identical inputs always yield identical outputs; the key space is bounded (1,642 roots ×
  a small number of views/pages), so there is no unbounded memory growth — except searched lists,
  which is exactly why those bypass the cache. Because there is **no global `SizeLimit`** configured,
  keeping the cached entry set bounded (no per-search keys) is what keeps memory safe; the roots
  key design above preserves that.

---

## 7. What logging should be added?

Follow `LOGGING_GUIDELINES.md` and copy F014's handler logging style (structured templates, stable
lower-camelCase placeholders). Logging lives in the **Application handlers** (the use-case
boundary); the reader/decorator stays quiet except optional `Debug` diagnostics.

### 7.1 Log points

| Event | Level | When |
| --- | --- | --- |
| List request completed | `Information` | After roots list read (mirror `GetUniqueWordsPage` "Completed …"). |
| Detail request completed | `Information` | After each detail read (words/ayahs/surahs/lemmas/stems). |
| Invalid input (bad id/paging/sort/sub-view) | `Warning` | Controlled validation failure (mirror F014 "Rejected …"). |
| Root not found | `Warning` | Reader returns `null` for a given `rootId` (mirror F014 "Not found …"). |
| Cache hit/miss | `Debug` (optional) | In the decorator, when explicitly diagnosing cache behavior. Keep off the hot Info path. |
| Slow query warning | `Warning` | Only if elapsed time is actually measured and exceeds a threshold; otherwise omit. |

### 7.2 Fields (safe to log)

`{feature}` (`"Roots"`), `{operation}`, `{rootId}`, `{view}`, `{subView}`, `{pageNumber}`,
`{pageSize}`, `{totalCount}`, `{itemCount}`, `{cacheResult}` (hit/miss/bypass), `{elapsedMs}`
(only if measured), `{reason}` (for rejections), `{hasSearch}` (boolean — never the search text).

### 7.3 Must NOT log

- Root text, ayah text, word text, or any Quran/lexical content (root strings are derived lexical
  content — log `rootId`, not `rootText`).
- Raw user search text — log only `hasSearch` boolean (F014 already does exactly this).
- Full ayah-match payloads, word lists, SQL rows, or any large payload — log **counts**, not bodies.

This matches the existing global observability direction: no new vendor/Serilog/OpenTelemetry; log
once at the owning boundary; counts and IDs over content; surface not-found/invalid clearly.

---

## 8. Are migrations / indexes needed or not?

### 8.1 Migrations: NOT required for the core feature

Every column and relationship the feature needs already exists. No table, column, or FK must be
added to ship the Roots Explorer as specified. (Per `Backend/CLAUDE.md`, migrations are only added
on explicit request and via EF tooling — none of that is triggered here.)

### 8.2 Indexes: existing indexes are sufficient

The queries are served by indexes already present (per the DB baseline report):

- `quran_word_morphology` has indexes on `root_id`, `lemma_id`, `stem_id` → drives every
  root-filtered aggregate and the ayah/word/stem reads.
- `quran_lemmas` has an index on `root_id` → direct lemmas-per-root.
- `quran_roots` has an index on `words_count` → supports the "sort by occurrences" list ordering.
- `quran_words` join is by primary key (`id`); `surah_number`/`ayah_id` and the
  `unique_simple_word_id` / `unique_tashkeel_word_id` lookup columns are indexed.

### 8.3 Optional (only if measured slow) — not required

If §8.4 verification shows the distinct simple/tashkeel/stem aggregation is genuinely slow at scale
**and** the cache-whole-list strategy is rejected, a covering index such as
`quran_word_morphology(root_id, stem_id)` or including `quran_words(id, unique_simple_word_id,
unique_tashkeel_word_id, surah_number, ayah_id)` could be considered. This is an **optimization, not
a requirement**, and should only be proposed with measured evidence — consistent with "no migrations
unless the analysis proves a missing index is truly required." The analysis does **not** prove that.

### 8.4 Read-only verification to run during planning (do not block this report)

Confirm three assumptions before committing the plan (all read-only; the DB baseline was produced
the same way):

1. **Aggregate counts are sane and cheap** — distribution and worst case:
   ```sql
   SELECT m.root_id,
          COUNT(*)                              AS occurrences,
          COUNT(DISTINCT w.ayah_id)             AS ayahs,
          COUNT(DISTINCT w.surah_number)        AS surahs,
          COUNT(DISTINCT w.unique_simple_word_id)   AS simple_words,
          COUNT(DISTINCT w.unique_tashkeel_word_id) AS tashkeel_words,
          COUNT(DISTINCT m.stem_id)             AS stems
   FROM quran_word_morphology m
   JOIN quran_words w ON w.id = m.quran_word_id
   WHERE m.root_id IS NOT NULL
   GROUP BY m.root_id
   ORDER BY occurrences DESC
   LIMIT 20;
   ```
   Plus `EXPLAIN ANALYZE` on the full grouped form to confirm the one-time full-aggregation cost.
2. **`words_count` reconciles with occurrences** — `SELECT id, words_count FROM quran_roots` vs the
   grouped `COUNT(*)` above; and `distinct_lemmas_count` vs `COUNT(quran_lemmas WHERE root_id = id)`.
3. **Identity links are populated for root-bearing words** — confirm `unique_simple_word_id` /
   `unique_tashkeel_word_id` are non-null for words that have a `root_id` (so columns 5/6 and the
   simple/tashkeel navigation are reliable):
   ```sql
   SELECT COUNT(*) AS root_words_missing_unique_ids
   FROM quran_word_morphology m
   JOIN quran_words w ON w.id = m.quran_word_id
   WHERE m.root_id IS NOT NULL
     AND (w.unique_simple_word_id IS NULL OR w.unique_tashkeel_word_id IS NULL);
   ```
   Expectation: `0`. If non-zero, planning must decide how those words appear under the affected tabs.

---

## 9. What is out of scope?

- **Lemma and stem detail pages.** Lemmas/stems are displayed and link-ready (IDs returned) but
  their own explorer/detail screens are future features.
- **Any write path.** No create/update/delete; no importer; no new data pipeline; no recompute job.
  This is strictly read-only.
- **New denormalized columns on `quran_roots`** (e.g. precomputed distinct ayahs/surahs/simple/
  tashkeel/stems). Not needed; aggregation + cache covers it. Reconsider only if §8.4 disproves the
  latency assumption.
- **POS-tag / `quran_pos_tags` exploration**, verb tense/voice/case features — not requested.
- **Morphology-segment-level views** (`quran_word_morphology_segments`) — the feature operates at
  the word/root level, not the segment level.
- **Cross-root search beyond root text** (e.g. searching by lemma or by meaning) — list search is
  over root text only, matching the F014 search scope discipline.
- **Changing F014 behavior.** The Unique Words flow is reused as the navigation target for simple/
  tashkeel words; it is not modified.
- **Global cache reconfiguration** (size limits, default expirations, eviction policy) — explicitly
  avoided to protect existing cache consumers.

---

## 10. Final verdict

### **READY_WITH_NOTES**

The Roots Explorer is fully buildable from existing data with read-only APIs, reusing the Feature
014 backend and frontend patterns, with no migrations and no importer. It is not a blocker; it is
not a clean READY_FOR_PLAN only because the planning step must resolve the notes below.

**Notes the plan must address:**

1. **Aggregated columns (decision required).** Five of eight table columns
   (ayahs, surahs, simple words, tashkeel words, stems) are not precomputed. Choose the strategy in
   §4.1 — **recommended: compute-once + cache-whole-list** (1,642 rows, immutable) — vs page-scoped
   aggregation. This is a design choice, not a data gap.
2. **Lemma/stem asymmetry.** Lemmas-per-root is precomputed and indexed (`quran_lemmas.root_id`);
   stems-per-root has no shortcut (`quran_stems` has no `root_id`) and must aggregate via morphology.
   Do not assume symmetry between the two tabs.
3. **Verification before committing (§8.4).** Confirm aggregation latency, `words_count`/
   `distinct_lemmas_count` reconciliation, and that root-bearing words have non-null
   `unique_simple_word_id` / `unique_tashkeel_word_id`.
4. **UX divergence from F014.** Feature 014 uses a modal drill-down; Feature 015 wants a persistent
   **side panel with independent scroll** (split-screen). The URL-state machinery, deep links, and
   data-loading rules are reusable; the presentation layer (panel vs modal) is new and is the main
   net-new frontend work.
5. **Navigation contract.** The simple/tashkeel word items must surface `uniqueSimpleWordId` /
   `uniqueTashkeelWordId` so the frontend builds Feature 014 deep links; lemmas/stems return IDs to
   stay link-ready for future detail pages.

If the §8.4 checks pass as expected (they are expected to), this feature is straightforward to plan
and implement as a close structural sibling of Feature 014.
