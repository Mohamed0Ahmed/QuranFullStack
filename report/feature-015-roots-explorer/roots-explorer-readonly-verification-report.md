# Feature 015 — Quran Roots Explorer — Read-Only Verification Report

> **Verification only.** No code, migrations, or source files were produced or changed. All database
> access was read-only (`default_transaction_read_only=on`, write attempt confirmed blocked). No
> Quran text was printed: every result below is IDs and counts only. Nothing was committed.
>
> This report verifies the open notes in §8.4 of
> `roots-explorer-capability-analysis-report.md` before the implementation plan is written.

| Item | Value |
| --- | --- |
| Database | `quran_dashboard` (localhost:5432) |
| Access mode | Read-only psql; `transaction_read_only=on` confirmed; `CREATE TEMP TABLE` rejected with `cannot execute … in a read-only transaction` |
| PostgreSQL | 18.4 |
| Roots | 1,642 · Morphology rows | 77,432 · Lemmas | 4,793 · Stems | 12,108 |
| **Verdict** | **VERIFIED_READY_WITH_NOTES** |

---

## Verdict: VERIFIED_READY_WITH_NOTES

All five §8.4 checks were run. Four pass cleanly; one (lemma-count reconciliation) surfaced a
**metric-definition divergence**, not a data defect. Nothing blocks planning. The single note below
is a definition the plan must lock, not a data problem to fix.

- Aggregation is fast and sane → cache-whole-list strategy confirmed viable.
- `words_count` reconciles **exactly** (occurrences).
- Unique-word links are **100% present** for root-bearing words.
- Stems must be derived via morphology — confirmed, and confirmed cheap; **not a blocker**.
- **No migration/index needed** — no measured evidence supports adding one.
- **NOTE:** `distinct_lemmas_count` does **not** equal `COUNT(quran_lemmas WHERE root_id)`
  (49 roots differ). They are two different, both-valid metrics. The plan must pick one definition
  for the لمmas column and the الصيغ المعجمية tab and use it consistently.

---

## Check 1 — Aggregate counts: sanity and cost

### Queries run

1. Overall sanity (roots, root-bearing vs root-less morphology rows).
2. Top-20 roots by occurrences with all six aggregates.
3. Suspicious-value scan (non-positive aggregates; `simple_words > tashkeel_words`; maxima).
4. `EXPLAIN (ANALYZE, BUFFERS, TIMING)` of the full grouped aggregation.
5. Wall-clock timing of the full materialized aggregation, 3 runs.

The grouped aggregation (per `root_id`): `COUNT(*)`, `COUNT(DISTINCT w.ayah_id)`,
`COUNT(DISTINCT w.surah_number)`, `COUNT(DISTINCT w.unique_simple_word_id)`,
`COUNT(DISTINCT w.unique_tashkeel_word_id)`, `COUNT(DISTINCT m.stem_id)` from
`quran_word_morphology m JOIN quran_words w ON w.id = m.quran_word_id WHERE m.root_id IS NOT NULL`.

### Results

**Sanity:**

| Metric | Value |
| --- | ---: |
| `quran_roots` rows | 1,642 |
| Roots with ≥1 word (distinct `root_id` in morphology) | 1,642 |
| Morphology rows **with** `root_id` | 50,298 |
| Morphology rows **without** `root_id` | 27,134 |
| Morphology rows total | 77,432 |

Every root has words. The aggregation drives off only **50,298** rows (root-less words — particles,
pronouns, etc. — are correctly excluded). This is expected and not a concern.

**Worst-case roots (top of 1,642 by occurrences):** `root_id | occ | ayahs | surahs | simple | tashkeel | stems`

```
264 | 2851 | 1879 | 86 | 31 |  81 | 23
429 | 1722 | 1383 | 84 | 96 | 167 | 42
106 | 1390 | 1176 | 86 | 83 | 127 | 38
 13 |  980 |  871 | 94 | 42 | 175 | 15
209 |  879 |  723 | 77 | 97 | 189 | 71
 79 |  854 |  728 | 85 | 105| 193 | 60
473 |  660 |  597 | 79 | 101| 185 | 84   ← max stems
759 |  549 |  486 | 72 | 183| 265 | 78   ← max distinct words
```

