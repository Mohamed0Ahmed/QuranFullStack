# Words Caching — Design Spec

**Date:** 2026-06-23
**Feature area:** Words / Unique Words Explorer (`features/words`) + backend unique-word reads
**Status:** Approved design, pending implementation plan

## Context

The **Mushaf reader** caches its reads on both tiers; the **Words** feature caches on
neither, so it re-queries Postgres and re-hits the API on nearly every interaction.

- **Backend today:** `EfUniqueWordsReader` is registered directly with a deliberate
  comment — *"No cache decorator on unique-word reads until a measured need appears"*
  (`Infrastructure/DependencyInjection.cs:62-63`). Every request runs fresh EF queries
  (all `AsNoTracking`, over precomputed aggregate columns). Mushaf, by contrast, has
  seven `Cached*Reader` decorators over a shared `IMemoryCache`, cache-forever until the
  host restarts after an import.
- **Frontend today:** Mushaf has a persistent in-memory LRU cache
  (`features/mushaf/state/mushaf-reader-cache.ts`: `Map` + in-flight dedupe + 48-entry
  LRU + `getOrLoad`/`peek`/`prefetch`). Words has only a "current loaded page already in
  memory" guard in `unique-words.facade.ts`; tab-switch, sort, search, pagination,
  re-opening a drill-down, and browser back/forward all re-fetch.

The underlying unique-word data is **read-only and seeded** (deterministic unique-word
IDs; changes only on reset/reseed/import), so a cache-forever-until-restart policy —
identical to Mushaf — is correct and safe.

**Goal:** bring Words to caching parity with Mushaf on both tiers, without introducing
unbounded-memory risk on the backend.

## Decisions (locked with the user)

1. **Both tiers** — frontend cache **and** backend cached-reader decorator (Mushaf
   parity). This intentionally overrides the documented "no cache until measured need"
   decision.
2. **Backend list strategy:** cache the **no-search** list pages and **all** drill-down
   reads; **search-filtered list queries pass straight through, uncached** — because the
   project's `IMemoryCache` has no size limit and free-text search keys are unbounded.
3. **Frontend cache reuse:** extract the Mushaf cache's generic logic into a **shared**
   cache in `core/`, consumed by both features.

## Backend Design

New decorator mirroring `CachedWordAnalysisReader` exactly (the established pattern).

- **New files (under `Infrastructure/Caching/Quran/Words/`):**
  - `CachedUniqueWordsReader.cs` — `sealed class CachedUniqueWordsReader(IUniqueWordsReader inner, IMemoryCache cache) : IUniqueWordsReader`.
  - `UniqueWordsCacheKeys.cs` — static key builders (English identifiers; Arabic is only
    for user-facing messages, not cache keys).
- **DI** (`DependencyInjection.cs:62-63`): replace the single registration and its stale
  comment with the EF-reader + decorator pair, matching the Mushaf block:
  ```csharp
  services.AddScoped<EfUniqueWordsReader>();
  services.AddScoped<IUniqueWordsReader>(sp => new CachedUniqueWordsReader(
      sp.GetRequiredService<EfUniqueWordsReader>(),
      sp.GetRequiredService<IMemoryCache>()));
  ```
  `IMemoryCache` is already registered (`AddMemoryCache()` inside `ConfigureMushafReader`,
  which runs before this line); no new registration needed.
- **Cached reads (bounded keys → cache-forever):**

  | Method | Cached when | Key |
  |---|---|---|
  | `GetUniqueWordsPageAsync(kind, search, sort, page, size)` | `search` is null/empty **only** | `words:{kind}:list:{sort}:p{page}:s{size}` |
  | `GetUniqueWordSummaryAsync(kind, id)` | result non-null | `words:{kind}:{id}:summary` |
  | `GetMentionedSurahsAsync(kind, id)` | result non-null | `words:{kind}:{id}:surahs` |
  | `GetMissingSurahsAsync(kind, id)` | result non-null | `words:{kind}:{id}:missing` |
  | `GetAyahMatchesAsync(kind, id, page, size)` | result non-null | `words:{kind}:{id}:ayahs:p{page}:s{size}` |

- **Bypass:** a list call with non-empty `search` delegates directly to `inner` and is
  never stored. Null results from the `?`-returning methods are never cached (matches the
  Mushaf "never cache misses" rule).
