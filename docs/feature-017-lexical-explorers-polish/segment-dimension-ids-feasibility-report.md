# Segment-Level Dimension IDs — Feasibility & Decision Report

**Feature:** 017 — Lexical Explorers Polish
**Question:** Should we add `lemma_id` / `stem_id` / `root_id` columns to `quran_word_morphology_segments` (segment-level dimension IDs) before fixing Lemma Details (and later Stems Details)?
**Task type:** FINAL REPORT ONLY — no code, DB, migration, importer, test, frontend, or commit changes.
**Branch:** `017-lexical-explorers-polish`
**Date:** 2026-06-27
**Predecessor:** `docs/feature-017-lexical-explorers-polish/lemma-details-matching-segment-pos-report.md`

> **Confirmed fact vs recommendation** is marked throughout. `[FACT]` = verified from repository/schema. `[REC]` = recommendation/judgement. `[UNVERIFIED]` = needs a live read-only DB query (queries supplied in §3).

---

## 0. TL;DR verdict

**RECOMMENDED WITH CONDITIONS.**

- **Add `lemma_id` and `root_id`** to `quran_word_morphology_segments` (nullable, FK, indexed), populated **at import time** from the buckwalter the segments already carry. `[REC]`
- **Defer `stem_id`** — segments carry **no** stem source, `quran_stems` has **no** buckwalter, and the stem is *by definition* the `kind='STEM'` segment whose POS already equals `head_pos`, so Stems Details does **not** have the lemma-style bug. `[REC]`
- **Condition:** run the §3 resolvability queries first. Proceed only if segment `lemma_buckwalter` resolves to exactly one `quran_lemmas` row for the overwhelming majority of non-null segments, with a documented null-safe/fallback policy for the residue. `[REC]`
- Treat as a **small prerequisite migration-feature** before the Lemma Details type fix. After it lands, the two `EfLemmasReader` methods become clean integer joins with **no string matching**. `[REC]`

Why persist instead of read-time string matching: the importer must resolve the buckwalter→dimension mapping **either way**. Doing it **once at import**, guarded by hard validation checks and an explicit FK, is strictly safer, faster, and simpler to read than repeating fragile string matching on every Lemmas/Stems/Roots query. `[REC]`

---

## 1. Feasibility of adding segment dimension IDs

### 1.1 Are the source values already available during import? `[FACT]`

Yes for lemma and root; **no** for stem.

`MorphologyBulkCopier.CopySegmentsAsync` already writes per segment:

```
quran_word_id, segment_location, segment_number, kind, pos,
form_buckwalter, form_arabic_normalized, arabic_render_tier, arabic_render_source,
root_buckwalter, lemma_buckwalter, features_raw, features_json
```

So segments persist **`lemma_buckwalter`** and **`root_buckwalter`** (corpus/QAC per-segment values) but carry **no stem column at all** (no `stem_buckwalter`, no stem text).

### 1.2 Do segments already store the buckwalter bridge? `[FACT]`

| Segment column | Present? | Source |
| --- | --- | --- |
| `lemma_buckwalter` | **Yes** | QAC corpus per-segment lemma (`AlignedSegmentDto.LemmaBuckwalter`) |
| `root_buckwalter` | **Yes** | QAC corpus per-segment root |
| stem (any form) | **No** | — (segments have no stem concept except the STEM-kind segment, which is the head stem) |

### 1.3 Import order — dimensions exist before segments `[FACT]`

`EfBulkMorphologyWriter.ImportAsync` order:

```
TRUNCATE → CopyPosTags → CopyRoots → CopyLemmas → CopyStems → CopyMorphology → CopySegments
```

Dimension rows (`quran_roots`, `quran_lemmas`, `quran_stems`) and their assigned IDs exist **before** segments are written, and the in-memory `MorphologyAssembler` indices (`rootIndex`, `lemmaIndex`, `stemIndex` → `DimensionEntry.Id`) are still in scope. So an importer **can** assign segment dimension IDs without a second pass. `[FACT]`

### 1.4 The provenance gap (the real feasibility constraint) `[FACT]`

