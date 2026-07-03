# Lemmas Explorer — Wrong Ayah Matches / Pronoun Highlighting — Root-Cause Report

**Feature:** 017 — Lexical Explorers Polish
**Scope:** Lemmas Explorer only — route `/dashboard/words/lemmas` (الصيغ المعجمية)
**Task type:** INVESTIGATION / REPORT ONLY — no code, backend, frontend, test, DB, migration, importer, or seed changes; no commits.
**Branch (workspace):** `chore/skills-tooling-refresh`
**Date:** 2026-07-03
**DB evidence:** live read-only `SELECT`s against the local `quran_dashboard` DB (executed this session; results inline in §3/§4/§9).

---

## 0. Verdict

**`DATA_BUG`** — a **word-level lemma misalignment in the QUL source** (`qul/word-lemma.json`): the verb lemma (كَانَ / ءَامَنَ) is recorded on the **wrong word location** — an adjacent **pronoun** word — instead of the verb word it belongs to. The importer faithfully stores the mis-positioned word lemma, and the single-STEM branch of `MorphologyAssembler.ResolveLemmaId` then stamps it onto the pronoun STEM segment. Everything downstream is correct and simply repeats the bad data.

Ruled out with live evidence:
- **`BACKEND_QUERY_BUG` — ruled out.** `EfLemmasReader` filters on `segment.lemma_id` + `segment.pos` correctly (no `head_pos`, no ayah/page-only join). It faithfully returns whatever the segment table holds.
- **`FRONTEND_MAPPING_BUG` — ruled out.** Index-consistent highlight; chips echo the API distribution verbatim. No re-tokenization, no local type derivation.
- **`STALE_DB` — ruled out.** The DB is the documented clean reseed: the non-STEM `lemma_id` invariant returns **0 rows**, and لا = **NEG 1406 / PRO 332** (matches `002-…failclosed-curated-reseed-report.md`). A fresh reseed will **reproduce** the bug because the defect is in the source.

**Are the Quran data tables corrupted?** No — `quran_words`, `quran_ayahs`, `quran_surahs`, and the `quran_lemmas`/`quran_roots` catalogues are sound. The wrong values are the **word-level and single-STEM-segment `lemma_id` assignments** for ~20 pronoun words, sourced from misaligned QUL entries. **Is the read path wrong?** No.

Contributing (non-primary) code-side gaps that let the bad source through:
1. **Importer policy:** `ResolveLemmaId`'s single-STEM path returns the word head lemma **even when the STEM segment has no corpus `lemma_buckwalter`** (a pronoun) — so it fabricates a lemma link the corpus never asserted.
2. **Validation gap:** `MORPH-WORD-LEMMA-SHIFT-CLEAN` (per `remaining-word-lemma-shifts-diagnostic-report.md`) only inspects **previous-word** shifts and excludes words that lack their own STEM lemma; it does **not** catch a content lemma landing on a pronoun word (these are effectively *next*-word shifts). Its earlier "0 true remaining shifts" conclusion is therefore a false negative for this class.

---

## 1. Data-source map (what feeds lemma identity)

Morphology is assembled from **two aligned sources** plus a **normalization/correction layer**, all keyed by word location `surah:ayah:word`.

| Concern | Source (staged under `resources/import-sources/quran-morphology/`) | Shape | Feeds |
| --- | --- | --- | --- |
| Structural morphology / POS / **segments** | `corpus/quranic-corpus-morphology-qpc-aligned.json` | per-word `segments[]`: `kind` (PREFIX/STEM/SUFFIX), `pos`, `form`, `root`(bw), `lemma`(bw) | segment rows, `pos`, `head_pos` (= STEM pos), segment `root_buckwalter`/`lemma_buckwalter` |
| Arabic **lemma** display | `qul/word-lemma.json` | `{ "s:a:w": "كَانَ" }` — **one lemma per word** | `quran_lemmas.lemma_text`, **word-level `lemma_id`** |
| Arabic **root** display | `qul/word-root.json` | one root per word | `quran_roots`, word-level `root_id` |
| Arabic **stem** display | `qul/word-stem-corrected-arabic.json` | one stem per word | `quran_stems`, word-level `stem_id` |
| Normalization/correction | `WordLemmaNormalizationReader.Apply(...)` → `word-lemma-normalization.json` | add/remove/replace over QUL lemma text | corrects/dedupes lemma text before the catalogue is built |

Key structural facts (verified): QUL supplies **one word-level lemma per location**; the corpus supplies **per-segment POS/kind**; the segment→lemma bridge in the DB is `segment.lemma_id` (STEM-only) plus, at the word level, `quran_word_morphology.lemma_id`. Pronoun/particle segments carry **no corpus lemma** (`lemma_buckwalter` NULL). The assembler dedups lemmas by the corrected QUL Arabic text.

