# Word-Level Lemma Alignment Correction Plan

**Project:** Quran Dashboard / المنهج القرآني  
**Feature:** 017 — Lexical Explorers Polish  
**Branch:** `017-lexical-explorers-polish`  
**Scope:** backend import-time data-quality correction plan only  
**Status:** implementation-ready after correction curation

---

## 1. Current Evidence

The audit verdict is **BLOCKED**: `quran_word_morphology.lemma_id` is not reliable enough today as the final Lemmas Explorer occurrence-set authority.

Evidence from `docs/feature-017-lexical-explorers-polish/word-level-lemma-alignment-audit-report.md`:

| Finding | Count |
| --- | ---: |
| Strict shifted audit rows requiring curation | 63 |
| Affected lemmas | 26 |
| Affected surahs | 28 |
| Affected ayahs | 61 |
| Strict rows hidden by matching `segment.lemma_id` inheritance | 62 |
| Strict rows visible in the simple no-matching-segment check | 1 |

The confirmed example is `28:50:10` / `28:50:11`: QUL word-level lemma `أَضَلّ` is absent from `28:50:10` (`أَضَلُّ`) and appears on `28:50:11` (`مِمَّنِ`). The Corpus segment lemma Buckwalter for `28:50:10` is `>aDal~`, while `28:50:11` has `min` and `man`.

This blocks Lemmas Explorer correctness because counts, details, ayahs, surahs, missing-surah views, stems relationships, type distribution, ayah type filtering, and word analysis lemma display all begin from persisted `quran_word_morphology.lemma_id`.

The 63 audit rows are not automatically corrections. They must be curated into either approved correction operations or documented accepted exceptions before the importer applies anything.

---

## 2. Actual Code Path Inspected

### CLI Entry Point

`Backend/tools/QuranDashboard.DataImporter/Program.cs`

- Dispatches verb `import-morphology` to `ImportMorphologyRunner.RunAsync(...)`.
- Creates the host with `AddApplication()` and `AddInfrastructure(...)`.

`Backend/tools/QuranDashboard.DataImporter/Import/VerbRunners/ImportMorphologyRunner.cs`

- Parses `--source`, `--report-out`, and `--force`.
- Defaults `sourcePath` through `DataImporterDefaults.ResolveDefaultMorphologySourcePath()`.
- Resolves the handler from DI and calls `ImportMorphologyHandler.HandleAsync(...)`.

`Backend/tools/QuranDashboard.DataImporter/Import/DefaultPaths/DataImporterDefaults.cs`

- Resolves the default morphology source folder to `resources/import-sources/quran-morphology`.

### Handler Orchestration

`Backend/application/QuranDashboard.Application/Quran/DataPipelines/Words/MorphologyImporting/ImportMorphologyHandler.cs`

- Calls `IMorphologyImportSource.LoadAsync(command.SourcePath, ct)`.
- Refuses non-forced imports when morphology target tables already have data.
- Calls `IMorphologyImportWriter.ImportAsync(...)` and passes `token => importSource.SourceUnchangedAsync(...)`.
- Writes the report through `IMorphologyReportWriter.WriteAsync(...)`.
- Converts source file IO/manifest failures to `MorphologyInvariants.SourceMismatch`.

### Source Loading and Manifest Validation

`Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologyImportSource.cs`

Actual load order:

1. `MorphologyManifestReader.ReadAsync(sourcePath, ct)`
2. `MorphologyManifestReader.CaptureDigestsAsync(sourcePath, ct)`
3. read readable Quran word ids from `quran_words` where `!IsAyahMarker`
4. `JsonAlignedCorpusReader.ReadAsync(...)`
5. `JsonQulRootReader.ReadAsync(...)`
6. `JsonQulLemmaReader.ReadAsync(...)`
7. `JsonQulStemReader.ReadAsync(...)`
8. `MorphologyAssembler.Assemble(corpusWords, readableWordIdsByLocation, roots, lemmas, stems)`

This is the correct insertion point for the correction overlay: after `JsonQulLemmaReader.ReadAsync(...)` returns the raw QUL word-lemma map and before `MorphologyAssembler.Assemble(...)` receives the `lemmas` dictionary.

`Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologyManifestReader.cs`

- Allows only `manifest.json`, `README.md`, `corpus/quranic-corpus-morphology-qpc-aligned.json`, `corpus/corpus-qpc-location-alignment-map.json`, `qul/word-root.json`, `qul/word-lemma.json`, and `qul/word-stem-corrected-arabic.json`.
- Requires manifest entries for the five data files, including the alignment map.
- Validates declared file set, on-disk folder contents, file existence, SHA-256, byte size, and object record counts when provided.
- Captures source digests after load and verifies they remain unchanged before commit.

`Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologySourceValidation.cs`

- Validates aligned Corpus coverage against readable `quran_words`.

### Source Readers

`Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/JsonQulReaders.cs`

- `JsonQulRootReader`, `JsonQulLemmaReader`, and `JsonQulStemReader` all read JSON object maps keyed by location.
- Blank values are skipped.
- QUL lemma values are Arabic strings, not Buckwalter ids.

`Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/JsonAlignedCorpusReader.cs`

- Reads location-keyed Corpus records.
- Uses `qpcUthmani` and `segments`.
- For each segment, reads `segmentNumber`, `kind`, `pos`, `form`, `features`, optional `root`, and optional `lemma`.
- Corpus root and lemma values are Buckwalter source keys.

### Dimension Resolution and Assignment

`Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologyAssembler.cs`

