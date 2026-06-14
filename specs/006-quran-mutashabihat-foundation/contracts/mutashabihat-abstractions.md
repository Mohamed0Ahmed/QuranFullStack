# Contract — Mutashabihat Import Abstractions (Application boundary)

Contracts the Application layer depends on, in `Application.Abstractions/Quran/Mutashabihat/`. These
expose **records / source DTOs only** — never EF entities — across the boundary (Clean Architecture).
Mirrors the Feature 002/004 import abstractions in spirit.

## `IMutashabihatImportSource`

Implemented by Infrastructure (`MutashabihatImportSource`). Reads the **local** staged package, verifies
the manifest, parses `phrases.json` + `matching-ayah.json`, builds the `verse_key → ayah_id` map from
`quran_ayahs` (read-only), and assembles the in-memory graph (resolving every reference, recomputing
counters, flagging the representative occurrence, collapsing the one duplicate occurrence). Reads files
only — never writes them.

```csharp
public interface IMutashabihatImportSource
{
    // Verify manifest (exact file set, expectedRecordCount, fileSizeBytes, sha256), parse both files,
    // resolve verse_key → ayah_id against quran_ayahs, recompute counters, flag the representative
    // occurrence, and assemble the full graph. Throws a typed error on any manifest/source mismatch,
    // a missing/empty quran_ayahs, or an unresolved reference (caller refuses / fails early).
    // Captures pre-run file digests for MUT-SOURCE-UNCHANGED.
    Task<MutashabihatSourceData> LoadAsync(string sourcePath, CancellationToken ct);

    // Re-verify the local source files' size/sha256 after assembly, before commit (MUT-SOURCE-UNCHANGED).
    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}
```

## `IMutashabihatImportWriter`

Implemented by Infrastructure (`EfBulkMutashabihatWriter`). Runs the entire bulk load + validation in
**one transaction** and commits **only** if every hard check passes.

```csharp
public interface IMutashabihatImportWriter
{
    // True if ANY of the three mutashabihat tables is non-empty (drives refuse-unless-empty).
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    // Truncate (if force) → COPY groups → occurrences → links → run validation queries →
    // commit iff all hard checks pass, else roll back. Returns the full result either way.
    // `expected` carries the production expected counts the hard checks compare against
    // (MutashabihatInvariants.* — 814 / 3,558 / 3,557 / 1,162 / 3,552); tests pass their fixture counts.
    // `sourceUnchangedCheck` is the MUT-SOURCE-UNCHANGED re-verification, injected by the handler.
    Task<MutashabihatImportResult> ImportAsync(
        MutashabihatSourceData source,
        bool force,
        MutashabihatExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        CancellationToken ct);
}
```

`sourceUnchangedCheck` is injected by the handler (it closes over
`IMutashabihatImportSource.SourceUnchangedAsync` and the source path). The writer therefore stays free of
source-path / file-digest knowledge, preserving Clean Architecture boundaries — file/source concerns live
in Infrastructure's source reader, not the bulk writer. The import transaction invokes the callback as the
`MUT-SOURCE-UNCHANGED` hard check, so a source that changed before commit fails the gate and rolls the
transaction back.

Behavioral contract:
- MUST NOT truncate, delete, or modify `quran_ayahs`, `quran_words`, the Quran text, or any non-mutashabihat
  table (FR-025, FR-026).
- With `force = false` and any target non-empty, the caller refuses **before** calling `ImportAsync`;
  `ImportAsync` also guards (defense in depth), consistent with the existing bulk writers.
- The transaction wraps truncate + `COPY` + validation; rollback on any hard-check failure or exception
  leaves the database in its pre-run state (FR-021).
- `Persisted = true` on the result **iff** the transaction committed.
- FK-safe `COPY` order: `quran_mutashabihat_groups` → `quran_mutashabihat_occurrences` →
  `quran_similar_ayah_links` (occurrences reference groups; all ayah FKs already exist in `quran_ayahs`).

## `IMutashabihatReportWriter`

```csharp
public interface IMutashabihatReportWriter
{
    Task WriteAsync(MutashabihatImportResult result, string outputDir, CancellationToken ct);
}
```

Writes a Markdown report and a JSON report (see `validation-report.schema.md`). Called for **every**
import that started (success or failure) (FR-032).

## Records