---

## 2. Concrete failing examples

| # | Lemma (displayed) | id | Type chip | Highlighted pronoun word(s) |
| --- | --- | --- | --- | --- |
| A | ءَامَنَ (آمن) | 210 | ضمير (PRON) | **وَهُم** (12:37:23, 6:150:25) |
| B | كَانَ (كان) | 107 | ضمير (PRON) / (LOC) | **بِكُمُ** (2:148:11), **فَهُوَ** (6:136:25), **لَّهُمْ** (9:74:29), **شَطْرَهُۥ** (2:144:20) |

These match the UI report exactly (وَهُم for آمن; بِكُم / فَهُو / هُم for كان).

---

## 3. Live DB evidence — the segment rows

```
-- كان (lemma 107): distribution by segment kind/pos among segments filed under it
 id  | lemma_text | lemma_buckwalter | kind | pos  |  n
 107 | كَانَ       | kaAna            | STEM | V    | 1358   ← correct
 107 | كَانَ       | kaAna            | STEM | PRON |    3   ← WRONG
 107 | كَانَ       | kaAna            | STEM | LOC  |    1   ← WRONG

-- The offending words (all segments of each word shown)
 location | text_uthmani | sn | kind   | pos  | form_bw | lemma_bw | lemma_id
 2:144:20 | شَطْرَهُۥ      |  1 | STEM   | LOC  | $aTora  | $aTor    |  107   ← own lemma $aTor overwritten by head 107
 2:144:20 | شَطْرَهُۥ      |  2 | SUFFIX | PRON | hu,     | (null)   | (null)
 2:148:11 | بِكُمُ        |  1 | PREFIX | P    | bi      | (null)   | (null)
 2:148:11 | بِكُمُ        |  2 | STEM   | PRON | kumu    | (null)   |  107   ← pronoun STEM, no corpus lemma, forced to 107
 6:136:25 | فَهُوَ        |  1 | PREFIX | SUP  | fa      | (null)   | (null)
 6:136:25 | فَهُوَ        |  2 | STEM   | PRON | huwa    | (null)   |  107
 9:74:29  | لَّهُمْ        |  1 | PREFIX | P    | l~a     | (null)   | (null)
 9:74:29  | لَّهُمْ        |  2 | STEM   | PRON | humo    | (null)   |  107

-- آمن (lemma 210)
 location | text_uthmani | sn | kind   | pos  | form_bw | lemma_bw | lemma_id
 6:150:25 | وَهُم         |  1 | PREFIX | REM  | wa      | (null)   | (null)
 6:150:25 | وَهُم         |  2 | STEM   | PRON | hum     | (null)   |  210
 12:37:23 | وَهُم         |  1 | PREFIX | CONJ | wa      | (null)   | (null)
 12:37:23 | وَهُم         |  2 | STEM   | PRON | hum     | (null)   |  210
```

Word-level heads are wrong too (drives symptom 2 — the word-analysis panel):

```
 location | text_uthmani | head_pos | word lemma_id | lemma_text
 12:37:23 | وَهُم         | PRON     | 210           | ءَامَنَ
 2:144:20 | شَطْرَهُۥ      | LOC      | 107           | كَانَ
 2:148:11 | بِكُمُ        | PRON     | 107           | كَانَ
 6:136:25 | فَهُوَ        | PRON     | 107           | كَانَ
 6:150:25 | وَهُم         | PRON     | 210           | ءَامَنَ
 9:74:29  | لَّهُمْ        | PRON     | 107           | كَانَ
```

Freshness / integrity cross-checks (prove the DB is the clean reseed, not stale):
```
-- non-STEM lemma_id invariant (must be 0 on a clean DB):     0 rows ✅
-- لا (bw 'laA') STEM POS distribution:                        NEG 1406, PRO 332  ✅ (matches reseed report)
```

Full bug surface:
```
-- STEM segments assigned a content lemma despite NO corpus lemma of their own:
 stem_no_corpus_lemma_but_assigned = 20   (all pos = PRON)
```
(20 pronoun STEM segments wrongly filed under content lemmas, plus at least the شَطْر `LOC` case whose own corpus lemma `$aTor` was overwritten by head `107`.)

---

## 4. Source proof — QUL `word-lemma.json` is misaligned

Direct reads of `resources/import-sources/quran-morphology/qul/word-lemma.json`:

```
"2:148:9":"أَيْن"   "2:148:10":"أَتَى"   "2:148:11":"كَانَ"   "2:148:12":"اللَّه"      ← word 11 is بِكُمُ, not a form of كان
"6:136:24":"اللَّه"  "6:136:25":"كَانَ"   "6:136:26":"يَصِلُ"                              ← word 25 is فَهُوَ
"9:74:28":"خَيْر"   "9:74:29":"كَانَ"    "9:74:30":"إِن"                                ← word 29 is لَّهُمْ
"2:144:19":"وَجْه"  "2:144:20":"كَانَ"   "2:144:21":"إِنّ"                              ← word 20 is شَطْرَهُۥ
```

