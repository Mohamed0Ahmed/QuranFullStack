# Quran Foundation Import — Source Readiness Report

**Scope:** Code-grounded inspection only. No source files were modified, no importer/migration/database
command was run. Findings are derived from the actual importer code, EF configurations, Domain entities,
and the Feature 002 contracts/specs.

**Inspected at:** 2026-06-08
**Branch:** `002-mushaf-words-foundation`
**Workspace root:** `/projects/Dashboard/App`

---

## 1. Verdict

> ## ✅ `READY`

The expected staging folder **exists and is fully populated** with all 7 required source files plus
`manifest.json` and `README.md`. The importer code, manifest contract, source-file contract, EF tables,
and Domain entities are **mutually consistent** — no `CODE_SPEC_MISMATCH` of substance.

**Additional critical finding:** the importer has **already been run successfully** against this exact
staging folder (report at `resources/report/`, run `2026-06-08 12:37:05Z`, `Verdict: pass-with-warnings`,
**`Persisted: True`**, `Forced: True`). The target database tables are therefore very likely **already
populated**. A plain re-run will be **refused** unless you pass `--force`. See §10 and Risks (§13).

### Why the premise in the request does not hold

The request assumed `resources/import-sources/quran-foundation` might be missing and that the live
`resources/` folder contains `audio, i3rab-quran, metadata, morphology, mushaf, mutashabihat, report,
tafsirs, translations, word-meanings, words`.

In **this repository**, the live `resources/` folder contains only:

```
resources/
  import-sources/quran-foundation/   ← the staging tree (present, complete)
  report/                            ← importer output (already written)
```

None of `audio, i3rab-quran, morphology, mutashabihat, tafsirs, translations, word-meanings, words,
metadata, mushaf` exist anywhere under the repo (searched, excluding `node_modules`). Those folders are
the **upstream/original data dump** described by `source-provenance.md`'s "Original source path" column —
they live outside this repo and are **gitignored** (`resources/` is in root `.gitignore`). The staging
set was already assembled from them. You are comparing the repo against an external source dump.

---

## 2. Expected source path

| Item | Value |
|---|---|
| Expected staging root | `resources/import-sources/quran-foundation` (absolute: `/projects/Dashboard/App/resources/import-sources/quran-foundation`) |
| How the importer receives it | **`--source <path>` — required.** `Program.cs` has **no built-in default**; omitting `--source` errors with `"--source is required."` |
| Conventional value (README/quickstart) | `--source ../resources/import-sources/quran-foundation` (run from `Backend/`) |
| Manifest the importer reads first | `<source-root>/manifest.json` |

> **Minor code/spec nuance (non-blocking):** `contracts/import-manifest.schema.md` says
> *"default `<source-root>` = `resources/import-sources/quran-foundation/`"*. The **code does not implement
> a default** — `--source` is mandatory. The "default" is documentation/convention only. This does not
> affect readiness because the conventional path is exactly what exists and what `quickstart.md` passes.

---

## 3. Existing source-path status

| Path | Status |
|---|---|
| `resources/import-sources/quran-foundation/` | ✅ EXISTS |
| `resources/import-sources/quran-foundation/manifest.json` | ✅ EXISTS (1,237 bytes, version `1`, 7 sources) |
| `resources/import-sources/quran-foundation/README.md` | ✅ EXISTS |
| `mushaf/qpc-v4.json` | ✅ EXISTS (~11.4 MB) |
| `mushaf/qpc-v4-pages-layout.json` | ✅ EXISTS (~1.94 MB) |
| `words/uthmani.json` | ✅ EXISTS (~8.87 MB) |
| `words/uthmani-simple.json` | ✅ EXISTS (~8.26 MB) |
| `words/imlaei-simple.json` | ✅ EXISTS (~8.25 MB) |
| `metadata/quran-metadata-surah-name.json` | ✅ EXISTS (~20 KB) |
| `metadata/quran-metadata-ayah.json` | ✅ EXISTS (~1.99 MB) |

No file is missing. (Record-/page-/line-count correctness is validated by the importer at runtime, not by
this inspection — but the prior successful run already confirmed every count; see §1 and §10.)

---

## 4. Required file list (from the actual code)

