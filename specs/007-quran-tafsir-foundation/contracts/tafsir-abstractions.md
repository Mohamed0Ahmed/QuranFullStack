# Contract — Tafsir Import Abstractions

Application depends on abstractions; Infrastructure implements file/database/report details. Contracts
use source/result records only and do not expose EF-specific types.

## `ITafsirImportSource`

```csharp
public interface ITafsirImportSource
{
    Task<TafsirSourceData> LoadAsync(string sourcePath, CancellationToken ct);

    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}
```

Responsibilities:

- Read the final package manifest.
- Verify final package shape and file integrity.
- Parse approved source files.
- Resolve every verse key and pointer target to canonical ayah identity.
- Assemble source metadata, tafsir text blocks, and ayah mappings.
- Capture source digests for `TAFSIR-SOURCE-UNCHANGED`.
- Never write source package files.

## `ITafsirImportWriter`

```csharp
public interface ITafsirImportWriter
{
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    Task<TafsirImportResult> ExecuteAcceptedImportAsync(
        TafsirSourceData source,
        bool force,
        TafsirExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<TafsirImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct);
}
```

Responsibilities:

- Refuse/guard non-empty targets without force.
- Rebuild only tafsir-owned tables when forced.
- Bulk write source rows, text-block rows, and ayah-link rows.
- Run validation checks inside the import transaction.
- Roll back on any hard-check failure.
- Invoke `acceptanceReportWrite` before committing a passing import.
- Roll back if acceptance report writing fails.
- Return complete totals/checks/warnings/errors.
- Never mutate `quran_ayahs`, `quran_words`, or source files.

## `ITafsirReportWriter`

```csharp
public interface ITafsirReportWriter
{
    Task WriteAsync(TafsirImportResult result, string outputDir, CancellationToken ct);
}
```

Responsibilities:

- Write `tafsir-import-report.md`.
- Write `tafsir-import-report.json`.
- Include verdict, persistence status, totals, source summaries, excluded-source summaries, checks,
  warnings, errors, and info notes.

Report writing is acceptance-critical: a run is not successful if required reports cannot be written.

## Required acceptance choreography

The implementation must follow this order:

1. `ITafsirImportSource.LoadAsync(...)` verifies and assembles the source package without database writes.
2. The Application handler checks existing tafsir data and `--force` intent through `ITafsirImportWriter`.
3. `ITafsirImportWriter.ExecuteAcceptedImportAsync(...)` opens one database transaction.
4. Inside that transaction, the writer clears tafsir-owned tables only when forced, writes sources/text
   blocks/ayah links, runs hard checks, and calls `sourceUnchangedCheck`.
5. If any hard check fails, the writer rolls back and returns `Persisted = false`.
6. If hard checks pass, the writer builds the candidate success `TafsirImportResult` and calls
   `acceptanceReportWrite(result, ct)` before commit.
7. If the report callback fails, the writer rolls back and returns/raises a failure where no tafsir changes
   are accepted.
8. Only after both required reports are written may the writer commit and return `Persisted = true`.

This choreography is mandatory because `spec.md` clarifies that validation success without reports is not
an accepted run.

## Records

```csharp
public sealed record TafsirExpectedCounts(
    int ApprovedSources,
    int ExcludedSources,
    int ArabicSources,
    int NonArabicSources,
    int Languages,
    int AyahsPerSource,
    int SourceAyahMappings);

public sealed record TafsirImportResult(
    DateTimeOffset RunAtUtc,
    string Verdict,                 // "pass" | "fail"
    bool Persisted,
    bool Forced,
    TafsirImportTotals Totals,
    IReadOnlyList<TafsirCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record TafsirImportTotals(
    int SourceRows,
    long TafsirTextBlockRows,
    long AyahMappingRows,
    int ApprovedSources,
    int ExcludedSources,
    int ArabicSources,
    int NonArabicSources,
    int LanguageCount,
    int DistinctAyahs);

public sealed record TafsirCheckResult(
    string Id,
    string Severity,                // "hard" | "warning" | "info"
    string Expected,
    string Observed,
    bool Passed);
```

## Source data records

```csharp
public sealed record TafsirSourceData(
    IReadOnlyList<TafsirSourceDto> Sources,
    IReadOnlyList<TafsirEntryDto> Entries,
    IReadOnlyList<TafsirAyahEntryDto> AyahEntries,
    IReadOnlyList<ExcludedTafsirSourceDto> ExcludedSources);

public sealed record TafsirSourceDto(
    string SourceKey,
    string LanguageCode,
    string LanguageNameAr,
    string LanguageNameEn,
    string Direction,
    string DisplayNameAr,
    string ShortNameAr,
    string DisplayNameEn,
    string ShortNameEn,
    string? ContributorKey,
    string? ContributorNameAr,
    string? ContributorNameEn,
    string ContributorType,
    string ResourceKind,
    string TafsirKind,
    short ContentCoverageCount,
    string PackageFile,
    string SourceFileOriginal,
    string Sha256,
    long FileSizeBytes,
    string LicenseStatus,
    string ProvenanceStatus,
    string ManifestMetadataJson);

public sealed record TafsirEntryDto(
    string SourceKey,
    string SourceEntryKey,
    int LeaderAyahId,
    string TafsirText,
    short CoveredAyahCount,
    string CoveredAyahKeysJson,
    string SourceShape,
    string TextHash);

public sealed record TafsirAyahEntryDto(
    string SourceKey,
    int AyahId,
    string VerseKey,
    string SourceValueKind,
    string SourceLeaderVerseKey,
    bool IsGroupLeader,
    int SortOrder);

public sealed record ExcludedTafsirSourceDto(
    string SourceKey,
    string Status,
    string ResourceKind,
    int ContentCoverageCount,
    string SourceFileOriginal,
    string ReviewReason);
```

## Invariant constants

```csharp
public static class TafsirInvariants
{
    public const int ExpectedApprovedSources = 84;
    public const int ExpectedExcludedSources = 9;
    public const int ExpectedArabicSources = 35;
    public const int ExpectedNonArabicSources = 49;
    public const int ExpectedLanguageCount = 33;
    public const int ExpectedAyahsPerSource = 6_236;
    public const int ExpectedSourceAyahMappings = 523_824;

    public const string TargetsNotEmpty =
        "Tafsir tables are not empty. Re-run with --force to rebuild them.";
    public const string SourceMismatch =
        "Local tafsir source package does not match manifest.json.";
    public const string AyahsMissing =
        "quran_ayahs is empty or missing; run import-foundation first.";
    public const string ReportRequired =
        "Tafsir import passed validation, but required reports could not be written; no tafsir changes were accepted.";
}
```
