# Words Caching — Implementation Plan

> Design rationale: `docs/superpowers/specs/2026-06-23-words-caching-design.md` (approved).

## Context

The Words feature caches nothing; the Mushaf reader caches on both tiers. Since unique-word
data is read-only and seeded (changes only on reset/reseed/import → host restart), we bring
Words to Mushaf caching parity: a backend `IMemoryCache` decorator and a shared frontend LRU
cache. Decisions locked with the user: **both tiers**; backend **caches no-search list pages +
all drill-down reads and bypasses search**; frontend **extracts the Mushaf cache into a shared
base** used by both features.

## Backend (mirror the `CachedWordAnalysisReader` pattern)

1. **`Infrastructure/Caching/Quran/Words/UniqueWordsCacheKeys.cs`** (new) — static key builders:
   - `List(kind, sort, page, size)` → `words:{kind}:list:{sort}:p{page}:s{size}`
   - `Summary(kind, id)` → `words:{kind}:{id}:summary`
   - `Surahs(kind, id)` → `words:{kind}:{id}:surahs`
   - `Missing(kind, id)` → `words:{kind}:{id}:missing`
   - `Ayahs(kind, id, page, size)` → `words:{kind}:{id}:ayahs:p{page}:s{size}`
2. **`Infrastructure/Caching/Quran/Words/CachedUniqueWordsReader.cs`** (new) —
   `sealed class CachedUniqueWordsReader(IUniqueWordsReader inner, IMemoryCache cache) : IUniqueWordsReader`.
   Each method: `TryGetValue` → on miss call `inner` → `cache.Set` **only** successful/non-null
   results. `GetUniqueWordsPageAsync`: when `search` is non-empty, call `inner` directly and
   return without caching (no key built). Match the exact null-skip semantics of
   `CachedWordAnalysisReader`.
3. **`Infrastructure/DependencyInjection.cs:62-63`** — replace the single registration and the
   "no cache until measured need" comment with the EF-reader + decorator pair (same shape as the
   Mushaf block at lines ~153-187):
   ```csharp
   services.AddScoped<EfUniqueWordsReader>();
   services.AddScoped<IUniqueWordsReader>(sp => new CachedUniqueWordsReader(
       sp.GetRequiredService<EfUniqueWordsReader>(),
       sp.GetRequiredService<IMemoryCache>()));
   ```
   `IMemoryCache` is already registered (`AddMemoryCache()` in `ConfigureMushafReader`, which runs
   first) — no new registration. No controller/contract/DTO changes.
   - Interface to honor: `Application.Abstractions/Quran/Words/IUniqueWordsReader.cs` (5 methods).
   - Inner impl unchanged: `Infrastructure/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs`.

## Frontend (extract a shared base; subclass per feature → no Mushaf rewiring)

4. **`src/app/core/caching/api-response-cache.ts`** (new) — `ApiResponseCache`: move the generic
   logic verbatim from `MushafReaderCache` (`Map` cache + `inFlight` dedupe via
   `shareReplay({bufferSize:1, refCount:false})` + 48-entry LRU + `getOrLoad`/`peek`/`prefetch`/
   `store`). This base is **not** `providedIn:'root'`.
5. **`src/app/features/mushaf/state/mushaf-reader-cache.ts`** — keep `MushafReaderCacheKeys`;
   change `MushafReaderCache` to `@Injectable({providedIn:'root'}) class MushafReaderCache extends
   ApiResponseCache {}`. The 6 Mushaf importers stay unchanged. (Mushaf specs are the regression
   gate.)
6. **`src/app/features/words/state/unique-words-cache.ts`** (new) —
   `@Injectable({providedIn:'root'}) class UniqueWordsCache extends ApiResponseCache {}` + a
   `UniqueWordsCacheKeys` builder:
   - list: `words:list:{mode}:{sort}:{search}:p{page}` (frontend **does** cache search, LRU-bounded)
   - drill-down: `words:{mode}:{wordId}:summary|:surahs|:missing`, `words:{mode}:{wordId}:ayahs:p{page}`
7. **Wire the facades through the cache:**
   - `unique-words.facade.ts` — route the list load through `cache.getOrLoad(key, () => api.getList(...))`,
     replacing the "current loaded page" reuse guard.
   - `unique-words-drilldown.facade.ts` — route summary/surahs/missing/ayahs through
     `cache.getOrLoad`, replacing the ad-hoc missing-from-surahs derivation. Result: tab-switch,
     sort re-select, paging back, re-opening a word, and back/forward restore are cache hits.
   - API service unchanged: `features/words/data-access/unique-words.api.ts`.

## Tests (no existing cache tests to mirror — add new)

- **Backend `CachedUniqueWordsReaderTests`** (fake inner reader): repeated read → inner called
  once; list-with-`search` → always delegates, never cached; `null` result → not cached.
- **Frontend `api-response-cache.spec.ts`**: hit reuse, concurrent in-flight dedupe (one loader
  call), LRU eviction past 48, `isSuccess:false`/null not stored.
- **Frontend Words facade specs**: repeated list page / drill-down read does not re-call
  `unique-words.api`.
- **Regression:** existing Mushaf feature specs stay green after the base extraction.

## Invalidation / scope

Cache-forever until process restart (parity with Mushaf; immutable seeded data). No fabricated
data — cache stores only what was returned. Out of scope: HTTP cache headers, HybridCache/output
caching, backend search caching, size/expiry tuning, manual flush.

## Verification

- Backend: `dotnet build` + backend tests green (incl. new decorator tests).
- Frontend: new cache + Words facade specs green **and Mushaf specs green**, run with the required
  `VITEST_MAX_FORKS` cap (`npm test`, e.g. `--include` the affected specs).
- Manual: browse `/dashboard/words/unique/…`, open a word, switch views, use back/forward — the
  network panel shows revisited reads are not re-requested.
