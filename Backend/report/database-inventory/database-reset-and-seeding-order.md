# Database Reset & Seeding Order (Dev Runbook)

**Date:** 2026-06-29 *(revised for Feature 018; prior revisions: 2026-06-17 Feature 010, 2026-06-16 Feature 009; original 2026-06-14)*
**Scope:** Documentation only. Records the canonical local-dev order to reset, migrate, and seed the
`quran_dashboard` PostgreSQL database across Features 002–018. Commands below are the documented/intended
sequence — confirm flags against each feature’s quickstart before running. Per-feature real-run
verifications (008–010, 018) are recorded in §4; no single end-to-end reset→reseed of the whole chain
has been captured in one report.

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
| 10 | `20260615112132_AddQuranTranslations` | 008 |
| 11 | `20260616095937_AddQuranNavigationMetadata` | 009 |
| 12 | `20260617104912_AddQuranFullI3rab` | 010 |
| 13 | `20260621181644_DeterministicUniqueWordIds` | 013 |
| 14 | `20260627144247_AddSegmentDimensionIds` | 017 |
| 15 | `20260628233646_AddSegmentStemId` | 018 |

> Features 011, 012, 014, 015, and 016 add **no migration** — they are read-only API/frontend features
> (mushaf reader/study context, ayah similarities, Words Hub, Roots Explorer, Lemmas/Stems Explorer).
> #13 changes only the unique-word id generation strategy (see §5); #14/#15 add nullable segment
> dimension ids (`root_id`/`lemma_id`/`stem_id`) populated in place by `import-morphology` (§3).

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
| 7 | `import-translations` | 008 | resolved ayahs (foundation only) | `dotnet run --project tools/QuranDashboard.DataImporter -- import-translations` (defaults resolve to the staged package and `report/feature-008-quran-translations-foundation/`); **verified end-to-end 2026-06-16** — see §4 |
| 8 | `import-navigation-metadata` | 009 | resolved ayahs (foundation only) | `dotnet run --project tools/QuranDashboard.DataImporter -- import-navigation-metadata` (defaults resolve to the staged package and `report/feature-009-quran-navigation-metadata-foundation/`); **verified end-to-end 2026-06-16** — see §4 |
| 9 | `import-full-i3rab` | 010 | resolved ayahs (foundation only) | `dotnet run --project tools/QuranDashboard.DataImporter -- import-full-i3rab` (defaults resolve to `resources/import-sources/quran-full-i3rab/` and `resources/report/quran-full-i3rab/`); **verified end-to-end 2026-06-17** — see §4 |

> `import-sources` is a staging/utility verb, not part of the per-feature seeding chain.
>
> **Order-7 dependency note:** `import-translations` (008) resolves `verse_key -> ayah_id` against
> `quran_ayahs` only. It does **not** depend on words, morphology, i3rab, mutashabihat, or tafsirs, so it
> may be run any time after `import-foundation` (order 1). It is listed at order 7 to preserve the existing
> numbering; it is independent of orders 2–6.
>
> **Order-8 dependency note:** `import-navigation-metadata` (009) also resolves `verse_key -> ayah_id`
> against `quran_ayahs` only and tags the three navigation columns on `quran_ayahs`. It does **not** depend
> on words, morphology, i3rab, mutashabihat, tafsirs, or translations, so it may be run any time after
> `import-foundation` (order 1). It is listed at order 8 for consistency with the feature sequence.
>
> **Order-9 dependency note:** `import-full-i3rab` (010) resolves `verse_key -> ayah_id` against
> `quran_ayahs` only. It does **not** depend on words, morphology, simple i3rab (005), mutashabihat,
> tafsirs, translations, or navigation metadata, so it may be run any time after `import-foundation`
> (order 1). It is listed at order 9 for consistency with the feature sequence.

> **No new seeding step for Features 011–018.** The verb list is unchanged from Feature 010; the chain
> is still orders 1–9 above. The schema/importer changes since 010 ride existing verbs:
>
> - **Order-2 (`rebuild-words`) — Feature 013:** now assigns `quran_words_unique_*`.`id`
>   deterministically (migration `DeterministicUniqueWordIds` drops `IDENTITY`), so unique-word ids are
>   stable across reseeds. Resolves the §5 caveat.
> - **Order-3 (`import-morphology`) — Features 017–018:** now also populates segment-level dimension ids
>   on `quran_word_morphology_segments` — `root_id`/`lemma_id` (017, `AddSegmentDimensionIds`) and
>   `stem_id` (018, `AddSegmentStemId`). The verb, source package, and command are unchanged. The
>   two-STEM secondary `stem_id` cases are resolved from a **curated correction artifact embedded in the
>   Infrastructure assembly** (`infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/Corrections/segment-stem-corrected-arabic.json`),
>   not a new `--source` file. Hard checks enforce the curation totals (483 two-STEM secondary
>   candidates = 479 curated `stem_id` + 4 intentionally null). Verified end-to-end 2026-06-29 — see §4.

## 4. What is documented vs. inferred

- **Documented & verified:** the reset → migrate → `import-foundation` → `rebuild-words` chain and the
  env-var requirement (Phase 7 report, with real counts: 83,668 words; unique-simple 14,783).
- **Inferred from feature dependencies (confirm before relying on for prod):** the relative ordering of
  `import-morphology` (004) → `generate-i3rab` (005), and that `import-mutashabihat` (006) /
  `import-tafsirs` (007) require foundation ayahs (mutashabihat confirms this via the
  `MUT-AYAH-RESOLVE` hard check). Steps 3–6 have not been captured in a single end-to-end reseed report.
