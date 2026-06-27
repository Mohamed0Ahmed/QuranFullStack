# Word-Level Lemma Full Normalization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. **Do not start any implementation task until every Phase 0 curation gate below is GREEN.**

**Project:** Quran Dashboard / المنهج القرآني
**Feature:** 017 — Lexical Explorers Polish
**Branch:** `017-lexical-explorers-polish`
**Scope:** backend morphology import-time word-level lemma normalization only
**Status:** **BLOCKED — curation not complete.** Implementation must not begin until the normalization artifact is final with zero active blockers.

**Goal:** Replace the limited "fix 63 shifted lemma rows" effort with one controlled, evidence-backed *word-level lemma normalization* pass over the Quran morphology importer that solves all known classes of word-level lemma defects (shifts, wrong assignments, missing recovery, uncertain cases, multi-STEM compounds) and prevents future wrong imports — while failing closed until curation is complete.

**Architecture:** Keep upstream QUL/Corpus source files immutable. Introduce one repo-tracked, embedded normalization artifact. The importer loads raw `qul/word-lemma.json`, applies the artifact's validated operations to an in-memory lemma map, then assembles morphology from the corrected map. Corpus is evidence/guard only, never the sole Arabic lemma source for an ambiguous mapping. A post-copy hard-check family proves corrections were applied and no unapproved defect remains; otherwise the transaction rolls back.

**Tech Stack:** .NET / C#, EF Core + Npgsql, PostgreSQL, the existing `QuranDashboard.DataImporter` morphology pipeline, embedded-resource JSON, xUnit + Testcontainers.

---

## 1. Executive Summary

### 1.1 Current status

**BLOCKED.** The final full audit verdict is `BLOCKED — broader source alignment issue exists beyond the 63` (`full-word-level-lemma-alignment-audit-report.md` §1, §13). `quran_word_morphology.lemma_id` is not yet reliable as the Lemmas Explorer occurrence-set authority.

### 1.2 Why the old 63-entry plan is insufficient

The earlier `word-level-lemma-alignment-correction-plan.md` and the curated `word-level-lemma-alignment-corrections.draft.json` (63 `approved` entries) are real but incomplete and partly wrong:

1. **Three entries are mis-modeled.** The draft applies `remove`-to-null at `3:33:7`, `21:51:3`, and `28:50:11`. The full audit (§7) proves those remove locations carry their **own** reliable lemma and must be `replace`, not null:
   - `3:33:7` `ءَال` → `إِبْرَاهِيم`
   - `21:51:3` `آتَى` → `إِبْرَاهِيم`
   - `28:50:11` `أَضَلّ` → `مِن`
   Applying the draft as-is would erase three legitimate word-level lemmas.
2. **59 additional previous-word shift candidates exist outside the 63** (full audit §11), several landing on another *content* word (so they need `replace`, not remove-to-null), and several being phrase-head / modeling cases that are not the rootless-pronoun pattern. They are not curated.
3. **1,595 QUL-missing words have reliable Arabic recovery candidates** outside the 63 (full audit §9). The old plan does not recover them.
4. **130 uncertain/manual-review cases** and **46 missing-with-Corpus-but-no-reliable-mapping** cases are unhandled.
5. **Multi-STEM/compound words** (e.g. `أَنَّمَآ`, `إِلَّا`, `مِمَّنِ`) can produce false positives and are not given explicit handling.
6. The draft schema (`schemaVersion: 1`) only supports paired `add` + `remove`; it has no `replace`, `keep`, or `exception` operation kind.

### 1.3 What the new normalization plan solves

One controlled feature that:
- reconciles and corrects the original 63 (including the 3 `replace` fixes);
- curates the 59 broader shift candidates into approved corrections or documented exceptions;
- recovers approved subsets of the 1,595 missing lemmas via reliable Buckwalter→Arabic mapping;
- resolves the 130 uncertain cases to approved / accepted-exception / blocker;
- handles multi-STEM/compound words with an explicit allow-list of `keep`/`exception` decisions;
- prevents future regressions with a fail-closed hard-check family and a normalization-applied proof in the import report.

### 1.4 Can implementation start now?

**No.** Implementation is gated behind Phase 0 curation. Code changes begin only after the **active normalization artifact** exists, contains **zero** `candidate` / `needs-review` entries, and every Phase 0 gate (0A–0E) is GREEN. See §4 and §10.4.

---

## 2. Scope Definition

**In scope — word-level lemma normalization for the Quran morphology import pipeline:**

- previous-word lemma shifts (original 63 + 59 broader candidates);
- wrong assignments where the defect location owns a different correct lemma (`replace`);
- missing word-level lemma recovery candidates backed by reliable Arabic mapping (`add`);
- uncertain / manual-review resolution (approve, accept-as-exception, or block);
- multi-STEM / compound exception handling (explicit `keep` / `exception` allow-list);
- prevention of future wrong word-level lemma imports (fail-closed hard checks + report proof).

