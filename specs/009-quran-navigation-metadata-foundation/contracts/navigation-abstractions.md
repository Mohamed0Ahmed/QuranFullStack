# Contract — Application-Boundary Abstractions (`Quran/Navigation`)

These are the Application-layer contracts the importer orchestrates against, mirroring the
`Quran/Translations` and `Quran/Tafsirs` abstraction sets. They are **not** public HTTP contracts. Shapes
below are the intended C# surface; the implementing model should match the existing importer idioms
(records, `sealed`, `CancellationToken ct`, async).

## Expected counts & check ids — `NavigationMetadataInvariants`

```csharp
public static class NavigationMetadataInvariants
{
    public const int ExpectedJuz   = 30;
    public const int ExpectedHizb  = 60;
    public const int ExpectedRub   = 240;
    public const int ExpectedSajda = 15;
    public const int ExpectedAyahs = 6_236;

    public const int ExpectedSajdaRequired = 4;
    public const int ExpectedSajdaOptional = 11;

    public static readonly NavigationExpectedCounts Production =
        new(ExpectedJuz, ExpectedHizb, ExpectedRub, ExpectedSajda, ExpectedAyahs);

    // refusal / failure messages
    public const string TargetsNotEmpty =
        "Navigation metadata tables (or quran_ayahs nav columns) are not empty. Re-run with --force to rebuild them.";
    public const string SourceMismatch  = "Local navigation source package does not match manifest.json.";
    public const string AyahsMissing    = "quran_ayahs is empty or missing; run import-foundation first.";
    public const string ReportRequired  =
        "Navigation import passed validation, but required reports could not be written; no navigation changes were accepted.";

    // hard check ids (see validation-report.schema.md)
    public const string CheckPackageShape       = "NAV-PACKAGE-SHAPE";
    public const string CheckManifestFinal      = "NAV-MANIFEST-FINAL";
    public const string CheckSourceCount        = "NAV-SOURCE-COUNT";
    public const string CheckSourceHash         = "NAV-SOURCE-HASH";
    public const string CheckJsonShape          = "NAV-JSON-SHAPE";
    public const string CheckVerseKeysResolve   = "NAV-VERSE-KEYS-RESOLVE";
    public const string CheckRangeCoverageJuz   = "NAV-RANGE-COVERAGE-JUZ";
    public const string CheckRangeCoverageHizb  = "NAV-RANGE-COVERAGE-HIZB";
    public const string CheckRangeCoverageRub   = "NAV-RANGE-COVERAGE-RUB";
    public const string CheckNoRangeGapsOverlaps= "NAV-NO-RANGE-GAPS-OVERLAPS";
    public const string CheckHierarchy          = "NAV-HIERARCHY";
    public const string CheckSajdaType          = "NAV-SAJDA-TYPE";
    public const string CheckAyahColumnsComplete= "NAV-AYAH-COLUMNS-COMPLETE";
    public const string CheckNoQuranTextCopy    = "NAV-NO-QURAN-TEXT-COPY";
    public const string CheckSourceUnchanged    = "NAV-SOURCE-UNCHANGED";
    public const string CheckReportWritten      = "NAV-REPORT-WRITTEN";
    public const string CheckRollbackOnFail     = "NAV-ROLLBACK-ON-FAIL";
    public const string CheckRerunGuard         = "NAV-RERUN-GUARD";

    // warnings (non-blocking)
    public const string WarningVerseCountMatch  = "NAV-VERSE-COUNT-MATCH";
    public const string WarningSajdaDistribution= "NAV-SAJDA-DISTRIBUTION";
}

public sealed record NavigationExpectedCounts(int Juz, int Hizb, int Rub, int Sajda, int Ayahs);
```

## Source loading — `INavigationMetadataImportSource`

```csharp
public interface INavigationMetadataImportSource
{
    // LoadAsync responsibility BOUNDARY (file-only, no DB):
    //   package-root load, manifest validation (packageType + isFinalImportManifest),
    //   file-set validation, sha256/size/recordCount validation, JSON parsing, required source-field
    //   validation (incl. sajda type allowed-set). Returns parsed source data with NO Quran ayah text.
    // LoadAsync MUST NOT access the database: it does NOT resolve verse_keys against quran_ayahs,
    //   does NOT expand verse_mapping, and does NOT run coverage/gap-overlap/hierarchy validation.
    //   Those run after loading — assembler (in-memory) then validator (against quran_ayahs).
    // Throws NavigationMetadataValidationException / NavigationMetadataSourceException on failure.
    Task<NavigationMetadataSourceData> LoadAsync(
        string sourcePath,                 // package root
        NavigationExpectedCounts expected,
        CancellationToken ct);

    // Re-verifies sha256/size of the package files (used just before commit).
    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}
```