**Suspicious-value scan:** all clean.

| Probe | Result |
| --- | ---: |
| Roots with non-positive occ / ayahs / surahs / simple / tashkeel / stems | 0 / 0 / 0 / 0 / 0 / 0 |
| Roots where `simple_words > tashkeel_words` (would be impossible) | 0 |
| Max occurrences / max ayahs / max stems | 2,851 / 1,879 / 84 |
| Roots aggregated | 1,642 (== `quran_roots` rows) |

Counts are internally consistent: `occ ≥ ayahs`, `surahs ≤ 114`, and `simple_words ≤ tashkeel_words`
for every root (simple identity merges tashkeel variants). No result looks suspicious.

### Query timing

`EXPLAIN (ANALYZE, BUFFERS)` plan for the full grouped aggregation:

```
GroupAggregate (rows=1642)  actual time=80.6..113.7 ms
  -> Sort (rows=50298)  Sort Method: quicksort  Memory: 3501kB
     -> Hash Join (rows=50298)  Hash Cond: w.id = m.quran_word_id
        -> Seq Scan on quran_words w (rows=83668)
        -> Hash -> Seq Scan on quran_word_morphology m
                     Filter: root_id IS NOT NULL  (Rows Removed: 27134)
Planning Time: 1.1 ms · Execution Time: 114.2 ms
```

Wall-clock (full aggregation materialized, 3 runs): **75 ms → 36 ms → 31 ms** (warm).

**Conclusion:** ~30–115 ms for the entire 1,642-root summary. **Comfortably fast enough for
compute-once + cache-whole-list.** Per-page aggregation would also be fine, but compute-once is
trivially affordable and is the recommended path. The plan (two seq scans + hash join + sort) is the
optimal shape for a whole-table aggregation; this is exactly why no index helps here (see Check 5).

---

## Check 2 — Reconcile precomputed counts

### 2a. `quran_roots.words_count` vs morphology `COUNT(*)` per root — **EXACT MATCH**

| Total roots | Matching | Mismatching |
| ---: | ---: | ---: |
| 1,642 | **1,642** | **0** |

`words_count` is a precise occurrence count. No mismatches. This confirms the capability report's
"occurrences = `words_count`" claim.

### 2b. `quran_roots.distinct_lemmas_count` vs `COUNT(quran_lemmas WHERE root_id = X)` — **49 MISMATCHES**

| Total roots | Matching | Mismatching |
| ---: | ---: | ---: |
| 1,642 | 1,593 | **49** |

Sample mismatches (`root_id | distinct_lemmas_count | COUNT(quran_lemmas WHERE root_id)`):

```
 54 |  9 | 6      209 | 22 | 18
 79 | 14 | 13     298 |  6 | 5
 95 |  5 | 3      310 | 10 | 8
106 |  8 | 5      459 |  9 | 3
132 |  9 | 8
191 |  7 | 6
```

In every case the precomputed value is **higher** than the `quran_lemmas.root_id` count.

### 2c. Root cause — two different metrics (verified)

`distinct_lemmas_count` vs morphology `COUNT(DISTINCT lemma_id)` per root:

| Total roots | Matching | Mismatching |
| ---: | ---: | ---: |
| 1,642 | **1,642** | **0** |

So `distinct_lemmas_count` is defined by **co-occurrence**: the number of distinct lemmas that appear
on words carrying the root. It matches the morphology-derived distinct-lemma count for **all** roots.

By contrast, `quran_lemmas.root_id` is a **single dominant-root link** (the lemma's earliest
co-occurring root, per the importer). Supporting evidence:

- **41 lemmas appear under more than one root** by co-occurrence; the most-shared lemma appears under
  **13 distinct roots**. Each such lemma adds to `distinct_lemmas_count` for every root it co-occurs
  with, but links to only one root via `quran_lemmas.root_id`.
