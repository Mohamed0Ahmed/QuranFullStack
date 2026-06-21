# Contract: Backend Read Abstractions

## Purpose

Define the application-level read boundary for Feature 014. Application handlers depend on this abstraction; Infrastructure implements it using existing read-only database tables.

## `IUniqueWordsReader`

Suggested methods:

```csharp
Task<PagedResult<UniqueWordListItemDto>> GetUniqueWordsPageAsync(
    UniqueWordKind kind,
    string? search,
    UniqueWordSort sort,
    int page,
    int pageSize,
    CancellationToken cancellationToken);

Task<UniqueWordSummaryDto?> GetUniqueWordSummaryAsync(
    UniqueWordKind kind,
    int id,
    CancellationToken cancellationToken);

Task<UniqueWordSurahsResponse?> GetMentionedSurahsAsync(
    UniqueWordKind kind,
    int id,
    CancellationToken cancellationToken);

Task<UniqueWordMissingSurahsResponse?> GetMissingSurahsAsync(
    UniqueWordKind kind,
    int id,
    CancellationToken cancellationToken);

Task<PagedResult<UniqueWordAyahMatchDto>?> GetAyahMatchesAsync(
    UniqueWordKind kind,
    int id,
    int page,
    int pageSize,
    CancellationToken cancellationToken);
```

Names may be adjusted during implementation to match project conventions, but responsibilities must stay equivalent and focused.

## Validation Ownership

Application handlers validate:

- `kind` is supported.
- `sort` is supported.
- `id` is positive.
- `page` and `pageSize` are valid and bounded.
- Unknown selected word maps to a not-found outcome.

Infrastructure readers:

- Return read DTOs only.
- Use no tracking reads.
- Do not expose EF entities.
- Do not mutate state.
- Exclude ayah markers from occurrence and highlight data.

## Outcome Pattern

Each query handler returns a discriminated outcome equivalent to existing project patterns:

- Success with data.
- Validation failure for bad kind/sort/paging/input.
- Not found for unknown unique-word ID.

Controllers map outcomes to `ApiResponse<T>` with `200`, `400`, or `404`.

## Data Safety Rules

- No database writes.
- No migrations.
- No source-data mutation.
- No invented Quran text.
- No string-based highlighting.
- No raw internal database/table names in API routes or user-facing messages.