Where word-level data enters memory:

- `roots.TryGetValue(location, out var rv)` sets `qulRoot`.
- `lemmas.TryGetValue(location, out var lv)` sets `qulLemma`.
- `stems.TryGetValue(location, out var sv)` sets `qulStem`.

Where lemma ids are resolved:

- Nonblank `qulLemma` creates or reuses an entry in `lemmaIndex` keyed by Arabic lemma text.
- `lemmaEntry.AddWord(wordId)` increments `quran_lemmas.words_count`.
- `lemmaEntry.AddBuckwalter(corpusLemma)` stores the first nonblank Corpus STEM lemma Buckwalter observed for that Arabic lemma.
- `lemmaRootLinks` links a lemma to the co-occurring QUL root on the earliest word where both appear.
- `BuildResolvedLemmas(...)` creates `ResolvedLemmaDto` rows ordered by first word order.

Where `quran_word_morphology.lemma_id` is assigned:

- `AlignedWordDto.LemmaId` receives `lemmaId` derived from QUL `word-lemma.json`.
- `MorphologyBulkCopier.CopyMorphologyAsync(...)` writes `word.LemmaId` into `quran_word_morphology.lemma_id`.

Where segment `lemma_id` is assigned:

- `ResolveSegmentDimensions(...)` calls `ResolveLemmaId(...)` for each segment.
- Non-STEM segments always return `null`.
- Single-STEM words return `wordHeadLemmaId`; this is the inheritance that hides 62 of the 63 shifted rows from a simple segment-id equality check.
- Multi-STEM words resolve from Corpus `lemma_buckwalter`, with head-id shortcut, Arabic form match, curated disambiguation, and fail-closed issue recording.

### Persistence, Validation, and Reports

`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/EfBulkMorphologyWriter.cs`

- Opens one transaction.
- Truncates morphology tables only when `--force` is set.
- Copies POS tags, roots, lemmas, stems, word morphology, and segments.
- Runs hard checks before commit.
- Runs `MORPH-SOURCE-UNCHANGED` before commit through the handler-provided source unchanged callback.
- Commits only if all hard checks pass; otherwise rolls back.

`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyBulkCopier.cs`

- `CopyLemmasAsync(...)` writes `ResolvedLemmaDto` to `quran_lemmas`.
- `CopyMorphologyAsync(...)` writes `AlignedWordDto.LemmaId` to `quran_word_morphology.lemma_id`.
- `CopySegmentsAsync(...)` writes resolved segment `LemmaId` to `quran_word_morphology_segments.lemma_id`.

`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyValidationRunner.cs`

- Current hard checks run inside the import transaction before commit.
- `AddSegmentDimensionChecksAsync(...)` is the correct home for segment/lemma relationship checks.
- Add `MORPH-WORD-LEMMA-SHIFT-CLEAN` to this validation runner after existing dimension checks or in a dedicated helper called from `RunAllHardChecksAsync(...)`.

`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologySql.cs`

- Current SQL constants contain all persisted-table hard check SQL.
- Add the strict shifted word-level lemma SQL here, using the audit's strict source-shift logic adapted to persisted tables and accepted-exception locations.

`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyImportReportBuilder.cs`

- Builds totals and warnings from `MorphologySourceData`.
- Add correction summary warnings/notes and spot-check evidence here, or expose structured correction metadata through `MorphologyImportResult` and let the report writer render it.

`Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/DataPipelines/Words/MorphologyImporting/MarkdownJsonMorphologyReportWriter.cs`

- Writes `morphology-import-report.json` and `morphology-import-report.md`.
- Add structured correction summary fields to the JSON document and a dedicated Markdown section.

---

## 3. Actual Source Data Inspected

All source package paths below are real current paths under `resources/import-sources/quran-morphology`. They are local/gitignored by `.gitignore:1 resources/`, not repo-tracked. The plan must not edit them.

### `resources/import-sources/quran-morphology/manifest.json`

- Purpose: source package manifest.
- Repo-tracked: no; local/gitignored.
- Key shape: top-level `{ "files": [...] }`.
- Sample record shape: each file entry has `role`, `path`, `originPath`, `expectedRecordCount`, `fileSizeBytes`, `sha256`, and `notes`.
- Value type: metadata only.
- Importer use: `MorphologyManifestReader.ReadAsync(...)` validates file set, path safety, size, SHA-256, and object record counts.
- Persistence mapping: none.
- Protection: yes for declared file size/hash/record counts at load; source unchanged validation captures data-file digests before and after the import.
- Mutability policy: immutable staged source metadata; do not edit for this correction.

### `resources/import-sources/quran-morphology/qul/word-lemma.json`

- Purpose: QUL whole-word lemma source.
- Repo-tracked: no; local/gitignored.
- Key shape: JSON object keyed by location, for example `"28:50:11": "أَضَلّ"`.
- Count inspected: 72,507 object entries.
- Sample shape:
  - `28:50:10` is absent/null.
  - `28:50:11` is `أَضَلّ`.
  - `2:44:5` is absent/null.
  - `2:44:6` is `نَفْس`.
- Lemma value type: Arabic display/source lemma text only; no Buckwalter and no numeric id.
- Importer use: `JsonQulLemmaReader` reads nonblank entries into `IReadOnlyDictionary<string,string> lemmas`.
- Mapping to `quran_lemmas.id`: `MorphologyAssembler` keys `lemmaIndex` by Arabic text, assigns deterministic ids by first word order, and stores Corpus STEM lemma Buckwalter when available on the same word.
- Protection: yes; manifest size/hash/count at load and source unchanged validation before commit.
- Mutability policy: immutable upstream/staged source; corrections apply in memory only.