- `quran_lemmas`: 4,793 total — **4,640 with `root_id`, 153 without**. The 153 unlinked lemmas
  further widen the gap for the `quran_lemmas`-based count.

### Mismatch assessment

**This is not a data defect.** It is two legitimate definitions:

- **Co-occurrence count** (`distinct_lemmas_count` ≡ morphology `COUNT(DISTINCT lemma_id) WHERE root_id`):
  "lemmas that occur under this root."
- **Ownership count** (`COUNT(quran_lemmas WHERE root_id)`): "lemmas whose dominant root is this root."

> **Correction to the capability report:** §3 / §2 stated `distinct_lemmas_count` is "equivalently
> `COUNT(quran_lemmas WHERE root_id = X)`." That equivalence is **false** for 49 roots. The two
> sources differ by definition. (Report not edited per task scope; flagged here for the plan.)

**Recommendation for the plan (note, not blocker):** use **co-occurrence semantics** for both the
لمmas count column and the الصيغ المعجمية tab — i.e. derive lemmas-per-root via
`quran_word_morphology` (`DISTINCT lemma_id WHERE root_id = X`). This (a) matches the precomputed
`distinct_lemmas_count` exactly, so the column and tab stay consistent, and (b) is **symmetric with
stems**, which must be derived the same way (Check 4). Whichever definition is chosen, the table
column and the detail tab must use the *same* one.

---

## Check 3 — Unique-word links for root-bearing words — **CLEAN (0 missing)**

Query: count root-bearing words (`m.root_id IS NOT NULL`, joined to `quran_words`) missing either
identity column.

| Probe | Result |
| --- | ---: |
| Root-bearing words missing `unique_simple_word_id` | **0** |
| Root-bearing words missing `unique_tashkeel_word_id` | **0** |
| Root-bearing words total | 50,298 |

**Expected result (0 missing) achieved.** Columns 5/6 (distinct simple/tashkeel words) and the
"click a simple/tashkeel word → open the existing Feature 014 word detail" navigation are fully
reliable for every root-bearing word. No fix needed.

---

## Check 4 — Stems aggregation path — **CONFIRMED, NOT A BLOCKER**

`quran_stems` columns (`information_schema.columns`):

| Column | Type |
| --- | --- |
| `id` | integer |
| `stem_text` | text |
| `words_count` | integer |
| `first_word_order_in_mushaf` | integer |

`EXISTS(column 'root_id' on 'quran_stems')` → **`f` (false)**.

So `quran_stems` carries **no** `root_id`. Stems-per-root must be derived via
`quran_word_morphology` (`COUNT(DISTINCT stem_id) WHERE root_id = X`), and the stems detail tab via
`DISTINCT stem_id` joined to `quran_stems` for text. **Confirmed exactly as the capability report
stated.**

**Not a blocker:** the single-root detail plan (Check 5) shows this path is cheap, and stems-per-root
is bounded (max 84). This is the same morphology-derived shape recommended for lemmas in Check 2, so
lemmas and stems can share one consistent derivation pattern.

---

## Check 5 — Migration / index requirement — **NONE NEEDED**

### Evidence

**Full-table aggregation (Check 1):** the planner chooses **sequential scans** + hash join + sort,
not an index. This is optimal — a whole-table grouped aggregation reads (nearly) all rows, so an
index would not help and the planner would not use one. Execution 31–114 ms.

**Single-root detail aggregation** (`EXPLAIN ANALYZE`, worst-case `root_id = 264`, 2,851 occurrences):

```
Aggregate  actual time=27.2..27.2 ms
  -> Sort (rows=2851)
     -> Hash Join  Hash Cond: w.id = m.quran_word_id
        -> Seq Scan on quran_words w (rows=83668)
        -> Hash -> Bitmap Heap Scan on quran_word_morphology m
                     Recheck Cond: root_id = 264
                     -> Bitmap Index Scan on "IX_quran_word_morphology_root_id"  (time≈0.24 ms)
Execution Time: 27.3 ms
```

