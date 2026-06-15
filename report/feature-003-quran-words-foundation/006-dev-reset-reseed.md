# Feature 003 — Phase 7: Dev Reset / Reseed Report

**Date:** 2026-06-10  
**Phase:** 7 only — developer-run reset → migrate → import → rebuild → audit (§12)  
**Verdict:** PASS

---

## Summary

Executed the documented dev reset/reseed workflow on the local `quran_dashboard` PostgreSQL
database after Feature 003 Phases 1–6. The database was dropped, all migrations applied
(including identity-link schema), canonical foundation data re-imported, and display tables
rebuilt with `--force`. Rebuild report verdict **PASS**; all hard checks green including the
four `LINK-*` checks; integration tests for real-import identity links **8/8 passed**.

---

## Workflow Executed

| Step | Command | Result |
| --- | --- | --- |
| 1. Reset + migrate | `./scripts/reset-db --yes` (after `clean-local-build` for stale sandbox assets) | Database dropped and recreated; 6 migrations applied |
| 2. Import foundation | `dotnet run --project tools/QuranDashboard.DataImporter -- import-foundation --source ../resources/import-sources/quran-foundation --report-out ../resources/report` | `pass-with-warnings`; 83,668 words imported |
| 3. Rebuild words | `dotnet run --project tools/QuranDashboard.DataImporter -- rebuild-words --force --report-out ../resources/report/words-display` | `PASS`; links populated |
| 4. Audit | Rebuild report + `DisplayWordsRealImportIdentityLinks` tests | All checks passed |

**Note:** DataImporter steps required `ConnectionStrings__QuranDashboardDb` env var because the
importer host does not load API user secrets (documented in `docs/feature-003-word-identity-links/quickstart.md`).

---

## Migrations Applied (fresh database)

1. `20260608095952_QuranFoundationSchema`
2. `20260609065804_WordsDisplayTables`
3. `20260610023128_AddWordKeyImlaeiSimple`
4. `20260610041226_AddUniqueSimpleImlaeiIdentity`
5. `20260610042841_AddQuranWordIdentityLinks`

---

## Import Report

**File:** `resources/report/quran-foundation-import-report.md`  
**Verdict:** `pass-with-warnings`  
**Totals:** surahs=114, ayahs=6,236, pages=604, lines=9,046, words=83,668 (77,432 readable + 6,236 markers)

After import, `unique_tashkeel_word_id` and `unique_simple_word_id` were **NULL** on all
`quran_words` rows (expected — links populated only by rebuild).

---

## Rebuild Report

**File:** `resources/report/words-display/words-display-report.md`  
**Verdict:** PASS  
**Run (UTC):** 2026-06-10 06:53:16Z

### Totals

| Table / metric | Observed | Expected |
| --- | ---: | ---: |
| `quran_words_ordered_tashkeel` | 77,432 | 77,432 |
| `quran_words_ordered_simple` | 77,432 | 77,432 |
| `quran_words_unique_tashkeel` | 21,294 | 21,294 |
| `quran_words_unique_simple` | 14,783 | 14,783 |
| readable words (source) | 77,432 | 77,432 |

### Hard checks (all passed)

| Id | Observed |
| --- | --- |
| `LINK-READABLE-COMPLETE` | 0 incomplete readable rows |
| `LINK-MARKERS-NULL` | 0 marker rows with non-null links |
| `LINK-RESOLVES` | 0 dangling link ids |
| `LINK-CONSISTENT` | 0 key mismatches |
| `SRC-UNTOUCHED` | words=83,668, ayahs=6,236, surahs=114 |
| (all other existing hard checks) | pass |

### Warnings (accepted)

- `UNQ-EXPECT-TASHKEEL`: expected 21,210, observed 21,294 (known Uthmani-mark splitting; per plan §10.2)

---

## Integration Test Audit

```bash
dotnet test tests/QuranDashboard.Tests --filter "FullyQualifiedName~DisplayWordsRealImportIdentityLinks"
```

**Result:** 8 passed, 0 failed, 0 skipped

Covers canonical counts, identity-link completeness/validity, anchors (`الله`=2,155,
`العظيم`=36, `الرحمان`=45 + representative Uthmani), `ال ياسين`=1, `5:52:12`→`دايرة`, and
source text column immutability after rebuild.

---

## Documentation Added

| File | Purpose |
| --- | --- |
| `docs/feature-003-word-identity-links/quickstart.md` | Developer quickstart for §12 workflow |
| `docs/README.md` | Index entry for the new quickstart |

---

## Future Production Note (not blocking)

Per implementation plan §12: before user/gate data depends on `unique_*_word_id` values,
adopt stable-id strategy (natural key alongside id, remap step, or upsert without
`RESTART IDENTITY`). Accepted for current dev phase.

---

## Final Verdict

**PASS** — Dev reset/reseed workflow completes successfully; canonical counts, identity links,
structural hard checks, and real-import integration tests all align with Feature 003 target state.