The required set is **enforced in code** by `ManifestReader.RequiredKeys`
(`Backend/infrastructure/.../Files/Quran/Import/ManifestReader.cs`). Exactly these **7 keys** must each
appear **once**, all `format: "json"`, each `relativePath` must resolve to an existing file:

| # | Manifest key | Required relative path | Reader |
|---|---|---|---|
| 1 | `qpc-glyph` | `mushaf/qpc-v4.json` | `JsonWordSourceReader` |
| 2 | `uthmani` | `words/uthmani.json` | `JsonWordSourceReader` |
| 3 | `uthmani-simple` | `words/uthmani-simple.json` | `JsonWordSourceReader` |
| 4 | `imlaei-simple` | `words/imlaei-simple.json` | `JsonWordSourceReader` |
| 5 | `layout` | `mushaf/qpc-v4-pages-layout.json` | `JsonLayoutSourceReader` |
| 6 | `surah-meta` | `metadata/quran-metadata-surah-name.json` | `JsonMetadataSourceReader.ReadSurahsAsync` |
| 7 | `ayah-meta` | `metadata/quran-metadata-ayah.json` | `JsonMetadataSourceReader.ReadAyahsAsync` |

Plus the orchestration files: `manifest.json` (required, read first) and `README.md` (documentation only,
not read by the importer). Fonts are **explicitly out of scope** and must not be present in the manifest.

`QuranImportSource.LoadAsync` resolves each reader's input strictly via the manifest key (it never
hardcodes filenames), so the manifest is the single source of truth for file locations.

---

## 5. Manifest requirements (`manifest.json`)

Enforced by `ManifestReader.ReadAsync` (read **before any data**; any violation = fail-fast, persist nothing):

- **`version`** — must equal `"1"` (ordinal). Anything else → `Unsupported manifest version`.
- **`sources`** — must contain **exactly 7** entries; keys must be the 7 above, **no unknown key, no
  duplicate, none missing**.
- **`format`** — each source must be `json` (case-insensitive).
- **`relativePath`** — non-empty; combined with source root, the file **must exist** or → `FileNotFoundException`.
- **Count fields** validated by re-reading each file:
  - non-`layout` keys: `expectedRecordCount` must equal the JSON object's top-level property count.
  - `layout` key: `expectedPageCount` must equal both top-level `pagesCount` **and** the number of page
    entries; `expectedLineCount` must equal the total of all per-page line arrays.
- **`sha256`** — **OPTIONAL.** Only validated **if present**. The current manifest omits it, so checksums
  are **not** enforced today. `source-provenance.md` confirms sha256 is optional but "if supplied later,
  importer validation must enforce it" — and the code does enforce it when present (`ValidateChecksum`).
- **`joinKey`** — documentation only (carried on the record; not used to gate).

**Current `manifest.json` is valid against all of the above** (version `1`, all 7 keys present once,
correct relative paths, declared counts 83668/83668/83668/83668, layout 604 pages / 9046 lines, 114, 6236).

---

## 6. File → resource → staging → entity/table mapping

Because the original upstream folders are gitignored and **not in this repo**, the "Original source path"
column is taken from `source-provenance.md` (the tracked provenance record) and is informational only —
it is **not** a path that exists locally. The importer only ever reads the **staging** path.

| Manifest key | Staging path (present) | Original source path (per provenance; not in repo, gitignored) | Imported into entity / EF table |
|---|---|---|---|
| `surah-meta` | `metadata/quran-metadata-surah-name.json` | `resources/metadata/surah-names/original/quran-metadata-surah-name.json` | `Surah` → **`quran_surahs`** |
| `ayah-meta` | `metadata/quran-metadata-ayah.json` | `resources/metadata/ayahs/original/quran-metadata-ayah.json` | `Ayah` → **`quran_ayahs`** (incl. `words_count_source` from metadata `words_count`) |
| `layout` | `mushaf/qpc-v4-pages-layout.json` | `resources/mushaf/qpc-v4-tajweed/layout/jsonData/qpc-v4-pages-layout.json` | `MushafPage` → **`quran_mushaf_pages`** **and** `MushafLine` → **`quran_mushaf_lines`** (first/last word-id ranges, line types) |
| `qpc-glyph` | `mushaf/qpc-v4.json` | `resources/mushaf/qpc-v4-tajweed/words/original/qpc-v4.json` | `QuranWord` → **`quran_words`** — populates `QpcGlyph`; also defines word identity (`id/surah/ayah/word/location`) |
| `uthmani` | `words/uthmani.json` | `resources/words/with-tashkeel/original/uthmani.json` | `QuranWord.TextUthmani` → **`quran_words`** |
| `uthmani-simple` | `words/uthmani-simple.json` | `resources/words/without-tashkeel/original/uthmani-simple.json` | `QuranWord.TextUthmaniSimple` → **`quran_words`** |
| `imlaei-simple` | `words/imlaei-simple.json` | `resources/words/without-tashkeel/original/imlaei-simple.json` | `QuranWord.TextImlaeiSimple` → **`quran_words`** |

