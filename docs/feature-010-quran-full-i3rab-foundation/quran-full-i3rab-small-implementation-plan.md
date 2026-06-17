# Feature 010 — Quran Full I‘rab Foundation: Small Implementation Plan

**Type:** Backend-only data-foundation import plan (no Spec Kit). Planning document only — no
code, migrations, imports, or source edits were produced by this task.

**Verdict:** `READY_FOR_PHASED_IMPLEMENTATION`

**Inputs (authoritative):**
- Staged package: `App/resources/import-sources/quran-full-i3rab/` (verdict
  `PACKAGE_READY_FOR_SMALL_IMPLEMENTATION_PLAN`).
- Source inspection: `docs/feature-010-quran-full-i3rab-foundation/quran-full-i3rab-source-inspection-report.md`.
- **Reference implementation to clone:** the Tafsir feature (Feature 007), which already solves
  the identical leader/pointer/`ayah_keys` shape end-to-end. Mirror it; do not reinvent.

**Provenance banner (must propagate to schema rows, run reports, and completion report):**
`licenseStatus: unknown` · `provenanceStatus: unknown` · `usageScope: internal-only-until-cleared`.
Never claim these sources are cleared for public distribution.

---

## 0. Why this is a clone, not a new design

The Tafsir pipeline is a near-exact template. Map the new feature onto these existing files
(rename `Tafsir`→`FullI3rab`, table prefix `quran_tafsir_`→`quran_full_i3rab_`):

| Layer | Tafsir reference (copy & adapt) |
|---|---|
| Domain | `domain/.../Quran/Tafsirs/{TafsirSource,TafsirEntry,TafsirAyahEntry}.cs` |
| App.Abstractions | `application/.../Quran/Tafsirs/{ITafsirImportSource,ITafsirImportWriter,ITafsirImportReportBuilder,ITafsirReportWriter,TafsirImportConstants,TafsirInvariants,TafsirSourceData,TafsirImportReport,TafsirImportResult,Tafsir*Exception}.cs` |
| Application | `application/.../Quran/Tafsirs/ImportTafsirs/{ImportTafsirsCommand,ImportTafsirsHandler,ImportTafsirsResult,TafsirImportReportEmitter}.cs` |
| Infra/Files | `infrastructure/.../Files/Quran/Tafsirs/{JsonTafsirSourceReader,TafsirAssembler,TafsirImportSource,TafsirManifestReader,TafsirValidationChecks}.cs` |
| Infra/Config | `infrastructure/.../Persistence/Configurations/Quran/Tafsirs/Tafsir{Source,Entry,AyahEntry}Configuration.cs` |
| Infra/Repos | `infrastructure/.../Persistence/Repositories/Quran/Tafsirs/{EfBulkTafsirImportWriter,TafsirBulkCopier,TafsirCommandExecutor,TafsirImportReportBuilder,TafsirSql,TafsirValidationRunner}.cs` |
| Infra/Reports | `infrastructure/.../Reports/Quran/Tafsirs/MarkdownJsonTafsirReportWriter.cs` |
| CLI | `tools/QuranDashboard.DataImporter/Program.cs` → `RunImportTafsirsAsync` + `TryParseTafsirArguments` |
| Tests | `tests/.../Quran/Tafsirs/Tafsir*Tests.cs` + `TafsirImportTestFixture.cs` |

**Simplifications vs Tafsir** (full i3rab is smaller in scope):
- Exactly **4 Arabic sources**, no language dimension, no curation/exclusions. Drop the
  multi-language and excluded-source machinery; hardcode `language_code='ar'`, `direction='rtl'`.
- Replace `tafsir_kind` with i3rab-specific markup metadata (`markup_format`,
  `has_quran_quotation_markup`).
- Payload is **HTML** (`i3rab_html`) instead of plain tafsir text — preserved raw.

---

## 1. Scope

**In scope:** backend data foundation that imports the four full-i‘rab books from the staged
package into three new tables, with validation, an idempotent CLI verb, and machine-readable
run reports.

**Explicitly out of scope (do not build):**
- No UI; no API endpoints; no search/indexing.
- No render-time HTML sanitization implementation (render-boundary concern; deferred).
- No public-distribution / license-clearance workflow (only record the unknown status).
- No word-level or segment-level modeling; this does **not** touch or replace Feature 005
  (`quran_i3rab_rules`).
- No new Quran-text column; embedded quotations stay inside the i‘rab HTML.

---

## 2. Database model

Three tables mirroring `quran_tafsir_*`, deliberately named distinct from Feature 005's
`quran_i3rab_rules`:

### `quran_full_i3rab_sources` (dimension — exactly 4 rows)
Domain `FullI3rabSource`. Columns (snake_case):

| column | type | notes |
|---|---|---|
| `id` | int PK identity | |
| `source_key` | text, **unique** | `muyassar` / `jadwal` / `daas` / `darwish` |
| `display_name_ar`, `short_name_ar`, `display_name_en`, `short_name_en` | text | |
| `language_code` | text | `'ar'` (check) |
| `direction` | text | `'rtl'` (check `IN ('rtl','ltr')`) |
| `contributor_name_ar`, `contributor_name_en` | text null | unverified attribution |
| `resource_kind` | text | check `= 'full_i3rab'` |
| `markup_format` | text | check `= 'html'` |
| `has_quran_quotation_markup` | bool | true for muyassar/daas |
| `content_coverage_count` | smallint | check `= 6236` |
| `package_file` | text, **unique** | `sources/<file>.json` |
| `source_file_original` | text | upstream relative path |
| `sha256` | text | from manifest |
| `file_size_bytes` | bigint | from manifest |
| `license_status` | text | check `= 'unknown'` (current reality) |
| `provenance_status` | text | check `= 'unknown'` |
| `usage_scope` | text | check `= 'internal-only-until-cleared'` |
| `manifest_metadata` | jsonb null | observed tags/classes, shape counts |
| `imported_at_utc` | timestamptz | |

### `quran_full_i3rab_entries` (block text — one row per leader/standalone block)
Domain `FullI3rabEntry`. Stores each i‘rab HTML once.

| column | type | notes |
|---|---|---|
| `id` | bigint PK identity | |
| `source_id` | int FK→sources | |
| `source_entry_key` | text | leader verse_key; **unique** `(source_id, source_entry_key)` |
| `leader_ayah_id` | int FK→`quran_ayahs` | |
| `i3rab_html` | text | check `<> ''`; raw HTML preserved exactly |
| `covered_ayah_count` | smallint | check `>= 1` |
| `covered_ayah_keys` | jsonb | the `ayah_keys` list (or `[self]` when flat) |
| `source_shape` | text | check `IN ('grouped_leader','flat')` |
| `text_hash` | text | sha256 of `i3rab_html`; idempotency + dedup audit |

Indexes: unique `(source_id, source_entry_key)`; `(leader_ayah_id)`; `(source_id, leader_ayah_id)`.

### `quran_full_i3rab_ayah_entries` (junction — one row per source × ayah)
Domain `FullI3rabAyahEntry`. Resolves every ayah to its block.

| column | type | notes |
|---|---|---|
| `id` | bigint PK identity | |
| `source_id` | int FK→sources | |
| `ayah_id` | int FK→`quran_ayahs` | |
| `entry_id` | bigint FK→entries | |
| `verse_key` | text | |
| `source_value_kind` | text | check `IN ('leader','member_pointer','flat')` |
| `source_leader_verse_key` | text | leader the pointer/ayah_keys referenced |
| `is_group_leader` | bool | |
| `sort_order` | int | order within block |

Indexes: unique `(source_id, ayah_id)`; unique `(source_id, verse_key)`; `(ayah_id, source_id)`;
`(entry_id)`. FKs to sources, `quran_ayahs`, entries.

**Registration:** add three `DbSet`s + the three `IEntityTypeConfiguration`s to
`QuranDashboardDbContext`, then a single EF migration. Do **not** extend any existing importer.

---

## 3. Import approach

- **Read only** from `App/resources/import-sources/quran-full-i3rab/` (default source path;
  overridable by `--source`). Never read the upstream `resources/i3rab-quran/`.
- **Manifest first:** read `manifest.json`; verify it is the final manifest
  (`manifestType = quran-full-i3rab-import-source-package`), the source set is exactly the four
  expected files, and each file's **sha256 + byte size match** the manifest (hard fail on
  mismatch). Mirror `TafsirManifestReader`.
- **Per-file assembly** (mirror `TafsirAssembler`): for each ayah value:
  - `{"text"}` → one **entry** (`source_shape='flat'`) + one junction row (`value_kind='flat'`).
  - `{"text","ayah_keys"}` → one **entry** (`source_shape='grouped_leader'`,
    `covered_ayah_count=len(ayah_keys)`) + one junction row per covered ayah
    (`value_kind='leader'` for the head, `'member_pointer'` for the rest).
  - `"<leader_verse_key>"` → one junction row (`value_kind='member_pointer'`) pointing at the
    leader's entry; no new entry.