### `resources/import-sources/quran-morphology/qul/word-root.json`

- Purpose: QUL whole-word root source.
- Repo-tracked: no; local/gitignored.
- Key shape: JSON object keyed by location.
- Count inspected: 50,298 object entries.
- Sample shape:
  - `28:50:10` is `ض   ل   ل`.
  - `28:50:11` is absent/null.
  - `2:44:5` is `ن ف س`.
  - `2:44:6` is absent/null.
- Root value type: Arabic root text, with spaces preserved as source text.
- Importer use: `JsonQulRootReader` reads it into `roots`.
- Mapping to `quran_roots.id`: `MorphologyAssembler` keys `rootIndex` by Arabic root text and adds Corpus root Buckwalter from the same word's STEM segment when present.
- Protection: yes; manifest size/hash/count at load and source unchanged validation before commit.
- Mutability policy: immutable upstream/staged source.

### `resources/import-sources/quran-morphology/qul/word-stem-corrected-arabic.json`

- Purpose: QUL/corrected Arabic word stem source.
- Repo-tracked: no; local/gitignored.
- Key shape: JSON object keyed by location.
- Count inspected: 77,432 object entries.
- Sample shape:
  - `28:50:10` is `أَضَلُّ`.
  - `28:50:11` is `مِ`.
  - `2:44:5` is `أَنفُسَ`.
  - `2:44:6` is `أَنتُمْ`.
- Stem value type: Arabic stem display/source text.
- Importer use: `JsonQulStemReader` reads it into `stems`.
- Mapping to `quran_stems.id`: `MorphologyAssembler` keys `stemIndex` by Arabic stem text and assigns ids by first word order.
- Protection: yes; manifest size/hash/count at load and source unchanged validation before commit.
- Mutability policy: immutable upstream/staged source.

### `resources/import-sources/quran-morphology/corpus/quranic-corpus-morphology-qpc-aligned.json`

- Purpose: aligned Corpus morphology source at QPC word locations.
- Repo-tracked: no; local/gitignored.
- Key shape: JSON object keyed by QPC location.
- Count inspected: 77,432 object entries.
- Sample record shape:
  - top-level fields include `qpcLocation`, `originalCorpusLocation`, `alignmentType`, `qpcUthmani`, `qpcImlaei`, `segments`, and `notes`.
  - each segment includes `segmentLocation`, `segmentNumber`, `form`, `posColumn`, `features`, `kind`, `root`, `lemma`, and `pos`.
- Lemma/root value type: Corpus `root` and `lemma` fields are Buckwalter source keys; `qpcUthmani` is Arabic display text.
- Importer use: `JsonAlignedCorpusReader` reads `qpcUthmani` and each segment's `segmentNumber`, `kind`, `pos`, `form`, `features`, optional `root`, and optional `lemma`. It ignores metadata fields such as `originalCorpusLocation`, `alignmentType`, `qpcImlaei`, `notes`, `segmentLocation`, and `posColumn`.
- Mapping to persisted data:
  - segment `root` and `lemma` are persisted as `root_buckwalter` and `lemma_buckwalter`.
  - dimension ids are resolved later by `MorphologyAssembler`.
- Protection: yes; manifest size/hash/count at load and source unchanged validation before commit.
- Mutability policy: immutable upstream/staged source.

Confirmed samples:

| Location | QUL lemma | QUL root | QUL stem | Corpus segment evidence |
| --- | --- | --- | --- | --- |
| `28:50:10` | null | `ض   ل   ل` | `أَضَلُّ` | one STEM `N`, root `Dll`, lemma `>aDal~` |
| `28:50:11` | `أَضَلّ` | null | `مِ` | two STEM segments: `P/min`, `REL/man`, no root |
| `2:44:5` | null | `ن ف س` | `أَنفُسَ` | STEM `N`, root `nfs`, lemma `nafos`; suffix pronoun |
| `2:44:6` | `نَفْس` | null | `أَنتُمْ` | prefix `CIRC`; STEM `PRON` with no lemma/root |

### `resources/import-sources/quran-morphology/corpus/corpus-qpc-location-alignment-map.json`

- Purpose: alignment provenance map for Corpus-to-QPC locations.
- Repo-tracked: no; local/gitignored.
- Key shape: top-level object with `source`, `status`, `baselineWordCount`, `normalizedWordCount`, `originalCorpusUniqueWordCount`, `affectedAyahs`, `splitPairs`, and `mappings`.
- Sample mapping shape: `{ "qpcLocation": "1:1:1", "originalCorpusLocation": "1:1:1", "alignmentType": "direct", "notes": [] }`.
- Lemma value type: none.
- Importer use: manifest validation requires and protects the file, but `MorphologyImportSource` does not read this map into assembly.
- Mapping to persisted data: none directly.
- Protection: yes; manifest size/hash at load and source unchanged validation before commit.
- Mutability policy: immutable provenance/source-package file.

### Existing Reports and Audit Artifacts

`docs/feature-017-lexical-explorers-polish/word-level-lemma-alignment-audit-report.md`

- Repo-tracked: yes.
- Purpose: authoritative audit for the 63 strict source-shift rows, including the strict SQL and source spot-check script.
- Contains the full visible 63-row table and the SQL that produced the counts.
- No separate generated audit data file was found under `docs/feature-017-lexical-explorers-polish`; this report is the current audit artifact to use for curation.