- **Implemented, not yet run end-to-end:** none as of 2026-06-16 for the full reset→reseed chain. Individual
  feature imports 008 and 009 have been run against an existing foundation-seeded DB (see below).
- **Documented & verified (Feature 008, real run 2026-06-16):** migration `20260615112132_AddQuranTranslations`
  applied via `./scripts/update-db`, then `import-translations` run against
  `resources/import-sources/quran-translations` (connection supplied via `ConnectionStrings__QuranDashboardDb`,
  since the DataImporter's `appsettings.json` default password differs from the local DB). Verdict `PASS`,
  `persisted = true`, all hard checks green. Real counts confirmed in both the run report and direct DB
  spot-checks: **167** sources (129 simple / 38 with-footnotes), **83** languages, **1,041,412** ayah
  mappings, **6,236** distinct ayahs, **19** excluded (report-only), 0 orphan FKs, 0 duplicate
  `(source_id, ayah_id)` rows, 0 copied-`text_uthmani` leaks. Canonical report at
  `report/feature-008-quran-translations-foundation/translation-import-report.{md,json}`.
  Note: this verified run applied only the 008 migration on top of an existing foundation-seeded DB; it was
  **not** a full reset→reseed of the whole chain in one go.
- **Documented & verified (Feature 009, real run 2026-06-16):** migration
  `20260616095937_AddQuranNavigationMetadata` applied via `./scripts/update-db`, then
  `import-navigation-metadata` run against `resources/import-sources/quran-navigation-metadata`
  (connection via `ConnectionStrings__QuranDashboardDb`). Console summary:
  `juz=30, hizb=60, rub=240, sajda=15, ayahsTagged=6236, warnings=0`. Post-run DB checks: 30/60/240/15
  division rows, **0** untagged ayahs (of 6,236), sajda split 11 optional / 4 required, 0 orphan hizbs/rubs,
  `text_uthmani` untouched. Importer report: `verdict=accepted`, `persisted=true`,
  `ayahCoverage.complete=true`, `noQuranAyahTextReadOrStored=true`. Canonical report at
  `report/feature-009-quran-navigation-metadata-foundation/navigation-metadata-import-report.{md,json}`.
  Note: same caveat as 008 — migration + import on an existing DB, not a full reset→reseed of the whole chain.
- **Documented & verified (Feature 010, real run 2026-06-17):** migration
  `20260617104912_AddQuranFullI3rab` applied via `dotnet ef database update` (Api startup project),
  then `import-full-i3rab` run against `resources/import-sources/quran-full-i3rab`
  (connection via `ConnectionStrings__QuranDashboardDb`). Console summary:
  `sources=4, entries=14513, ayahMappings=24944, distinctAyahs=6236, contentWarnings=0`. Post-run DB
  checks: 4 source rows, 14,513 entry rows, 24,944 junction rows, 6,236 mappings per source
  (`daas`, `darwish`, `jadwal`, `muyassar`). Importer report: `verdict=pass`, `persisted=true`,
  `forced=false`, 21/21 hard checks green, provenance warning present. The generated
  full-i3rab import report was swept with its feature folder; the counts above are the
  surviving record — regenerate by re-running the verb, or recover from git history.
  Note: same caveat as 008/009 — migration + import on an existing foundation-seeded DB, not a full
  reset→reseed of the whole chain.
- **Feature 013 (deterministic unique word ids):** migration `20260621181644_DeterministicUniqueWordIds`
  drops the `IDENTITY` strategy on `quran_words_unique_simple.id` / `quran_words_unique_tashkeel.id`;
  `rebuild-words` (order 2) now assigns these ids deterministically, so they are stable across reseeds.
  This is the stable-id strategy anticipated in §5.
- **Feature 017 (segment root_id / lemma_id):** migration `20260627144247_AddSegmentDimensionIds` adds
  nullable `root_id` / `lemma_id` (+ FKs to `quran_roots` / `quran_lemmas`, lookup indexes) to
  `quran_word_morphology_segments`, populated in place by `import-morphology` (order 3).
- **Documented & verified (Feature 018, real run 2026-06-29):** migration
  `20260628233646_AddSegmentStemId` applied via `dotnet ef database update` (Api startup project), then
  `import-morphology --force` reseeded morphology. Console summary:
  `morphology=77432, segments=128219, roots=1642, lemmas=4790, stems=12108, pos_tags=49`. Post-run DB
  checks on `quran_word_morphology_segments`: **483** two-STEM secondary segments, **479** with a curated
  `stem_id`, **4** intentionally null (`78:1:1:2`, `86:5:3:2`, `72:16:1:3`, `20:94:2:3`), **0** non-STEM
  segments carrying a `stem_id`, **0** dangling stem FKs, **0** single/primary-STEM head mismatches. All
  `SEG-STEM-ID-*` hard checks green. Note: same caveat as 008/009/010 — migration + import on an existing
  foundation-seeded DB, not a full reset→reseed of the whole chain.

## 5. Production note (largely resolved by Feature 013)

Per the Feature 003 plan (§12) and the Phase 7 report, a stable-id strategy was required before any
user/gate data depends on `unique_*_word_id` values. **Feature 013 implemented this** (migration
`20260621181644_DeterministicUniqueWordIds`): the unique-word tables no longer use `IDENTITY`, and
`rebuild-words` assigns ids deterministically, so they are stable across dev reseeds. Re-confirm
determinism after any change to the unique-word derivation in `DisplayWordsSql` before relying on these
ids in production.
</content>
