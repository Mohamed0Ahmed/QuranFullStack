# Performance Remediation Report — Roots Explorer (Feature 015)

**Date:** 2026-06-24  
**Trigger:** Backend performance review (PASS WITH NOTES) — findings B1, B2, B3  
**Verdict after remediation:** PASS — all noted findings addressed (B3 re-scoped per re-review Option A)

## Summary

Three read-side performance findings from the Roots Explorer backend review were remediated without changing API contracts, Quranic data semantics, or introducing migrations.

| Finding | Severity | Status | Approach |
| --- | --- | --- | --- |
| B1 — per-page full reload in `GetRootWordsAsync` | MINOR | Fixed | Compute-once grouped whole per `(rootId, kind)`; pages sliced in memory |
| B2 — correlated first-word subquery on cold summary | NOTE | Fixed | Replaced with single `DISTINCT ON (root_id)` subquery join |
| B3 — unbounded `IMemoryCache` growth | NOTE | Fixed (scoped) | 30‑min sliding expiration on Roots detail entries only; no global `SizeLimit` |

## B1 — Grouped root words: compute once, page in memory

### Problem

`GetRootWordsAsync` loaded every morphology occurrence for a root, grouped in memory, then applied `Skip/Take`. The cache key was per-page (`roots:{id}:words:{kind}:p{page}:s{pageSize}`), so each distinct page re-ran the full O(occurrences) work.

### Changes

1. **`EfRootsReader.LoadGroupedRootWordsAsync`** — new internal method that loads, groups, orders, and returns the full `IReadOnlyList<RootWordItemDto>` for a root/kind.
2. **`RootsWordsDerivation.ToPage`** — mirrors `RootsListDerivation` for in-memory paging over the grouped whole.
3. **`CachedRootsReader`** — caches under `roots:{id}:words:{kind}:all` (`RootsCacheKeys.WordsAll`); `GetRootWordsAsync` slices pages from the cached whole.
4. **`RootsCacheKeys.WordsAll`** — new key; legacy per-page `Words` key retained for contract traceability but no longer used by the decorator.

### Effect

Browsing pages 1…N for the same root/kind now pays one DB load + one grouping pass. Page 2+ are pure in-memory slices from the cached grouped list.

## B2 — Summary cold-start SQL shape

### Problem

`LoadWholeSummaryAsync` used a correlated `LIMIT 1` subquery per root for first-occurrence metadata. Correct and cached, but suboptimal on the first cold request.

### Changes

Replaced the per-root correlated subquery with a single derived table:

```sql
LEFT JOIN (
    SELECT DISTINCT ON (root_id) root_id, quran_word_id
    FROM quran_word_morphology
    WHERE root_id IS NOT NULL
    ORDER BY root_id, quran_word_id
) first_m ON first_m.root_id = r.id
```

No migration required; existing `root_id` index supports the scan.

## B3 — Roots-specific cache lifetime (not global SizeLimit)

### Problem

`CachedRootsReader` called `_cache.Set(key, value)` with no `MemoryCacheEntryOptions`. The shared `AddMemoryCache()` had no expiration, so long browsing sessions could accumulate Roots detail entries indefinitely (especially when keys were per-page for words).

### Changes

1. **`RootsCacheEntryOptions`** — factory methods for:
   - `SummaryAll()` — no expiration (process-lifetime compute-once)
   - `GroupedWords`, `PagedDetail`, `WholeDetail` — 30‑minute sliding expiration
2. **`CachedRootsReader`** — all `_cache.Set` calls now pass entry options.

### Intentionally not done (re-review correction)

An initial remediation set `SizeLimit = 10_000` on the **shared** `IMemoryCache` in `MushafReaderDependencyInjection`. That was **reverted**. When `SizeLimit` is configured, every `cache.Set` must specify `Size` or `MemoryCache` throws `InvalidOperationException` — Feature 014 (`CachedUniqueWordsReader`) and all seven Mushaf cache decorators do not set sizes, so a global limit would cause runtime 500s on first cache write.

**Final B3 scope:** Roots-only sliding expiration on detail entries. The shared cache remains unbounded (same as Feature 014 / Mushaf baseline). This addresses the original NOTE (unbounded TTL for browsed detail keys) without cross-feature risk.

## Files changed

| File | Change |
| --- | --- |
| `Persistence/Reads/Quran/Words/Roots/EfRootsReader.cs` | `LoadGroupedRootWordsAsync`; summary SQL `DISTINCT ON` |
| `Persistence/Reads/Quran/Words/Roots/RootsWordsDerivation.cs` | **New** — in-memory paging helper |
| `Caching/Quran/Words/Roots/CachedRootsReader.cs` | Grouped-whole cache; sliding expiration on detail sets |
| `Caching/Quran/Words/Roots/RootsCacheKeys.cs` | `WordsAll` key |
| `Caching/Quran/Words/Roots/RootsCacheEntryOptions.cs` | **New** — sliding expiration (no `SetSize`) |
| `DependencyInjection/MushafReaderDependencyInjection.cs` | `AddMemoryCache()` unchanged (no `SizeLimit`) |
| `tests/.../RootsCacheReadTests.cs` | Cross-page grouped-words cache test |

## Verification

```bash
cd Backend
dotnet build QuranDashboard.sln
dotnet test QuranDashboard.sln --filter "FullyQualifiedName~Roots"
dotnet test QuranDashboard.sln --filter "FullyQualifiedName~CachedUniqueWordsReader|MushafReaderCache"
```

**Results:** Build succeeded. Roots tests, Unique Words cache tests, and Mushaf cache tests pass against the production-equivalent unbounded `MemoryCache`.

## Quranic data safety

No Quran text, counts, roots, morphology, or co-occurrence invariants were altered. Changes are read-path caching/query-shape only; projections and ordering semantics are preserved.

## Out of scope (unchanged)

- No new PostgreSQL indexes or migrations (read-only feature constraint).
- No `SetSize` added to Feature 014 or Mushaf cache decorators (would require a cross-feature cache refactor).
- Lemmas/stems reads were already whole-list (no per-page re-group issue).
- Ayah matches already paginate in the DB; per-page cache retained with sliding expiration.