`docs/feature-017-lexical-explorers-polish/lemma-details-segment-matched-word-types-feasibility-report.md`

- Repo-tracked: yes.
- Purpose: earlier related feasibility report that identifies the 8 no-matching-segment cases and flags `28:50:11` / wid `53708` as a pre-existing QUL head-lemma misalignment.
- Use as supporting evidence only; the correction curation source is the word-level lemma alignment audit report plus source JSON.

---

## 4. Phase 0 — Correction Curation

This phase happens before production-code implementation and before creating the final active overlay.

Required curation workflow:

1. Inspect all 63 strict source-shift rows from `word-level-lemma-alignment-audit-report.md`.
2. For each row, compare:
   - current location's raw QUL lemma/root/stem;
   - previous location's raw QUL lemma/root/stem;
   - current and previous Corpus segment `root`/`lemma` Buckwalter values;
   - current and previous `qpcUthmani` text;
   - audit reason and strict heuristic match.
3. Confirm which rows are real one-word QUL word-lemma shifts.
4. Mark real corrections as `reviewStatus = approved` with explicit operations.
5. Mark reviewed false positives or legitimate modeling divergences as `reviewStatus = accepted-exception`.
6. Leave no `candidate` or `needs-review` entries in the active overlay.
7. Do not apply all 63 rows automatically.

Decision rule:

- Approve only when the raw QUL current word has the content lemma, the previous content word has the matching QUL root/stem and Corpus lemma Buckwalter, and the current word's own Corpus segments do not support that content lemma.
- Accept as exception only when the heuristic is explained by legitimate QUL-vs-Corpus modeling divergence or other documented source semantics.
- Keep uncertain rows outside the active overlay or mark them `candidate` in a non-active draft file; active overlays with pending statuses must fail validation.

---

## 5. Correction Overlay Design

Add a repo-tracked overlay near the importer:

`Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/Corrections/word-lemma-alignment-corrections.json`

Rationale:

- `resources/import-sources/quran-morphology/` is local/gitignored staged source.
- Upstream QUL and Corpus source files remain immutable and are not edited.
- The overlay must be reviewable with code, deterministic in tests, and independent from staged source mutation.

### Canonical Operation Keys

Do not key or match operations by Arabic display text alone.

Canonical matching keys:

- `location`: primary Quran word location key, matching the source JSON object key and `quran_words.location`.
- `expectedCurrentLemmaArabic`: raw expected value from `qul/word-lemma.json` at that location; `null` means the location must be absent from the raw QUL lemma map.
- `correctedLemmaArabic`: Arabic value to write into the in-memory QUL lemma map before assembly; `null` removes the location from the in-memory map.
- `shiftedLemmaArabic`: Arabic QUL lemma that is currently on the wrong location.
- `shiftedLemmaBuckwalter`: Buckwalter for `shiftedLemmaArabic`; this is the lexical lemma being moved and is not automatically a Corpus lemma for every operation location.
- `targetLocation`: content-word location that should receive `shiftedLemmaArabic`.
- `targetCorpusLemmaBuckwalter`: Corpus STEM lemma Buckwalter evidence on `targetLocation`; for `28:50:10`, this is `>aDal~`.
- `targetLocationCorpusRootBuckwalter`: Corpus STEM root Buckwalter evidence on `targetLocation`; for `28:50:10`, this is `Dll`.
- `currentLocationCorpusLemmaBuckwalters`: Corpus segment lemma Buckwalters at the operation's own `location`; for a remove operation on `28:50:11`, this is `["min", "man"]`, not `>aDal~`.

Actual application uses `location` and `expectedCurrentLemmaArabic` against the raw QUL lemma map, then writes `correctedLemmaArabic` into an in-memory copy of the map. Directional Buckwalter fields are validation guards:

- add operations require the operation `location` to equal `targetLocation`, `correctedLemmaArabic` to equal `shiftedLemmaArabic`, and `targetCorpusLemmaBuckwalter` to be present on the target location's Corpus STEM evidence;
- remove operations require `expectedCurrentLemmaArabic` to equal `shiftedLemmaArabic`, `correctedLemmaArabic` to be `null`, and `currentLocationCorpusLemmaBuckwalters` to prove the remove location's own Corpus segments do not carry `shiftedLemmaBuckwalter`;
- no validation step should match `shiftedLemmaBuckwalter` against the remove location's Corpus lemmas unless the operation explicitly records that as a failure/evidence contradiction.

### Overlay Schema

Example only; this is not the final curated overlay:

```json
{
  "schemaVersion": 1,
  "sourceAudit": "docs/feature-017-lexical-explorers-polish/word-level-lemma-alignment-audit-report.md",
  "sourcePackage": "resources/import-sources/quran-morphology",
  "entries": [
    {
      "id": "WLA-0001",
      "reviewStatus": "approved",
      "verseKey": "28:50",
      "shiftedLemmaArabic": "أَضَلّ",
      "shiftedLemmaBuckwalter": ">aDal~",
      "targetLocation": "28:50:10",
      "targetCorpusLemmaBuckwalter": ">aDal~",
      "targetLocationCorpusRootBuckwalter": "Dll",
      "removeLocation": "28:50:11",
      "removeLocationCorpusLemmaBuckwalters": ["min", "man"],
      "evidence": [
        "QUL word-lemma has 28:50:10=null and 28:50:11=أَضَلّ",
        "Corpus 28:50:10 STEM lemma is >aDal~ with root Dll",
        "Corpus 28:50:11 segments are min/man with no root"
      ],
      "reason": "QUL word-level lemma is shifted one word forward from the content word to the following particle/relative word.",
      "operations": [
        {
          "location": "28:50:10",
          "expectedCurrentLemmaArabic": null,
          "correctedLemmaArabic": "أَضَلّ",
          "operationKind": "add",
          "currentLocationCorpusLemmaBuckwalters": [">aDal~"]
        },
        {
          "location": "28:50:11",
          "expectedCurrentLemmaArabic": "أَضَلّ",
          "correctedLemmaArabic": null,
          "operationKind": "remove",
          "currentLocationCorpusLemmaBuckwalters": ["min", "man"]
        }
      ]
    }
  ]
}
```

