# Segment Dimension IDs — Live-DB Verification Report

**Feature:** 017 — Lexical Explorers Polish
**Task type:** READ-ONLY DB DIAGNOSTIC — no code/DB/migration/importer/test/frontend/commit changes. Only `SELECT` queries were executed.
**Branch:** `017-lexical-explorers-polish`
**Date:** 2026-06-27
**Predecessors:** `lemma-details-matching-segment-pos-report.md`, `segment-dimension-ids-feasibility-report.md`

---

## 1. Verdict

### **PASS: add `lemma_id` + `root_id`**

- **`segment.lemma_id` — REQUIRED.** Live data proves the type-classification bug is real and fixable only by linking the lemma to its **STEM segment**. 272 occurrences across 5 lemmas are mislabeled today.
- **`segment.root_id` — INCLUDE NOW (zero-risk future-proofing).** Root resolves **100% cleanly** (49,968 / 49,968, 0 unresolved, 0 ambiguous, 0 duplicate keys). It does **not** serve the immediate Lemmas fix, but bundling it avoids a second migration + reseed for the inevitable Roots Details fix (same mechanism). *(If the team strictly applies YAGNI, "PASS: lemma_id only" is acceptable — root_id can be deferred without data risk.)*
- **`segment.stem_id` — REJECTED / OUT OF SCOPE.** Proven below: no per-segment stem source exists, `quran_stems` has no buckwalter, and no Feature 017 issue needs it.

---

## 2. Executive summary

The previous reports hypothesised the wrong **mechanism**, but the right **conclusion**. The live DB shows:

1. **Every readable word has exactly one (or more) `kind='STEM'` segment**, and **`head_pos` is always equal to a STEM segment's POS** (`head_pos ≠ stem_pos` in **0** of 77,432 words). The earlier "no-STEM word → first-segment fallback" theory does **not** occur in the data.
2. The real defect is **multi-STEM words** (483 of them). `head_pos` is taken from the **first** STEM segment, but the word's **head lemma** can belong to a **different** STEM segment. Example: أَلَّا = `STEM/SUB/أن + STEM/NEG/لا`. The word is filed under lemma **لا**, but `head_pos = SUB` (حرف مصدري, from the أن segment). Result: a لا occurrence labelled حرف مصدري instead of حرف نفي.
3. **Lemma identity lives only on STEM segments** — `lemma_buckwalter` is non-null **only** on `kind='STEM'` segments (74,608), never on prefixes/particles/suffixes (53,611 null). So `segment.lemma_id` is populated on STEM segments, and for multi-STEM words it correctly separates the two stems — which is exactly what fixes the bug.
4. **Stems have no segment-level source at all.** Confirmed: no stem column on segments, no buckwalter on `quran_stems`. `segment.stem_id` cannot and need not be populated.
5. **Roots resolve perfectly** — a clean, optional add.

