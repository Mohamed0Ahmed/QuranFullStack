# Lemma Details — Segment-Matched Word Types — Feasibility & Implementation-Readiness Report

**Feature:** 017 — Lexical Explorers Polish
**Scope:** Lemmas Explorer only — route `/dashboard/words/lemmas` (الصيغ المعجمية)
**Task type:** REPORT ONLY — no production code, tests, migrations, importers, seeds, or commits changed. Only read-only `SELECT` queries were run against the local dev DB.
**Branch:** `017-lexical-explorers-polish`
**Date:** 2026-06-27
**Supersedes / builds on:**
- `lemma-details-matching-segment-pos-report.md` — first diagnosis (when segments had **no** `lemma_id`; bridge was `lemma_buckwalter`). Its mechanism is now obsolete: the prerequisite column exists.
- `segment-dimension-ids-feasibility-report.md` / `segment-dimension-ids-db-verification-report.md` — proved the prerequisite and recommended adding `segment.lemma_id` + `segment.root_id`.
- `Backend/report/feature-017-lexical-explorers-polish/001-segment-dimension-ids-phase5-verification-report.md` — confirms the prerequisite was implemented, migrated, and reseeded (all `SEG-*` checks green).
- `lemma-ayah-type-filter-focused-report.md` — shipped the `typeCode` ayah filter on **head POS**; this report corrects that source.

---

## 1. Verdict

### **READY WITH NOTES**

The prerequisite (`quran_word_morphology_segments.lemma_id` + `root_id`) is **implemented, mapped, populated, and reseeded** on the live dev DB. The correct fix — classify each lemma occurrence by the POS of the segment whose `lemma_id` equals the selected lemma — is **fully feasible** with:

- **No migration** (columns, FKs, and indexes already exist).
- **No API/DTO shape change** (values change, not contracts).
- **No frontend logic change** (the UI renders whatever the API returns).
- **No N+1** (one extra join replaces the existing `head_pos` join).
- **No change to the lemma occurrence set** (Option A; head `lemma_id` stays the occurrence key).

Two data realities must be handled in the implementation (the "NOTES"), both confirmed against live data and both deterministic:

1. **Fan-out (35 occurrences):** 35 `(word, lemma)` pairs match **more than one** segment, and in **all 35** the matching segments carry **different POS**. A deterministic tie-break is mandatory — it affects the *type*, not only the count. **One small product/linguistic decision is requested** (recommended rule below).
2. **Null-match fallback (8 occurrences):** 8 head-lemma occurrences have **no** segment with a matching `lemma_id`. The reader must fall back to `head_pos` (or be deliberately excluded) — a defined rule is required.

Neither note blocks implementation; both have safe defaults. Hence READY **WITH NOTES**, not unconditionally READY.

---

## 2. Executive summary

Lemma Details classifies every occurrence by the **whole-word head POS** (`quran_word_morphology.head_pos`). For multi-STEM words this is wrong: the head POS is taken from the **first** STEM segment, while the word can be filed under a lemma that lives on a **different** STEM segment.

Lemma **لا** (id 77) is the textbook case. Current type distribution shows **NEG 1364, PRO 327, SUB 40, INT 5, ACC 1** — the 46 `SUB`/`INT`/`ACC` labels (حرف مصدري / حرف تفسير / حرف نصب) are foreign to لا and come from the **أن** segment of أَلَّا (= أَنْ + لا). Reclassifying by the segment whose `lemma_id = 77` yields **NEG 1405, PRO 332 and nothing else** — exactly negation/prohibition, total unchanged at 1737. Verified live (§5).

Corpus-wide the defect is small and concentrated: **272 occurrences across 5 high-frequency particle lemmas** — مَا (177), لَا (46), مِن (46), لَن (2), لَو (1). The fix corrects exactly these 272 and leaves the other 72,262 untouched.

The change is **backend-only**, in **two methods** of `EfLemmasReader`. No schema, contract, or frontend work is needed.

---

## 3. Current behavior

### 3.1 Occurrence set and type axis (today)

| Concern | Today's source | Location |
| --- | --- | --- |
| Lemma occurrence set / counts | head `quran_word_morphology.lemma_id` (one head row per word) | `EfLemmasReader.LoadWholeSummaryAsync` raw SQL, `GROUP BY m.lemma_id` |
| **Type distribution** (chips labels/counts) | **`head_pos`** joined to `quran_pos_tags` | `LoadWholeSummaryAsync` `rawRows` LINQ: `join t in _db.PosTags … on m.HeadPos equals t.Code` |
| **Ayah type filter** (`typeCode`) | **`head_pos`** | `GetLemmaAyahMatchesAsync`: `Where(m => m.LemmaId == id && (typeCode == null || m.HeadPos == typeCode))` |
| **Ayah word highlight** (`isMatched`) | **`head_pos`** | `GetLemmaAyahMatchesAsync` `matchedRows`: same `m.HeadPos == typeCode` predicate |