For the `28:50:11` remove operation, `shiftedLemmaBuckwalter = ">aDal~"` describes the wrongly assigned QUL lemma being removed. It must not be validated as a Corpus lemma for `مِمَّنِ`; the current/remove location's Corpus lemmas are `min` and `man`. The matching `>aDal~` Corpus evidence belongs to `targetLocation = "28:50:10"`.

### Review Status Rules

`reviewStatus = approved`

- Requires one or more operations.
- Operations are applied to an in-memory copy of the QUL word-level lemma map.
- Every operation must be reported as applied.

`reviewStatus = accepted-exception`

- Must not have operations.
- Documents a reviewed false positive or legitimate modeling divergence.
- Requires location, evidence, and reason.
- Exists only to allow diagnostics/hard checks to pass for documented exceptions.

`reviewStatus = candidate` or `reviewStatus = needs-review`

- Must fail validation in the active overlay.
- Must never be applied silently.
- Belongs only in a draft/non-active curation file.

Fail closed on duplicate entry ids, duplicate operation locations, missing evidence, malformed statuses, unsupported schema versions, mismatched expected values, or non-readable target locations.

### Runtime Loading Strategy

Load the active overlay as an `EmbeddedResource` from `QuranDashboard.Infrastructure`.

Required project item:

```xml
<EmbeddedResource
  Include="Files\Quran\DataPipelines\Words\MorphologyImporting\Corrections\word-lemma-alignment-corrections.json"
  LogicalName="QuranDashboard.Infrastructure.MorphologyImporting.word-lemma-alignment-corrections.json" />
```

Runtime behavior:

- `WordLemmaAlignmentCorrectionReader` loads the JSON with `typeof(WordLemmaAlignmentCorrectionReader).Assembly.GetManifestResourceStream("QuranDashboard.Infrastructure.MorphologyImporting.word-lemma-alignment-corrections.json")`.
- Local DataImporter runs use the embedded JSON from the referenced Infrastructure assembly; no working-directory or source-path resolution is involved.
- Published/runtime scenarios use the same embedded JSON inside the Infrastructure assembly.
- Tests use the production reader for the active overlay and add test-only overloads/helpers that parse supplied JSON strings/streams for malformed-overlay cases.
- Missing embedded resource fails closed before assembly or database writes with a specific overlay validation failure; no fallback path search is allowed.
- The report records the overlay identity as the embedded logical resource name plus SHA-256 of the embedded JSON content.
- The overlay is not included in `resources/import-sources/quran-morphology/manifest.json` because that manifest protects immutable upstream/staged source files. The overlay is repo-tracked correction policy bundled with importer code, has its own hash in the import report, and must not change source-package size/hash/source-unchanged semantics.

---

## 6. Actual Files/Classes Involved