Each of these ayat contains a genuine كان/آمن verb nearby (تَكُونُوا۟ / كُنتُمْ / آمَنُوا۟, etc.). The QUL `word-lemma` value for that verb has landed **one word off**, on the adjacent pronoun word, because QUL's per-word numbering does not line up with the QPC/DB word boundaries for these positions. This is a **QUL↔QPC word-alignment defect** (the same family the `word-level-lemma-alignment-*` and `corpus-qpc-location-alignment-map.json` work addresses), **not** a random value error.

---

## 5. Read path & frontend — correct, faithful (evidence)

**Backend** (`EfLemmasReader.cs`): type distribution = `from s in Segments join PosTags on s.Pos where s.LemmaId != null` grouped by segment POS; ayah filter/highlight = `where s.LemmaId == id && (typeCode == null || s.Pos == typeCode)` then `IsMatched = matchedSet.Contains(w.Id)`. Correct segment-level semantics; simply reflects the bad `lemma_id`.

**Frontend**: `mapLemmaAyahMatchToShared` uses the array **index** for both `quranWordId` and `matchedQuranWordIds`; `HighlightedAyahComponent` highlights the word whose index is in that set (iterating the backend-ordered array, no re-tokenize). `LemmaAyahTypeFiltersComponent` renders `TypeSummaryDto[]` verbatim. So the ضمير chip and the highlighted pronoun are exactly what the backend sent — which is exactly what the DB holds.

---

## 6. Reproduction steps (UI)

1. Run backend + frontend on the current local DB; open `/dashboard/words/lemmas`.
2. Select lemma **كان** → details → **Ayahs** tab → a **ضمير** chip appears (count 3) and a **LOC/ظرف** chip (count 1).
3. Click **ضمير** → the ayahs of 2:148 / 6:136 / 9:74 highlight **بِكُمُ / فَهُوَ / لَّهُمْ** instead of the كان verb.
4. Repeat with lemma **آمن** (ءَامَنَ) → ضمير highlights **وَهُم** (12:37 / 6:150).
5. Open the word-analysis panel for a highlighted pronoun (e.g. via the Mushaf deep-link) → it reports lemma كَانَ / ءَامَنَ (because `quran_word_morphology.lemma_id` is wrong at word level too).

---

## 7. Minimal fix plan (do NOT implement yet)

**Primary (source alignment — the real fix).** Correct the misaligned QUL word-lemma entries so the verb lemma sits on the verb word, not the pronoun:
- Add `replace`/`remove` corrections in **`word-lemma-normalization.json`** for the affected locations (2:148:11, 6:136:25, 9:74:29, 2:144:20, 12:37:23, 6:150:25, and the rest of the 20-PRON surface + شَطْر-class LOC cases). For a pronoun word the correct word-level lemma is the pronoun's own lemma (or none), never a verb.
- Preferable systemic option: fix the QUL→QPC word-location alignment for the affected ayat (via `corpus-qpc-location-alignment-map.json` / the alignment audit pipeline) so the shift is corrected at the boundary rather than per-location.
- Reseed (`reset-db` → `import-foundation` → `rebuild-words --force` → `import-morphology --force` → `generate-i3rab --force`), then restart the API to flush `CachedLemmasReader`.
- This fix corrects **both** the word-level head lemma (symptom 2) and the single-STEM segment lemma (symptoms 1/3) in one place.

**Defense-in-depth (importer + validation).** Even with clean source, harden so this class can never silently pass:
- `ResolveLemmaId` single-STEM path: do **not** assign the word head lemma to a STEM segment whose corpus `lemma_buckwalter` is NULL (or whose POS is a pronominal/particle category that carries no lemma). Fail closed / leave null instead of fabricating.
- Add a hard check `SEG-LEMMA-ID-POS-CATEGORY-CONSISTENT` (a verb/noun lemma must not be filed on a `PRON` segment) and extend `MORPH-WORD-LEMMA-SHIFT-CLEAN` to catch **next-word** shifts and content-lemma-on-pronoun (its current previous-word-only scope missed all 20).

No backend reader change and no frontend change are required — those are correct.

---

## 8. Tests to add before fixing