There is a source mismatch that governs how cleanly IDs resolve:

- **Dimension identity** (`quran_lemmas` / `quran_roots` / `quran_stems`) is built from the **QUL whole-word** value (`lemmas[location]`, `roots[location]`, `stems[location]`) — **one lemma/root/stem per readable word**. IDs are assigned per distinct QUL string.
- **`quran_lemmas.lemma_buckwalter` / `quran_roots.root_buckwalter`** are stored as the **STEM segment's QAC corpus buckwalter** of the first word the dimension appeared in (`DimensionEntry.AddBuckwalter(corpusLemma/corpusRoot)` where `corpusLemma = stemSegment?.LemmaBuckwalter`). They are an **attribute, not the identity**.
- **Segment `lemma_buckwalter` / `root_buckwalter`** are **QAC corpus per-segment** values.
- **`quran_stems` has no buckwalter** — identity is `stem_text` only — and segments have no stem field.

**Consequence:** mapping a segment's corpus buckwalter to a dimension ID is a **buckwalter↔buckwalter** match against a representative value, not a guaranteed key. This is feasible for lemma/root (the strings generally agree for the same lexical unit) but must be **data-verified** (§3), and is **not feasible** for stem from segment data.

### 1.5 Nullable? FK? Index? `[REC]`

- **Nullable: yes.** Many segments legitimately have no lemma/root (prefixes, particles, punctuation-like segments). `head_pos`/head IDs are already nullable; mirror that.
- **FK: yes.** Mirror the head pattern (`WordMorphologyConfiguration`): `HasOne(...).WithMany().HasForeignKey(...)`, default delete `NO ACTION`, matching the existing `quran_word_morphology.lemma_id/root_id` FKs.
- **Index: yes.** Single-column indexes on `lemma_id` and `root_id` (read path filters by these), mirroring the head indexes.

---

## 2. Persisted IDs vs read-time buckwalter matching

| Dimension | Comparison axis | A. Read-time string match (`segment.lemma_buckwalter = lemma.lemma_buckwalter`) | B. Persisted ID (`segment.lemma_id = @id`) |
| --- | --- | --- | --- |
| | **Correctness** | Depends on string equality + word-scoping; silent mis-match if buckwalter differs | Resolved once, validated by hard checks; FK-guaranteed referent |
| | **Ambiguity/collisions** | `lemma_buckwalter` non-unique (9 dup values `[FACT]`); must scope per `quran_word_id`; null on some segments | Collisions resolved deterministically at import (one tie-break, recorded) |
| | **Performance** | String compare + join per query, per page, ×3 explorers | Indexed integer join; trivial |
| | **Read simplicity (Lemmas/Stems/Roots)** | Every reader re-implements word-scoped string matching + fallback | `WHERE segment.lemma_id = @id` — same shape the head already uses |
| | **Migration/import complexity** | None now (read-only) | One migration + importer population + reseed/backfill |
| | **Testability** | Hard to assert; correctness hidden in query | Import hard-checks (`SEG-*-RESOLVES`, `SEG-DIM-CONSISTENT`) assert it once |

**Net `[REC]`:** the matching problem exists in **both** options — the difference is *where* and *how often* it is paid. Option B pays it **once**, under validation, with FK integrity and cheap reads. Option A pays a fragile version **on every read** across three explorers forever. B is the better architecture **if** §3 confirms clean resolvability.

---

## 3. Data uniqueness / collision analysis

> **Live DB status: UNAVAILABLE during this audit.** `[FACT]` The running PostgreSQL on `localhost:5432` rejected every credential in `appsettings.json`, `appsettings.Development.json`, and the DataImporter `appsettings.json` (`FATAL: password authentication failed for user "postgres"`). Figures below are taken from schema/config and the Feature 016 capability report; rows marked `[UNVERIFIED]` require the SQL in §3.3.

### 3.1 What schema/config already proves `[FACT]`