| Area | Actual file/class | Implementation change | Exact reason |
| --- | --- | --- | --- |
| CLI dispatch | `Backend/tools/QuranDashboard.DataImporter/Program.cs` | No | Existing verb dispatch already routes `import-morphology` to the runner. |
| CLI runner | `Backend/tools/QuranDashboard.DataImporter/Import/VerbRunners/ImportMorphologyRunner.cs` | No | Existing CLI shape remains `import-morphology [--source] [--report-out] [--force]`; overlay is importer-internal. |
| Default source path | `Backend/tools/QuranDashboard.DataImporter/Import/DefaultPaths/DataImporterDefaults.cs` | No | Existing default source path is correct: `resources/import-sources/quran-morphology`. |
| Handler command | `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Words/MorphologyImporting/ImportMorphologyCommand.cs` | No | No new CLI options or command inputs are needed. |
| Handler orchestration | `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Words/MorphologyImporting/ImportMorphologyHandler.cs` | No | It already loads source, delegates import, runs source-unchanged callback, and writes reports. |
| Source abstraction | `Backend/application/QuranDashboard.Application.Abstractions/Quran/DataPipelines/Words/MorphologyImporting/IMorphologyImportSource.cs` | No | `LoadAsync` and `SourceUnchangedAsync` remain sufficient. |
| Source data DTO | `Backend/application/QuranDashboard.Application.Abstractions/Quran/DataPipelines/Words/MorphologyImporting/MorphologySourceData.cs` | Yes | Add structured correction summary metadata so validation/reporting can prove overlay identity, counts, applied operations, accepted-exception locations/evidence, and spot checks. |
| Import result DTO | `Backend/application/QuranDashboard.Application.Abstractions/Quran/DataPipelines/Words/MorphologyImporting/MorphologyImportResult.cs` | Yes | Add correction summary to persisted report model if not carried entirely in `MorphologySourceData` warnings. Prefer structured JSON fields. |
| Invariants | `Backend/application/QuranDashboard.Application.Abstractions/Quran/DataPipelines/Words/MorphologyImporting/MorphologyInvariants.cs` | Yes | Add `MORPH-WORD-LEMMA-SHIFT-CLEAN` constant and any overlay validation check ids. |
| DI registration | `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/MorphologyImportDependencyInjection.cs` | Yes | Register the overlay reader/applicator service. |
| Test DI registration | `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyTestServiceCollectionExtensions.cs` | Yes | Register the same overlay service for importer tests. |
| Manifest reader | `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologyManifestReader.cs` | No for upstream source; optional for overlay identity | Keep source-package validation strict and unchanged. Do not add the overlay to the local source folder manifest because that would mutate source-package semantics. |
| QUL readers | `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/JsonQulReaders.cs` | No | Existing readers correctly expose raw location-to-Arabic maps. |
| Corpus reader | `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/JsonAlignedCorpusReader.cs` | No | Existing reader exposes the Corpus Buckwalter segment fields needed for validation through `AlignedCorpusWord`. |
| Source loader | `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologyImportSource.cs` | Yes | Load/apply overlay after QUL lemma map read and before `MorphologyAssembler.Assemble(...)`; validate operation locations against readable words and Corpus evidence. |
| New overlay reader/applicator | `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/Corrections/WordLemmaAlignmentCorrectionReader.cs` | Yes | Parse JSON, compute hash, validate statuses/schema, apply approved operations to in-memory lemma map, and produce correction summary. |
| New overlay data types | `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/Corrections/WordLemmaAlignmentCorrectionModels.cs` | Yes | Keep parsing/apply types local to infrastructure. |
| Assembler | `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologyAssembler.cs` | No | It should receive an already-corrected `lemmas` map; no reader-layer or assembler workaround is needed. |
| Source coverage validation | `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologySourceValidation.cs` | No or small helper reuse | Existing Corpus/readable-word coverage remains correct; overlay-specific location validation can call new helper code. |
| Bulk writer | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/EfBulkMorphologyWriter.cs` | Yes | Include correction summary in result and ensure hard check participates before commit. |
| Bulk copier | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyBulkCopier.cs` | No | It already persists `word.LemmaId` and segment `LemmaId` from `MorphologySourceData`. |
| Validation runner | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyValidationRunner.cs` | Yes | Add `MORPH-WORD-LEMMA-SHIFT-CLEAN` as a hard check inside the import transaction. |
| Validation SQL | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologySql.cs` | Yes | Add strict shift detection SQL that returns suspected rows from persisted tables; accepted-exception filtering happens in C# using `MorphologySourceData` metadata. |
| Report builder | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyImportReportBuilder.cs` | Yes | Include correction summary, accepted exception counts, pending/rejected counts, and spot-check evidence. |
| Report writer | `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/DataPipelines/Words/MorphologyImporting/MarkdownJsonMorphologyReportWriter.cs` | Yes | Render correction summary in both JSON and Markdown reports. |
| Import tests | `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTests.cs` | Yes | Cover successful application, report summary, and confirmed anomaly behavior. |
| Fixture | `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTestFixture.cs` | Yes | Add fixture helpers for synthetic shifted source, overlay files, expected-current mismatches, duplicate operations, source digest checks, and report parsing. |
| Assembler tests | `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyAssemblerTests.cs` | No for overlay; keep existing | Overlay applies before assembler, so assembler behavior should remain focused on dimension derivation. Add tests here only if a dimension side effect needs direct proof. |
| Validation failure tests | `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyValidationFailureTests.cs` | Yes | Cover fail-closed hard check, pending statuses, expected-current mismatch, duplicate operation locations, non-readable target, and rollback. |
| Refusal/source safety tests | `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyRefusalForceTests.cs` | Yes | Extend source byte-identical coverage to prove upstream QUL/Corpus files remain unchanged. |

---

## 7. Exact Importer Integration Point

Apply the overlay immediately after QUL word-lemma loading and before `MorphologyAssembler.Assemble(...)`.

Implementation target in `MorphologyImportSource.LoadAsync(...)`:

```csharp
var lemmas = await lemmaReader.ReadAsync(
    GetManifestPath(manifest, "qul/word-lemma.json"), ct);

var correctedLemmas = correctionReader.Apply(
    lemmas,
    corpusWords,
    readableWordIdsByLocation,
    sourcePath);