### 3.2 Why head POS is wrong for multi-STEM words

`MorphologyAssembler` sets the word's `head_pos` from the **first** STEM segment, and the word's head `lemma_id` from the QUL whole-word lemma. For a single-STEM word these always coincide (live: `head_pos` diverges from the matching-segment POS in **0** single-STEM cases). For a **multi-STEM** word (e.g. أَلَّا = STEM/SUB/أن + STEM/NEG/لا) the word is filed under **لا** but `head_pos = SUB` (from أن). The type axis therefore shows أن's POS under لا.

### 3.3 What the frontend does

Nothing type-deriving. `LemmaAyahTypeFiltersComponent` renders `summary.typeDistribution` (`TypeSummaryDto[]`) and echoes the selected `code` back as the `typeCode` query param. A corrected backend response flows through unchanged. (Established in the predecessor report; reconfirmed — no frontend code reads `head_pos` or recomputes types.)

---

## 4. Data / schema readiness

**All verified against the current entity, EF configuration, importer, reseed report, and live DB.**

### 4.1 Schema / entity / configuration — DONE

- `WordMorphologySegment` (domain) now exposes `public int? RootId` and `public int? LemmaId`, plus `Root`/`Lemma` navigations.
  *(`Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/WordMorphologySegment.cs`)*
- `WordMorphologySegmentConfiguration` maps `lemma_id` / `root_id`, adds FKs `lemma_id → quran_lemmas.id` and `root_id → quran_roots.id` (`DeleteBehavior.Restrict`), and indexes `IX_quran_word_morphology_segments_lemma_id` and `…_root_id`.
  *(`…/Persistence/Configurations/Quran/Words/Morphology/WordMorphologySegmentConfiguration.cs`)*
- Migration `20260627144247_AddSegmentDimensionIds` applied during the Phase 5 clean reseed.

### 4.2 Importer population — DONE

`MorphologyAssembler.ResolveLemmaId` / `ResolveRootId` populate the columns at import:

- STEM segment only (non-STEM ⇒ `null`).
- Single-STEM word ⇒ segment `lemma_id` = word head `lemma_id` (sidesteps all buckwalter ambiguity).
- Multi-STEM word ⇒ resolve each STEM by `lemma_buckwalter`, with head-id shortcut, Arabic-form fallback, and lowest-id tie-break for the 9 duplicate buckwalters.
- `root_id` by `root_buckwalter → quran_roots` (100% clean).
- Null source ⇒ null id (never fabricated).

### 4.3 Validation — no known violations