**Entity-table-by-table summary (requested mapping):**

| Table | Entity | Driven by source key(s) | Notes |
|---|---|---|---|
| `quran_surahs` | `Surah` | `surah-meta` | 114 rows expected |
| `quran_ayahs` | `Ayah` | `ayah-meta` | 6,236 rows expected |
| `quran_mushaf_pages` | `MushafPage` | `layout` | 604 rows expected |
| `quran_mushaf_lines` | `MushafLine` | `layout` | 9,046 rows expected |
| `quran_words` | `QuranWord` | `qpc-glyph` + `uthmani` + `uthmani-simple` + `imlaei-simple` (joined on `location`), with page/line placement from `layout` | 83,668 rows; 6,236 ayah markers + 77,432 readable words |

The four word files MUST agree on `id/surah/ayah/word` per `location` (the `source-alignment` hard check =
0 mismatches). They differ only in their `text` field, which lands in the four distinct `quran_words`
columns above (`QpcGlyph`, `TextUthmani`, `TextUthmaniSimple`, `TextImlaeiSimple`). Table names confirmed
from EF `ToTable(...)` in `Persistence/Configurations/Quran/*Configuration.cs`; columns confirmed from
`Domain/Quran/Words/QuranWord.cs`.

---

## 7. Does the importer need files copied into the staging folder, or can it read the originals directly?

**It reads only the staging folder**, exclusively via `manifest.json` relative paths. It never reaches into
`resources/metadata/...`, `resources/mushaf/...`, or `resources/words/...`. The staging set is a
self-contained, read-only input tree. (Provenance: README states the staged files are "verbatim copies"
of the originals; "No bytes were modified.") Since the staging tree is already complete, **no copying is
required** for this run.

---

## 8. Source-provenance expectations (`source-provenance.md`)

The current resources **satisfy** the provenance contract:

- ✅ Staged tree matches the documented tree exactly (7 data files + `manifest.json` + `README.md`).
- ✅ No font files staged; manifest has no font entry (fonts are out of scope and must not be copied/validated).
- ✅ No derived/search-normalized text staged.
- ✅ `sha256` omitted (allowed — optional); if it is added later the importer will enforce it.
- ✅ Staged files are read-only import inputs; importer validates presence + counts before reading data.
- ✅ Provenance correctly explains why the large source files are untracked (`resources/` is gitignored),
  which is also why the upstream "Original source path" folders do not appear in the repo.

No provenance violation found.

---

## 9. Report output behavior

From `ImportQuranFoundationHandler.ResolveReportOutDir` + `MarkdownJsonImportReportWriter`:

- **`--report-out <dir>` given** → reports written there (`Directory.CreateDirectory` first).
- **`--report-out` omitted (default)** → `<source-root>/../../report`, i.e. the parent of `import-sources`.
  For source `resources/import-sources/quran-foundation`, the default resolves to **`resources/report`** —
  the same place `quickstart.md` passes explicitly.
- **Two files are always written** (on both pass and fail):
  - `quran-foundation-import-report.json`
  - `quran-foundation-import-report.md`
- The report is written **after** persistence on success, and **instead of** persistence on a hard-fail.

> Note: this readiness report is a **separate, manually-authored** document at
> `Backend/report/feature-002-quran-foundation/quran-foundation-import-source-readiness-report.md`. The importer does **not** write to
> `Backend/report/`; it writes to `resources/report/` (or `--report-out`). They will not collide.