**Explicitly out of scope:**

- changing Quran text (`quran_words`, Uthmani/imlaei);
- changing upstream QUL/Corpus source files under `resources/import-sources/quran-morphology/` (immutable);
- changing frontend behavior directly (Lemmas/Stems/Roots Explorer UI);
- using Corpus alone to invent Arabic lemma values without a reliable mapping or explicit curation;
- segment-level `lemma_id` direct correction (segment ids are re-derived by the assembler from the corrected head map; no separate segment overlay);
- the Lemma Details segment-matched type-distribution reader fix (separate work);
- Roots/Stems reader behavior, migrations (none expected), and any post-import SQL fix.

---

## 3. Normalization Artifact Design

### 3.1 Files

| Purpose | Path | Tracked | Active? |
| --- | --- | --- | --- |
| **Active artifact** (embedded resource, loaded by importer) | `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/Corrections/word-lemma-normalization.json` | yes (repo) | **yes** |
| **Curation draft / staging** (may contain `candidate` / `needs-review`) | `docs/feature-017-lexical-explorers-polish/word-lemma-normalization.draft.json` | yes (repo) | no |

Rationale: `resources/import-sources/quran-morphology/` is local/gitignored staged source and must stay byte-identical; the normalization artifact is reviewable correction *policy* bundled with importer code as an `EmbeddedResource`. This mirrors the existing embedded-correction precedent (`I3rabSeedLabelCorrections.cs`) and the integration design already validated in `word-level-lemma-alignment-correction-plan.md` §5.

The active artifact must never contain `candidate` or `needs-review`. In-progress curation lives only in the draft file.

### 3.2 Operation kinds

| `operationKind` | Meaning | `expectedCurrentLemmaArabic` | `correctedLemmaArabic` | Mutates map? |
| --- | --- | --- | --- | --- |
| `add` | No raw QUL lemma exists; a corrected lemma is added. | must be `null` (location absent in raw QUL map) | non-null Arabic | yes |
| `remove` | Raw QUL lemma exists but should become null (true rootless target with no own lemma). | non-null Arabic | must be `null` | yes |
| `replace` | Raw QUL lemma exists but should become a different correct lemma (location owns its own reliable lemma). | non-null Arabic | non-null Arabic, different from expected | yes |
| `keep` | Reviewed suspicious case deliberately left unchanged (e.g. legitimate QUL-vs-Corpus modeling difference). | non-null Arabic | must equal `expectedCurrentLemmaArabic` | no |
| `exception` | Reviewed case excluded from correction, with a reason; satisfies/suppresses a diagnostic or hard check for that location. | non-null or null (records observed value) | omitted / ignored | no |

Clarifications (authoritative):
- `correctedLemmaArabic: null` is valid **only** for `remove`.
- `replace` is **required** (not `remove`) when the defect location has its own reliable lemma (the 3 known cases + any 59-candidate landing on a content word).
- `add` is **required** for approved missing-lemma recovery.
- `keep` and `exception` are reviewed non-correction decisions; they never mutate the lemma map.
- `decisionStatus ∈ {approved, accepted-exception}` only. `candidate` and `needs-review` are forbidden in the active artifact and must fail validation (they belong only in the draft).

### 3.3 Entry shape