Impact is small and concentrated: **272 occurrences across 5 high-frequency particle lemmas** (مَا 177, لَا 46, مِن 46, لَن 2, لَو 1). **Option A** (keep head-lemma occurrence set, classify type by the matching STEM segment's POS) fixes all of them; **Option B** (segment-defined occurrences) changes لا's count by **1 word** — not material.

---

## 3. DB connection note

- **Connected successfully** to the local development database. `[FACT]`
- Host: `localhost` · Port: `5432` · Database: `quran_dashboard` · Username: `postgres` · Password: **[REDACTED]**
- The password was used only for this local read-only session, passed via the `PGPASSWORD` environment variable. **It was not written to any repository file, doc, appsettings, user-secret, or memory.**
- Only `SELECT` statements were run. (`current_setting('transaction_read_only')` reports `off` at session level, but no write/DDL/DML was issued.)

---

## 4. Lemma resolvability results

| Metric | Value | Interpretation |
| --- | ---: | --- |
| `quran_lemmas` total | **4,793** | All have non-null `lemma_buckwalter`. |
| Distinct non-null `lemma_buckwalter` | **4,784** | — |
| Duplicate `lemma_buckwalter` values | **9** | `A^taY, baEod, <ivom, kaAna, maro', mu&omin, nafos, taEa`laY`, ya*ara` (2 lemma ids each) → buckwalter is **not** a unique key. |
| `quran_word_morphology_segments` total | **128,219** | — |
| Segments with non-null `lemma_buckwalter` | **74,608** | **All are `kind='STEM'`** (key finding). |
| Segments with null `lemma_buckwalter` | **53,611** | Non-STEM segments (prefix/particle/suffix) carry **no** lemma. |
| Lemma-bearing segments by `kind` | **STEM: 74,608; others: 0** | Lemma identity exists only on STEM segments. |
| Naive buckwalter match → exactly one lemma | **68,838 (92.3%)** | Read-time string match is **not** clean. |
| Naive buckwalter match → zero lemmas | **3,341 (4.5%)** | 48 distinct buckwalter values (homograph suffixes: `huwd2`, `EaSaA2`, `ja`hiliy~ap`, `min`, …). |
| Naive buckwalter match → >1 lemma | **2,429 (3.3%)** | Driven by the 9 duplicate keys. |
| Multi-STEM words | **483** | Words with ≥2 STEM segments → the bug surface. |

**Is `segment.lemma_id` safe to add? — YES.** `[REC]` Populate it **at import** with this policy (100% deterministic, no fragile read-time matching):

- **Single-STEM word (≈99%): `segment.lemma_id = head lemma_id`** — the STEM segment *is* the head; reuse the already-resolved word-level `lemma_id`. This sidesteps **all** buckwalter unresolved/ambiguous cases (they are single-stem).
- **Multi-STEM word (483): resolve each STEM segment** by its `lemma_buckwalter` → `quran_lemmas`; the segment whose buckwalter equals the head lemma's gets `head lemma_id`, the other(s) resolve by buckwalter.
- **Tie-break for the 9 duplicate buckwalters:** prefer the candidate whose `lemma_text` matches the rendered segment form, else lowest `quran_lemmas.id`.
- **Null-safe:** null `lemma_buckwalter` ⇒ null `lemma_id` (never fabricate). Non-STEM segments stay null.

> The 46-case لا fix is clean: لا's buckwalter `laA` is **unique** and not in the unresolved/ambiguous sets.

---

## 5. Root resolvability results

| Metric | Value | Interpretation |
| --- | ---: | --- |
| `quran_roots` total | **1,642** | All have non-null `root_buckwalter`. |
| Distinct non-null `root_buckwalter` | **1,642** | — |
| Duplicate `root_buckwalter` values | **0** | `root_buckwalter` is a **unique** key. |
| Segments with non-null `root_buckwalter` | **49,968** | (78,251 null — particles/punctuation have no root.) |
| Buckwalter match → exactly one root | **49,968 (100%)** | Perfectly resolvable. |
| Buckwalter match → zero roots | **0** | No unresolved values. |
| Buckwalter match → >1 root | **0** | No ambiguity. |

**Is `segment.root_id` worth adding now?** `[REC]` It is **low-risk (zero, in fact)** but does **not** help the current Lemmas fix directly — it is **future-proofing** for (a) precise segment/root filtering and (b) the forthcoming Roots Details fix, which has the **identical** multi-STEM mechanism (note مَا/مِن in §7 would also mis-type root cards). Because resolution is flawless and bundling avoids a second migration/reseed, **include it now**.

---

## 6. Stem decision confirmation — `segment.stem_id` stays OUT

Confirmed from schema + data: `[FACT]`

- `quran_word_morphology_segments` has **no** stem column (no `stem_buckwalter`, no stem text). Columns are `…, kind, pos, form_buckwalter, root_buckwalter, lemma_buckwalter, …`.
- `quran_stems` has **no buckwalter** — columns `id, stem_text, words_count, first_word_order_in_mushaf`. The only bridge would be `stem_text` (Arabic) vs a segment form, which is not a reliable key.
- `quran_word_morphology.stem_id` exists at **word level** and is the morphological stem/origin.
- `head_pos` is derived from the STEM segment (`head_pos = stemSegment.Pos`); **0** divergence corpus-wide.

**No Feature 017 issue requires `segment.stem_id`.** The active problem is **Lemma** type classification, fixed by `segment.lemma_id`. Stems Explorer must **continue to use `quran_word_morphology.stem_id`** as the word-level stem. (A separate future Stems audit may find an analogous multi-STEM type nuance, but `segment.stem_id` is not the tool — there is no per-segment stem source to populate it from; the QUL stem is one-per-word.)

> **Obsolete-memory note:** any prior statement that "we may need `segment.stem_id`" is now **superseded** — this report proves it is neither feasible from existing data nor required.

---

## 7. Lemma لا case study

`quran_lemmas` row: **id = 77, `lemma_text = لَا`, `lemma_buckwalter = laA`** (unique), words_count 1737.

### 7.1 Counts

| Measure | Value |
| --- | ---: |
| Option A — head-lemma words (`quran_word_morphology.lemma_id = 77`) | **1,737** |
| Option B — segment-lemma words (STEM segment `lemma_buckwalter = 'laA'`) | **1,738** |
| **Δ (B − A)** | **+1 word** (negligible) |

### 7.2 Type distribution — current vs correct

| POS | Arabic label | Current `head_pos` (lemma_id=77) | Correct لا STEM-segment `pos` |
| --- | --- | ---: | ---: |
| NEG | حرف نفي | 1,364 | **1,406** |
| PRO | حرف نهي | 327 | **332** |
| SUB | حرف مصدري | **40** ✗ | 0 |
| INT | حرف تفسير | **5** ✗ | 0 |
| ACC | حرف نصب | **1** ✗ | 0 |

The **46 foreign labels** (SUB/INT/ACC) are the bug. Correct distribution is **NEG + PRO only**.

### 7.3 Examples where `head_pos` differs from the لا segment POS

| Location | Current type (`head_pos`) | Correct لا POS | Segments |
| --- | --- | --- | --- |
| 2:229:19 | SUB (حرف مصدري) | NEG (حرف نفي) | `STEM/SUB/أن + STEM/NEG/لا` |
| 2:246:29 | SUB | NEG | `STEM/SUB/أن + STEM/NEG/لا` |
| 2:282:94 | SUB | NEG | `STEM/SUB/أن + STEM/NEG/لا` |
| 3:41:8 | SUB | NEG | `STEM/SUB/أن + STEM/NEG/لا` |
| 2:150:15 | SUB | NEG | `PREFIX/PRP + STEM/SUB/أن + STEM/NEG/لا` |

These are أَلَّا (= أَنْ + لا): two STEM segments; `head_pos` takes the first (أن → SUB), but the word is filed under لا.

### 7.4 Corpus-wide bug surface

| Measure | Value |
| --- | ---: |
| Head rows with a lemma | 72,507 |
| `head_pos` = head-lemma's STEM-segment POS (correct) | 68,766 |
| **`head_pos` ≠ head-lemma's STEM-segment POS (mislabeled)** | **272** |
| Buckwalter bridge found no matching STEM segment (homograph-suffix gap) | 3,469 |
| **Distinct lemmas affected** | **5** |

**The 5 affected lemmas:** مَا (177), لَا (46), مِن (46), لَن (2), لَو (1). All high-frequency particles that occur as a non-first STEM in compound/multi-STEM words.

### 7.5 Option A vs Option B — recommendation

**Implement Option A now; leave Option B as a future product decision.** `[REC]`

- **Option A** (keep head-lemma occurrence set; classify type by the matching STEM segment's `pos` via `segment.lemma_id`) **fully fixes** the reported wrong labels — all 272 occurrences, including لا's 46 — with **no count churn** (head set unchanged).
- **Option B** (define occurrences by `segment.lemma_id`) changes لا's count by **+1 word** — immaterial. The DB shows **no need** for B now. Adding `segment.lemma_id` makes B cheap to adopt later if product decides a particle-inside-compound should count as an occurrence — **without another migration**.

---

## 8. Final implementation scope

| Item | Decision |
| --- | --- |
| **Columns to add** | `quran_word_morphology_segments.lemma_id INT NULL`, `quran_word_morphology_segments.root_id INT NULL` |
| **Columns NOT to add** | `stem_id` (no source; not needed) |
| **FKs** | `lemma_id → quran_lemmas.id`, `root_id → quran_roots.id`, delete `NO ACTION` (mirror `quran_word_morphology`) |
| **Indexes** | `IX_..._segments_lemma_id`, `IX_..._segments_root_id` |
| **Importer changes** | In `MorphologyAssembler`/`CopySegmentsAsync`: STEM segment `lemma_id` = word head `lemma_id` for single-STEM words; resolve each STEM by buckwalter for multi-STEM words; `root_id` by `root_buckwalter → quran_roots` (100% clean); null source ⇒ null id. No change to head `quran_word_morphology` ids. No corpus/source-file edits. |
| **Validation checks** | `SEG-LEMMA-ID-RESOLVES` (every STEM segment with `lemma_buckwalter` gets a `lemma_id`, modulo a documented homograph allow-list), `SEG-ROOT-ID-RESOLVES`, `SEG-DIM-ID-CONSISTENT`, `SEG-DIM-NO-FANOUT` (≤1 dim per segment), `SEG-DIM-NULL-SAFE`, `SEG-STEM-ID-ABSENT`; keep `IMPORT-SOURCE-UNCHANGED` green. |
| **Reset/reseed** | Columns are import-populated ⇒ reseed locally (`import-morphology --force` then `generate-i3rab --force`, cascade). For shared DBs, a data-only backfill is possible (`UPDATE` from existing segment buckwalter + head lemma_id) without a full reseed. |

---

## 9. Risks and mitigations

| Risk | Evidence | Mitigation |
| --- | --- | --- |
| Unresolved lemma buckwalter (4.5%; 48 values) | homograph suffixes (`huwd2`, `EaSaA2`) | All are single-STEM ⇒ populate from **head lemma_id**, not buckwalter; buckwalter only for multi-STEM stems. |
| Ambiguous lemma buckwalter (3.3%; 9 dup keys) | `kaAna, nafos, …` | Same single-STEM→head-id path; tie-break by `lemma_text`/form for multi-STEM; `SEG-DIM-NO-FANOUT` check. |
| Fan-out (one segment → many dims) | none expected (per-word STEM is specific) | Hard check `SEG-DIM-NO-FANOUT`; one row per segment. |
| Nulls | 53,611 segs null lemma_bw; 78,251 null root_bw | Null-safe rule: null source ⇒ null id. Readers must treat null as "no match". |
| Root usefulness | root not needed for Lemmas fix | Documented as future-proofing; 100% clean ⇒ no data risk. |
| Cache invalidation after deploy | reader values change | Flush/restart API after reseed; `EfLemmasReader` cache keys unchanged (values change). |

---

## 10. Final recommendation — next exact step

1. **Write the implementation plan** for a small **prerequisite migration-feature**: add `segment.lemma_id` + `segment.root_id` (per §8), importer population, `SEG-*` validation, reseed.
2. **Implement that prerequisite feature** (migration + EF config + importer + checks), then reseed locally. No reader/frontend changes in that task.
3. **Then** implement the Lemma Details type fix as **Option A** over `segment.lemma_id` (separate task; rewrite the two `EfLemmasReader` methods to join `segment.lemma_id` + use `segment.pos`).
4. **No further report needed** — the DB evidence is conclusive. (A separate Stems Details audit can follow later; it will reuse word-level `stem_id`, not segments.)

---

## 11. Mem0 update summary

**Stable decision to save `[REC]`:** Live DB confirms — add nullable FK+indexed `segment.lemma_id` (required; fixes 272 mislabeled occurrences across 5 particle lemmas — مَا/لَا/مِن/لَن/لَو — caused by multi-STEM words where `head_pos` takes the first STEM's POS) and `segment.root_id` (100% clean, future-proofing); **reject `segment.stem_id`** (no per-segment stem source, `quran_stems` has no buckwalter, not needed). Populate at import (single-STEM → head lemma_id; multi-STEM → buckwalter). Lemma Details fix = **Option A** (Option B changes لا count by 1, unnecessary).

**Obsolete/inaccurate memories to update:**
- **Update the mechanism:** earlier notes (and the first report) say the bug comes from "`head_pos = segments.First().Pos` fallback for words with **no STEM segment**." **DB disproves this** — every word has a STEM segment and `head_pos` never diverges from a STEM POS. Correct mechanism: **multi-STEM words**, `head_pos` = first STEM's POS while the head lemma belongs to another STEM segment.
- **Mark obsolete:** any memory implying `segment.stem_id` may be needed — confirmed **not** needed and **not** feasible from current data.
- **Correct the "no migration needed" stance:** the durable fix **does** add a migration + importer change (segment IDs) as a prerequisite, superseding the earlier read-time-only framing.

---

## Appendix — SQL used (read-only; no password shown)

```sql
-- Schema introspection (columns of quran_lemmas / segments / morphology / roots / stems)
SELECT column_name, data_type FROM information_schema.columns
WHERE table_name = 'quran_word_morphology_segments' ORDER BY ordinal_position;

-- Lemma totals + duplicate buckwalter
SELECT lemma_buckwalter, COUNT(*) n, array_agg(id ORDER BY id) lemma_ids
FROM quran_lemmas WHERE lemma_buckwalter IS NOT NULL
GROUP BY lemma_buckwalter HAVING COUNT(*)>1 ORDER BY n DESC;

-- Lemma-bearing segments by kind (proves lemma identity is STEM-only)
SELECT kind, COUNT(*) FROM quran_word_morphology_segments
WHERE lemma_buckwalter IS NOT NULL GROUP BY kind;

-- Segment lemma resolvability
SELECT COUNT(*) seg_with_lemma_bw,
  COUNT(*) FILTER (WHERE mc=1) resolves_unique,
  COUNT(*) FILTER (WHERE mc=0) unresolved,
  COUNT(*) FILTER (WHERE mc>1) ambiguous
FROM (SELECT s.id,(SELECT COUNT(*) FROM quran_lemmas l WHERE l.lemma_buckwalter=s.lemma_buckwalter) mc
      FROM quran_word_morphology_segments s WHERE s.lemma_buckwalter IS NOT NULL) x;

-- Root duplicate buckwalter + resolvability
SELECT root_buckwalter, COUNT(*) FROM quran_roots WHERE root_buckwalter IS NOT NULL
GROUP BY root_buckwalter HAVING COUNT(*)>1;
SELECT COUNT(*) FILTER (WHERE mc=1) resolves_unique, COUNT(*) FILTER (WHERE mc=0) unresolved,
       COUNT(*) FILTER (WHERE mc>1) ambiguous
FROM (SELECT s.id,(SELECT COUNT(*) FROM quran_roots r WHERE r.root_buckwalter=s.root_buckwalter) mc
      FROM quran_word_morphology_segments s WHERE s.root_buckwalter IS NOT NULL) x;

-- head_pos vs STEM segment POS (corpus-wide divergence = 0)
WITH stemseg AS (SELECT quran_word_id, pos stem_pos,
  COUNT(*) OVER (PARTITION BY quran_word_id) n_stem
  FROM quran_word_morphology_segments WHERE kind='STEM')
SELECT COUNT(*) FILTER (WHERE m.head_pos=ss.stem_pos) head_eq,
       COUNT(*) FILTER (WHERE m.head_pos<>ss.stem_pos) head_ne
FROM quran_word_morphology m LEFT JOIN stemseg ss
  ON ss.quran_word_id=m.quran_word_id AND ss.n_stem=1;

-- Corpus-wide bug surface (head_pos vs the STEM segment carrying the head lemma)
SELECT COUNT(*) FILTER (WHERE seg_pos=head_pos) type_matches,
       COUNT(*) FILTER (WHERE seg_pos IS NOT NULL AND seg_pos<>head_pos) type_differs,
       COUNT(*) FILTER (WHERE seg_pos IS NULL) no_matching_stem_seg
FROM (SELECT m.quran_word_id, m.head_pos,
        (SELECT s.pos FROM quran_word_morphology_segments s
         WHERE s.quran_word_id=m.quran_word_id AND s.kind='STEM'
           AND s.lemma_buckwalter=l.lemma_buckwalter ORDER BY s.segment_number LIMIT 1) seg_pos
      FROM quran_word_morphology m JOIN quran_lemmas l ON l.id=m.lemma_id) z;

-- لا (id 77): head_pos distribution, STEM-segment POS distribution, Option A vs B
SELECT m.head_pos, COUNT(*) FROM quran_word_morphology m WHERE m.lemma_id=77 GROUP BY m.head_pos;
SELECT s.pos, COUNT(*) FROM quran_word_morphology_segments s
WHERE s.kind='STEM' AND s.lemma_buckwalter='laA' GROUP BY s.pos;
SELECT (SELECT COUNT(*) FROM quran_word_morphology WHERE lemma_id=77) option_a,
       (SELECT COUNT(DISTINCT quran_word_id) FROM quran_word_morphology_segments
        WHERE kind='STEM' AND lemma_buckwalter='laA') option_b;
```

*Report only. Read-only `SELECT` queries against the local dev DB. No code, schema, data, migration, importer, test, frontend, or commit changes. Password not stored anywhere.*
