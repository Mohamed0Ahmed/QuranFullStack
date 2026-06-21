# Feature 013 — Deterministic Unique Word IDs: Reset/Reseed Acceptance Report

> Final acceptance gate (plan §7 / Phase 6). Executed against the **local
> development database only**. Nothing was committed. No production logic was
> changed; the only code edit this session was a test-only cleanup (review MINOR
> note). Companion docs: `001-implementation-report.md`,
> `docs/feature-013-words-roots-explorer/feature-013-deterministic-unique-word-ids-plan.md`.

## 0. Verdict

**PASS.**

A full local drop → migrate → reseed (all 10 documented steps) completed
successfully; every deterministic-ID hard check and every read-only integrity
check passed; a second `rebuild-words --force` produced byte-identical id/identity/
link mappings; and all canonical row counts match the baseline. The only warning
is the pre-existing, out-of-scope `UNQ-EXPECT-TASHKEEL` informational expectation.

## 1. Branches and repo status

- **Backend repo branch:** `013-deterministic-unique-word-ids`
- **Workspace repo branch:** `main` (no workspace-level changes made)
- **Committed this session:** nothing (per instruction).

### Working-tree status (after the test cleanup, before/after the reseed)

Implementation + acceptance change set (staged/tracked edits, **not committed**):

```
A  …/Migrations/20260621181644_DeterministicUniqueWordIds.cs (+ .Designer.cs)
M  …/Migrations/QuranDashboardDbContextModelSnapshot.cs
M  …/Configurations/Quran/Words/Display/UniqueSimpleWordConfiguration.cs
M  …/Configurations/Quran/Words/Display/UniqueTashkeelWordConfiguration.cs
M  …/DataPipelines/Quran/Words/DisplayRebuilding/DisplayWordsSql.cs
M  …/DataPipelines/Quran/Words/DisplayRebuilding/SqlDisplayWordsRebuilder.cs
A  report/feature-013-deterministic-unique-word-ids/001-implementation-report.md
M  tests/.../MushafReader/mushaf-reader-seed.sql
AM tests/.../WordsDisplay/DisplayWordsDeterministicIdTests.cs   ← this session's cleanup
M  tests/.../WordsDisplay/DisplayWordsRealImportIdentityLinksTests.cs
M  tests/.../WordsDisplay/DisplayWordsValidationSuccessReportTests.cs
```

Benign reseed side effect (regenerated canonical importer outputs, **not
committed**): `report/feature-008-quran-translations-foundation/translation-import-report.{json,md}`
and `report/feature-009-quran-navigation-metadata-foundation/navigation-metadata-import-report.json`.
These default report dirs live under the tracked `Backend/report/` tree
(per `DataImporterDefaults`), so the 008/009 importers rewrote them during the
reseed. Content matches the prior verified runs.

## 2. Confirmation: only the local dev DB was reset

| Check | Value |
| --- | --- |
| Target connection (EF reset, from API user-secrets) | `Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres` |
| Importer connection (explicit env var) | same — `ConnectionStrings__QuranDashboardDb` set to the local string (the DataImporter `appsettings.json` default password is wrong, per the seeding doc) |
| Server address verified | `127.0.0.1` (loopback) via `SELECT inet_server_addr()` |
| Database verified | `quran_dashboard` via `SELECT current_database()` |
| Pre-reset sanity | existing baseline data present (83,668 / 21,294 / 14,783) |
| Shared/staging/prod/remote touched | **No** — loopback host only |

The target was verified **before** running any destructive command. No remote,
staging, or production endpoint was involved.

## 3. Test cleanup (review MINOR note)

`tests/.../WordsDisplay/DisplayWordsDeterministicIdTests.cs` now implements
`IDisposable`, routes all temp report paths through a `CreateTempReportDir(label)`
helper that records each dir, and deletes them best-effort in `Dispose()`
(swallowing only `IOException` / `UnauthorizedAccessException`). xUnit creates one
test instance per test, so cleanup is per-test. Test-only; no production behavior
changed; not broadened to sibling tests (none of which clean up — left as-is per
the "do not broaden scope" instruction).

## 4. Build / test status (after the cleanup)

| Item | Result |
| --- | --- |
| `dotnet build QuranDashboard.sln` | **Build succeeded — 0 Warning(s), 0 Error(s)** |
| Focused: `--filter FullyQualifiedName~DisplayWordsDeterministicIdTests` | **4 passed, 0 failed** |
| Full suite: `dotnet test tests/QuranDashboard.Tests` | **538 passed, 0 failed, 0 skipped** (≈5m08s, Testcontainers `postgres:16-alpine`) |