- **No controller/contract changes** — the decorator is transparent behind
  `IUniqueWordsReader`; `UniqueWordsController` and the `ApiResponse`/DTO shapes are
  untouched.

## Frontend Design

Extract the generic cache via a **shared base class**, so Mushaf's existing wiring is
untouched (lowest-risk way to satisfy "extract to a shared cache").

- **New `core/caching/api-response-cache.ts`** — `ApiResponseCache`: the generic logic
  moved verbatim from `MushafReaderCache` (`Map` + `inFlight` dedupe via
  `shareReplay({bufferSize:1, refCount:false})` + 48-entry LRU + `getOrLoad` / `peek` /
  `prefetch` / `store`). Not `providedIn: 'root'` itself — it is the shared base.
- **`features/mushaf/state/mushaf-reader-cache.ts`** — `MushafReaderCacheKeys` stays
  here; `MushafReaderCache` becomes
  `@Injectable({providedIn:'root'}) class MushafReaderCache extends ApiResponseCache {}`.
  The 6 Mushaf files that inject `MushafReaderCache` need **no change**, and the Mushaf
  feature specs must stay green (verification gate).
- **`features/words/state/unique-words-cache.ts` (new)** —
  `@Injectable({providedIn:'root'}) class UniqueWordsCache extends ApiResponseCache {}`
  plus `UniqueWordsCacheKeys`. A separate root singleton → its own 48-entry LRU, isolated
  from Mushaf's.
- **Key builders (`UniqueWordsCacheKeys`):**
  - list: `words:list:{mode}:{sort}:{search}:p{page}` — the **frontend caches search
    results too** (per-user, LRU-bounded). Only the *backend* skips search.
  - drill-down: `words:{mode}:{wordId}:summary | :surahs | :missing`, and
    `words:{mode}:{wordId}:ayahs:p{page}`.
- **Integration:** `unique-words.facade.ts` and `unique-words-drilldown.facade.ts` route
  their API calls through `cache.getOrLoad(key, () => api.…)`, replacing the ad-hoc
  "current loaded page" guard and the missing-from-surahs derivation. Effect: tab-switch,
  sort re-select, paging back, re-opening a word, and back/forward restore become cache
  hits with no API call.

## Invalidation & Data Safety

- **No invalidation logic.** Cache-forever until process restart, identical to Mushaf and
  justified by immutable seeded data. A manual flush endpoint/command is an explicit
  future follow-up, not in scope.
- **Quranic safety:** the cache only stores what the API/EF reader actually returned; no
  fabricated text, counts, or fallback data. Tests use synthetic placeholder data only.

## Testing

No dedicated unit tests exist today for either the Mushaf FE cache or the BE cached
readers, so this work **adds** tests:

- **Backend — `CachedUniqueWordsReaderTests`:** with a fake/mock inner reader,
  - a repeated identical read calls the inner **once** (second is a cache hit);
  - a list read **with `search`** always delegates to the inner (never cached);
  - a `null` drill-down result is not cached (next call re-queries).
- **Frontend — `api-response-cache.spec.ts`:** hit reuse, in-flight dedupe (two
  concurrent `getOrLoad` share one loader call), LRU eviction past 48 entries, and that
  failed/`isSuccess:false` responses are not stored.
- **Frontend — Words facade specs:** a repeated list page / drill-down read does **not**
  re-invoke the `unique-words.api` service.
- **Regression gate:** the existing Mushaf feature specs continue to pass after the base
  extraction.

## Out of Scope (YAGNI)

HTTP `Cache-Control`/ETag headers, `HybridCache`/distributed cache, ASP.NET output
caching, backend caching of search-filtered lists, and cache size/expiry tuning — none
are needed for immutable seeded data at current scale.

## Acceptance / Verification

- Backend: new decorator unit tests green; `dotnet build` + backend test suite green.
- Frontend: new cache + Words facade specs green; Mushaf specs still green
  (run with the required `VITEST_MAX_FORKS` cap).
- Manual: open `/dashboard/words/unique/…`, browse pages, open a word, switch views,
  use browser back/forward — confirm via network panel that revisited reads do not
  re-hit the API; the backend logs/queries do not repeat for cached browse/drill-down
  reads.