```csharp
public sealed record MutashabihatImportResult(
    DateTimeOffset RunAtUtc,
    string Verdict,                 // "pass" | "fail"
    bool Persisted,
    bool Forced,
    MutashabihatImportTotals Totals,
    IReadOnlyList<MutashabihatCheckResult> Checks,
    IReadOnlyList<string> Warnings, // e.g. coverage>100=4, duplicate-occurrence=1, source-key-absent=1
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes); // one-way links, cross-dataset overlap, surah coverage

public sealed record MutashabihatImportTotals(
    int GroupRows,                  // expected 814
    int RawOccurrenceEntries,       // raw source occurrence entries (expected 3,558)
    int StoredOccurrenceRows,       // stored unique occurrences after dedupe (expected 3,557)
    int LinkRows,                   // directed links (expected 3,552)
    int DistinctSimilarSources,     // distinct source ayahs (expected 1,162)
    int DistinctReferencedAyahs);   // union across both datasets (expected 3,084)

public sealed record MutashabihatCheckResult(
    string Id,                      // e.g. "MUT-AYAH-RESOLVE" (see data-model.md)
    string Severity,                // "hard" | "warning" | "info"
    string Expected,
    string Observed,
    bool Passed);                   // warning/info checks set Passed=true (recorded, never gate)
```

## Source DTOs (assembly inputs — no EF types)

```csharp
public sealed record MutashabihatSourceData(
    IReadOnlyList<PhraseGroupDto> Groups,        // 814 groups, each with its occurrences
    IReadOnlyList<SimilarLinkDto> Links);        // 3,552 directed links

public sealed record PhraseGroupDto(
    int SourceGroupId,
    int RepresentativeAyahId,                     // resolved from source.key
    short RepresentativeWordFrom,
    short RepresentativeWordTo,
    short OccurrenceCount,                         // recomputed
    short DistinctAyahCount,                       // recomputed (≥ 2)
    short DistinctSurahCount,                      // recomputed
    string? RawSourceCountsJson,                   // original {surahs, ayahs, count} (audit only)
    IReadOnlyList<OccurrenceDto> Occurrences);

public sealed record OccurrenceDto(
    int AyahId,                                    // resolved from verse_key
    short WordFrom,
    short WordTo,
    bool IsRepresentative);                        // at most one true per group

public sealed record SimilarLinkDto(
    int SourceAyahId,                              // resolved from the map key
    int TargetAyahId,                              // resolved from matched_ayah_key
    short Score,                                   // 50–100
    short Coverage,                                // raw, may exceed 100
    short MatchedWordsCount,
    string MatchWordsJson);                        // source ranges, preserved exactly
```

## `MutashabihatInvariants` (constants + messages) and `MutashabihatExpectedCounts`

```csharp
public sealed record MutashabihatExpectedCounts(
    int Groups,
    int RawOccurrences,
    int StoredOccurrences,
    int SimilarSources,
    int SimilarLinks,
    int DistinctAyahs);

public static class MutashabihatInvariants
{
    public const int ExpectedGroups             = 814;
    public const int ExpectedRawOccurrences     = 3_558;
    public const int ExpectedStoredOccurrences  = 3_557;   // after collapsing 1 duplicate
    public const int ExpectedSimilarSources     = 1_162;
    public const int ExpectedSimilarLinks       = 3_552;
    public const int ExpectedDistinctAyahs      = 3_084;

    // Known-anomaly baselines (warnings — never hard thresholds):
    public const int ExpectedCoverageGt100      = 4;
    public const int ExpectedDuplicateOccurrence = 1;      // group 75, ayah 16:28
    public const int ExpectedSourceKeyAbsent    = 1;       // group 1782, 3:28

    // Score/coverage ranges (hard: score; raw: coverage):
    public const short MinScore = 50;
    public const short MaxScore = 100;

    public static readonly MutashabihatExpectedCounts Production = new(
        ExpectedGroups, ExpectedRawOccurrences, ExpectedStoredOccurrences,
        ExpectedSimilarSources, ExpectedSimilarLinks, ExpectedDistinctAyahs);

    public const string TargetsNotEmpty =
        "Mutashabihat tables are not empty. Re-run with --force to truncate and rebuild them.";
    public const string SourceMismatch =
        "Local mutashabihat source files do not match manifest.json (file set / size / sha256).";
    public const string AyahsMissing =
        "quran_ayahs is empty or missing; run import-foundation first.";
}
```

## Verdict constants

`Verdict` uses the importer vocabulary: `"pass"` / `"fail"`. Severity `"hard"` failures set the verdict to
`"fail"` and force a rollback; `"warning"` and `"info"` checks never change the verdict.