## 5. Reset / reseed — exact commands and results

All importer commands were prefixed with
`ConnectionStrings__QuranDashboardDb="Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=…"`
and run from `Backend/`. The importer was built once, then run with `--no-build`.

| # | Command | Result |
| --- | --- | --- |
| 1 | `./scripts/reset-db --yes` | Dropped + recreated + applied **all** migrations; `DeterministicUniqueWordIds` ran `ALTER TABLE quran_words_unique_tashkeel ALTER COLUMN id DROP IDENTITY;` and the same for `…_simple`. |
| 2 | `… import-foundation --source /…/resources/import-sources/quran-foundation --report-out /…/resources/report` | surahs=114, ayahs=6236, pages=604, lines=9046, words=83668 |
| 3 | `… rebuild-words --force --report-out /…/resources/report/words-display` | ordered=77432/77432, unique=21294/14783; verdict **pass**, persisted |
| 4 | `… import-morphology` (default source) | morphology=77432, segments=128219, roots=1642, lemmas=4793, stems=12108, pos_tags=49 |
| 5 | `… generate-i3rab` | 128,219 segments (128,219 approved) |
| 6 | `… import-mutashabihat` (default source) | groups=814, occurrences=3557, links=3552, sources=1162 |
| 7 | `… import-tafsirs` (default source) | sources=84, ayahMappings=523824, languages=33, warnings=2 |
| 8 | `… import-translations` (default source) | sources=167, ayahMappings=1041412, languages=83 (simple:129 / with_footnotes:38), warnings=1 |
| 9 | `… import-navigation-metadata` (default source) | juz=30, hizb=60, rub=240, sajda=15, ayahsTagged=6236, warnings=0 |
| 10 | `… import-full-i3rab` (default source) | sources=4, entries=14513, ayahMappings=24944, distinctAyahs=6236, contentWarnings=0 |