- `quran_lemmas`: unique indexes on `lemma_text` and `first_word_order_in_mushaf`. **No unique index on `lemma_buckwalter`** (nullable, non-unique). Capability report: **4,793 lemmas, 9 duplicate `lemma_buckwalter` values**.
- `quran_stems`: unique on `stem_text` and `first_word_order_in_mushaf`. **No `*_buckwalter` column.** 12,108 stems.
- `quran_roots`: unique on `root_text` and `first_word_order_in_mushaf`. **No unique index on `root_buckwalter`**, but capability report shows **0 duplicate `root_buckwalter` values** (de-facto unique). 1,642 roots.
- `quran_word_morphology_segments`: 128,219 rows; `lemma_buckwalter`, `root_buckwalter` nullable.

### Table 2 — Dimension key uniqueness

| Dimension | Total rows | Distinct Buckwalter keys | Duplicate keys count | Examples | Risk level | Recommendation |
| --- | ---: | ---: | ---: | --- | --- | --- |
| `quran_lemmas` | 4,793 `[FACT]` | `[UNVERIFIED]` (≈4,784) | **9 dup values** `[FACT]` | `[UNVERIFIED]` — list via §3.3 Q1 | **Medium** — non-unique + nullable | Persist `lemma_id`, resolve word-scoped + tie-break; FK |
| `quran_stems` | 12,108 `[FACT]` | n/a — no buckwalter `[FACT]` | n/a | n/a | **High (for segment linking)** | **Do not** add `stem_id` from segments; defer |
| `quran_roots` | 1,642 `[FACT]` | `[UNVERIFIED]` (≈1,642) | **0 dup values** `[FACT]` | none | **Low** | Cleanest key; persist `root_id`, FK |

### Table 3 — Segment resolvability

| Dimension | Segment source column | Non-null segment values | Resolvable values | Unresolved values | Ambiguous values | Null-safe behavior | Recommendation |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| Lemma | `segment.lemma_buckwalter` | `[UNVERIFIED]` Q2 | `[UNVERIFIED]` Q3 | `[UNVERIFIED]` Q4 | `[UNVERIFIED]` Q5 (dup-buckwalter words) | null buckwalter → null `lemma_id` | Persist with hard checks + fallback |
| Root | `segment.root_buckwalter` | `[UNVERIFIED]` Q6 | `[UNVERIFIED]` (high, 0 dup key) | `[UNVERIFIED]` | ~0 expected | null → null `root_id` | Persist; lowest risk |
| Stem | — (no column) | 0 `[FACT]` | 0 | — | — | always null except STEM segment = head stem | Defer; not derivable from segments |

### 3.3 Exact verification SQL (run read-only before implementing) `[REC]`