- **Preserve raw HTML exactly** in `i3rab_html` (no trimming, normalization, or tag stripping).
  Keep embedded Quran quotations (`qpc-hafs`/`hlt` spans, `﴿…﴾`) inside the HTML. **No separate
  Quran-text column.**
- **Resolve `ayah_id`** by joining `verse_key` → `quran_ayahs` (must already be imported).
- **Write** via the bulk copier path (mirror `EfBulkTafsirImportWriter` / `TafsirBulkCopier`)
  inside one transaction; populate `manifest_metadata` jsonb with shape counts + observed
  tags/classes; stamp `license_status/provenance_status/usage_scope` from the manifest.

---

## 4. Validation checks

**Pre-import (fail fast before any write):**
1. Package shape: `README.md`, `manifest.json`, `package-report.md`, `sources/` all present.
2. Manifest final and readable; `manifestType` correct.
3. Exact source file set = the four expected filenames (no missing, no extras).
4. Each file's sha256 + byte size match the manifest.
5. Each file: exactly **6,236** canonical verse keys.
6. Zero missing / extra / malformed keys (`^\d+:\d+$`, surahs 1–114, canonical counts).
7. Zero broken string pointers (every pointer → existing dict leader).
8. Zero `ayah_keys` member→leader mismatches.
9. Zero empty/blank texts.
10. Block partition: zero gaps, zero overlaps (blocks cover all 6,236 exactly once).
11. **HTML allowlist:** observed tags ⊆ `{div,p,span,b,h3}`, classes ⊆ `{ar,hlt,qpc-hafs}`.
    Out-of-allowlist markup → **warning** (severity `warning`, recorded, non-blocking) so a new
    benign tag does not hard-block; a future tightening can promote to `hard` if needed.

**Post-import (assert persisted state; mirror `TafsirValidationRunner`):**
- Exactly **4** source rows.
- Exactly **6,236** ayah-junction rows per source; `COUNT(DISTINCT verse_key)=6236` per source.
- `SUM(covered_ayah_count)` over entries per source `= 6236`.
- Every junction row references an entry with the **same `source_id`**.
- Every `ayah_id` / `leader_ayah_id` / junction `verse_key` resolves in `quran_ayahs`.
- No empty `i3rab_html`.

Each check emits `{id, severity(hard|warning|info), expected, observed, passed}`; overall
`verdict ∈ {pass, fail}` (reuse `TafsirImportConstants` pattern).

---

## 5. CLI

New verb in `tools/QuranDashboard.DataImporter/Program.cs` switch:

```
dotnet run --project Backend/tools/QuranDashboard.DataImporter -- import-full-i3rab \
  [--source <dir>] [--report-out <dir>] [--force]
```

- `--source` — package dir. Default `resources/import-sources/quran-full-i3rab/`.
- `--report-out` — report dir. Default `resources/report/quran-full-i3rab/`.
- `--force` — rebuild when data already present.
- **Idempotency / rerun refusal:** with data already present and no `--force`, **refuse**
  (verdict `fail`, non-zero exit, report written, no mutation) — mirror
  `TafsirRefusalForceTests`. With `--force`, truncate-and-rebuild the three tables in one
  transaction (mirror `TafsirForceRebuildTests`). Add `RunImportFullI3rabAsync` +
  `TryParseFullI3rabArguments` mirroring the Tafsir helpers.

---

## 6. Reports

Reuse the `MarkdownJsonTafsirReportWriter` pattern → `MarkdownJsonFullI3rabReportWriter`.

- **Machine run reports** (gitignored, like Tafsir): `resources/report/quran-full-i3rab/`
  - `full-i3rab-import-report.json`
  - `full-i3rab-import-report.md`
- **Human feature reports** (committed): `Backend/report/feature-010-quran-full-i3rab-foundation/`
  (implementation / real-run / validation / completion), per workspace report conventions.

**Report contents (key checks):** `runAtUtc`, `verdict`, `persisted`, `forced`, `sourcePath`;
totals (`sourceRows`, `entryRows`, `ayahMappingRows`, `distinctAyahs`); per-source summary
(`sourceKey`, `sha256`, `license`, `provenance`, shape counts, qpc-hafs flag); the full pre/post
checks table; warnings; errors.

**Mandatory provenance emission** — every report (JSON + Markdown) must surface, prominently:
`licenseStatus: unknown`, `provenanceStatus: unknown`, `usageScope: internal-only-until-cleared`,
and the line "not cleared for public distribution — internal use only until cleared."

---

## 7. Tests

Pragmatic, source-safe, real-infra where correctness matters. Mirror `TafsirImportTestFixture`
(PostgreSQL via Testcontainers, `Database.MigrateAsync()`, synthetic ayahs in **surah 900**,
synthetic i‘rab HTML — **no real Quran or i‘rab text**). Recommended minimal set:

1. **Schema shape** — three tables, columns, unique indexes, and check constraints exist
   (mirror `TafsirSchemaShapeTests`).
2. **Source reader / assembler** — synthetic grouped fixture covering all three value kinds
   (flat, grouped leader, member pointer) assembles correct entries + junction rows
   (mirror `TafsirAssemblerTests` / `TafsirSourceReaderTests`); data-driven over the variants.
3. **Validation failures** — data-driven: broken pointer, `ayah_keys` mismatch, empty text,
   coverage gap/overlap, wrong key count, sha256 mismatch each yield `fail` with the right
   check id and **no persistence** (mirror `TafsirValidationFailureTests`).
4. **Happy-path import** — small synthetic package imports; post-import counts hold; raw HTML
   round-trips byte-for-byte; embedded quotation markup preserved (mirror `TafsirImportTests`).
5. **Rerun refusal / force** — second run without `--force` refuses and mutates nothing; with
   `--force` rebuilds to identical counts (mirror `TafsirRefusalForceTests` /
   `TafsirForceRebuildTests`).
6. **Report emission** — JSON + Markdown written, shape correct, and the provenance/usage-scope
   warning is present (mirror `TafsirJsonReportShapeTests` / `TafsirMarkdownReportShapeTests`).

Avoid: tests for EF/framework guarantees, mocking real DTOs/entities, or per-book duplicated
tests where one data-driven test suffices.

---

## 8. Phasing (Cursor/Codex-sized)

**Phase 1 — Model / schema / contracts.** Three domain entities; three EF configurations (table
names, columns, checks, indexes, FKs); DbSets in `QuranDashboardDbContext`; one EF migration;
App.Abstractions interfaces + constants/invariants (clone Tafsir). Plus Phase-1 schema-shape
tests.
*Done when:* migration applies on a fresh test DB and schema-shape tests pass.

**Phase 2 — Source reader + assembler.** `FullI3rabManifestReader`, `JsonFullI3rabSourceReader`,
`FullI3rabAssembler`, `FullI3rabValidationChecks` (pre-import). Reader/assembler + validation
tests on synthetic fixtures.
*Done when:* assembler emits correct entries/junctions for all three value kinds; pre-import
checks pass/fail correctly; no DB needed for these unit tests.

**Phase 3 — Writer + CLI happy path.** Bulk writer/copier, command executor, `ImportFullI3rab`
command/handler/result, DI registration, `import-full-i3rab` verb (default paths). Happy-path
import test green.
*Done when:* synthetic package imports and post-import counts hold in the test DB.

**Phase 4 — Validation runner + report + rerun/force.** Post-import `FullI3rabValidationRunner`;
`MarkdownJsonFullI3rabReportWriter` + emitter (with mandatory provenance warning); rerun-refusal
and `--force` rebuild. Validation-failure, rerun/force, and report-shape tests green.
*Done when:* all §4 checks run, reports emit with the warning, refusal/force behave per convention.

**Phase 5 — Polish + real local import.** Clean-code + test self-checks; run the real local
import against `resources/import-sources/quran-full-i3rab/`; capture run reports; write the
completion report under `Backend/report/feature-010-quran-full-i3rab-foundation/`.
*Done when:* real import yields 4 sources, 6,236 mappings/source, verdict `pass`, reports archived.

---

## 9. Risks / non-goals

- **License/provenance NOT cleared.** `unknown`/`unknown`/`internal-only-until-cleared` must
  persist to rows and reports; no public distribution. Author attributions are unverified.
- **HTML is stored raw and is NOT sanitized here.** Sanitization is a later render-boundary
  concern; storing raw is deliberate and must be documented for whoever builds the read API/UI.
- **Heterogeneous markup across books** — handling/rendering must stay source-aware; the import
  only records the markup profile, it does not normalize it.
- **This is ayah-level full i‘rab, not a replacement for Feature 005** simple segment i‘rab
  (`quran_i3rab_rules`). Names and tables are intentionally separate; do not merge or migrate
  between them.
- **Depends on `quran_ayahs`** being fully imported (verse_key → ayah_id resolution); fail
  clearly if any verse_key is unresolved.

---

## Final verdict

**`READY_FOR_PHASED_IMPLEMENTATION`** — the staged package is validated, the data shape is a
direct clone of the proven Tafsir pipeline, the model/CLI/report/test conventions are
established in-repo, and the only open item (license/provenance clearance) is correctly carried
as an explicit, non-blocking `unknown` status for internal use.