---

## 10. Evidence the import already succeeded

`resources/report/quran-foundation-import-report.md` (run `2026-06-08 12:37:05Z`):

- `Verdict: pass-with-warnings`, `Persisted: True`, `Forced: True`
- All **16 hard checks pass**; totals exactly: Surahs 114, Ayahs 6236, Pages 604, Lines 9046, Words 83668,
  Ayah markers 6236, Readable words 77432.
- Only warning: `37:130` word-count differs (metadata 4 vs records 3) — the **expected** warning per
  `quickstart.md` "Done criteria"; word records are treated as canonical.

This matches the hardcoded expected counts in `ImportValidationExpectedCounts.cs` (114 / 6236 / 604 / 9046
/ 83668 / markers 6236 / readable 77432 / bismillah surahs 112). **Everything is internally consistent.**

---

## 11. Exact next steps to (re)run safely

The staging folder needs **no creation or copying** — it already exists and already validated. So the
"propose the exact folder tree to create" step is **N/A**; for reference, the required tree (already
present) is:

```
resources/import-sources/quran-foundation/
  manifest.json
  README.md
  mushaf/qpc-v4.json
  mushaf/qpc-v4-pages-layout.json
  words/uthmani.json
  words/uthmani-simple.json
  words/imlaei-simple.json
  metadata/quran-metadata-surah-name.json
  metadata/quran-metadata-ayah.json
```
(copy, not symlink — provenance treats them as verbatim read-only copies; checksums optional/not enforced.)

Before running, decide based on whether the DB tables are already populated (the prior report says they
are):

1. **Confirm DB state first** (read-only) — e.g. `SELECT count(*) FROM quran_words;`. If it returns 83668,
   the import is already done and re-running is unnecessary.
2. **If tables are non-empty and you do NOT pass `--force`** → the importer **refuses** and changes nothing
   (`ImportRefusalMessages.TablesNotEmpty`). Safe, but it will not import.
3. **If you intend to reload**, use `--force` (wipe-and-reload in one transaction; counts come out identical).
4. **Connection string** for the importer is `appsettings.json` → `ConnectionStrings:QuranDashboardDb`
   (currently `Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=postgres`).
   Ensure it points at the intended database before running.

---

## 12. Exact command to run (from `Backend/`, per quickstart)

First-time / empty database:

```bash
dotnet run --project tools/QuranDashboard.DataImporter -- \
  --source ../resources/import-sources/quran-foundation \
  --report-out ../resources/report
```

Reload an already-populated database (wipe-and-reload):

```bash
dotnet run --project tools/QuranDashboard.DataImporter -- \
  --source ../resources/import-sources/quran-foundation \
  --report-out ../resources/report \
  --force
```

Exit code `0` on `pass` / `pass-with-warnings`; non-zero on hard `fail`.

---

## 13. Risks / notes

- **Likely already imported (highest-impact).** A prior `Persisted: True` run exists. Without `--force`
  the importer will refuse; with `--force` it wipes and reloads. Verify DB state before deciding.
- **Premise mismatch resolved.** The folders you listed (`audio, i3rab-quran, morphology, …`) are the
  external upstream dump, not this repo's `resources/`. The repo's `resources/` only holds the staging
  tree and the report output — both gitignored. The staging folder you thought was missing **is present
  and complete**.
- **`--source` has no code default**, despite the manifest contract calling that path a "default". Always
  pass `--source` explicitly. (Minor doc-vs-code nuance; not blocking.)
- **Counts not re-verified by this inspection.** Determining record counts would require parsing ~50 MB of
  JSON; I relied on the manifest's declared counts and the prior successful run instead, to avoid heavy
  reads. The importer itself re-validates every count at runtime and fails fast on any mismatch.
- **Checksums optional.** No `sha256` in the manifest, so no integrity check runs today. If byte-integrity
  matters, add `sha256` per source (the code will then enforce it).
- **Quran data safety.** No Quranic text was opened, transcribed, or altered during this inspection; only
  structure, counts, and code were examined.
- **No mutations performed.** The only file created is this report (and its `Backend/report/` parent
  directory). No source file, migration, or database was touched; the importer was not run.