```sql
-- Q1: duplicate lemma_buckwalter values + examples
SELECT lemma_buckwalter, COUNT(*) AS n, array_agg(id ORDER BY id) AS lemma_ids
FROM quran_lemmas
WHERE lemma_buckwalter IS NOT NULL
GROUP BY lemma_buckwalter HAVING COUNT(*) > 1
ORDER BY n DESC;

-- Q2: segments with a non-null lemma_buckwalter
SELECT COUNT(*) AS seg_total,
       COUNT(*) FILTER (WHERE lemma_buckwalter IS NOT NULL) AS seg_with_lemma_bw,
       COUNT(*) FILTER (WHERE root_buckwalter  IS NOT NULL) AS seg_with_root_bw
FROM quran_word_morphology_segments;

-- Q3/Q4: segment lemma_buckwalter that resolves to exactly one / zero quran_lemmas rows
SELECT
  COUNT(*) FILTER (WHERE m.match_count = 1) AS resolves_unique,
  COUNT(*) FILTER (WHERE m.match_count = 0) AS unresolved,
  COUNT(*) FILTER (WHERE m.match_count > 1) AS ambiguous_by_buckwalter
FROM (
  SELECT s.id,
         (SELECT COUNT(*) FROM quran_lemmas l WHERE l.lemma_buckwalter = s.lemma_buckwalter) AS match_count
  FROM quran_word_morphology_segments s
  WHERE s.lemma_buckwalter IS NOT NULL
) m;

-- Q5: words containing >1 segment that map to the SAME lemma (fan-out risk)
SELECT s.quran_word_id, l.id AS lemma_id, COUNT(*) AS matching_segments
FROM quran_word_morphology_segments s
JOIN quran_lemmas l ON l.lemma_buckwalter = s.lemma_buckwalter
GROUP BY s.quran_word_id, l.id
HAVING COUNT(*) > 1
ORDER BY matching_segments DESC
LIMIT 50;

-- Q6: root resolvability (expected near-perfect; root_buckwalter has 0 dups)
SELECT
  COUNT(*) FILTER (WHERE r.match_count = 1) AS resolves_unique,
  COUNT(*) FILTER (WHERE r.match_count = 0) AS unresolved,
  COUNT(*) FILTER (WHERE r.match_count > 1) AS ambiguous
FROM (
  SELECT s.id,
         (SELECT COUNT(*) FROM quran_roots r WHERE r.root_buckwalter = s.root_buckwalter) AS match_count
  FROM quran_word_morphology_segments s
  WHERE s.root_buckwalter IS NOT NULL
) r;

-- Q7: head-lemma vs segment-lemma coverage of selected lemma (e.g. لا) — informs §7 A/B decision
-- Replace :lemmaId with the لا lemma id.
SELECT
  (SELECT COUNT(*) FROM quran_word_morphology wm WHERE wm.lemma_id = :lemmaId)                                  AS head_lemma_words,
  (SELECT COUNT(DISTINCT s.quran_word_id)
     FROM quran_word_morphology_segments s
     JOIN quran_lemmas l ON l.id = :lemmaId
    WHERE s.lemma_buckwalter = l.lemma_buckwalter)                                                              AS segment_lemma_words;
```

If Q3 `resolves_unique` ≫ `unresolved`+`ambiguous`, condition is met. If `ambiguous_by_buckwalter` or Q5 fan-out is material, the importer must add a tie-break (prefer `kind='STEM'`, then `lemma_text` agreement, then lowest `segment_number`) and a hard check.

---

## 4. Correct target model (if approved)

### Table 1 — Current schema vs proposed schema

| Table | Current relevant columns | Missing columns | Proposed columns | Nullable? | FK? | Index? | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `quran_word_morphology_segments` | `quran_word_id, segment_number, kind, pos, lemma_buckwalter, root_buckwalter, form_buckwalter` | `lemma_id, root_id` (`stem_id` intentionally omitted) | **`lemma_id`**, **`root_id`** | **Yes** | **Yes** → `quran_lemmas.id`, `quran_roots.id` (`NO ACTION`) | **Yes** (one each) | Populated at import from existing segment buckwalter |
| `quran_word_morphology` (head) | `lemma_id, stem_id, root_id, head_pos` | — | unchanged | — | — | — | **Head dimension IDs stay as-is** (do not touch) |
| `quran_lemmas` | `id, lemma_text, lemma_buckwalter, root_id` | — | unchanged | — | — | — | `lemma_buckwalter` non-unique — used only as import-time bridge |
| `quran_roots` | `id, root_text, root_buckwalter` | — | unchanged | — | — | — | `root_buckwalter` de-facto unique (0 dups) |
| `quran_stems` | `id, stem_text` (no buckwalter) | — | unchanged | — | — | — | No buckwalter ⇒ no segment-level stem link |

- **Columns to add:** `lemma_id` ✅, `root_id` ✅, `stem_id` ❌ (defer).
- **Nullable:** yes (both).
- **FK targets:** `quran_lemmas.id`, `quran_roots.id`; delete behavior `NO ACTION` (mirror head).
- **Indexes:** `IX_quran_word_morphology_segments_lemma_id`, `IX_quran_word_morphology_segments_root_id`.
- **EF changes:** add `LemmaId`/`RootId` (+ optional navigations) to `WordMorphologySegment`; map columns + FKs + indexes in `WordMorphologySegmentConfiguration`.
- **Migration name `[REC]`:** `AddSegmentDimensionIds` (or `20260628_AddSegmentLemmaRootIdsToMorphologySegments`).
- **Population:** **import-morphology only** (segment copy step), with an optional **data-only backfill** path (UPDATE … FROM by buckwalter, word-scoped) for environments that must avoid a full reseed — possible *because the buckwalter source already lives in the table*.
- **Head lemma/stem/root: unchanged.** `[REC]`