Phase 5 live import reports **all `SEG-*` checks green**: `SEG-LEMMA-ID-STEM-ONLY`, `SEG-LEMMA-ID-SINGLE-STEM-HEAD-CONSISTENT`, `SEG-LEMMA-ID-NO-FANOUT`, `SEG-LEMMA-ID-MULTI-STEM-RESOLVES`, `SEG-LEMMA-ID-REQUIRED-FOR-STEM`, `SEG-ROOT-ID-RESOLVES`, `SEG-ROOT-ID-CONSISTENT`, `SEG-DIM-NULL-SAFE`, `SEG-STEM-ID-ABSENT`, and `MORPH-SOURCE-UNCHANGED`. ~1,704 Buckwalter-only words legitimately remain null at both head and segment level (they carry no head `lemma_id`, so they never enter a lemma's occurrence set).

### 4.4 Live population counts (query A, §5)

| Metric | Value |
| --- | ---: |
| Total segments | 128,219 |
| Segments with `lemma_id` | 72,990 |
| Segments with `root_id` | 49,968 |
| STEM segments | 77,915 |
| STEM segments with `lemma_id` | 72,990 |

> Note: `SEG-LEMMA-ID-NO-FANOUT` guarantees ≤1 dimension *per segment*. It does **not** guarantee ≤1 matching segment *per `(word, lemma)`*; that is a different relation and is where the fan-out note (§8.1) arises.

---

## 5. Real-data verification (read-only)

Connected to local `quran_dashboard` (localhost:5432, user `postgres`) read-only. Only `SELECT` ran. Password sourced from local dotnet user-secrets, used via `PGPASSWORD` for this session only — **not written to any file, doc, or memory.**

### Query A — segment dimension population
```
total_seg=128219 | seg_lemma_id=72990 | seg_root_id=49968 | stem_seg=77915 | stem_with_lemma=72990
```

### Query B — لا (id 77) CURRENT `head_pos` distribution (the bug)
```
NEG=1364 | PRO=327 | SUB=40 | INT=5 | ACC=1   (total 1737)
```
`SUB`/`INT`/`ACC` = 46 foreign labels (حرف مصدري / حرف تفسير / حرف نصب).

### Query C — لا (id 77) FIX distribution via `segment.lemma_id`
```sql
SELECT s.pos, COUNT(*) FROM quran_word_morphology m
 JOIN quran_word_morphology_segments s
   ON s.quran_word_id=m.quran_word_id AND s.lemma_id=m.lemma_id
 WHERE m.lemma_id=77 GROUP BY s.pos ORDER BY 2 DESC;
```
```
NEG=1405 | PRO=332   (total 1737)
```
**Exactly negation + prohibition; the 46 foreign labels are gone; total unchanged.** This is the decisive proof.

### Query D — fan-out risk: `(word, lemma)` pairs matching >1 segment
```
words_with_multi_match = 35
```

### Query E — null-match gap: head occurrences with **no** matching `segment.lemma_id`
```
head_occ_no_seg = 8
```

### Query F — لا occurrence count: head-set vs fix-join
```
head_occ=1737 | join_occ=1737    (لا has no fan-out; counts identical)
```

### Query G — corpus-wide `head_pos` vs matching-segment POS
```
same=72262 | diff=272
```

### Query H — every lemma whose label changes
```
مَا=177 | لَا=46 | مِن=46 | لَن=2 | لَو=1     (Σ = 272)
```

### Query I — nature of the 35 fan-out pairs
```
same_pos_pairs=0 | diff_pos_pairs=35
```
**All 35 fan-out pairs have segments of *different* POS** ⇒ the tie-break determines the assigned type, not just the count.

### Query J — tie-break feasibility
All 35 fan-out pairs consist entirely of STEM segments, so `kind='STEM'` does not disambiguate; **`min(segment_number)`** (or `min(segment.id)`) selects exactly one segment per pair deterministically.

---

## 6. Correct target behavior

For each occurrence already filed under the selected lemma (head `lemma_id = @id`), classify it by the **POS of the segment in that same word whose `lemma_id = @id`** — not by `head_pos`.

```
occurrence(word w, head lemma L)
  matchSeg = the single segment s where s.quran_word_id = w.id AND s.lemma_id = L
             (tie-break when >1: lowest segment_number)        -- §8.1
  typePos  = matchSeg?.pos ?? w.head_pos                        -- fallback for the 8 null-match -- §8.2
  label    = join quran_pos_tags on typePos
```

- **Type distribution:** group occurrences by `typePos`; counts and labels follow.
- **Ayah filter / highlight:** an occurrence matches `typeCode` iff its `typePos = typeCode`; matched ayahs = `DISTINCT ayah_id` over matched occurrences; highlight = matched word ids.
- **Occurrence set:** unchanged (head `lemma_id`). **Option A.** Totals across types still equal the lemma's occurrence count.

---

## 7. Feasibility analysis

| Question | Answer | Evidence |
| --- | --- | --- |
| Derive type distribution by joining `segment.lemma_id = @id`? | **Yes** | Query C returns the correct لا distribution directly. |
| Ayah type filter use the same segment-matched POS? | **Yes** | Same join/predicate replaces `m.HeadPos == typeCode`. |
| Without changing the occurrence set? | **Yes (Option A)** | Query F: لا head_occ = join_occ = 1737; aggregation SQL still groups by head `lemma_id`. |
| Without migrations? | **Yes** | Columns, FKs, indexes already exist (§4.1); Phase 5 migration applied. |
| Without API/DTO breaking changes? | **Yes** | Only computed *values* change; `TypeSummaryDto` / `LemmaAyahMatchDto` shapes unchanged (§11). |
| Without N+1? | **Yes** | One join (`m ⋈ segments on quran_word_id AND lemma_id ⋈ pos_tags`) replaces the existing `head_pos` join. Single set-based query, as today. |

**Net:** the fix swaps the POS *source* in two queries from `m.head_pos` to the matched segment's `pos`, plus a tie-break and a fallback. Everything else stays.

---

## 8. Edge cases and decisions

### 8.1 Fan-out — 35 pairs, all different-POS — **DECISION REQUESTED**
35 `(word, lemma)` pairs match >1 segment (query D), and in **all** of them the segments carry **different POS** (query I). A naive join would (a) inflate totals by ~35 and (b) be non-deterministic about which POS wins.

- **Required:** pick exactly one segment per `(word, lemma)`.
- **Recommended rule:** lowest `segment_number` (then `segment.id`). Deterministic, yields exactly one (query J), matches the earliest in-word reading.
- **Human decision:** these 35 are genuine "same lemma on two STEM segments with different POS" cases. `min(segment_number)` is a safe, defensible default, but the team may prefer a linguistic preference order (e.g. a POS priority). **Confirm the rule before implementation.** Impact is ≤35 occurrences corpus-wide.

### 8.2 Null-match — 8 occurrences — **DECISION (default available)**
8 head-lemma occurrences have no segment with the matching `lemma_id` (query E). **Default:** fall back to `head_pos` so the occurrence still gets a type and the count stays whole. (Alternative — exclude them — would make `Σ types < occurrencesCount`; not recommended.) Recommend the `head_pos` fallback.

### 8.3 Count integrity
With one segment per `(word, lemma)` (§8.1) and the fallback (§8.2), **`Σ typeDistribution[].occurrencesCount == lemma.occurrencesCount`** holds (one type per occurrence word, no fan-out, no drops). This must be an explicit test assertion.

### 8.4 Non-STEM / null-form segments
`lemma_id` is populated only on STEM segments; non-STEM/null segments are `null` and never match `s.lemma_id = @id`, so they are correctly ignored.

### 8.5 Occurrence-set boundary (Option A vs B) — already decided
Words where the selected lemma appears **only** as a non-head segment (it would have to be a different word's head lemma) are **not** in the head-lemma occurrence set today. Query F shows لا's head and join counts are identical (1737), so Option A introduces **no** count churn. Widening to a segment-defined occurrence set (Option B) changes لا by +1 word only and is **explicitly out of scope** — a separate product decision.

### 8.6 Counting unit
Unchanged: **per word occurrence** (one type per occurrence word), `COUNT(DISTINCT ayah_id)` for ayah counts. The fix changes only the *type assigned* to each occurrence, never the unit.

---

## 9. Query / code locations to implement later (no change made here)

| # | File · method | Current | Change |
| --- | --- | --- | --- |
| 1 | `EfLemmasReader.LoadWholeSummaryAsync` — `rawRows` LINQ (`join t in PosTags on m.HeadPos equals t.Code`) | type distribution from `head_pos` | join `quran_word_morphology_segments s on s.quran_word_id = m.quran_word_id and s.lemma_id = m.lemma_id`, then `PosTags on s.pos`; resolve one segment per occurrence (tie-break) and fall back to `head_pos` when no match. `MaterializeTypeDistribution` unchanged. |
| 2 | `EfLemmasReader.GetLemmaAyahMatchesAsync` — `matchedAyahIds` predicate | `m.HeadPos == typeCode` | match on the resolved segment POS (`EXISTS segment s where s.quran_word_id=m.quran_word_id AND s.lemma_id=id AND s.pos=typeCode`, tie-broken consistently with #1). |
| 3 | `EfLemmasReader.GetLemmaAyahMatchesAsync` — `matchedRows` (highlight) | `m.HeadPos == typeCode` | same segment-POS predicate so highlight matches the filter. |
| — | `LoadWholeSummaryAsync` aggregation raw SQL (`GROUP BY m.lemma_id`, counts, first verse) | — | **No change** (occurrence set / counts stay head-lemma). |
| — | `CachedLemmasReader` / `LemmasCacheKeys` | — | **No key change**; flush/restart after deploy (values change). |

Supporting (read-only, no change): `LemmasListDerivation`, `LemmasSummaryRow`/`LemmaTypeDistributionRow`, `LemmasController` (`{id}`, `{id}/ayahs?typeCode=`).

---

## 10. Test plan

### 10.1 Existing coverage
- `LemmasListReadTests.cs` — `TypeDistribution` count + `Sum == OccurrencesCount` + dominant code (`MultiTypeLemmaId=503`). Asserts shape/ordering, **not** the head-vs-segment source.
- `LemmasAyahsReadTests.cs` — `typeCode` filter, paging, highlight, unknown lemma. Uses synthetic ids (500/503); **no multi-STEM segment fixture.**
- `LemmasSurahsReadTests.cs` — surah aggregation (unaffected).
- `MorphologyAssemblerTests.cs` / `MorphologyValidationFailureTests.cs` — already cover `lemma_id` population + `SEG-*` checks (prerequisite).

### 10.2 Missing — to add with the fix
| Test | Scenario | Assertion |
| --- | --- | --- |
| Segment-matched distribution | multi-STEM lemma (لا-class) | distribution codes come from the matching segment; **no** `SUB`/`INT`/`ACC` for لا — only `NEG`/`PRO`. |
| Count integrity | any lemma | `Σ typeDistribution.occurrencesCount == occurrencesCount`. |
| Single-STEM regression | normal single-STEM lemma | distribution identical to today (segment POS == head POS). |
| Ayah filter by segment POS | `typeCode=NEG` on لا | returned ayahs exactly those with a لا segment of POS `NEG`; `typeCode=SUB` returns **empty** for لا. |
| Highlight | filtered multi-segment ayah | only the لا-segment word is `isMatched`. |
| Fan-out tie-break | `(word, lemma)` with 2 different-POS segments | counted **once**; chosen POS = the agreed tie-break rule. |
| Null-match fallback | occurrence with no matching `segment.lemma_id` | falls back to `head_pos`; no crash, no dropped occurrence. |
| Pagination / search regression | list + ayah paging | unchanged (occurrence set untouched). |

Use a **real morphology import** (Testcontainers) for the لا-class assertions; synthetic ids cannot reproduce multi-STEM segment shapes. Keep Quranic test data source-safe.

---

## 11. Affected response fields (no shape change)

| DTO · field | Current meaning | New meaning | Breaking? |
| --- | --- | --- | --- |
| `LemmaSummaryDto.typeDistribution` | head-POS distribution | matched-segment-POS distribution | No |
| `TypeSummaryDto.code` / `arabicLabel` / `englishLabel` | head POS | matched-segment POS | No |
| `TypeSummaryDto.occurrencesCount` / `firstSurah/Ayah/WordNumber` | per head POS | per matched-segment POS | No (same totals) |
| `GET …/ayahs?typeCode=` | filter on head POS | filter on matched-segment POS | No (same param, same value space) |
| `LemmaAyahWordDto.isMatched` | matched by head POS | matched by matched-segment POS | No |

No renames required. A clarifying comment that `code` is now "the POS of the segment carrying this lemma" is advisable.

---

## 12. Recommended implementation phases

1. **Decide the tie-break rule (§8.1)** and confirm the null-match fallback (§8.2). *(One short product/linguistic confirmation; default = `min(segment_number)` + `head_pos` fallback.)*
2. **Phase A — type distribution:** rewrite the `rawRows` derivation in `LoadWholeSummaryAsync` (location #1). Add distribution-source + count-integrity tests.
3. **Phase B — ayah filter + highlight:** rewrite both predicates in `GetLemmaAyahMatchesAsync` (locations #2/#3) consistently with Phase A. Add filter/highlight tests.
4. **Phase C — verify & cache:** run the لا (id 77) assertions against a real import; confirm `NEG/PRO` only and `Σ == 1737`; flush the Lemmas cache on deploy.
5. **Out of scope (separate tasks):** Stems Explorer (likely the same head-POS pattern — flag, do not change here), POS label-text cleanup (`pos-segment-type-label-cleanup-plan.md`), Option B occurrence-set widening, Roots Details (same multi-STEM mechanism, future).

---

## 13. Risks / open questions

| Risk | Severity | Mitigation |
| --- | --- | --- |
| Contract risk | **None** | Values only; DTO shapes unchanged (§11). |
| Frontend impact | **None** | UI renders API values; no type derivation client-side. |
| Performance | **Low** | One join replaces one join; both indexed (`IX_…_lemma_id`, `IX_…segments_pos`). No N+1. Validate plan on `LoadWholeSummaryAsync` (whole-corpus scan, already the case today). |
| Data correctness — fan-out | **Medium until tie-break chosen** | 35 different-POS pairs (query I); enforce one-segment-per-occurrence + count-integrity test (§8.1/§8.3). |
| Data correctness — null-match | **Low** | 8 occurrences; `head_pos` fallback (§8.2). |
| Cache staleness post-deploy | **Low** | Flush/restart API; keys unchanged. |
| **Open question** | — | Tie-break rule for the 35 fan-out pairs — accept `min(segment_number)` or define a POS priority? (§8.1) |

---

## 14. Final recommendation

**Proceed.** The prerequisite is done and verified end-to-end (schema → EF → importer → reseed → live data). The fix is backend-only, two methods, no migration, no contract change, no frontend change, no N+1, and is **proven correct on live data** for the reported لا case (`NEG/PRO` only, total preserved). Before coding, get the single tie-break confirmation for the 35 fan-out occurrences (default `min(segment_number)`) and confirm the `head_pos` fallback for the 8 null-match occurrences; then implement as **Option A** over `segment.lemma_id`, keeping the occurrence set unchanged.

---

## 15. Evidence index

- `Backend/.../Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs` — `LoadWholeSummaryAsync` (`rawRows` head-POS join; aggregation `GROUP BY m.lemma_id`), `GetLemmaAyahMatchesAsync` (`m.HeadPos == typeCode` ×2).
- `Backend/domain/.../Quran/Words/Morphology/WordMorphologySegment.cs` — `LemmaId` / `RootId` + navigations.
- `Backend/infrastructure/.../Configurations/Quran/Words/Morphology/WordMorphologySegmentConfiguration.cs` — `lemma_id`/`root_id` mapping, FKs, indexes.
- `Backend/.../Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologyAssembler.cs` — `ResolveLemmaId` / `ResolveRootId`.
- `Backend/report/feature-017-lexical-explorers-polish/001-segment-dimension-ids-phase5-verification-report.md` — reseed + `SEG-*` checks green; `002-…failclosed-curated-reseed-report.md`.
- Live DB (read-only) — queries A–J (§5).
- Tests: `LemmasListReadTests.cs`, `LemmasAyahsReadTests.cs`, `LemmasSurahsReadTests.cs`, `MorphologyAssemblerTests.cs`.
- Predecessors: `lemma-details-matching-segment-pos-report.md`, `segment-dimension-ids-db-verification-report.md`, `segment-dimension-ids-feasibility-report.md`, `lemma-ayah-type-filter-focused-report.md`.

---

## Appendix A — Null-match occurrence details

**Purpose:** before adopting the `head_pos` fallback (§8.2) for the 8 head-lemma occurrences with no matching segment, inspect each one and confirm the fallback is safe.

**Definition of the set:** rows where `quran_word_morphology.lemma_id IS NOT NULL` **and** no `quran_word_morphology_segments` row for the same `quran_word_id` has `segment.lemma_id = quran_word_morphology.lemma_id`.

### A.1 SQL used (read-only)

```sql
-- Q1: the 8 occurrences (word + head lemma + head POS)
SELECT m.quran_word_id AS wid,
       w.surah_number||':'||w.ayah_number||':'||w.word_number AS loc,
       w.text_uthmani, w.text_imlaei_simple,
       m.lemma_id AS head_lemma_id, l.lemma_text AS head_lemma, l.lemma_buckwalter AS head_lemma_bw,
       m.head_pos, pt.arabic_label AS head_pos_label, m.segment_count
FROM quran_word_morphology m
JOIN quran_words w  ON w.id = m.quran_word_id
JOIN quran_lemmas l ON l.id = m.lemma_id
LEFT JOIN quran_pos_tags pt ON pt.code = m.head_pos
WHERE m.lemma_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM quran_word_morphology_segments s
                  WHERE s.quran_word_id = m.quran_word_id AND s.lemma_id = m.lemma_id)
ORDER BY w.surah_number, w.ayah_number, w.word_number;

-- Q2: every segment of those 8 words
WITH bad AS (
  SELECT m.quran_word_id, m.lemma_id AS head_lemma_id
  FROM quran_word_morphology m
  WHERE m.lemma_id IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM quran_word_morphology_segments s
                    WHERE s.quran_word_id = m.quran_word_id AND s.lemma_id = m.lemma_id))
SELECT s.quran_word_id AS wid, b.head_lemma_id, s.segment_number AS seg_no, s.kind, s.pos,
       s.form_arabic_normalized AS form_ar, s.form_buckwalter AS form_bw,
       s.lemma_id AS seg_lemma_id, sl.lemma_text AS seg_lemma, s.lemma_buckwalter AS seg_lemma_bw,
       s.root_id AS seg_root_id
FROM quran_word_morphology_segments s
JOIN bad b ON b.quran_word_id = s.quran_word_id
LEFT JOIN quran_lemmas sl ON sl.id = s.lemma_id
ORDER BY s.quran_word_id, s.segment_number;
```

### A.2 Result — the 8 occurrences (Q1)

| wid | loc | uthmani | imlaei | head lemma_id | head lemma | head bw | head_pos | head_pos label | seg_count |
| ---: | --- | --- | --- | ---: | --- | --- | --- | --- | ---: |
| 24120 | 8:28:2 | أَنَّمَآ | انما | 11 | إِنّ | `<in~` | ACC | حرف نصب | 2 |
| 24967 | 8:73:6 | إِلَّا | الا | 205 | إِلَّا | `<il~aA` | COND | حرف شرط | 2 |
| 29823 | 11:14:5 | أَنَّمَآ | انما | 11 | إِنّ | `<in~` | ACC | حرف نصب | 2 |
| 41358 | 18:110:8 | أَنَّمَآ | انما | 11 | إِنّ | `<in~` | ACC | حرف نصب | 2 |
| 45135 | 21:108:5 | أَنَّمَآ | انما | 11 | إِنّ | `<in~` | ACC | حرف نصب | 2 |
| 53708 | 28:50:11 | مِمَّنِ | ممن | 5942 | أَضَلّ | `>aDal~` | P | حرف جر | 2 |
| 62917 | 38:70:5 | أَنَّمَآ | انما | 11 | إِنّ | `<in~` | ACC | حرف نصب | 2 |
| 65654 | 41:6:8 | أَنَّمَآ | انما | 11 | إِنّ | `<in~` | ACC | حرف نصب | 2 |

### A.3 Result — segments of those 8 words (Q2)

| wid | seg_no | kind | pos | form_ar | form_bw | seg lemma_id | seg lemma | seg bw |
| ---: | ---: | --- | --- | --- | --- | ---: | --- | --- |
| 24120 | 1 | STEM | ACC | أَنَّ | `>an~a` | 250 | أَنّ | `>an~` |
| 24120 | 2 | STEM | PREV | مَآ | `maA^` | 4 | مَا | `maA` |
| 24967 | 1 | STEM | COND | إِ | `<i` | 541 | إِن | `<in` |
| 24967 | 2 | STEM | NEG | لَّا | `l~aA` | 77 | لَا | `laA` |
| 29823 | 1 | STEM | ACC | أَنَّ | `>an~a` | 250 | أَنّ | `>an~` |
| 29823 | 2 | STEM | PREV | مَآ | `maA^` | 4 | مَا | `maA` |
| 41358 | 1 | STEM | ACC | أَنَّ | `>an~a` | 250 | أَنّ | `>an~` |
| 41358 | 2 | STEM | PREV | مَآ | `maA^` | 4 | مَا | `maA` |
| 45135 | 1 | STEM | ACC | أَنَّ | `>an~a` | 250 | أَنّ | `>an~` |
| 45135 | 2 | STEM | PREV | مَآ | `maA^` | 4 | مَا | `maA` |
| 53708 | 1 | STEM | P | مِ | `mi` | 130 | مِن | `min` |
| 53708 | 2 | STEM | REL | مَّنِ | `m~ani` | 130 | مِن | `man` |
| 62917 | 1 | STEM | ACC | أَنَّ | `>an~a` | 250 | أَنّ | `>an~` |
| 62917 | 2 | STEM | NEG | مَآ | `maA^` | 4 | مَا | `maA` |
| 65654 | 1 | STEM | ACC | أَنَّ | `>an~a` | 250 | أَنّ | `>an~` |
| 65654 | 2 | STEM | PREV | مَآ | `maA^` | 4 | مَا | `maA` |

*(root_id is null on every segment above — these are particles with no triliteral root; not relevant to the analysis.)*

### A.4 Per-occurrence cause analysis

Every one of the 8 is a **multi-STEM word where the QUL whole-word (head) lemma does not equal any of the Corpus segment lemmas**. The head lemma is a *compound / whole-particle* lexical unit; the segments carry the *constituent-stem* lemmas. In every case `head_pos` is the **first STEM segment's POS** (the existing assembler rule), so the fallback value is a real POS of a real segment in the word — just not "the lemma's own segment", because that segment does not exist.

**Group 1 — أَنَّمَآ (= أَنَّ + مَا), 6 occurrences** — wids 24120, 29823, 41358, 45135, 62917, 65654.
- Head lemma = **إِنّ** (id 11, `<in~`). Segments resolve to **أَنّ** (id 250) + **مَا** (id 4). إِنّ (11) and أَنّ (250) are **distinct lemma rows** (different orthography of the same particle family), so the head lemma 11 legitimately matches neither segment.
- Cause: **QUL files أَنَّمَا under the lemma إِنّ; the Corpus segments it as أَنَّ + مَا.** A real, expected QUL-vs-Corpus modeling divergence — not a resolver error.
- `head_pos = ACC` (حرف نصب). إِنّ is an accusative particle ⇒ ACC is the **linguistically correct family label** for lemma إِنّ.

**Group 2 — إِلَّا (= إِ + لَّا), 1 occurrence** — wid 24967 (8:73:6).
- Head lemma = **إِلَّا** (id 205, the exception/compound particle). Segments resolve to **إِن** (id 541, conditional) + **لَا** (id 77, negation). The compound lemma 205 matches neither constituent.
- Cause: **QUL treats إِلَّا as one lexical unit (lemma 205); the Corpus splits it into إِن + لا.** Expected modeling divergence.
- `head_pos = COND` (حرف شرط), inherited from the إِ (conditional) segment. *Label nuance:* COND/شرط is the constituent's POS, not the exception particle's conventional label — but this is a label-quality question, not a fallback-safety question (the value is unchanged from today).

**Group 3 — مِمَّنِ (= مِن + مَن), 1 occurrence** — wid 53708 (28:50:11).
- Head lemma = **أَضَلّ** (id 5942) — a **verb** lemma ("more astray"), which is semantically impossible for the preposition+relative word مِمَّنِ (= مِمَّن). The preceding word in 28:50 is أَضَلُّ; this is almost certainly a **QUL head-lemma alignment artifact** that shifted onto مِمَّنِ. Both segments correctly resolve to **مِن** (id 130).
- `head_pos = P` (حرف جر) from the مِ segment — which is correct *for the word*, but wrong *for the verb lemma أَضَلّ* that the head row points at.
- Cause: **pre-existing QUL whole-word lemma-alignment anomaly in the source morphology** — independent of the segment-id work.

### A.5 Classification of each occurrence

| wid | loc | word | head lemma | classification |
| ---: | --- | --- | --- | --- |
| 24120 | 8:28:2 | أَنَّمَآ | إِنّ (11) | **Safe for head_pos fallback** (ACC = correct family POS) |
| 29823 | 11:14:5 | أَنَّمَآ | إِنّ (11) | **Safe for head_pos fallback** |
| 41358 | 18:110:8 | أَنَّمَآ | إِنّ (11) | **Safe for head_pos fallback** |
| 45135 | 21:108:5 | أَنَّمَآ | إِنّ (11) | **Safe for head_pos fallback** |
| 62917 | 38:70:5 | أَنَّمَآ | إِنّ (11) | **Safe for head_pos fallback** |
| 65654 | 41:6:8 | أَنَّمَآ | إِنّ (11) | **Safe for head_pos fallback** |
| 24967 | 8:73:6 | إِلَّا | إِلَّا (205) | **Safe for head_pos fallback** (minor label note: COND inherited from constituent إِن) |
| 53708 | 28:50:11 | مِمَّنِ | أَضَلّ (5942) | **Data anomaly requiring further investigation** (QUL head-lemma misalignment) — still head_pos-fallback-safe for *this* fix (no-op) |

**Net: 8 / 8 are safe for the `head_pos` fallback.** 7 are legitimate QUL-vs-Corpus compound divergences; 1 (wid 53708) is a pre-existing source alignment anomaly that the fallback neither creates nor worsens.

### A.6 Why the fallback is safe by construction

These 8 occurrences are **exactly the set where the segment-matched fix is a no-op**: there is no matching segment, so the new join contributes nothing and the code falls back to `head_pos` — which is **what the type axis already uses for every occurrence today**. The fix therefore **cannot regress these 8**; their displayed type is identical before and after. Using the fallback also preserves count integrity (`Σ types == occurrencesCount`); excluding them would break it.

### A.7 Answers to the four questions

1. **Did the Segment Dimension IDs report miss these 8 because it checked a different invariant?**
   Yes. The `SEG-*` checks validate the **segment → dimension** direction ("does each STEM segment with a `lemma_buckwalter` resolve to a `lemma_id`, single-STEM head-consistency, no fan-out, null-safe"). They never assert the **reverse** relation ("does the word's *head* lemma_id appear on some segment"). For multi-STEM compound lemmas that reverse relation is *expected to fail*, so it was correctly not made an invariant. The segments here all resolved fine (250, 4, 541, 77, 130); the head lemma simply isn't one of them.

2. **Do any of the 8 violate existing `SEG-*` checks?**
   No. All 8 pass every `SEG-*` check (consistent with Phase 5 all-green). `SEG-LEMMA-ID-REQUIRED-FOR-STEM` is satisfied (the STEM segments *do* get lemma_ids); `SEG-LEMMA-ID-SINGLE-STEM-HEAD-CONSISTENT` does not apply (all are multi-STEM); no fan-out, no null-safety breach.

3. **Would a new "head lemma must have a matching segment lemma" check be correct, or falsely fail legitimate cases?**
   It would **falsely fail legitimate cases.** 7 of the 8 are correct QUL-vs-Corpus modeling differences (أَنَّمَا→إِنّ, إِلَّا→إِن+لا) that *should* disagree. A hard check would block valid reseeds. Recommended instead: a **soft/diagnostic** check that lists head lemmas with no matching segment for manual review, with a known-compound allow-list (إِنّ/أَنَّمَا, إِلَّا). Only wid 53708 (أَضَلّ on مِمَّنِ) merits escalation, and as a **head-lemma alignment** issue, not a segment-id one.

4. **Should Lemma Details use `head_pos` fallback for all 8, or correct some first?**
   Use the **`head_pos` fallback for all 8** — it is a strict no-op versus current behavior and carries zero regression risk for the type-source fix. Do **not** block the fix on any correction. Separately (out of scope for the reader fix): open a **data-curation ticket for wid 53708** (QUL head lemma أَضَلّ misaligned onto مِمَّنِ), and optionally a minor label-review note for إِلَّا (8:73:6). Neither is a prerequisite for implementing the segment-matched type fix.

### A.8 Effect on the open decision (§8.2)

The §8.2 fallback recommendation stands and is now evidence-backed: **head_pos fallback for all 8 null-match occurrences.** The only newly surfaced follow-up is the **single source anomaly (wid 53708)**, tracked separately and not blocking.

---

*Report only. No production code, tests, migrations, importers, seeds, or commits were changed. Read-only `SELECT` queries against the local dev DB; DB password not stored anywhere.*
