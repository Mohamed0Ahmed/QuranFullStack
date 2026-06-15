# Database Reset & Seeding Order (Dev Runbook)

**Date:** 2026-06-14
**Scope:** Documentation only. Records the canonical local-dev order to reset, migrate, and seed the
`quran_dashboard` PostgreSQL database across Features 002–007. No command was run to produce this
report; commands below are the documented/intended sequence — confirm flags against each feature’s
quickstart before running.

**Companion:** [`current-database-inventory.md`](./current-database-inventory.md) (live catalog
snapshot). The reset/reseed workflow itself is verified end-to-end in
`../feature-003-quran-words-foundation/006-dev-reset-reseed.md` (Feature 003 Phase 7, through the
rebuild-words step).

> **Importer host note (from the Phase 7 report):** the DataImporter does **not** load API user
> secrets. Provide the connection string via the `ConnectionStrings__QuranDashboardDb` environment
> variable when running any importer verb.

---

## 1. Reset + migrate

| Step | Command | Notes |
| --- | --- | --- |
| 1. Reset | `./scripts/reset-db --yes` | Drops and recreates the local database. Run `clean-local-build` first if stale sandbox assets exist. |
| 2. Migrate | (applied by reset, or `dotnet ef database update`) | Applies all EF migrations in order (§2). Do **not** run `dotnet ef database update` in shared/prod contexts without explicit approval. |

## 2. Migrations (apply in this order — current set)

| # | Migration | Feature |
| ---: | --- | --- |
| 1 | `20260608095952_QuranFoundationSchema` | 002 |
| 2 | `20260609065804_WordsDisplayTables` | 003 |
| 3 | `20260610023128_AddWordKeyImlaeiSimple` | 003 |
| 4 | `20260610041226_AddUniqueSimpleImlaeiIdentity` | 003 |
| 5 | `20260610042841_AddQuranWordIdentityLinks` | 003 |
| 6 | `20260610155434_AddQuranWordMorphology` | 004 |
| 7 | `20260612151359_AddWordSimpleI3rab` | 005 |
| 8 | `20260613152703_AddQuranMutashabihat` | 006 |
| 9 | `20260614120520_AddQuranTafsirs` | 007 |

## 3. Seeding order (by data dependency)

Run the importer verbs in this order; each later step assumes the earlier data exists. The verb names
are from `Backend/tools/QuranDashboard.DataImporter/Program.cs`. The foundation + rebuild commands are
the exact ones verified in the Phase 7 report; the remaining verbs are shown in base form — confirm
`--source` / `--report-out` / `--force` against each feature’s quickstart.

| Order | Verb | Feature | Depends on | Verified example |
| ---: | --- | --- | --- | --- |
| 1 | `import-foundation` | 002 | fresh schema | `dotnet run --project tools/QuranDashboard.DataImporter -- import-foundation --source ../resources/import-sources/quran-foundation --report-out ../resources/report` |
| 2 | `rebuild-words` | 003 | foundation words | `dotnet run --project tools/QuranDashboard.DataImporter -- rebuild-words --force --report-out ../resources/report/words-display` |
| 3 | `import-morphology` | 004 | foundation words | verb present; source per Feature 004 quickstart |
| 4 | `generate-i3rab` | 005 | morphology/segments (004) | `generate-i3rab [--report-out <path>] [--force]` |
| 5 | `import-mutashabihat` | 006 | resolved ayahs (foundation) | verb present; `--source`/`--report-out`/`--force` per Feature 006 quickstart |
| 6 | `import-tafsirs` | 007 | resolved ayahs (foundation) | verb present; `--source`/`--report-out`/`--force` per Feature 007 quickstart |

> `import-sources` is a staging/utility verb, not part of the per-feature seeding chain.

## 4. What is documented vs. inferred

- **Documented & verified:** the reset → migrate → `import-foundation` → `rebuild-words` chain and the
  env-var requirement (Phase 7 report, with real counts: 83,668 words; unique-simple 14,783).
- **Inferred from feature dependencies (confirm before relying on for prod):** the relative ordering of
  `import-morphology` (004) → `generate-i3rab` (005), and that `import-mutashabihat` (006) /
  `import-tafsirs` (007) require foundation ayahs (mutashabihat confirms this via the
  `MUT-AYAH-RESOLVE` hard check). Steps 3–6 have not been captured in a single end-to-end reseed report.

## 5. Production note (not blocking dev)

Per the Feature 003 plan (§12) and the Phase 7 report: before any user/gate data depends on
`unique_*_word_id` values, adopt a stable-id strategy (natural key alongside id, remap step, or upsert
without `RESTART IDENTITY`). Identity ids assigned by a dev reseed are not guaranteed stable across runs.
</content>
