# Phase 0 Research: Quran Navigation Metadata Foundation

All choices below are **locked** — inherited from the companion planning report and the two
`/speckit-clarify` answers. There are **no open `NEEDS CLARIFICATION` items**. This document records each
decision, why it was chosen, and the alternatives rejected, so the implementing model never has to re-derive
them.

---

## D1 — Division representation: header tables + denormalized ayah columns

- **Decision**: Store each division type in its own header table (`quran_juzs`, `quran_hizbs`,
  `quran_rubs`) AND add denormalized `juz_number` / `hizb_number` / `rub_number` columns to `quran_ayahs`,
  populated by expanding `verse_mapping` at import time.
- **Rationale**: The consuming need is O(1) "which juz/hizb/rub is this ayah in?" for a future reader. A
  column on the ayah answers it with a single indexed lookup; the header table answers "give me juz N's
  bounds / verse count". Together they cover both navigation directions cheaply.
- **Alternatives considered**:
  - *Normalized child range table* (`quran_division_ayah_ranges`): more rows and a range-join for every
    ayah→division lookup; rejected for v1 as unnecessary complexity (the denormalized column is exact and
    simpler).
  - *Store `verse_mapping` JSON in a column*: not query-friendly, duplicates derivable data; rejected.

## D2 — Sajda representation: dedicated table

- **Decision**: A dedicated `quran_sajdas` table (`sajdah_number`, `ayah_id`, `verse_key`, `sajdah_type`).
  Do **not** add `is_sajda` / `sajda_type` columns to `quran_ayahs` in v1.
- **Rationale**: A table preserves `sajdah_number` ordering and the type faithfully, is the source-faithful
  shape (15 discrete records), and keeps `quran_ayahs` minimal. Only 15 rows; a boolean column would spread
  sajda semantics across 6,236 rows for 15 truthy ones.
- **Alternatives considered**: ayah boolean+type columns (rejected: 15/6236 sparsity, loses ordering); a
  shared "ayah marks" table (rejected: out of scope, no other marks in this feature).

## D3 — Link strategy: `verse_key`, never numeric-id alignment

- **Decision**: Resolve every `first_verse_key`, `last_verse_key`, and sajda `verse_key` to
  `quran_ayahs.verse_key` (unique). Store the resolved `*_ayah_id` foreign keys. Never assume the source
  record `id` equals `quran_ayahs.id`.
- **Rationale**: `verse_key` ("surah:ayah") is the stable, validated contract (the `VerseKey` value object
  already enforces its shape). The source ayah metadata `id` happens to run 1..6236 today, but that is not a
  guaranteed contract.
- **Alternatives considered**: trusting numeric `id` alignment (rejected: brittle, not a contract).

## D4 — `--source` points at the package root *(clarification 2026-06-16)*

- **Decision**: `--source` (and the default) is the **package root** directory that directly contains
  `manifest.json` and the `sources/` subfolder. The importer resolves `manifest.json` and each source file
  relative to that root.
- **Rationale**: The manifest is the validation anchor and lives at the root; pointing there keeps the
  default and any override symmetric and matches every other importer's `--source` convention
  (`ResolveDefaultXSourcePath()` → `<repo-root>/resources/import-sources/<name>`).
- **Alternatives considered**: pointing at `sources/` (manifest via `../manifest.json`) or at the
  `manifest.json` file directly — both rejected for asymmetry and convention drift.

## D5 — Stored division verse count = computed range count *(clarification 2026-06-16)*

- **Decision**: The persisted `verses_count` for each juz/hizb/rub is the count of ayahs **computed from its
  `verse_mapping` ranges**. The source `verses_count` field is informational; a non-blocking warning
  (`NAV-VERSE-COUNT-MATCH`) is raised, carrying the source value, when the two differ.
- **Rationale**: The stored count then always matches the ayahs the division actually covers and the
  coverage the DB enforces; the warning preserves provenance for human review. For this clean package the
  two always agree, so this only governs the corruption-guard path.
- **Alternatives considered**: store source value (rejected: could disagree with actual coverage); hard-fail
  on divergence (rejected: too strict for a non-blocking provenance signal; coverage checks already guard
  correctness).

## D6 — Hierarchy (hizb→juz, rub→hizb) is derived, not sourced

- **Decision**: The source hizb/rub files carry **no** parent number. Derive `juz_number` for each hizb and
  `hizb_number` for each rub from **range containment** (the parent whose ayah range fully contains the
  child's range), then validate exact one-parent containment (`NAV-HIERARCHY`).
- **Rationale**: The standard division structure is strictly nested (2 hizb per juz, 4 rub per hizb).
  Containment is unambiguous and verifiable; deriving avoids inventing data not present in the source.
- **Alternatives considered**: expecting a parent field in source (rejected: not present); hard-coding the
  2:1 / 4:1 arithmetic by number (rejected: derive-and-verify from actual ranges is safer than assuming).

## D7 — Separate importer following the established pipeline

- **Decision**: A new console verb `import-navigation-metadata` with its own
  reader → assembler → validator → EF bulk writer/transaction → report-writer pipeline, mirroring
  `import-translations` / `import-tafsirs` / `import-mutashabihat`. Do **not** extend or re-run the
  Feature 002 foundation importer.
- **Rationale**: Matches the codebase's proven importer shape (`IXImportSource`, `IXImportWriter`,
  `XInvariants`, `XValidationRunner`, `MarkdownJson…ReportWriter`), keeps the change isolated and reviewable,
  and reuses the host/DI wiring.