**Differences vs documented quickstart (all benign):**
- Used **absolute** paths for `import-foundation` `--source`/`--report-out` (doc shows the equivalent `../resources/...` relative form).
- Relied on the built-in **default sources** for steps 4–10 (`DataImporterDefaults` → `resources/import-sources/<package>`), which matches the doc's "defaults resolve to the staged package."
- Passed `ConnectionStrings__QuranDashboardDb` explicitly (documented requirement, since the importer's default password differs from the local DB).
- The second `rebuild-words --force` (Step 4 verification) wrote to `…/resources/report/words-display-2`.

## 6. First-rebuild deterministic-ID verification

### 6a. Rebuild hard-check report (`words-display-report.json`, verdict `pass`, persisted)

| Check | Severity | Result |
| --- | --- | --- |
| ORD-COUNT, ORD-READABLE, ORD-NO-MARKERS, ORD-BIJECTION, ORD-MUSHAF-CONTIG, ORD-SURAH-CONTIG, ORD-AYAH-CONTIG | hard | **pass** |
| UNQ-COUNT (tashkeel=21294, simple=14783) | hard | **pass** |
| **UNQ-ID-DETERMINISTIC** (`id = first_quran_word_id`) | hard | **pass — 0 violations** |
| **UNQ-ID-UNIQUE** (0 duplicate ids) | hard | **pass — 0** |
| STAT-MATCH (sum occurrences=77432) | hard | **pass** |
| FIRST-OCC | hard | **pass — 0 violations** |
| LINK-READABLE-COMPLETE | hard | **pass — 0** |
| LINK-MARKERS-NULL | hard | **pass — 0** |
| LINK-RESOLVES | hard | **pass — 0** |
| LINK-CONSISTENT | hard | **pass — 0** |
| SRC-UNTOUCHED (words=83668, ayahs=6236, surahs=114) | hard | **pass — unchanged** |
| UNQ-EXPECT-SIMPLE (14783) | warning | pass |
| UNQ-EXPECT-TASHKEEL (expected 21210, observed 21294) | warning | **fail (known, out of scope)** |

### 6b. Independent read-only SQL verification (all expect 0)

| Check | Observed |
| --- | --- |
| `quran_words_unique_tashkeel.id <> first_quran_word_id` | **0** |
| `quran_words_unique_simple.id <> first_quran_word_id` | **0** |
| duplicate ids — tashkeel | **0** |
| duplicate ids — simple | **0** |
| readable words with a NULL unique link | **0** |
| ayah markers with a NON-NULL unique link | **0** |
| tashkeel links that do not resolve | **0** |
| simple links that do not resolve | **0** |
| tashkeel link identity mismatch (`u.text_uthmani <> w.text_uthmani`) | **0** |
| simple link identity mismatch (`u.word_key_imlaei_simple <> w.word_key_imlaei_simple`) | **0** |

## 7. Second-rebuild determinism verification

Captured MD5 of the ordered mappings before and after a second
`rebuild-words --force`:

| Mapping | Before | After | Result |
| --- | --- | --- | --- |
| tashkeel `(id ‖ text_uthmani)` | `578a613c…aa0f` | `578a613c…aa0f` | **IDENTICAL** |
| simple `(id ‖ word_key_imlaei_simple)` | `6a8afc80…3b93a1` | `6a8afc80…3b93a1` | **IDENTICAL** |
| `quran_words (id ‖ tashkeel_link ‖ simple_link)` | `95749bd8…5abdf` | `95749bd8…5abdf` | **IDENTICAL** |

Gap evidence (expected): `quran_words_unique_tashkeel` has 21,294 rows with
`min(id)=1`, `max(id)=83660` — ids are sparse occurrence ids
(`= first_quran_word_id`), not a dense 1..N sequence. Gaps are present and
acceptable; values are identical across rebuilds.

## 8. Canonical counts vs baseline

All match `Backend/report/database/current-database-tables-and-relationships-report.md`.

| Table | Rows | Baseline | Match |
| --- | --- | --- | --- |
| `quran_words` (total) | 83,668 | 83,668 | ✅ |
| — readable words | 77,432 | 77,432 | ✅ |
| — ayah markers | 6,236 | 6,236 | ✅ |
| `quran_words_ordered_tashkeel` | 77,432 | 77,432 | ✅ |
| `quran_words_ordered_simple` | 77,432 | 77,432 | ✅ |
| `quran_words_unique_tashkeel` | 21,294 | 21,294 | ✅ |
| `quran_words_unique_simple` | 14,783 | 14,783 | ✅ |
| `quran_surahs` | 114 | 114 | ✅ |
| `quran_ayahs` | 6,236 | 6,236 | ✅ |
| `quran_mushaf_pages` | 604 | 604 | ✅ |
| `quran_mushaf_lines` | 9,046 | 9,046 | ✅ |
| `quran_word_morphology` | 77,432 | 77,432 | ✅ |
| `quran_word_morphology_segments` | 128,219 | 128,219 | ✅ |
| `quran_roots` | 1,642 | 1,642 | ✅ |
| `quran_pos_tags` | 49 | 49 | ✅ |
| `quran_tafsir_sources` | 84 | 84 | ✅ |
| `quran_translation_sources` | 167 | 167 | ✅ |
| `quran_juzs` / `quran_hizbs` / `quran_rubs` / `quran_sajdas` | 30 / 60 / 240 / 15 | 30 / 60 / 240 / 15 | ✅ |
| `quran_full_i3rab_sources` | 4 | 4 | ✅ |

## 9. Warnings

- **`UNQ-EXPECT-TASHKEEL` (warning, fail):** expected 21,210, observed 21,294.
  Pre-existing informational expectation in `DisplayWordsInvariants`; explicitly
  **out of scope** for this feature and unchanged. The actual distinct-tashkeel
  count (21,294) is internally consistent (matches `UNQ-COUNT` and the live
  `COUNT(DISTINCT text_uthmani)`); only the hard-coded informational constant is
  stale. Recommend a separate follow-up to reconcile the constant.
- **Importer content warnings (informational, expected):** tafsirs warnings=2,
  translations warnings=1; navigation warnings=0, full-i3rab contentWarnings=0.
  These match the previously verified import runs and are unrelated to this feature.

## 10. Final recommendation

**PASS** — the deterministic-unique-word-IDs change is fully verified end-to-end on
a clean local reseed: `id = first_quran_word_id` holds for every unique row, ids
are unique, links/markers are correct and resolve, the contract is enforced by two
new hard checks, rebuilds are byte-identical, all canonical counts match, and the
full test suite is green. Nothing was committed. The branch is ready to be
committed/merged through the normal workflow when you choose; the only open item is
the unrelated, pre-existing `UNQ-EXPECT-TASHKEEL` constant (separate follow-up).
