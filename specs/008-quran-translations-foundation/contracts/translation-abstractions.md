# Contract — Translation Import Abstractions

Application depends on abstractions; Infrastructure implements file/database/report details. Contracts use
source/result records only and do not expose EF-specific types.

## `ITranslationImportSource`

```csharp
public interface ITranslationImportSource
{
    Task<TranslationSourceData> LoadAsync(
        string sourcePath,
        TranslationExpectedCounts expectedCounts,
        CancellationToken ct);

    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}
```

Responsibilities:

- Read the final package manifest.
- Read the final display metadata contract.
- Verify package shape, final manifest, final display metadata, and source/display alignment.
- Verify file set, size, and sha256 for approved source files.
- Parse approved source files.
- Resolve every verse key to canonical ayah identity.
- Assemble source metadata and ayah translation rows.
- Capture source digests for `TR-SOURCE-UNCHANGED`.
- Never write source package files.

## `ITranslationImportWriter`

```csharp
public interface ITranslationImportWriter
{
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    Task<TranslationImportResult> ExecuteAcceptedImportAsync(
        TranslationSourceData source,
        bool force,
        TranslationExpectedCounts expected,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        Func<TranslationImportResult, CancellationToken, Task> acceptanceReportWrite,
        CancellationToken ct);
}
```

Responsibilities:

- Refuse/guard non-empty targets without force.
- Rebuild only translation-owned tables when forced.
- Bulk write source rows and ayah translation rows.
- Run validation checks inside the import transaction.
- Roll back on any hard-check failure.
- Invoke `acceptanceReportWrite` before committing a passing import.
- Roll back if acceptance report writing fails.
- Return complete totals/checks/warnings/errors.
- Never mutate `quran_ayahs`, `quran_words`, tafsir tables, mutashabihat tables, or source files.

## `ITranslationReportWriter`

```csharp
public interface ITranslationReportWriter
{
    Task WriteAsync(TranslationImportReport report, string outputDir, CancellationToken ct);
}
```

Responsibilities:

- Write `translation-import-report.md`.
- Write `translation-import-report.json`.
- Include verdict, persistence status, totals, source summaries, excluded-source summaries, checks,
  warnings, errors, and info notes.

Report writing is acceptance-critical: a run is not successful if required reports cannot be written.

## `ITranslationImportReportBuilder`

```csharp
public interface ITranslationImportReportBuilder
{
    TranslationImportReport BuildValidationFailure(
        string sourcePath,
        TranslationSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        TranslationImportTotals totals,
        IReadOnlyList<TranslationCheckResult> checks,
        IReadOnlyList<string> errors);

    TranslationImportReport BuildRefusal(
        string sourcePath,
        TranslationSourceData? source,
        bool forced,
        DateTimeOffset runAtUtc,
        string refusalMessage);

    TranslationImportReport BuildCandidateSuccess(
        string sourcePath,
        TranslationSourceData source,
        bool forced,
        DateTimeOffset runAtUtc,
        TranslationImportTotals totals,
        IReadOnlyList<TranslationCheckResult> postCopyChecks,
        TranslationExpectedCounts expected);
}
```

Responsibilities:

- Assemble the report payload (`TranslationImportReport`) from import outcomes and loaded source data.
- Populate source summaries, excluded-source summaries, warnings, errors, and informational notes.
- For a passing import, enumerate **every** hard check (the load-time checks verified during `LoadAsync`,
  plus the supplied post-copy checks), all marked passed.
- Keep report text free of translation body text and Arabic Quran ayah text.

The Application handler depends on this abstraction; Infrastructure implements it.

## Required acceptance choreography

The implementation must follow this order:

1. `ITranslationImportSource.LoadAsync(...)` verifies and assembles the source package without database writes, using the supplied `TranslationExpectedCounts`.
2. The Application handler checks existing translation data and `--force` intent through `ITranslationImportWriter`.
3. `ITranslationImportWriter.ExecuteAcceptedImportAsync(...)` opens one database transaction.
4. Inside that transaction, the writer clears translation-owned tables only when forced, writes sources and ayah entries, runs hard checks, and calls `sourceUnchangedCheck`.
5. If any hard check fails, the writer rolls back and returns `Persisted = false`.
6. If hard checks pass, the writer builds the candidate success `TranslationImportResult` and calls `acceptanceReportWrite(result, ct)` before commit.
7. If the report callback fails, the writer rolls back and returns/raises a failure where no translation changes are accepted.
8. Only after both required reports are written may the writer commit and return `Persisted = true`.

This choreography is mandatory because `spec.md` states that validation success without reports is not an
accepted run.

## Records

```csharp
public sealed record TranslationExpectedCounts(
    int ApprovedSources,
    int SimpleSources,
    int WithFootnotesSources,
    int ExcludedSources,
    int Languages,
    int AyahsPerSource,
    int SourceAyahMappings);

public sealed record TranslationImportResult(
    DateTimeOffset RunAtUtc,
    string Verdict,                 // "pass" | "fail"
    bool Persisted,
    bool Forced,
    TranslationImportTotals Totals,
    IReadOnlyList<TranslationCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record TranslationImportTotals(
    int SourceRows,
    long AyahMappingRows,
    int ApprovedSources,
    int SimpleSources,
    int WithFootnotesSources,
    int ExcludedSources,
    int LanguageCount,
    int DistinctAyahs);

public sealed record TranslationCheckResult(
    string Id,
    string Severity,                // "hard" | "warning" | "info"
    string Expected,
    string Observed,
    bool Passed);

public sealed record TranslationImportReport(
    DateTimeOffset RunAtUtc,
    string Verdict,                 // "pass" | "fail"
    bool Persisted,
    bool Forced,
    string SourcePath,
    TranslationImportTotals Totals,
    IReadOnlyList<TranslationSourceSummary> SourceSummaries,
    IReadOnlyList<TranslationExcludedSourceSummary> ExcludedSourceSummaries,
    IReadOnlyList<TranslationCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record TranslationSourceSummary(
    string SourceKey,
    string LanguageCode,
    string Direction,
    string TranslationType,
    string DisplayNameEn,
    string DisplayNameAr,
    string PackageFile,
    string Sha256,
    long FileSizeBytes,
    bool ContainsInlineFootnotes,
    bool ContainsHtmlMarkup);

public sealed record TranslationExcludedSourceSummary(
    string SourceKey,
    string Status,
    string Reason);
```

## Source data records

```csharp
public sealed record TranslationSourceData(
    IReadOnlyList<TranslationSourceDto> Sources,
    IReadOnlyList<TranslationAyahEntryDto> AyahEntries,
    IReadOnlyList<ExcludedTranslationSourceDto> ExcludedSources);

public sealed record TranslationSourceDto(
    string SourceKey,
    string LanguageCode,
    string LanguageNameEn,
    string LanguageNameAr,
    string? NativeName,
    string Direction,
    string TranslationType,
    string DisplayNameEn,
    string DisplayNameAr,
    string? TranslatorKey,
    string? TranslatorNameEn,
    string? TranslatorNameAr,
    bool ContainsInlineFootnotes,
    bool ContainsHtmlMarkup,
    int ContentCoverageCount,
    string PackageFile,
    string Sha256,
    long FileSizeBytes);

public sealed record TranslationAyahEntryDto(
    string SourceKey,
    int AyahId,
    string VerseKey,
    string Text);

public sealed record ExcludedTranslationSourceDto(
    string SourceKey,
    string Status,
    string Reason,
    string? PackageFile);
```

`PackageFile`, `Sha256`, and `FileSizeBytes` are DTO/report validation values only. They must not become
v1 columns on `quran_translation_sources`.