---

## 5. Importer changes

### 5.1 Where to populate `[FACT]`/`[REC]`

`MorphologyAssembler.Assemble(...)` already, per word, holds: the ordered segments (each with `Pos`, `LemmaBuckwalter`, `RootBuckwalter`, `Kind`) **and** the dimension indices `lemmaIndex` / `rootIndex` (`Buckwalter → DimensionEntry.Id`). The cleanest design `[REC]`:

1. When emitting each `AlignedSegmentDto`, resolve:
   - `segment.LemmaId = lemmaIndex` lookup keyed by the **dimension's stored buckwalter** matching `segment.LemmaBuckwalter` (word-scoped; tie-break STEM→min `segment_number`); else null.
   - `segment.RootId` similarly from `rootIndex` via `segment.RootBuckwalter`; else null.
2. `CopySegmentsAsync` writes the two new integer columns alongside the existing buckwalter columns.

> Note: `lemmaIndex`/`rootIndex` are currently keyed by the **QUL** value (`qulLemma`/`qulRoot`), while segment buckwalter is **corpus**. The importer must build (or reuse) a **corpus-buckwalter → dimension-id** map. The dimension already stores one representative corpus buckwalter (`DimensionEntry.Buckwalter`); the safest implementation builds an explicit `Dictionary<string,int>` from `ResolvedLemmas`/`ResolvedRoots` on `lemma_buckwalter`/`root_buckwalter`, then resolves each segment. `[REC]`

### 5.2 Rules `[REC]`

- **Dimensions before segments:** already guaranteed (§1.3).
- **Mapping dictionaries:** partially exist (`lemmaIndex`/`rootIndex` by QUL key); a buckwalter-keyed map must be derived from `ResolvedLemmas`/`ResolvedRoots`.
- **Unresolved values:** if a non-null segment buckwalter resolves to **zero** dimension rows → that is a **hard-check failure** to investigate (provenance drift), not a silent null. If it resolves to **>1** → tie-break deterministically and log.
- **Null source values stay null:** null/empty `lemma_buckwalter`/`root_buckwalter` ⇒ null `lemma_id`/`root_id`. Never invent.
- **`stem_id`: not populated** (no segment stem source). `[REC]`
- **Quran text / source files untouched:** all values come from already-imported segment columns; **no** corpus/QUL source edits, no `IMPORT-SOURCE-UNCHANGED` impact. `[FACT]`

---

## 6. Validation / hard checks `[REC]`

Add to `MorphologyValidationRunner` (run inside the import transaction, fail ⇒ rollback):

| Check ID | Assertion |
| --- | --- |
| `SEG-LEMMA-ID-RESOLVES` | Every segment with non-null `lemma_buckwalter` has non-null `lemma_id` (or is on an explicit allow-list of known-unresolvable buckwalters). |
| `SEG-ROOT-ID-RESOLVES` | Every segment with non-null `root_buckwalter` has non-null `root_id`. |
| `SEG-DIM-ID-CONSISTENT` | For every segment with `lemma_id`/`root_id`, the referenced dimension row's `lemma_buckwalter`/`root_buckwalter` equals the segment's value. |
| `SEG-DIM-NO-FANOUT` | One segment resolves to **at most one** lemma row and one root row (no multi-id assignment). |
| `SEG-DIM-NULL-SAFE` | Null/empty segment buckwalter ⇒ null ID (no fabricated dimension link). |
| `SEG-STEM-ID-ABSENT` | (If `stem_id` column is *not* added) no-op; if later added, every non-STEM segment has null `stem_id`. |
| `IMPORT-SOURCE-UNCHANGED` | Existing check still passes — segment-ID population reads only already-imported columns; manifest sha256/size unaffected. |

