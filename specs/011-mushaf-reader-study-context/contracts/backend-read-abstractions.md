# Contract: Backend Read Abstractions & Cache Seam

Internal Application-boundary contracts (not HTTP). They keep handlers testable and give the cache a clean decoration seam. Names are guidance; shapes are binding.

## Application.Abstractions (`Quran/MushafReader/`)

```csharp
public interface IMushafPageReader
{
    // null => page not found; out-of-range handled before calling (validation)
    Task<MushafPageResponse?> GetPageAsync(int pageNumber, CancellationToken ct);
}

public interface IAyahStudyReader
{
    // Resolved source keys are passed in already (defaults applied by the handler).
    // Each source kind may resolve to null inside the response (missing source).
    Task<AyahStudyResponse?> GetAyahStudyAsync(
        string verseKey,
        string? tafsirSourceKey,
        string? translationSourceKey,
        string? fullI3rabSourceKey,
        CancellationToken ct);
}

public interface IWordAnalysisReader
{
    // Returns a discriminated outcome: found / not-found / not-analyzable (ayah marker).
    Task<WordAnalysisOutcome> GetWordAnalysisAsync(string wordLocation, CancellationToken ct);
}
```

- `WordAnalysisOutcome`: a small result type (e.g., `Found(WordAnalysisResponse)`, `NotFound`, `NotAnalyzable`, `IncompleteData`) so the controller maps to `200/404/400` without exceptions for expected cases.
- Response DTOs (`MushafPageResponse`, `AyahStudyResponse`, `WordAnalysisResponse`) are defined with their use cases (see data-model.md §B).

## Options & defaults

```csharp
public sealed class MushafReaderOptions   // bound from configuration section "MushafReader"
{
    public string? DefaultTafsirSourceKey { get; init; }       // ar-muyassar
    public string? DefaultTranslationSourceKey { get; init; }  // en-sahih-international
    public string? DefaultFullI3rabSourceKey { get; init; }    // muyassar
}
```

- The `GetAyahStudyHandler` resolves each kind: explicit arg → option default → null.
- Validate configured keys against the source catalogue on first use; a configured key that does not exist yields a per-kind empty state (logged once), never a substitution.

## Cache decorators (Phase 5 — added after readers + tests are stable)

- `CachedMushafPageReader`, `CachedAyahStudyReader`, `CachedWordAnalysisReader` wrap the EF readers and use `IMemoryCache` with the keys in data-model.md §E.
- Cache only successful, non-null immutable reads. Never cache not-found/not-analyzable or any user-specific data. Registered in DI as decorators so handlers depend only on the interfaces.

## Handler → controller mapping

| Outcome | HTTP | `ApiResponse` |
|---|---|---|
| page/ayah/word found | 200 | `Ok(data, message)` |
| not found | 404 | `Fail(Common.NotFound)` |
| word analysis incomplete (readable word, missing required rows) | 404 | `Fail(MushafWords.AnalysisIncomplete)` |
| invalid input | 400 | `Fail(<feature key>)` |
| word is ayah marker | 400 | `Fail(MushafWords.NotAnalyzable)` |

## Layer rules

- Application depends on these abstractions, not on Infrastructure.
- EF queries live only in the Infrastructure readers.
- Controllers depend on Application (queries/handlers), never on Infrastructure or `DbContext`.
