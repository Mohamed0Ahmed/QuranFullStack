# Contract — Morphology Import Abstractions (Application boundary)

Contracts the Application layer depends on, in `Application.Abstractions/Quran/Words/Morphology/`. These
expose **records / source DTOs only** — never EF entities — across the boundary (Clean Architecture).
Mirrors the Feature 002 import abstractions (`Quran/Import/`) in spirit.

## `IMorphologyImportSource`

Implemented by Infrastructure (`MorphologyImportSource`). Reads the **local** source tree, verifies the
manifest, parses the corpus + QUL files, and assembles the in-memory graph (including segment Arabic
rendering). Reads files only — never writes them.

```csharp
public interface IMorphologyImportSource
{
    // Verify manifest (presence, expectedRecordCount, fileSizeBytes, sha256) and parse + assemble the
    // full source graph from the local quran-morphology/ tree. Throws a typed error on any
    // manifest/source mismatch (caller refuses early). Captures pre-run file digests for
    // MORPH-SOURCE-UNCHANGED.
    Task<MorphologySourceData> LoadAsync(string sourcePath, CancellationToken ct);

    // Re-verify the local source files' size/sha256 after the run (MORPH-SOURCE-UNCHANGED).
    Task<bool> SourceUnchangedAsync(string sourcePath, CancellationToken ct);
}
```

## `IMorphologyImportWriter`

Implemented by Infrastructure (`EfBulkMorphologyWriter`). Runs the entire bulk load + validation in **one
transaction** and commits **only** if every hard check passes.

```csharp
public interface IMorphologyImportWriter
{
    // True if ANY of the six morphology tables is non-empty (drives refuse-unless-empty).
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    // Truncate (if force) → seed quran_pos_tags → COPY dimensions → morphology → segments →
    // run validation queries → commit iff all hard checks pass, else roll back. Returns the full
    // result either way. `expectedReadableWords` is the value the hard checks compare against
    // (MORPH-READABLE-COMPLETE). The CLI passes MorphologyInvariants.ExpectedReadableWords (77,432);
    // tests pass their synthetic fixture's readable-row count. `sourceUnchangedCheck` is the
    // MORPH-SOURCE-UNCHANGED re-verification, injected by the handler (see note below).
    Task<MorphologyImportResult> ImportAsync(
        MorphologySourceData source,
        bool force,
        int expectedReadableWords,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        CancellationToken ct);
}
```

`sourceUnchangedCheck` is injected by the handler (it closes over `IMorphologyImportSource.SourceUnchangedAsync`
and the source path). The writer therefore stays free of source-path / file-digest knowledge, preserving
Clean Architecture boundaries — file/source concerns live in Infrastructure's source reader, not the bulk
writer. The import transaction invokes the callback as the `MORPH-SOURCE-UNCHANGED` hard check, so a source
that changed before commit fails the gate and rolls the transaction back.

Behavioral contract:
- MUST NOT truncate, delete, or modify `quran_words` or any non-morphology table (FR-034).
- With `force = false` and any target non-empty, the caller refuses **before** calling `ImportAsync`;
  `ImportAsync` also guards (defense in depth), consistent with `EfBulkQuranImportWriter`.
- The transaction wraps truncate + seed + `COPY` + validation; rollback on any hard-check failure or
  exception leaves the database in its pre-run state (FR-031, FR-028).
- `Persisted = true` on the result **iff** the transaction committed.
- FK-safe `COPY` order: `quran_pos_tags` → dimensions (`quran_roots`/`quran_lemmas`/`quran_stems`) →
  `quran_word_morphology` → `quran_word_morphology_segments`.

## `IMorphologyReportWriter`

```csharp
public interface IMorphologyReportWriter
{
    Task WriteAsync(MorphologyImportResult result, string outputDir, CancellationToken ct);
}
```

Writes a Markdown report and a JSON report (see `validation-report.schema.md`). Called for **every**
import that started (success or failure) (FR-030).

## Records

```csharp
public sealed record MorphologyImportResult(
    DateTimeOffset RunAtUtc,
    string Verdict,                 // "pass" | "fail"
    bool Persisted,
    bool Forced,
    MorphologyImportTotals Totals,
    IReadOnlyList<MorphologyCheckResult> Checks,
    IReadOnlyList<string> Warnings, // e.g. whole-word-agreement %, tier distribution, review lists
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);

public sealed record MorphologyImportTotals(
    int MorphologyRows,             // expected 77,432
    int SegmentRows,                // derived; reported (≈128,219)
    int RootRows,                   // derived; reported
    int LemmaRows,                  // derived; reported
    int StemRows,                   // derived; reported
    int PosTagRows,                 // ≈30 (seeded)
    int ReadableWords,              // from quran_words; expected 77,432
    int EmptyFormRenders,           // expected 208 (NULL renders)
    IReadOnlyDictionary<string,int> RenderTierCounts); // clean / quranic_marks / review / multiword

public sealed record MorphologyCheckResult(
    string Id,                      // e.g. "MORPH-SEG-CHARSET" (see data-model.md)
    string Severity,                // "hard" | "warning"
    string Expected,
    string Observed,
    bool Passed);
```

## Source DTOs (assembly inputs — no EF types)

```csharp
public sealed record MorphologySourceData(
    IReadOnlyList<AlignedWordDto> Words,         // per readable word: segments + head fields
    IReadOnlyDictionary<string,string> Roots,    // location → Arabic root (QUL)
    IReadOnlyDictionary<string,string> Lemmas,   // location → Arabic lemma (QUL)
    IReadOnlyDictionary<string,string> Stems,    // location → Arabic stem (QUL)
    IReadOnlyList<string> CharsetWarnings);      // any out-of-map characters (drives MORPH-SEG-CHARSET)

public sealed record AlignedWordDto(
    string Location, string HeadPos, bool IsVerb, string? VerbTense, string? VerbVoice,
    string? CaseFeature, string? HeadFeaturesJson, IReadOnlyList<AlignedSegmentDto> Segments);

public sealed record AlignedSegmentDto(
    short SegmentNumber, string Kind, string Pos, string FormBuckwalter,
    string? FormArabicNormalized, string? RenderTier, string RenderSource,
    string? RootBuckwalter, string? LemmaBuckwalter, string FeaturesRaw, string? FeaturesJson);
```

## `MorphologyInvariants` (constants + messages)

```csharp
public static class MorphologyInvariants
{
    public const int ExpectedReadableWords = 77_432;  // morphology rows
    public const int ExpectedEmptyForms    = 208;     // empty (SUFFIX, PRON) renders → NULL
    public const string RenderSource       = "buckwalter-transliteration";
    // Informational baselines — NEVER hard thresholds (warnings only):
    public const double InformationalWholeWordAgreement = 0.7983; // ≈79.83%

    public const string TargetsNotEmpty =
        "Morphology tables are not empty. Re-run with --force to truncate and rebuild them.";
    public const string SourceMismatch =
        "Local morphology source files do not match manifest.json (presence/count/size/sha256).";
}
```

## Verdict constants

`Verdict` uses the importer vocabulary: `"pass"` / `"fail"`. Severity `"hard"` failures set the verdict to
`"fail"` and force a rollback; `"warning"` checks never change the verdict.