---

## 7. Effect on Lemmas Explorer (after the model exists)

After `segment.lemma_id` exists, the two `EfLemmasReader` methods become clean integer joins `[REC]`:

- **Type distribution:** for each occurrence, the matching segment is `segment WHERE quran_word_id = w.id AND lemma_id = @lemmaId`; group by `segment.pos`. No string matching.
- **Ayah type filter:** filter occurrences by `segment.lemma_id = @id AND (typeCode IS NULL OR segment.pos = @typeCode)`.
- **Highlight:** matched word ids from the same `segment.lemma_id (+ optional pos)` predicate.
- **No frontend logic change** (frontend renders API `arabicLabel`/`code`, echoes `code` as `typeCode`). `[FACT]`
- **No POS-seed / i‘rab-seed change** (only the *source field* of the type changes; label text is the separate `pos-segment-type-label-cleanup-plan.md`). `[FACT]`

### 7.1 Occurrence-set decision — **A now, B as the deliberate follow-up** `[REC]`

| Option | Definition | Effect |
| --- | --- | --- |
| **A** | Keep the existing **head-lemma** occurrence set (`quran_word_morphology.lemma_id = @id`); only correct the **type axis** to the matching segment's POS. | Minimal, fixes the reported wrong-label bug. **No count churn** for STEM-headed lemmas; counts shift only where `head_pos` was a non-matching first-segment particle. |
| **B** | Define occurrences by **segments** (`segment.lemma_id = @id`). | Also surfaces non-head occurrences (e.g. the لا inside أَلَّا when that word's head lemma ≠ لا). **Changes every count** (occurrences/ayahs/surahs/words). Larger product/validation change. |

**Recommendation:** Adopt **Option A immediately** (it is the precise fix for the reported type-label bug and avoids page-wide count churn). Plan **Option B as a separate, explicitly-scoped follow-up** once `segment.lemma_id` exists and product confirms that "a لا particle inside أَلَّا is a لا occurrence." Adding `segment.lemma_id` now makes B cheap to adopt later **without another migration**. This staging is unambiguous: **A is the next implementation step; B is a future product decision, not part of the type-label fix.**

> Verify the A-vs-B materiality for لا with §3.3 **Q7** (head-lemma vs segment-lemma word counts). If the two counts are equal, A and B coincide for لا and the decision is moot for that lemma.

---

## 8. Effect on Stems Explorer (later) — not planned here

- **Would `stem_id` allow the same clean fix?** It is **largely unnecessary.** `[FACT]/[REC]` The stem of a word is, by construction, the `kind='STEM'` segment, and `head_pos = stemSegment?.Pos` (from `MorphologyAssembler`). So for a stem occurrence the "matching segment POS" **already equals `head_pos`** — the lemma-style head/segment divergence (caused by *non-STEM* particle lemmas) **does not occur** for stems.
- **Follow-up?** Yes — a **separate Stems audit** should confirm Stems Details is correct (or find a different, smaller issue). Do **not** bundle a Stems fix or `stem_id` into this work.
- **Special stem risks:** `quran_stems` has **no buckwalter** and segments have **no stem field**, so a segment-level `stem_id` could only ever be derived for the STEM segment as the head stem — duplicating existing head data at low value. If a future Stems need is proven, source it from the STEM segment + head `stem_id`, not from a new buckwalter bridge.

---

## 9. Effect on Roots — include `root_id` now (cheap, cleanest)

- Roots already exist as head/root data, and Roots Details largely keys off `quran_word_morphology.root_id`. `[FACT]`
- Segment-level `root_id` is nonetheless **valuable for precise segment filtering** (e.g., a word whose root sits on a non-head segment) and is the **lowest-risk** of the three keys: `root_buckwalter` has **0 duplicates** `[FACT]`.
- **Decision `[REC]`:** include `root_id` **now**, alongside `lemma_id`, in the same migration/import change. Marginal cost (one column, one FK, one index, one resolver), and it future-proofs Roots segment filtering without a second migration. It is **not blocking** for the Lemmas fix, but bundling avoids a repeat migration.

---

## 10. Implementation recommendation

### Verdict: **RECOMMENDED WITH CONDITIONS** `[REC]`

### Table 4 — Implementation impact

| Area | Files/modules affected | Expected change | Risk | Tests needed | Notes |
| --- | --- | --- | --- | --- | --- |
| Domain | `WordMorphologySegment.cs` | add `int? LemmaId`, `int? RootId` (+ optional navs) | Low | entity shape | mirror head |
| EF config | `WordMorphologySegmentConfiguration.cs` | map columns, FKs (`NO ACTION`), indexes | Low | model/migration snapshot | mirror `WordMorphologyConfiguration` |
| Migration | new `AddSegmentDimensionIds` | add 2 nullable FK columns + indexes | Low | migration applies/reverts | no data in migration (populate via import) or optional backfill UPDATE |
| Importer | `MorphologyAssembler`, `MorphologyBulkCopier.CopySegmentsAsync`, resolver dict | resolve + write `lemma_id`/`root_id` | **Medium** | resolver unit tests; import hard checks | provenance buckwalter map (§5.1) |
| Validation | `MorphologyValidationRunner` | add `SEG-*` checks (§6) | Low | check unit/integration | rollback on fail |
| Read model (later) | `EfLemmasReader` (2 methods) | integer joins replace head_pos/string match | Low | Lemmas read tests | §7; separate task |
| Frontend | none | none | None | regression only | renders API values |

### Table 5 — Decision matrix

| Option | Pros | Cons | Correctness risk | Performance risk | Implementation cost | Recommended? |
| --- | --- | --- | --- | --- | --- | --- |
| **Persist `lemma_id`+`root_id`** (defer `stem_id`) | Resolve once; FK integrity; hard-checked; fast indexed reads; enables Option B; clean Lemmas/Roots readers | One migration + importer change + reseed/backfill | **Low** (validated at import) | **Low** | **Medium (one-time)** | ✅ **Yes (with §3 condition)** |
| Persist all three incl. `stem_id` | Symmetry | `stem_id` has no segment source; near-useless; fabrication risk | Med (stem) | Low | Higher | ❌ No — defer `stem_id` |
| Read-time buckwalter matching (no migration) | Zero schema change now | Fragile string match on every read ×3 explorers; non-unique/null; no FK; hard to test; blocks clean B | **Medium-High (recurring)** | Medium | Low now, high lifetime | ❌ Not as the durable fix |
| Do nothing | — | Bug persists | High | — | — | ❌ No |

### A. Final verdict
**RECOMMENDED WITH CONDITIONS.** Add `lemma_id` and `root_id` to `quran_word_morphology_segments` (nullable, FK, indexed), populated at import from existing segment buckwalter, guarded by `SEG-*` hard checks. **Defer `stem_id`.** Condition: pass the §3.3 resolvability queries first (especially Q3/Q5 for lemma). This is the correct architectural fix and should be a **small prerequisite migration-feature** *before* the Lemma Details type fix.

### B. Exact recommended next step
1. Run §3.3 Q1–Q7 read-only on the live DB; confirm lemma resolvability and capture the A-vs-B materiality (Q7).
2. If clean: implement the `AddSegmentDimensionIds` migration + EF config + importer resolver + `SEG-*` checks (this prerequisite feature only — **not** the readers yet).
3. **Reseed locally** with `import-morphology --force` then downstream `generate-i3rab --force` (cascade); for a shared DB, use the optional data-only backfill UPDATE instead of a full reseed.
4. Then implement the Lemma Details type fix as **Option A** over `segment.lemma_id` (separate task).

### C. Follow-up implementation prompt draft

> **Task:** Implement segment-level dimension IDs (prerequisite for Feature 017 Lemma Details fix).
> **Scope:** Add nullable `lemma_id` and `root_id` to `quran_word_morphology_segments` (FK → `quran_lemmas.id` / `quran_roots.id`, `NO ACTION`; single-column indexes). **Do NOT add `stem_id`.** Update `WordMorphologySegment` + `WordMorphologySegmentConfiguration`, create migration `AddSegmentDimensionIds`. In the morphology importer, build a corpus-buckwalter→dimension-id map from `ResolvedLemmas`/`ResolvedRoots` and populate the two segment IDs in `CopySegmentsAsync` (word-scoped; tie-break STEM→min segment_number; null source ⇒ null id; never fabricate). Add hard checks `SEG-LEMMA-ID-RESOLVES`, `SEG-ROOT-ID-RESOLVES`, `SEG-DIM-ID-CONSISTENT`, `SEG-DIM-NO-FANOUT`, `SEG-DIM-NULL-SAFE`; keep `IMPORT-SOURCE-UNCHANGED` green. Do **not** change head `quran_word_morphology` IDs, POS/i‘rab seeds, frontend, or the Lemma readers in this task. Before coding, run §3.3 Q1–Q7 and abort if lemma resolvability is poor. Reseed (`import-morphology --force` then `generate-i3rab --force`) or apply the optional data-only backfill on shared DBs. Add tests for the resolver and the new hard checks. **No Stems work. No Quran text mutation.**

### D. Mem0 update summary

**Stable decision to save `[REC]`:**
- "Feature 017 — approved direction: add nullable FK+indexed `lemma_id` and `root_id` to `quran_word_morphology_segments`, populated at import from existing segment buckwalter with `SEG-*` hard checks; **defer `stem_id`** (no segment stem source; stem POS already equals head_pos). Lemma Details fix = Option A (head-lemma occurrence set, segment-POS type) over `segment.lemma_id`; Option B (segment-defined occurrences) is a later product decision. Verdict: RECOMMENDED WITH CONDITIONS, pending live resolvability queries."

**Old/obsolete memories to delete/update:**
- **Delete (misleading):** memory `46f1b85d-5613-420b-9664-cabb7581cf7b` — claims the prior session "modified three files … `appsettings.Development.js` and `lemmas-explorer-page.component.html`." That session was **report-only**; no frontend or appsettings file was modified. The `files_touched` lists on the related session-summaries (`457ebaa8`, `ad4af33d`, `ca3b17f1`, `84842b45`, `b0fca3c0`, `080a107f`) are similarly inaccurate, but their **textual technical claims are correct** — keep the text, do not trust their `files_touched`.
- **Keep (accurate):** the head_pos-fallback fact (`24764297`), the no-`lemma_id`-on-segments fact (`84842b45`), and the two-method diagnosis (`b0fca3c0`).
- **Update nuance:** memory `080a107f` ("no migration/importer/seed needed; only two reader methods") describes the *minimal read-time* fix from the predecessor report. This report **supersedes** it for the durable solution: the **recommended** path now **does** add a migration + importer change (segment IDs) as a prerequisite. Not wrong for the quick fix; note it is superseded by the persisted-ID decision.

---

## 11. Confirmed facts vs recommendations (separation)

**Confirmed `[FACT]`:** segment COPY columns include `lemma_buckwalter`/`root_buckwalter` but no stem; dimensions are copied before segments; `quran_stems` has no buckwalter; `quran_lemmas.lemma_buckwalter` is non-unique (9 dups) and not uniquely indexed; `quran_roots.root_buckwalter` has 0 dups; head `quran_word_morphology` already uses nullable FK+indexed `lemma_id/stem_id/root_id`; `head_pos = stemSegment?.Pos ?? first segment`; live DB was unreachable this session.

**Recommendations `[REC]`:** add `lemma_id`+`root_id` (defer `stem_id`); nullable/FK/indexed; import-time population + `SEG-*` checks; Option A now / Option B later; reseed-or-backfill; treat as a small prerequisite feature.

**Unverified `[UNVERIFIED]`:** exact distinct/duplicate buckwalter counts; segment resolvable/unresolved/ambiguous counts; لا head-vs-segment occurrence delta — all pending §3.3 queries on the live DB.

*Report only. No code, DB, migration, importer, test, frontend, or commit changes were made.*
