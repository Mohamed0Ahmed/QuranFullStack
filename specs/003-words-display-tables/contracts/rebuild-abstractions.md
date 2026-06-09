# Contract — Rebuild Abstractions (Application boundary)

Contracts the Application layer depends on, in
`Application.Abstractions/Quran/Words/Display/`. These expose **records only** — never EF
entities — across the boundary (Clean Architecture). Mirrors the Feature 002 import
abstractions in spirit.

## `IDisplayWordsRebuilder`

Implemented by Infrastructure (`SqlDisplayWordsRebuilder`). Runs the entire rebuild +
validation in **one transaction** and commits **only** if every hard check passes.

```csharp
public interface IDisplayWordsRebuilder
{
    // True if ANY of the four derived tables is non-empty (drives refuse-unless-empty).
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    // Truncate (if force) → INSERT…SELECT the 4 tables → run validation queries →
    // commit iff all hard checks pass, else roll back. Returns the full result either way.
    // `expectedReadableWords` is the value the hard checks compare against (the
    // ORD-READABLE / "exactly 77,432" gate, FR-031). The CLI always passes the production
    // default DisplayWordsInvariants.ExpectedReadableWords (77,432); tests pass their
    // synthetic fixture's readable-row count so small fixtures don't trip the production
    // gate. This keeps the hard invariant faithful in production while remaining testable.
    Task<DisplayWordsRebuildResult> RebuildAsync(bool force, int expectedReadableWords, CancellationToken ct);
}
```

Behavioral contract:
- MUST NOT truncate, delete, or modify `quran_words`, `quran_ayahs`, `quran_surahs`, or
  any non-derived table (FR-029).
- With `force = false` and any target non-empty, the caller refuses **before** calling
  `RebuildAsync`; `RebuildAsync` itself also guards (defense in depth) and throws/returns
  a refusal consistently with the importer's `EfBulkQuranImportWriter`.
- The transaction wraps truncate + inserts + validation; rollback on any hard-check
  failure or exception leaves the database in its pre-run state (FR-026, FR-032).
- `Persisted = true` on the result **iff** the transaction committed.

## `IDisplayWordsReportWriter`

```csharp
public interface IDisplayWordsReportWriter
{
    Task WriteAsync(DisplayWordsRebuildResult result, string outputDir, CancellationToken ct);
}
```

Writes a Markdown report and a JSON report (see `validation-report.schema.md`). Called for
**every** run, success or failure (FR-033).

## Records

```csharp
public sealed record DisplayWordsRebuildResult(
    DateTimeOffset RunAtUtc,
    string Verdict,                 // "pass" | "fail"
    bool Persisted,
    bool Forced,
    DisplayWordsTotals Totals,
    IReadOnlyList<DisplayWordsCheckResult> Checks,
    IReadOnlyList<string> Warnings, // e.g. unique-count differs from informational expectation
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record DisplayWordsTotals(
    int OrderedTashkeelRows,        // expected 77,432
    int OrderedSimpleRows,          // expected 77,432
    int UniqueTashkeelRows,         // derived; reported (≈21,210 informational)
    int UniqueSimpleRows,           // derived; reported (≈14,783 informational)
    int ReadableWords);             // from quran_words; expected 77,432

public sealed record DisplayWordsCheckResult(
    string Id,                      // e.g. "ORD-MUSHAF-CONTIG" (see data-model.md)
    string Severity,                // "hard" | "warning"
    string Expected,
    string Observed,
    bool Passed);
```

## `DisplayWordsInvariants` (constants + messages)

```csharp
public static class DisplayWordsInvariants
{
    public const int ExpectedReadableWords = 77_432; // ordered rows per table
    // Informational only — NEVER a hard threshold (FR-015):
    public const int InformationalUniqueTashkeel = 21_210;
    public const int InformationalUniqueSimple   = 14_783;

    public const string TargetsNotEmpty =
        "Display word tables are not empty. Re-run with --force to truncate and rebuild them.";
}
```

## Verdict constants

`Verdict` uses the same vocabulary as the importer: `"pass"` / `"fail"`. Severity
`"hard"` failures set the verdict to `"fail"` and force a rollback; `"warning"` checks
never change the verdict.