var source = assembler.Assemble(corpusWords, readableWordIdsByLocation, roots, correctedLemmas.Map, stems);
return source with { CorrectionSummary = correctedLemmas.Summary };
```

Design answers:

- Apply after QUL word-lemma loading: yes.
- Apply before `MorphologyAssembler`: yes.
- Affect only the word-level lemma map: yes.
- Direct segment-level corrections: no. Segment `lemma_id` should continue to be derived by `MorphologyAssembler.ResolveSegmentDimensions(...)`.
- Effect on `quran_word_morphology.lemma_id`: corrected naturally because `AlignedWordDto.LemmaId` is derived from the corrected in-memory QUL lemma map.
- Effect on `quran_lemmas.words_count` and `first_word_order_in_mushaf`: corrected naturally because `lemmaEntry.AddWord(...)` and `BuildResolvedLemmas(...)` consume the corrected map.
- Effect on lemma/root links: corrected naturally when a corrected lemma co-occurs with a QUL root at the target content word.
- Effect on single-STEM segment inheritance: corrected naturally because single-STEM segment `lemma_id` inherits corrected `wordHeadLemmaId`; removal at the shifted pronoun/particle also removes inherited wrong segment ids.
- Effect on multi-STEM segment resolution: no direct special case; existing Corpus Buckwalter resolution stays authoritative.

---

## 8. Validation and Fail-Closed Checks

### Overlay Validation Before Assembly

Run inside `MorphologyImportSource.LoadAsync(...)`, before `MorphologyAssembler.Assemble(...)` and before any database write.

Fail when:

- overlay file is missing, malformed, or has unsupported `schemaVersion`;
- active overlay contains any `candidate` or `needs-review` entry;
- an `approved` entry has no operations;
- an `accepted-exception` entry has operations;
- duplicate entry ids exist;
- duplicate operation locations exist;
- operation `expectedCurrentLemmaArabic` does not match the raw staged QUL lemma map;
- operation Buckwalter guards do not match the inspected Corpus evidence for the source/target direction;
- operation points to a location absent from readable `quran_words`;
- corrected Arabic lemma cannot produce a resolvable `quran_lemmas` row with the expected Buckwalter after assembly;
- accepted exception is malformed or lacks evidence/reason/location;
- overlay hash cannot be computed.

### Import Transaction Hard Check

Add hard check:

`MORPH-WORD-LEMMA-SHIFT-CLEAN`

Location:

- SQL constant in `MorphologySql.cs`.
- Execution and `MorphologyCheckResult` creation in `MorphologyValidationRunner.RunAllHardChecksAsync(...)`.
- Runs after copy and before commit, inside the existing transaction.

It must fail if:

- any unapproved strict shifted word-level lemma row remains;
- any candidate/needs-review entry exists in the active overlay;
- any approved operation was not applied;
- any operation expected value did not match raw staged data;
- any accepted exception is malformed or lacks evidence;
- any operation points to a non-readable Quran word;
- source files were mutated.

### Accepted-Exception Transport and Filtering

Use C# filtering after the strict SQL returns suspected rows. This is the simplest robust fit for the current code because `MorphologyValidationRunner` already receives `MorphologySourceData`, while `MorphologySql` currently stores static SQL text and the command executor is optimized for scalar/static queries.

Storage after overlay loading:

- Add `MorphologyCorrectionSummary` to `MorphologySourceData`.
- Store accepted exceptions as structured records, for example `AcceptedExceptionLocation`, `VerseKey`, `ShiftedLemmaArabic`, `ShiftedLemmaBuckwalter`, `Evidence`, and `Reason`.
- Store approved operation application results in the same summary, including operation id, location, operation kind, and applied/not-applied status.
- Store rejected/pending counts even when zero.

Validation flow:

1. `WordLemmaAlignmentCorrectionReader` parses and validates the embedded overlay before assembly.
2. Malformed `accepted-exception` entries fail during overlay validation before any database write.
3. `MorphologyImportSource.LoadAsync(...)` puts validated accepted-exception metadata into `MorphologySourceData.CorrectionSummary`.
4. `MorphologyValidationRunner.RunAllHardChecksAsync(...)` calls a new query in `MorphologySql` that returns all strict suspected rows; the SQL does not filter exceptions.
5. The validation runner materializes suspected rows with an `NpgsqlCommand`/reader helper, then filters rows whose `Location` appears in `source.CorrectionSummary.AcceptedExceptions`.
6. Any suspected row not covered by a validated accepted exception fails `MORPH-WORD-LEMMA-SHIFT-CLEAN`.
7. Any accepted-exception location not returned by the strict query remains reportable as an accepted exception but does not fail by itself; it still had to pass overlay schema/evidence validation.

Report flow:

- The report records accepted-exception count from `MorphologySourceData.CorrectionSummary.AcceptedExceptions.Count`.
- The report includes each accepted-exception location, evidence, and reason in JSON; Markdown summarizes the count and lists representative entries.
- The report records strict suspected row count before filtering, accepted-exception filtered count, and unapproved remaining count.
- If unapproved remaining count is nonzero, `MORPH-WORD-LEMMA-SHIFT-CLEAN` fails and the transaction rolls back.

Source mutation handling:

- Keep existing `MORPH-SOURCE-UNCHANGED` in `EfBulkMorphologyWriter.ImportAsync(...)`.
- Do not edit staged QUL/Corpus files.
- Include overlay hash separately in the report; the overlay is repo-tracked correction data, not part of the upstream source package manifest.

Strict shift SQL:

- Use the audit's strict previous-word source-shift heuristic adapted to persisted tables.
- Do not rely on `segment.lemma_id = m.lemma_id`; 62 of 63 current shifted rows are hidden by single-STEM inheritance.
- Return row details needed for C# filtering and reporting: location, word id, word text, head lemma id/text/Buckwalter, current segment lemma Buckwalters, previous location, previous root id/text/Buckwalter, and previous segment lemma Buckwalters.
- Do not embed accepted-exception filtering in SQL.

---

## 9. Reports

Update the morphology import report JSON and Markdown to include a correction section.

Required fields:

- overlay file path or embedded resource identity;
- overlay hash;
- approved correction entry count;
- applied operation count;
- accepted exception count;
- rejected/pending candidate count, including zero;
- active overlay status validation result;
- result of `MORPH-WORD-LEMMA-SHIFT-CLEAN`;
- list of unapplied operation ids/locations when nonzero;
- source package path and statement that upstream source files were not mutated;
- spot-check evidence for `28:50:10` / `28:50:11`.

Required spot check:

| Location | Required after correction |
| --- | --- |
| `28:50:10` | word-level lemma `أَضَلّ`; Corpus segment lemma Buckwalter `>aDal~`; root evidence `Dll` / QUL root `ض   ل   ل` |
| `28:50:11` | no word-level lemma `أَضَلّ`; no inherited `أَضَلّ` segment id; Corpus segment lemmas remain `min` and `man` |

---

## 10. Tests

Add focused backend tests using actual current test infrastructure.

| Test | Actual test target | Expected coverage |
| --- | --- | --- |
| Confirmed `28:50:10` / `28:50:11` anomaly | `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTests.cs` or new focused file in the same folder | Overlay moves `أَضَلّ` from `28:50:11` to `28:50:10`; shifted location no longer carries it. |
| Representative non-28:50 case | same | Use one curated row such as `2:44:5` / `2:44:6`; prove the correction is data-driven. |
| Overlay parser/schema validation | new tests near importer tests | Valid overlay parses; unsupported schema, malformed entries, missing evidence, and duplicate ids fail closed. |
| Approved operations apply | `MorphologyImportTests.cs` | Approved operations alter only the in-memory lemma map and persist corrected lemma ids. |
| Accepted exception does not apply changes | `MorphologyImportTests.cs` | `accepted-exception` entries have no operations, change no rows, and allow documented diagnostic exception. |
| Accepted exception C# filtering | `MorphologyValidationFailureTests.cs` or new focused validation test | Strict SQL returns the suspected row; validation runner filters it only when `MorphologySourceData.CorrectionSummary.AcceptedExceptions` contains a validated matching location. |
| Candidate/needs-review fails closed | `MorphologyValidationFailureTests.cs` | Active overlay with pending status fails before persistence. |
| Expected-current mismatch fails closed | `MorphologyValidationFailureTests.cs` | Operation whose `expectedCurrentLemmaArabic` differs from raw QUL source fails before persistence. |
| Duplicate operation locations fail closed | `MorphologyValidationFailureTests.cs` | Duplicate operation target location is rejected. |
| Non-readable target fails closed | `MorphologyValidationFailureTests.cs` | Operation targeting an ayah marker or absent location is rejected. |
| Embedded resource loading | new overlay reader tests | Production reader locates `QuranDashboard.Infrastructure.MorphologyImporting.word-lemma-alignment-corrections.json`, computes hash, and fails closed if the resource is absent in a test-only reader configuration. |
| Source files remain byte-identical | `MorphologyRefusalForceTests.cs` or fixture-backed new test | Existing source digest helpers prove QUL/Corpus staged files are unchanged before/after import. |
| Report includes correction summary | `MorphologyImportTests.cs` | JSON and Markdown include overlay path, hash, counts, check result, and spot checks. |
| Hard check fails without overlay on shifted synthetic data | `MorphologyValidationFailureTests.cs` | Synthetic source reproducing strict shifted pattern fails `MORPH-WORD-LEMMA-SHIFT-CLEAN`. |
| Reproducibility | `MorphologyRefusalForceTests.cs` | If practical, two forced imports from same source plus overlay produce stable table hashes and correction summary. |

Fixture support:

- Extend `MorphologyImportTestFixture` to write active overlay files into temporary source/test folders or inject a test overlay reader.
- Reuse existing synthetic source helpers (`WriteSyntheticSourceFolderAsync`, `PatchQulLemmaMapAsync`, `PatchQulRootMapAsync`, `PatchCorpusSegmentsAsync`, `CaptureSourceDigestsAsync`, `AreSourceFilesUnchangedAsync`).
- Keep Quranic test data source-safe and minimal.

Run the test-code self-check after adding tests:

- behavior over implementation details;
- real importer boundary where persistence correctness matters;
- real DTOs/entities;
- source-safe Quran data;
- mocks only at real boundaries.

---

## 11. Local Acceptance After Implementation

Do not run this during planning.

Acceptance flow:

1. Confirm current branch:
   - `git branch --show-current`
2. Confirm database target is local only before destructive commands:
   - inspect local connection settings/user secrets without recording credentials.
3. Reset/drop local database only:
   - `cd Backend`
   - `./scripts/reset-db --yes`
4. Apply migrations:
   - `./scripts/update-db`
5. Run imports in correct dependency order:
   - foundation
   - rebuild-words
   - morphology
   - i3rab
   - any later imports required for normal local state if needed
6. Rerun the same word-level lemma alignment audit/search:
   - strict shifted count;
   - no-matching-segment count;
   - affected lemmas/ayahs/surahs;
   - `28:50:10` / `28:50:11` spot check;
   - morphology report correction summary.
7. Compare before/after against `word-level-lemma-alignment-audit-report.md`.

Expected result:

- no unapproved shifted word-level lemma rows remain;
- only documented accepted exceptions remain;
- `28:50:10` carries `أَضَلّ`;
- `28:50:11` no longer carries `أَضَلّ`;
- morphology report includes overlay identity, hash, counts, spot checks, and `MORPH-WORD-LEMMA-SHIFT-CLEAN`;
- Lemmas Explorer occurrence-set foundation is safe to continue.

---

## 12. Scope Boundaries

Confirmed out of scope:

- no frontend changes;
- no Lemma Details type-distribution fix in this plan;
- no Roots/Stems behavior changes unless the inspected audit proves direct persisted-data impact;
- no migration unless implementation inspection proves unavoidable;
- no reader-layer workaround;
- no manual SQL/post-import fix;
- no upstream QUL/Corpus mutation;
- no destructive DB commands during planning;
- no final correction overlay during planning;
- no commit.

Implementation should stay small: curation output, overlay reader/applicator, importer application point, report metadata, strict-shift hard check, and targeted tests.
