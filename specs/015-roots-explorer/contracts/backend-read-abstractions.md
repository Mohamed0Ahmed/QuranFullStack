# Contract: Backend Read Abstractions

## Purpose

Define the application-level read boundary for Feature 015. Application handlers depend on this
abstraction; Infrastructure implements it (EF reader + cache decorator) using existing read-only
tables. Mirrors the Feature 014 `IUniqueWordsReader` shape and conventions.

## `IRootsReader`

Suggested methods (names may be adjusted to project conventions; responsibilities must stay equivalent):

```csharp
Task<PagedResult<RootListItemDto>> GetRootsPageAsync(
    string? search, RootSort sort, int page, int pageSize, CancellationToken ct);

Task<RootSummaryDto?> GetRootSummaryAsync(int id, CancellationToken ct);

Task<PagedResult<RootWordItemDto>?> GetRootWordsAsync(
    int id, RootWordKind wordKind, int page, int pageSize, CancellationToken ct);

Task<PagedResult<RootAyahMatchDto>?> GetRootAyahMatchesAsync(
    int id, int page, int pageSize, CancellationToken ct);

Task<RootSurahsResponse?> GetRootMentionedSurahsAsync(int id, CancellationToken ct);

Task<RootMissingSurahsResponse?> GetRootMissingSurahsAsync(int id, CancellationToken ct);

Task<RootLemmasResponse?> GetRootLemmasAsync(int id, CancellationToken ct);   // co-occurrence

Task<RootStemsResponse?> GetRootStemsAsync(int id, CancellationToken ct);
```

`null` return for single-root reads means the root id does not exist → handler maps to controlled `404`.

## Implementation notes

- `EfRootsReader` performs the reads with `AsNoTracking`; the whole-summary aggregation is computed
  once and the list handler/decorator serves search/sort/page from the cached structure (see
  research D2).
- `CachedRootsReader` decorates `EfRootsReader` over the shared `IMemoryCache` with `roots:` keys
  (see roots-api.md). DI registers `EfRootsReader` then wraps it, exactly like
  `UniqueWordsDependencyInjection`.
- **Lemmas use co-occurrence** (`DISTINCT quran_word_morphology.lemma_id` where `root_id = id`); never
  `COUNT(quran_lemmas WHERE root_id)`. The list `LemmasCount` and `GetRootLemmasAsync` count must agree.
- **Stems** derived via morphology (`DISTINCT stem_id`), joined to `quran_stems` for text.
- Ayah matches: page the distinct matched ayah ids, batch-load the page's ayah words (no per-ayah
  N+1), and build `MatchedQuranWordIds` from the root's `quran_words.id` set.

## Validation ownership

Application handlers validate:

- `sort` is supported (empty → default `mushaf-order`; unknown → 400).
- `wordKind` is supported (simple|tashkeel).
- root `id` is positive.
- `page`/`pageSize` are valid and bounded.
- Unknown root id maps to a not-found outcome.

Infrastructure readers:

- Return read DTOs only; `AsNoTracking`; no EF entities exposed; no state mutation.
- Exclude ayah markers from occurrence/highlight data (morphology rows are readable words only).

## Outcome pattern

Each query handler returns a discriminated outcome (mirroring existing project patterns):

- Success with data.
- Validation failure (bad kind/sort/paging/id) → `400`.
- Not found (unknown root id) → `404`.

Controllers map outcomes to `ApiResponse<T>` with `200`, `400`, or `404`, and emit the structured log.

## Data-safety rules

- No database writes. No migrations. No source-data mutation. No invented Quran text.
- No string-based highlighting (use `quran_words.id`).
- No raw internal database/table names in API routes or user-facing messages.
- Backend identifiers are never surfaced for display (the API may return ids for navigation only).