Flat, one entry per word location (not the draft's paired `add`+`remove` entry). Each shift is expressed as two entries linked by `relatedLocation`. This is what lets a single uniform model carry all five kinds and both directions of a shift.

```json
{
  "schemaVersion": 2,
  "artifactId": "word-lemma-normalization",
  "sourcePackage": "resources/import-sources/quran-morphology",
  "sourceAudit": "docs/feature-017-lexical-explorers-polish/full-word-level-lemma-alignment-audit-report.md",
  "generatedFromReports": [
    "docs/feature-017-lexical-explorers-polish/full-word-level-lemma-alignment-audit-report.md",
    "docs/feature-017-lexical-explorers-polish/word-level-lemma-alignment-correction-curation-report.md"
  ],
  "entries": [
    {
      "id": "WLN-00001",
      "location": "28:50:10",
      "operationKind": "add",
      "expectedCurrentLemmaArabic": null,
      "correctedLemmaArabic": "أَضَلّ",
      "wordTextUthmani": "أَضَلُّ",
      "corpusLemmaBuckwalter": ">aDal~",
      "corpusRootBuckwalter": "Dll",
      "corpusPos": "N",
      "currentLocationCorpusLemmaBuckwalters": [">aDal~"],
      "arabicMappingEvidence": ">aDal~ -> أَضَلّ (reliable; unique/dominant mapping)",
      "decisionStatus": "approved",
      "confidence": "high",
      "problemClass": "shift-63",
      "relatedLocation": "28:50:11",
      "isMultiStem": false,
      "reason": "Content-word lemma was shifted onto the following particle; recover it on the content word.",
      "sourceReportRef": "full-...-audit-report.md §6 (WLA-53708), §8"
    },
    {
      "id": "WLN-00002",
      "location": "28:50:11",
      "operationKind": "replace",
      "expectedCurrentLemmaArabic": "أَضَلّ",
      "correctedLemmaArabic": "مِن",
      "wordTextUthmani": "مِمَّنِ",
      "corpusLemmaBuckwalter": "min",
      "corpusRootBuckwalter": null,
      "corpusPos": "P",
      "currentLocationCorpusLemmaBuckwalters": ["min", "man"],
      "arabicMappingEvidence": "man->مِن (870/870, 100%); min->مِن (3103/3225, 96.2%)",
      "decisionStatus": "approved",
      "confidence": "high",
      "problemClass": "shift-63-replace",
      "relatedLocation": "28:50:10",
      "isMultiStem": true,
      "reason": "Remove location owns its own reliable lemma (مِن); replace, not remove-to-null.",
      "sourceReportRef": "full-...-audit-report.md §7, §10"
    }
  ]
}
```

Per-entry fields (maps to the task requirement):
- `id` — stable, unique (`WLN-#####`).
- `location` — Quran word location key, matches `qul/word-lemma.json` key and `quran_words.location`.
- `operationKind` — one of §3.2.
- `expectedCurrentLemmaArabic` — raw expected value at `location` in `qul/word-lemma.json` (`null` ⇒ must be absent).
- `correctedLemmaArabic` — value to write into the in-memory map.
- `wordTextUthmani` — display text (review aid).
- `corpusLemmaBuckwalter`, `corpusRootBuckwalter`, `corpusPos` — Corpus evidence at `location`.
- `currentLocationCorpusLemmaBuckwalters` — all Corpus segment lemma Buckwalters at `location` (guards remove vs replace).
- `arabicMappingEvidence` — Buckwalter→Arabic mapping justification (reliability statement).
- `decisionStatus` — `approved` | `accepted-exception`.
- `confidence` — `high` | `medium` (medium requires explicit curation note).
- `problemClass` — `shift-63` | `shift-63-replace` | `shift-59` | `missing-recovery` | `uncertain` | `multi-stem`.
- `relatedLocation` — paired location for a shift (target↔remove).
- `isMultiStem` — true if the word has multiple Corpus STEM segments.
- `reason` — human reason.
- `sourceReportRef` — originating report + section/row.

---

## 4. Curation Phases (Phase 0 — all are GO/NO-GO gates, before any code)

Curation produces the draft, then promotes only resolved entries into the active artifact. **No active entry may be `candidate`/`needs-review`.** Each phase ends with an explicit gate.

### Phase 0A — Reconcile the existing 63

- [ ] Import the 63 curated rows from `word-level-lemma-alignment-correction-curation-report.md` / `...corrections.draft.json` into the new flat schema: each shift becomes one `add` (target) + one `remove`/`replace` (defect location), linked by `relatedLocation`.
- [ ] Preserve confirmed target `add` operations (60 + 3 = 63 adds at target locations).
- [ ] **Convert the three remove locations to `replace`** (this is the core fix vs. the draft):
  - `3:33:7`: expected `ءَال` → corrected `إِبْرَاهِيم` (Corpus `<iboraAhiym`; mapping `<iboraAhiym→إِبْرَاهِيم` 55/56, 98.2%).
  - `21:51:3`: expected `آتَى` → corrected `إِبْرَاهِيم` (same mapping).
  - `28:50:11`: expected `أَضَلّ` → corrected `مِن` (`man→مِن` 870/870; `min→مِن` 3103/3225, 96.2%).
- [ ] Keep `remove`-to-null only where the defect location has **no** own lemma (the remaining 60 rows; `currentLocationCorpusLemmaBuckwalters` empty or non-content).
- [ ] Validate: no duplicate `id`, no duplicate `location` across all entries, every `expectedCurrentLemmaArabic` matches raw QUL.

**Gate 0A:** 63 reconciled; exactly 3 `replace`; 60 `remove`; 63 `add`; zero duplicates; the 3 replace spot-checks correct.

### Phase 0B — Curate the 59 new previous-word candidates

For each of the 59 rows (full audit §11), produce one decision:

- [ ] **approved add** at the target/previous content location (when its own Arabic candidate is reliable) **plus**:
  - **approved replace** at the defect location when it owns a reliable lemma (e.g. `2:126:23 قَلِيلًۭا` carries `مَّتَّعْ` but owns `قَلِيل` → replace `مَّتَّعْ`→`قَلِيل`; `3:116:15 ٱلنَّارِ` carries `أَصْحَٰب` but owns `نَار` → replace);
  - **approved remove**-to-null at the defect location only when it has no own lemma (own Arabic candidate `-`, e.g. `3:49:13 لَكُم`, `28:79:12 لَنَا`, `41:15:19 هُوَ`).
- [ ] **accepted exception** when the heuristic is explained by legitimate QUL-vs-Corpus modeling (phrase-head choices).
- [ ] **manual blocker** (stays in draft as `candidate`/`needs-review`) when evidence is insufficient — this blocks promotion of that row only, but a remaining blocker keeps Gate 0B RED.

Required evidence per decision: raw QUL lemma at current & previous location; current/previous word text; current & previous Corpus lemma/root/POS Buckwalter; reliable Arabic mapping for any `add`/`replace`; multi-STEM safety check (`isMultiStem`).

**Gate 0B:** every one of the 59 is `approved` or `accepted-exception`; zero `candidate`/`needs-review`; all `add`/`replace` have reliable mapping evidence; no defect location that owns a reliable lemma was modeled as `remove`.

### Phase 0C — Curate the 1,595 missing lemma recovery candidates

Do **not** blanket-defer these. Curate in batches:

- [ ] Group the 1,595 by `(corpusLemmaBuckwalter, recoveredArabicCandidate)`.
- [ ] Keep only groups whose Buckwalter→Arabic mapping is **reliable** (the audit's threshold: unique, or ≥5 examples and ≥80% share). The 4,797 reliable mappings are the allow-list; the 9 ambiguous Buckwalters are excluded.
- [ ] Exclude ambiguous mappings, compound/multi-STEM words (`isMultiStem = true`) unless individually approved, and any location whose own Corpus segments contradict the candidate.
- [ ] Prioritize high-confidence, high-frequency lemmas first (`كَانَ`, `ءَامَنَ`, `نَفْس`, `شَىْء`, `أَحَد`, `قَلِيل`, …) for early batches.
- [ ] Classify each candidate as: **approved `add`** (reliable, unambiguous, single-STEM or approved), **valid null** (no entry — Corpus evidence does not require recovery), or **blocker** (stays in draft).
- [ ] Record batch provenance and counts in the curation report.

Batching is allowed, but **the active artifact must only contain resolved (`add` or `accepted-exception`) entries** for the candidates that are promoted. Implementation may not start while any promoted candidate is unresolved.

**Gate 0C:** every promoted missing-recovery entry is `approved add` (reliable mapping, single-STEM or explicitly approved) or `accepted-exception`; zero ambiguous mappings promoted; zero `candidate`/`needs-review` in active artifact; remaining non-promoted candidates are explicitly logged as "valid null / out of this batch" with rationale.

### Phase 0D — Resolve the 130 uncertain / manual-review cases

For each uncertain case (full audit category H = 130; plus the 46 missing-with-Corpus-but-no-reliable-mapping):

- [ ] **approve** a correction (only with reliable evidence), or
- [ ] mark **accepted-exception** with reason (documented modeling/normalization difference), or
- [ ] keep as **blocker** (draft only).

**Gate 0D:** zero blockers in the active artifact; every uncertain case is `approved`, `accepted-exception`, or quarantined in the draft (not promoted). Ambiguous Arabic mappings must never become automatic `add`/`replace`.

### Phase 0E — Multi-STEM / compound review

- [ ] Produce an explicit allow-list for multi-STEM / compound words. Known set (full audit §10, audit §6/§9):
  - `أَنَّمَآ` → QUL `إِنّ`, Corpus `>an~ + maA` (`8:28:2`, `11:14:5`, `18:110:8`, `21:108:5`, `38:70:5`, `41:6:8`) → **keep** / **exception** (legitimate divergence).
  - `إِلَّا` → QUL `إِلَّا`, Corpus `<in + laA` (`8:73:6`) → **keep** / **exception**.
  - `مِمَّنِ` (`28:50:11`) → **replace** `أَضَلّ`→`مِن` (already in 0A; the one true defect in the multi-STEM set).
  - Other compound particles surfaced during 0B/0C (`إِنَّمَا`, `مِمَّا`, `عَمَّا`, `مِمَّن`) → review individually; default to **keep**/**exception** unless a clear single-word defect is proven.
- [ ] For each multi-STEM word, mark `isMultiStem: true` and choose exactly one of corrected / kept / exception with reason.

**Gate 0E:** every multi-STEM/compound word in the affected set has an explicit decision; none was auto-corrected by a one-STEM heuristic; the 7 legitimate compound divergences are `keep`/`exception`, not `remove`/`replace`.

### Phase 0 exit

- [ ] Promote all resolved entries from the draft into `word-lemma-normalization.json`.
- [ ] Re-run the artifact self-validator (a small script/test) over the active artifact: zero `candidate`/`needs-review`, zero duplicate ids, zero duplicate locations, every `expectedCurrentLemmaArabic` matches raw QUL, every `add`/`replace` Arabic resolves under a reliable mapping.
- [ ] Update `word-level-lemma-alignment-correction-curation-report.md` (or a new full-normalization curation report) with final counts per problem class and per operation kind.

**MASTER GATE (Phase 0 → implementation):** Active artifact final; all of Gate 0A–0E GREEN; zero active blockers. Only then start §5.

---

## 5. Importer Implementation Plan

> Start only after the MASTER GATE is GREEN.

### 5.1 File structure

| Area | File | Change |
| --- | --- | --- |
| Active artifact | `.../MorphologyImporting/Corrections/word-lemma-normalization.json` | **Create** (from Phase 0) |
| Artifact data types | `.../MorphologyImporting/Corrections/WordLemmaNormalizationModels.cs` | **Create** — parse/apply records |
| Reader / applicator | `.../MorphologyImporting/Corrections/WordLemmaNormalizationReader.cs` | **Create** — load embedded JSON, hash, validate schema, apply, summarize |
| Source loader | `.../MorphologyImporting/MorphologyImportSource.cs` | **Modify** lines 54–59 — apply artifact between lemma read and `Assemble` |
| Source data DTO | `Application.Abstractions/.../MorphologySourceData.cs` | **Modify** — add `CorrectionSummary` |
| Correction summary DTO | `Application.Abstractions/.../MorphologyCorrectionSummary.cs` | **Create** — counts, applied ops, accepted exceptions, spot checks |
| Invariants | `Application.Abstractions/.../MorphologyInvariants.cs` | **Modify** — add new check-id constants |
| Validation SQL | `.../MorphologyImporting/MorphologySql.cs` | **Modify** — strict-shift + missing-recovery detection SQL |
| Validation runner | `.../MorphologyImporting/MorphologyValidationRunner.cs` | **Modify** — register new hard checks |
| Bulk writer | `.../MorphologyImporting/EfBulkMorphologyWriter.cs` | **Modify** — pass summary into result; ensure new checks run pre-commit |
| Report builder | `.../MorphologyImporting/MorphologyImportReportBuilder.cs` | **Modify** — emit correction section |
| Report writer | `.../MorphologyImporting/MarkdownJsonMorphologyReportWriter.cs` | **Modify** — render JSON + Markdown |
| DI | `.../DependencyInjection/MorphologyImportDependencyInjection.cs` | **Modify** — register reader |
| Test DI | `Backend/tests/.../MorphologyTestServiceCollectionExtensions.cs` | **Modify** — register reader |

The `MorphologyAssembler`, `MorphologyBulkCopier`, `JsonQulReaders`, and `JsonAlignedCorpusReader` need **no** change — the assembler receives an already-corrected `lemmas` map and derives `quran_word_morphology.lemma_id`, `quran_lemmas`, links, and single-STEM segment inheritance from it.

### 5.2 Behavior

After curation, the importer must:
- load raw `qul/word-lemma.json` (unchanged reader);
- load the embedded `word-lemma-normalization.json`;
- validate artifact schema, version, ids, locations, statuses (§6);
- compute and record the artifact SHA-256 and the raw QUL lemma SHA-256;
- validate each operation's `expectedCurrentLemmaArabic` against the raw QUL map;
- apply `add`, `remove`, `replace` to an in-memory copy of the map;
- treat `keep` and `exception` as reviewed non-mutating decisions (validated, recorded, not applied);
- produce the corrected in-memory lemma map and pass it to `MorphologyAssembler.Assemble(...)`;
- expose a `MorphologyCorrectionSummary` on `MorphologySourceData` for validation + reporting.

### 5.3 Integration point (exact)

`MorphologyImportSource.LoadAsync` today (verbatim, lines 54–59):

```csharp
var lemmas = await lemmaReader.ReadAsync(
    GetManifestPath(manifest, "qul/word-lemma.json"), ct);
var stems = await stemReader.ReadAsync(
    GetManifestPath(manifest, "qul/word-stem-corrected-arabic.json"), ct);

return assembler.Assemble(corpusWords, readableWordIdsByLocation, roots, lemmas, stems);
```

Target:

```csharp
var lemmas = await lemmaReader.ReadAsync(
    GetManifestPath(manifest, "qul/word-lemma.json"), ct);
var stems = await stemReader.ReadAsync(
    GetManifestPath(manifest, "qul/word-stem-corrected-arabic.json"), ct);

// Normalize word-level lemmas before assembly. Fails closed on any invalid artifact/operation.
var normalized = normalizationReader.Apply(lemmas, corpusWords, readableWordIdsByLocation);

var source = assembler.Assemble(
    corpusWords, readableWordIdsByLocation, roots, normalized.CorrectedLemmas, stems);

return source with { CorrectionSummary = normalized.Summary };
```

`normalizationReader` is the new `WordLemmaNormalizationReader`, constructor-injected into `MorphologyImportSource`.

### 5.4 Task list (TDD, bite-sized)

**Task 1 — Artifact models + reader skeleton**
- [ ] Write failing test `WordLemmaNormalizationReaderTests.Parses_valid_artifact` (valid embedded JSON → entries parsed, hash non-empty).
- [ ] Run it; expect FAIL (type not defined).
- [ ] Add `WordLemmaNormalizationModels.cs` (records: artifact, entry, enum operationKind/decisionStatus) and `WordLemmaNormalizationReader.Load()` that reads the embedded resource and computes SHA-256.
- [ ] Run; expect PASS.
- [ ] Commit.

**Task 2 — Schema/status validation (fail closed)**
- [ ] Write failing tests: unsupported `schemaVersion`, duplicate `id`, duplicate `location`, `candidate`/`needs-review` present, `add` with non-null expected, `remove` with non-null corrected, `replace` with equal/empty corrected, `keep` with corrected≠expected, missing evidence/reason → each throws a specific validation failure.
- [ ] Run; expect FAIL.
- [ ] Implement `Validate()` in the reader covering each rule.
- [ ] Run; expect PASS. Commit.

**Task 3 — Apply to in-memory map**
- [ ] Write failing tests for `Apply(...)`: `add` inserts; `remove` deletes; `replace` swaps; `keep`/`exception` leave map unchanged; `expectedCurrentLemmaArabic` mismatch vs raw map throws; operation location absent from readable words throws.
- [ ] Run; expect FAIL.
- [ ] Implement `Apply(rawLemmas, corpusWords, readableWordIds)` returning `(CorrectedLemmas, Summary)`; never mutate the input map.
- [ ] Run; expect PASS. Commit.

**Task 4 — Wire into `MorphologyImportSource` + DI**
- [ ] Add `MorphologyCorrectionSummary` to `MorphologySourceData` (nullable for unrelated paths, populated here).
- [ ] Inject `WordLemmaNormalizationReader`; apply at §5.3 point; register in both DI extensions.
- [ ] Run existing morphology source/assembler tests; expect PASS (assembler signature unchanged). Commit.

**Task 5 — Hard checks + SQL** (see §6) — TDD against synthetic shifted/missing source.

**Task 6 — Report** (see §7) — TDD that JSON + Markdown carry all required fields and spot checks.

---

## 6. Validation and Fail-Closed Checks

### 6.1 Pre-assembly artifact validation (in `WordLemmaNormalizationReader`, before `Assemble`, before any DB write)

Fail the import when:
- artifact JSON is missing, malformed, or has an unsupported `schemaVersion`;
- active artifact contains any `candidate` / `needs-review` entry;
- duplicate `id` or duplicate `location` exists;
- a correction location conflicts (two operations target the same location);
- `expectedCurrentLemmaArabic` does not match the raw QUL source at that location;
- `add` has non-null expected; `remove` has non-null corrected; `replace` corrected equals/blank; `keep` corrected ≠ expected;
- `remove` is used where own-lemma evidence requires `replace` (i.e. `currentLocationCorpusLemmaBuckwalters` resolves to a reliable content lemma);
- `replace`/`add` corrected Arabic cannot resolve to a known lemma dimension under a reliable mapping;
- an operation points to a location absent from readable `quran_words`;
- an `accepted-exception` lacks location / evidence / reason;
- the artifact hash cannot be computed.

### 6.2 Post-copy hard checks (in `MorphologyValidationRunner.RunAllHardChecksAsync`, inside the import transaction, before commit)

Add a `MorphologyCheckResult` per check (existing pattern; accepted-exception filtering done in C# over `source.CorrectionSummary`, mirroring the existing `CountIssues(source, checkId)` precedent):

| Check id (constant in `MorphologyInvariants`) | Fails when |
| --- | --- |
| `MORPH-WORD-LEMMA-NORMALIZATION-APPLIED` | any approved operation was not applied (DB state ≠ corrected expectation); applied-count ≠ approved-count. |
| `MORPH-WORD-LEMMA-SHIFT-CLEAN` | any unapproved strict previous-word shift row remains (strict heuristic adapted to persisted tables; **not** `segment.lemma_id = m.lemma_id`, which hides 62/63), excluding `accepted-exception` locations. |
| `MORPH-WORD-LEMMA-REPLACE-VALID` | a `replace` target's persisted lemma ≠ its corrected Arabic, or the 3 known replace spot-checks are wrong, or a `replace` lacked reliable evidence. |
| `MORPH-WORD-LEMMA-MISSING-RECOVERY-CLEAN` | an approved missing-recovery `add` is not present in `quran_word_morphology.lemma_id` after import. |
| `MORPH-WORD-LEMMA-UNCERTAIN-ZERO` | the active artifact contains any `candidate`/`needs-review` (defense-in-depth; should already be caught pre-assembly). |
| `MORPH-WORD-LEMMA-SOURCE-UNCHANGED` | upstream QUL/Corpus files mutated — reuse existing `MORPH-SOURCE-UNCHANGED` (`CheckSourceUnchanged`); add an alias/assertion that the normalization artifact did **not** enter the source-package manifest. |

Multi-STEM safety: the strict-shift SQL must not flag the 7 legitimate compound divergences; they are covered as `accepted-exception`/`keep`. A multi-STEM word must never be auto-corrected by a one-STEM heuristic — enforced at curation (Phase 0E) and asserted by `MORPH-WORD-LEMMA-REPLACE-VALID` evidence requirements.

Any failed hard check rolls back the transaction (existing `EfBulkMorphologyWriter` behavior).

---

## 7. Report Requirements

Extend `morphology-import-report.json` + `.md` with a normalization section:

- normalization artifact path / embedded logical resource name;
- artifact SHA-256;
- raw QUL `word-lemma.json` SHA-256;
- counts by operation kind: `add`, `remove`, `replace`, `keep`, `exception`;
- `candidate`/`needs-review` count — **expected 0**;
- original-63 summary (60 remove + 3 replace + 63 add reconciled);
- 59-candidate summary (approved add/replace/remove + accepted-exception breakdown);
- 1,595 missing-recovery summary (promoted adds, valid-null, batch counts);
- uncertain / manual-review resolution summary (approved / accepted-exception);
- multi-STEM exception summary (keep/exception allow-list);
- failed / skipped operation count — **expected 0**;
- remaining unapproved previous-word shift count — **expected 0**;
- remaining unresolved missing-recovery count — **expected 0, or explicitly explained as accepted exception**;
- result of each new hard check;
- statement that upstream source files were not mutated.

Required spot checks (after correction):

| Location | Expected |
| --- | --- |
| `3:33:7` | word-level lemma `إِبْرَاهِيم` (replace from `ءَال`) |
| `21:51:3` | word-level lemma `إِبْرَاهِيم` (replace from `آتَى`) |
| `28:50:10` | word-level lemma `أَضَلّ` (add); Corpus `>aDal~`/root `Dll` |
| `28:50:11` | word-level lemma `مِن` (replace from `أَضَلّ`); Corpus segments remain `min`/`man` |
| one 59-candidate example | e.g. `3:116:15 ٱلنَّارِ` → `نَار` (replace); `3:116:14` → `أَصْحَٰب` (add) |
| one 1,595 missing-recovery example | e.g. `2:10:11 كَانُوا۟` → `كَانَ` (add) |
| one multi-STEM accepted exception | e.g. `8:28:2 أَنَّمَآ` → kept `إِنّ`, recorded as exception |

---

## 8. Tests

Backend tests (`Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/`), TDD, real importer boundary + real DTOs, source-safe synthetic Quran data, Testcontainers where persistence correctness matters:

| Test | Asserts |
| --- | --- |
| `add` operation | absent raw lemma → present corrected lemma id after import |
| `remove`-to-null operation | present raw lemma → null after import |
| `replace` operation | present raw lemma → different corrected lemma id |
| 3 known replace examples | `3:33:7`→`إِبْرَاهِيم`, `21:51:3`→`إِبْرَاهِيم`, `28:50:11`→`مِن` |
| 59-candidate approved shape | one curated 59-row (add+replace pair) applies and persists |
| 1,595 missing-recovery approved `add` shape | one reliable-mapping add applies and persists |
| `candidate`/`needs-review` fails closed | active artifact with pending status throws pre-assembly |
| uncertain unresolved fails closed | blocker entry promoted to active → fails |
| accepted exception non-mutating | `keep`/`exception` change no lemma rows |
| ambiguous mapping blocks add/replace | corrected Arabic without reliable mapping → fails |
| duplicate id fails | duplicate `id` rejected |
| duplicate location conflict fails | two ops same `location` rejected |
| expected-current mismatch fails | `expectedCurrentLemmaArabic` ≠ raw QUL → fails |
| source-unchanged guard | QUL/Corpus byte-identical before/after; artifact absent from source manifest |
| `MORPH-WORD-LEMMA-SHIFT-CLEAN` catches remaining shift | synthetic shifted row without correction → check fails, rollback |
| `MORPH-WORD-LEMMA-MISSING-RECOVERY-CLEAN` catches unapplied add | approved add not present → check fails |
| `MORPH-WORD-LEMMA-NORMALIZATION-APPLIED` | applied-count ≠ approved-count → fails |
| multi-STEM not auto-corrected | compound divergence row is not flagged/corrected by one-STEM heuristic |
| report completeness | JSON + Markdown contain all §7 counts + spot checks |

Run the test-code self-check (`.claude/skills/test-guard/`): behavior over implementation, real boundaries, data-driven variants, real DTOs/entities, source-safe Quran data.

---

## 9. Local Acceptance Flow (after implementation; not during planning)

- [ ] `git branch --show-current` → confirm `017-lexical-explorers-polish`.
- [ ] Confirm DB target is local/dev only (inspect connection without recording credentials).
- [ ] Reset/drop **local** DB only: `cd Backend && ./scripts/reset-db --yes`.
- [ ] Apply migrations: `./scripts/update-db`.
- [ ] Run imports in order: **foundation → rebuild-words → morphology → i3rab**.
- [ ] Rerun the full word-level lemma alignment audit (strict-shift SQL + no-matching-segment SQL + missing-recovery scan) and the morphology import report.
- [ ] Verify:
  - 0 active blockers; 0 unresolved candidates in the artifact;
  - 0 unapproved previous-word shifts;
  - 0 failed approved operations;
  - 3 known replace spot-checks correct (`3:33:7`, `21:51:3`, `28:50:11`);
  - no next-word shift introduced (audit confirms 0 next-word pattern);
  - no source-file mutation;
  - i3rab still generates successfully.

---

## 10. Deliverables, Risks, Gates

### 10.1 Deliverables
- This plan: `docs/feature-017-lexical-explorers-polish/word-level-lemma-full-normalization-implementation-plan.md`.
- Curation draft: `docs/feature-017-lexical-explorers-polish/word-lemma-normalization.draft.json`.
- Active artifact: `Backend/infrastructure/.../Corrections/word-lemma-normalization.json`.
- Updated curation report with final per-class / per-kind counts.
- Importer code (§5), hard checks (§6), report (§7), tests (§8) — implemented only after the MASTER GATE.

### 10.2 Implementation dependencies
Phase 0 (curation) → active artifact final → Task 1–4 (reader+wire) → Task 5 (hard checks) → Task 6 (report) → §9 acceptance.

### 10.3 Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| `remove` applied where `replace` was needed (data loss) | §6.1 own-lemma guard + `MORPH-WORD-LEMMA-REPLACE-VALID` + 3 explicit spot-checks |
| Ambiguous Buckwalter→Arabic auto-recovery (false adds) | reliable-mapping threshold gate (4,797 allow-list; 9 ambiguous excluded); ambiguous-mapping test fails closed |
| Multi-STEM/compound false positives | Phase 0E allow-list; `keep`/`exception`; no one-STEM heuristic correction |
| 1,595 recovery scope creep / partial curation | batch curation with per-batch gates; only promoted+resolved entries enter active artifact |
| Hidden segment inheritance hides shifts (62/63) | strict-shift SQL avoids `segment.lemma_id = m.lemma_id` equality |
| Source mutation | upstream files immutable; `MORPH-SOURCE-UNCHANGED`; artifact kept out of source manifest |
| Curated decision drift over time | every operation carries evidence + `sourceReportRef`; report records artifact hash |

### 10.4 Go / No-Go gates
- **NO-GO for implementation** until: Gate 0A, 0B, 0C, 0D, 0E all GREEN **and** the active artifact passes the self-validator with zero `candidate`/`needs-review`.
- **NO-GO for commit/merge** until: all §8 tests pass and §9 acceptance shows 0 unapproved shifts, 0 failed operations, correct 3 replace spot-checks, and no source mutation.

---

## Appendix A — Self-Review (writing-plans checklist)

- **Spec coverage:** all 7 known problem classes mapped — shift-63 (0A), shift-59 (0B), 3 replace (0A/§6/§7), missing-1595 (0C), uncertain-130 (0D), multi-STEM (0E), prevention (§6 hard checks + §7 report). All 10 required plan sections present plus risks/gates.
- **Operation kinds:** add/remove/replace/keep/exception defined once (§3.2) and used consistently in §4, §6, §7, §8.
- **Type consistency:** artifact field names (`expectedCurrentLemmaArabic`, `correctedLemmaArabic`, `currentLocationCorpusLemmaBuckwalters`, `operationKind`, `decisionStatus`) identical across §3, §6, §8; check-id constants identical across §6 and §7.
- **Integration point** verified against `MorphologyImportSource.cs:54–59`; assembler signature unchanged.
- **No placeholders:** every gate has explicit pass criteria; spot-checks use concrete locations/lemmas from the audits.