- **Alternatives considered**: bolting navigation onto the foundation importer (rejected: that importer is
  complete/locked and out of scope to touch).

## D8 — Re-run guard + scoped `--force`

- **Decision**: Refuse if any navigation target is already populated (any of the four tables non-empty, or
  any `quran_ayahs` nav column populated) unless `--force` is given. With `--force`, clear and reload **only**
  the four nav tables and reset/repopulate the three ayah nav columns — inside one transaction.
- **Rationale**: Mirrors `AnyTargetTableHasDataAsync` + `ExecuteAcceptedImportAsync` in the existing writers;
  prevents silent clobbering while keeping reloads safe and idempotent.
- **Alternatives considered**: always-overwrite (rejected: unsafe); fail-only-never-reload (rejected: not
  operable).

## D9 — Single transaction, hard-gated, source re-verified before commit

- **Decision**: All writes (4 header tables + the ayah `UPDATE`) commit in one transaction. Persist only if
  every hard check passes AND the source package still matches the manifest (sha256/size) at commit time AND
  both reports are written. Any hard-check failure → full rollback, nothing persisted.
- **Rationale**: All-or-nothing guarantees no partial/half-written navigation state; re-verifying the source
  before commit closes the "source changed mid-run" window (`NAV-SOURCE-UNCHANGED`), matching the
  translation importer's `sourceUnchangedCheck` callback.
- **Alternatives considered**: per-dataset commits (rejected: partial state risk); skip pre-commit source
  re-check (rejected: loses tamper detection).

## D10 — Reports: Markdown + JSON, audit-grade

- **Decision**: Emit both a JSON and a Markdown report per run via a `MarkdownJsonNavigationMetadataReportWriter`,
  with verdict, persisted/forced flags, resolved source path, per-dataset totals, ayah-coverage summary,
  per-check results, warnings/errors, and an explicit "no Quran ayah text read or stored" assertion. Default
  output dir `Backend/report/feature-009-quran-navigation-metadata-foundation/`.
- **Rationale**: Matches `Backend/report/feature-XXX/` convention and the audit gate (no accepted run without
  both reports).
- **Alternatives considered**: single-format report (rejected: machine + human both required by FR-023/024).

## D11 — Additive, EF-tooling-generated migration

- **Decision**: One additive migration adds the four tables and the three nullable `quran_ayahs` columns.
  Columns are nullable at the schema level (migration safety); completeness (all 6,236 non-null) is enforced
  by the importer/validator (`NAV-AYAH-COLUMNS-COMPLETE`), not by a NOT NULL constraint in v1.
- **Rationale**: Additive + nullable lets the migration apply to a populated DB without a backfill default;
  the import then fills every ayah. Follows Backend rule: EF-tooling only, generate on explicit request, no
  hand-written migration/snapshot edits, no `database update` without explicit request.
- **Alternatives considered**: NOT NULL with default (rejected: a fake default would violate "no invented
  data"); separate migrations per table (rejected: one cohesive additive migration is cleaner).

## D12 — Source package & manifest are authoritative

- **Decision**: Validate `packageType = "quran-navigation-metadata-import-source-package"`,
  `isFinalImportManifest = true`, the exact expected file set (juz/hizb/rub/sajda), and each file's
  `sha256` / `sizeBytes` / `recordCount` against `manifest.json` before importing.
- **Rationale**: Ties imported data to a verified, immutable package (the staged package's manifest already
  records these); folder location alone is insufficient.
- **Alternatives considered**: read raw files without manifest verification (rejected: no provenance/tamper
  guarantee).

---

## Resolved unknowns summary

| Unknown | Resolution |
|---|---|
| Division storage shape | Header tables + denormalized ayah columns (D1) |
| Sajda shape | Dedicated `quran_sajdas` table (D2) |
| Ayah linking | `verse_key` resolution to `quran_ayahs`, store `*_ayah_id` (D3) |
| `--source` meaning | Package root containing `manifest.json` + `sources/` (D4) |
| Verse-count authority | Computed range count stored; source value → warning (D5) |
| Hierarchy source | Derived by range containment + validated (D6) |
| Importer placement | New `import-navigation-metadata` verb, established pipeline (D7) |
| Re-run / force | Guarded; `--force` reloads navigation-owned data only (D8) |
| Atomicity / safety | Single transaction, hard-gated, source re-verified pre-commit (D9) |
| Reporting | Markdown + JSON audit reports (D10) |
| Migration | Additive, nullable columns, EF-tooling-generated (D11) |
| Provenance | Manifest sha256/size/count authoritative (D12) |