Backend (Testcontainers, real morphology import):
1. **Word-level correctness** — for locations 2:148:11 / 6:136:25 / 9:74:29 / 12:37:23 / 6:150:25, `quran_word_morphology.lemma_id` is **not** 107/210; head is the pronoun lemma (or null), `head_pos = PRON`.
2. **Segment correctness** — no STEM segment with `pos = PRON` (or LOC for شَطْر) carries a verb lemma_id; assert the شَطْر case keeps its own `$aTor` lemma.
3. **Invariant** — `COUNT(*) WHERE kind='STEM' AND lemma_id IS NOT NULL AND lemma_buckwalter IS NULL` = 0 after import (the 20-row surface goes to 0).
4. **Read-layer regression** — `GetLemmaSummary(107).TypeDistribution` contains verb POS only (no PRON/LOC); `GetLemmaAyahMatchesAsync(107, typeCode:"PRON")` returns an empty match set.
5. **Validation** — the new `SEG-LEMMA-ID-POS-CATEGORY-CONSISTENT` / extended shift check fails on a fixture that plants a verb lemma on a pronoun word.

Frontend (keep as regression guards, no new logic):
6. `mapLemmaAyahMatchToShared` index-consistency contract; `HighlightedAyahComponent` highlights exactly the `isMatched` indices.

---

## 9. Confirmation queries (executed this session; also reusable)

```sql
-- Non-STEM invariant (clean DB → 0 rows): CONFIRMED 0
SELECT s.kind, s.pos, COUNT(*) FROM quran_word_morphology_segments s
WHERE s.lemma_id IS NOT NULL AND s.kind <> 'STEM' GROUP BY 1,2;

-- كان / آمن distribution by segment kind/pos (shows the PRON/LOC contamination)
SELECT l.id, l.lemma_text, s.kind, s.pos, COUNT(*)
FROM quran_word_morphology_segments s JOIN quran_lemmas l ON l.id = s.lemma_id
WHERE l.id IN (107,210) GROUP BY 1,2,3,4 ORDER BY 1,5 DESC;

-- Offending words + all their segments (word 107/210)
SELECT w.location, w.text_uthmani, s.segment_number, s.kind, s.pos,
       s.form_buckwalter, s.lemma_buckwalter, s.lemma_id
FROM quran_word_morphology_segments s JOIN quran_words w ON w.id = s.quran_word_id
WHERE w.id IN (SELECT quran_word_id FROM quran_word_morphology_segments
               WHERE lemma_id IN (107,210) AND pos IN ('PRON','LOC'))
ORDER BY w.location, s.segment_number;

-- Word-level heads for the pronoun words (drives the analysis panel)
SELECT w.location, w.text_uthmani, m.head_pos, m.lemma_id, l.lemma_text
FROM quran_word_morphology m JOIN quran_words w ON w.id = m.quran_word_id
LEFT JOIN quran_lemmas l ON l.id = m.lemma_id
WHERE w.location IN ('2:148:11','6:136:25','9:74:29','2:144:20','12:37:23','6:150:25');

-- Full pronoun surface (clean DB → 0): CONFIRMED 20
SELECT COUNT(*) FROM quran_word_morphology_segments
WHERE kind='STEM' AND lemma_id IS NOT NULL AND lemma_buckwalter IS NULL;
```
Source verification: `grep -o '"2:148:11":"[^"]*"' resources/import-sources/quran-morphology/qul/word-lemma.json` → `"2:148:11":"كَانَ"`.

---

## 10. Evidence index

- `Backend/infrastructure/.../Reads/Quran/Words/Lemmas/EfLemmasReader.cs` — segment-level type distribution + ayah filter/highlight (correct).
- `Backend/infrastructure/.../MorphologyImporting/MorphologyAssembler.cs` — `ResolveLemmaId` single-STEM branch stamps word head lemma onto the STEM (root of the segment-level propagation); `MorphologyImportSource.cs`, `JsonQulReaders.cs` (word-level QUL maps).
- `Backend/domain/.../Morphology/WordMorphologySegment.cs` (`LemmaId`,`Pos`,`Kind`), `WordMorphology.cs` (`HeadPos`,`LemmaId`).
- `resources/import-sources/quran-morphology/qul/word-lemma.json` — misaligned entries (§4).
- `Backend/report/feature-017-lexical-explorers-polish/002-segment-dimension-ids-failclosed-curated-reseed-report.md` — clean-reseed baseline used to rule out STALE_DB.
- `Backend/report/feature-017-lexical-explorers-polish/remaining-word-lemma-shifts-diagnostic-report.md` — the shift detector whose "0 true shifts" is a false negative for this class (§0.2).
- Frontend: `utils/lemma-ayah-match.mapper.ts`, `components/highlighted-ayah/*`, `components/lemma-ayah-type-filters/*`.

*Report only. No code, tests, DB, migrations, importers, seeds, or commits were changed. Live queries were read-only `SELECT`s; the DB password was supplied for this local session and is not stored in any repo file, doc, or memory.*