The morphology filter already uses the existing **`IX_quran_word_morphology_root_id`** index
(bitmap index scan, ~0.24 ms). The worst-case single-root detail read is **27 ms**; typical roots
are faster. Detail reads are cheap on the existing schema.

### Statement

**No migration and no new index are required.** The measured query plans and timings provide **no
evidence** that any index is missing:

- Full aggregation: index-irrelevant by design (whole-table scan is optimal); 31–114 ms.
- Single-root detail: already index-served on `root_id`; 27 ms worst case.

Per the task rule, **no index is recommended** — there is no measured evidence to justify one. (The
seq scan on `quran_words` during single-root joins is the planner's choice and remains fast at this
scale; a covering index would be a speculative micro-optimization with no current justification.)

---

## Commands / queries run (summary)

All via read-only psql against `quran_dashboard` (`PGOPTIONS='-c default_transaction_read_only=on'`):

1. Connection + read-only guard verification (write attempt rejected).
2. Overall sanity counts (roots, root-bearing / root-less morphology rows).
3. Top-20 roots by occurrences, all six aggregates.
4. Suspicious-value scan (non-positive aggregates, `simple > tashkeel`, maxima).
5. `EXPLAIN (ANALYZE, BUFFERS, TIMING)` full grouped aggregation + 3× wall-clock runs.
6. `words_count` vs morphology `COUNT(*)` per root (+ sample mismatches).
7. `distinct_lemmas_count` vs `COUNT(quran_lemmas WHERE root_id)` per root (+ sample mismatches).
8. `distinct_lemmas_count` vs morphology `COUNT(DISTINCT lemma_id)` per root.
9. Lemmas appearing under multiple roots (co-occurrence) + `quran_lemmas.root_id` null/non-null split.
10. Root-bearing words missing `unique_simple_word_id` / `unique_tashkeel_word_id`.
11. `quran_stems` column list + `root_id` existence probe.
12. `EXPLAIN (ANALYZE, BUFFERS, TIMING)` single-root detail aggregation (`root_id = 264`).

No Quran text was selected or printed; results are IDs and counts only.

---

## Query timing notes

| Operation | Timing |
| --- | --- |
| Full 1,642-root summary aggregation (EXPLAIN ANALYZE) | 114 ms |
| Full 1,642-root summary aggregation (warm wall-clock) | 31–36 ms (cold 75 ms) |
| Single-root worst-case detail (`root_id = 264`, 2,851 occ) | 27 ms |
| `IX_quran_word_morphology_root_id` bitmap index scan | ~0.24 ms |

All within an interactive budget; trivially within a cache-once budget.

---

## Final recommendation

**Proceed to the implementation plan.** All §8.4 assumptions are verified:

- ✅ Aggregation is fast (~30–115 ms full) and every result is sane → **compute-once + cache-whole-list confirmed**.
- ✅ `words_count` reconciles exactly (occurrences). No mismatches.
- ✅ All 50,298 root-bearing words have both `unique_simple_word_id` and `unique_tashkeel_word_id` → simple/tashkeel columns and Feature-014 navigation are reliable.
- ✅ Stems-per-root via morphology confirmed and cheap → not a blocker; derive lemmas the same way for symmetry.
- ✅ No migration/index needed — no measured evidence supports one.

**One note to lock during planning (not a blocker):** choose a single definition for the
**lemmas count** — recommended **co-occurrence** (`DISTINCT lemma_id` via morphology, which equals
the precomputed `distinct_lemmas_count`) — and use it for **both** the table column and the
الصيغ المعجمية tab so they agree. Avoid the `quran_lemmas.root_id` ownership count for the column
unless the product explicitly wants "lemmas owned by this root" (which differs for 49 roots and
omits 153 unlinked lemmas). Also correct the capability report's §2/§3 "equivalence" statement when
the plan is written.
