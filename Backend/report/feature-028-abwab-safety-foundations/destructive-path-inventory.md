# Destructive-path inventory — Quran import safety (028 US2)

**Feature**: `028-abwab-safety-foundations` · **Story**: US2 (Quran import safety & destructive-path
lockdown) · **Task**: T018 · **Date**: 2026-07-22

This inventory enumerates **every** destructive / force / importer path that can delete or truncate
Quran (or any) rows, and records the fail-closed decision applied to each (T019/T020). It is the
analysis basis for the lockdown. Because US2 runs **before** the kernel/schema (US3) and the first
Abwab→Quran FK is still prohibited (FR-009), no Abwab table/FK exists yet; the guarantees here are
therefore **structural** — they hold now and stay fail-closed the instant the first Abwab→Quran FK is
introduced.

## 1. Enumerated destructive statements (importer/pipeline write paths)

All eight live in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/`
and every one is reached **only** on the `--force` branch of its writer (a non-force re-import refuses
against non-empty tables instead). Each targets `quran_*` tables only.

| # | Path (file) | Statement | `--force`-gated | Cascade | Lockdown decision |
|---|-------------|-----------|:---:|:---:|-------------------|
| 1 | `Foundation/EfBulkQuranImportWriter.cs` (`TruncateTargetTablesSql`) | `TRUNCATE quran_words, quran_mushaf_lines, quran_mushaf_pages, quran_ayahs, quran_surahs RESTART IDENTITY CASCADE` | yes | yes | Routed through `QuranImportDestructiveGuard.ExecuteDestructiveAsync` |
| 2 | `Words/MorphologyImporting/MorphologySql.cs` (`TruncateMorphologyTables`) | `TRUNCATE` 7 morphology tables `RESTART IDENTITY CASCADE` | yes | yes | Routed through the guard |
| 3 | `Tafsirs/TafsirSql.cs` (`TruncateTafsirTables`) | `TRUNCATE` 3 tafsir tables `RESTART IDENTITY CASCADE` | yes | yes | Routed through the guard |
| 4 | `FullI3rab/FullI3rabSql.cs` (`TruncateFullI3rabTables`) | `TRUNCATE` 3 full-i3rab tables `RESTART IDENTITY CASCADE` | yes | yes | Routed through the guard |
| 5 | `Mutashabihat/MutashabihatSql.cs` (`TruncateMutashabihatTables`) | `TRUNCATE` 3 mutashabihat tables `RESTART IDENTITY CASCADE` | yes | yes | Routed through the guard |
| 6 | `Translations/TranslationSql.cs` (`TruncateTranslationTables`) | `TRUNCATE` 2 translation tables `RESTART IDENTITY CASCADE` | yes | yes | Routed through the guard |
| 7 | `Words/DisplayRebuilding/DisplayWordsSql.cs` (`TruncateDerivedTables`) | `TRUNCATE` 4 derived display tables `RESTART IDENTITY` | yes | no | Routed through the guard |
| 8 | `Navigation/NavigationMetadataSql.cs` (`ClearNavigationData`) | `UPDATE quran_ayahs …; DELETE FROM quran_sajdas/rubs/hizbs/juzs` | yes | no | Guard preflight + advisory lock **before** the existing navigation write-isolation guard |

Consumers (call sites), for traceability: `EfBulkMorphologyWriter`, `EfBulkTafsirImportWriter`,
`EfBulkFullI3rabImportWriter`, `EfBulkMutashabihatWriter`, `EfBulkTranslationImportWriter`,
`SqlDisplayWordsRebuilder`, `EfBulkNavigationMetadataImportWriter`.

### Pre-existing isolation guard (retained)

`Navigation/NavigationMetadataCommandExecutor.EnsureWriteIsolation` already parsed and rejected any
TRUNCATE/DELETE/INSERT/UPDATE/COPY outside the four navigation-owned tables. It is **kept**; the shared
guard is layered on top of it for the advisory lock + cross-domain FK-closure preflight. The generic
guard generalizes that same idea to every domain.

## 2. Operational (non-importer) destructive scripts

| Path | Statement | Gating | Decision |
|------|-----------|--------|----------|
| `Backend/scripts/drop-db` / `reset-db` | `dotnet ef database drop --force` (full DB) then `database update` | requires `--yes`; forces `DOTNET_ENVIRONMENT=Development`; sandbox preflight | Out of US2 importer scope. These nuke the **entire** database and re-run migrations — not a selective CASCADE that could silently keep Quran while destroying Abwab. Already environment- and confirmation-gated. Left unchanged. |

No other `TRUNCATE` / `DELETE FROM` / `DROP TABLE` destructive statements exist under
`Backend/infrastructure` or `Backend/tools` (migrations excluded — schema is EF-generated, never
hand-edited). CI import gate `Backend/scripts/check-import-source-strategy.sh` is source-inspection
only and executes no import.

## 3. Lockdown design

### 3.1 Shared fail-closed guard (T019) — `QuranImportDestructiveGuard`

Location: `…/Persistence/DataPipelines/Quran/Safety/QuranImportDestructiveGuard.cs`. Every destructive
step routes through it. `ExecuteDestructiveAsync` performs, in order:

1. **Advisory lock** (`pg_advisory_xact_lock`, key `20280002`, transaction-scoped) — see §3.2.
2. **FK-closure preflight** — parses the destructive target tables from the SQL, then computes their
   **transitive FK-dependent closure** from `pg_catalog` (exactly what a `TRUNCATE … CASCADE` reaches)
   and throws `QuranImportSafetyException` (fail closed) if **any** reached table is a persistent table
   outside the Quran domain (not named `quran_*`). Today the closure is entirely `quran_*`
   (verified: the only non-Quran tables are `users`/`roles`, which reference neither), so every current
   import passes; the moment an Abwab table gains an FK into a Quran table it enters the closure and the
   import is refused.
3. **Execute** the destructive SQL.

Why a preflight rather than `CASCADE`→`RESTRICT`: several statements legitimately CASCADE across
intra-Quran FKs, and single-table truncates (DisplayWords) have Quran FK dependents that RESTRICT alone
would wrongly block. The closure preflight keeps the in-domain cascade working while fail-closing on any
out-of-domain reach.

### 3.2 Race-safe dependent lock/preflight (T020)

The transaction-scoped `pg_advisory_xact_lock` is taken **before** the destructive step, on the same
key any future dependent-creating writer will take. A dependent created concurrently therefore either
serializes **before** the import (and is seen by the closure preflight → fail closed) or **after** it
(never overlapping the destructive window). The importer-side `DestructiveImportGate`
(`Backend/tools/QuranDashboard.DataImporter/Import/Safety/`) adds the environment + source-identity
authorization layer before delegating to a pipeline handler; the authoritative race-safe lock + closure
preflight run deeper, at the destructive SQL step, so both the CLI and any direct writer are covered.

### 3.3 Environment restriction + restricted DB privileges (T021)

- **Environment restriction**: `DestructiveImportPolicy` refuses `--force` imports unless
  `QURANDASHBOARD_ALLOW_DESTRUCTIVE_IMPORT` is set, and always refuses them in the `Production`
  environment. Enforced by `DestructiveImportGate`, wired into every force-capable verb runner.
- **Restricted DB privileges**: the FK-closure preflight is **privilege-agnostic** — it fails closed on
  a cross-domain cascade regardless of the DB role (even a superuser), so US2's Abwab protection does
  not depend on grants. The **seeded restricted application role** itself (GRANT/REVOKE) is a migration
  concern owned by **US3 / T038** ("restricted application role"), because US2 forbids migrations. US2
  therefore covers privilege-safety structurally (closure guard) plus the operational environment gate.

### 3.4 Canonical source identity + stable IDs (T022)

`CanonicalQuranSourceVerifier` verifies a staged package's `source-identity.json` against the pinned
`CanonicalSourceRegistry`: refuses an un-pinned source (**forbidden**), a mismatched canonical id
(**wrong identity**), non-monotonic/duplicated **unstable IDs**, and a **missing manifest** (only the
importer gate treats a missing manifest leniently, as a warning, so legacy staged packages keep
importing while the manifest is rolled out; the verifier itself reports it as not-accepted).

## 4. Boundary confirmations

- **No Abwab type/DbSet/FK/entity/migration** is introduced by this work (T023). The T006
  FK-prohibition guard (`Abwab/_Guards/NoPrematureQuranFkTests`) stays green.
- Quranic test data is **source-safe**: the source-identity fixtures carry only identity metadata; the
  destructive-race synthetic dependent is a raw-SQL, clearly-synthetic table (`abwab_synthetic_dependent_us2`)
  with **no real ayah key**, created and dropped inside the test, never an EF entity.