`NavigationMetadataSourceData` carries the parsed, manifest-verified datasets (juz/hizb/rub records with
their `verse_mapping`, and sajda records) plus the resolved package file hashes. **It MUST NOT carry any
Quran ayah text.**

## Persistence — `INavigationMetadataImportWriter`

```csharp
public interface INavigationMetadataImportWriter
{
    // True if any of the 4 nav tables is non-empty OR any quran_ayahs nav column is populated.
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    // Single transaction: (optionally clear under force) -> resolve verse_keys -> insert headers ->
    // UPDATE quran_ayahs nav columns -> run hard checks -> source-unchanged re-check -> acceptance report ->
    // commit, else rollback. Returns persisted/forced, totals, per-check results, warnings, errors.
    Task<NavigationMetadataImportResult> ExecuteAcceptedImportAsync(
        NavigationMetadataSourceData source,
        bool force,
        NavigationExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<NavigationMetadataImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct);
}
```

## Reporting — `INavigationMetadataReportWriter` / `INavigationMetadataImportReportBuilder`

```csharp
public interface INavigationMetadataReportWriter   // writes Markdown + JSON to report dir
{
    Task WriteAsync(NavigationMetadataImportReport report, string reportOutDir, CancellationToken ct);
}

public interface INavigationMetadataImportReportBuilder
{
    NavigationMetadataImportReport BuildCandidateSuccess(/* sourcePath, source, force, runAtUtc, totals, checks, expected */);
    NavigationMetadataImportReport BuildValidationFailure(/* … checks, errors */);
    NavigationMetadataImportReport BuildRefusal(/* sourcePath, source, force, runAtUtc, message */);
}
```

## Use case — `ImportNavigationMetadataCommand` / `Handler` / `Result`

```csharp
public sealed record ImportNavigationMetadataCommand(
    string SourcePath,                       // package root
    bool Force,
    NavigationExpectedCounts? ExpectedCounts,
    string? ReportOutDir);

// Handler flow (mirrors ImportTranslationsHandler):
//  1. LoadAsync (file-only: manifest + file-set + counts + hash + json + required-fields; NO DB)
//     - on NavigationMetadataValidationException -> write failure report -> non-zero
//     - on NavigationMetadataSourceException / Json / IO -> refusal report -> non-zero
//  2. if !Force && AnyTargetTableHasDataAsync -> refusal (NAV-RERUN-GUARD)
//  3. ExecuteAcceptedImportAsync (transaction): require quran_ayahs non-empty (else AyahsMissing) ->
//     assemble verse_mapping (in-memory) -> validate against quran_ayahs (resolve verse_keys,
//     coverage 6236-once, gaps/overlaps, hierarchy) -> persist -> sourceUnchangedCheck -> acceptanceReportWrite
//  4. if !Persisted -> failure report; else success report
public sealed class ImportNavigationMetadataResult
{
    public bool Succeeded { get; }
    public string Message { get; }
    public int ExitCode { get; }            // success 0, failure non-zero (const FailureExitCode)
    public NavigationImportTotals? Totals { get; }
    public string? ReportOutDir { get; }
    public int WarningCount { get; }
    // factory: Success(totals, dir, warnings) / Failure(msg, dir[, warnings]) / Refused(msg, dir)
}

public sealed record NavigationImportTotals(
    int Juz, int Hizb, int Rub, int Sajda, int AyahsTagged);
```

## Exceptions

- `NavigationMetadataSourceException` — package/manifest mismatch or unreadable source (refusal path).
- `NavigationMetadataValidationException` — carries failed `NAV-*` checks (`{ Id, Expected, Observed }`)
  for the failure report (validation-failure path).

## Notes for the implementer

- Follow the `Quran/Translations` file set 1:1 (rename `Translation…` → `NavigationMetadata…`).
- The Application layer depends only on these abstractions; all EF/Npgsql/file work lives in Infrastructure.
- Domain entities (`Juz`, `Hizb`, `Rub`, `Sajda`, `SajdahType`) contain no EF/IO concerns.
- Resolve `verse_key` via the existing `quran_ayahs` unique `verse_key` index; reuse the `VerseKey` value
  object for shape validation.
