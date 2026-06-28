# Lemma Details — Matching-Segment POS vs Head POS — Investigation Report

**Feature:** 017 — Lexical Explorers Polish
**Scope:** Lemmas Explorer only — route `/dashboard/words/lemmas` (الصيغ المعجمية)
**Task type:** REPORT ONLY — no code, backend, frontend, test, DB, migration, importer, or seed changes; no commits.
**Branch:** `017-lexical-explorers-polish`
**Date:** 2026-06-27
**Related prior reports (do not contradict):**
- `docs/feature-017-lexical-explorers-polish/lemma-ayah-type-filter-focused-report.md` — added the `typeCode` ayah filter (now implemented). It accepted `head_pos` as the type identity; **this report challenges that source for multi-segment correctness.**
- `docs/feature-017-lexical-explorers-polish/pos-segment-type-label-cleanup-plan.md` / `pos-tag-arabic-labels-review-report.md` — fix the *wording* of POS labels (`PRO → حرف نهي`, etc.). That is a **label-text** concern; this report is a **label-source** concern. They are complementary, not overlapping.

---

## 1. Verdict (summary first)

**The current Lemma details type behavior is WRONG for multi-segment words.** Both the type cards/filters and the type-based ayah filtering classify each occurrence by the **word-level head POS** (`quran_word_morphology.head_pos`), not by the POS of the **morphology segment that actually carries the selected lemma**.

- **Where the bug lives:** Backend read model only. Two methods in `EfLemmasReader`.
- **Issue is backend-only.** The frontend performs **no** local type derivation — it renders whatever `typeDistribution` / `typeCode` the API returns and echoes the POS `code` back as the ayah filter. A correct backend response fully fixes the UI with **no required frontend logic change**.
- **One important factual correction to the task brief:** there is **no `lemma_id` column on `quran_word_morphology_segments`.** The segment→lemma bridge in this database is the **`lemma_buckwalter` string**, plus `quran_word_id`. The "join segments by `segment.lemma_id`" path does not exist as written; the no-migration fix must match on `lemma_buckwalter` (with caveats in §3/§4).

---

## 2. How Lemma details currently derive the type (proof)

### 2.1 The lemma → occurrence link is word-level head lemma, not segment

`quran_word_morphology` is **one head row per readable word** (PK `quran_word_id`, 77,432 rows). It carries `head_pos`, `lemma_id`, `stem_id`, `root_id`. The lemma a word is "filed under" is `quran_word_morphology.lemma_id` — a **single** lemma per word.

How that head lemma and head POS are assigned, from `MorphologyAssembler.Assemble(...)`:

```csharp
var stemSegment = segments.FirstOrDefault(s => string.Equals(s.Kind, "STEM", StringComparison.Ordinal));
...
var headPos = stemSegment?.Pos ?? segments.FirstOrDefault()?.Pos ?? string.Empty;   // STEM segment POS, else FIRST segment POS
...
if (lemmas.TryGetValue(location, out var lv)) qulLemma = lv;                          // ONE QUL lemma per whole-word location
...
lemmaId = lemmaEntry.Id;                                                              // head lemma_id
```

**Consequence:** `head_pos` describes the **STEM segment** (or, when there is no STEM segment, the **first** segment). The head `lemma_id` is the QUL whole-word lemma. For a word with a STEM segment these usually coincide, **but for multi-particle words with no STEM segment, `head_pos` is taken from the first particle and is decoupled from whichever segment carries the filed lemma.**

### 2.2 Type cards / type distribution — uses `head_pos`

`EfLemmasReader.LoadWholeSummaryAsync(...)` builds the per-lemma type distribution from the **head row joined on `head_pos`**:

```csharp
var rawRows = await (
    from m in _db.WordMorphologies.AsNoTracking()
    join w in _db.QuranWords.AsNoTracking() on m.QuranWordId equals w.Id
    join t in _db.PosTags.AsNoTracking() on m.HeadPos equals t.Code      // <-- HEAD POS
    where m.LemmaId != null
    select new { LemmaId = m.LemmaId!.Value, m.QuranWordId, t.Code, t.ArabicLabel, t.EnglishLabel, ... })
    .ToListAsync(cancellationToken);
```

`MaterializeTypeDistribution(...)` then groups by `t.Code` and counts. The result becomes `LemmaSummaryDto.TypeDistribution` (`TypeSummaryDto { code, arabicLabel, englishLabel, occurrencesCount, firstSurah/Ayah/WordNumber }`), returned by `GET /api/words/lemmas/{id}`.

### 2.3 Ayah type filter + highlight — uses `head_pos`

`EfLemmasReader.GetLemmaAyahMatchesAsync(id, page, pageSize, typeCode, ...)`:

```csharp
var matchedAyahIds = _db.WordMorphologies.AsNoTracking()
    .Where(m => m.LemmaId == id && (normalizedTypeCode == null || m.HeadPos == normalizedTypeCode))   // <-- HEAD POS
    .Join(_db.QuranWords..., (_, w) => w.AyahId)
    .Distinct();
...
// matched word ids for highlight — same head-pos filter
where m.LemmaId == id && ayahIds.Contains(w.AyahId) && (normalizedTypeCode == null || m.HeadPos == normalizedTypeCode)
```

So the type **filter** and the **highlight** (`IsMatched`) both gate on `head_pos == typeCode`. This is exactly the behavior added by the earlier `lemma-ayah-type-filter-focused-report.md`; it is internally consistent with the (wrong) type cards, but both share the same head-POS source defect.

### 2.4 Answers to the required backend questions

| Question | Answer |
| --- | --- |
| Which methods use `head_pos`? | `EfLemmasReader.LoadWholeSummaryAsync` (type distribution) and `EfLemmasReader.GetLemmaAyahMatchesAsync` (ayah filter + highlight). |
| Which methods join `quran_word_morphology_segments`? | **None** in the Lemmas read path. No method touches the segments table. |
| Do they filter segments by lemma? | N/A — segments are never queried. The lemma link is the head `quran_word_morphology.lemma_id`. |
| Selected-lemma segment POS, or whole-word head POS? | **Whole-word head POS** (`head_pos`) in every case. |
| Counts based on words, ayahs, or segments? | `occurrencesCount` = count of **head morphology rows** (one per word) grouped by `head_pos`. `ayahsCount`/ayah list = `COUNT(DISTINCT ayah_id)`. Word rows = grouped by `unique_*_word_id`. No segment-level counting anywhere. |
| Double-counting risk today? | **No.** One head row per word ⇒ one type contribution per occurrence. (A naive segment-based rewrite *would* introduce double-count risk — see §3.5 / §4.) |

---

## 3. Data-model feasibility (and a correction to the brief)

### 3.1 Tables involved

`quran_lemmas`, `quran_word_morphology` (head), `quran_word_morphology_segments` (segments), `quran_pos_tags`, `quran_words`, `quran_ayahs`.

### 3.2 The segment table has NO `lemma_id`

Confirmed three independent ways — entity `WordMorphologySegment`, `WordMorphologySegmentConfiguration`, and `Backend/report/database/current-database-tables-and-relationships-report.md`:

`quran_word_morphology_segments` columns: `id`, `quran_word_id`, `segment_location`, `segment_number`, `kind`, **`pos`**, `form_buckwalter`, `form_arabic_normalized`, `arabic_render_tier`, `arabic_render_source`, `root_buckwalter`, **`lemma_buckwalter`**, `features_raw`, `features_json`, `i3rab_*`.

Foreign keys: `quran_word_id → quran_words.id`, **`pos → quran_pos_tags.code`**, `i3rab_rule_id → quran_i3rab_rules.id`. **There is no `lemma_id` FK and no `lemma_id` column.**

> **Correction:** the task's stated identity `quran_word_morphology_segments.lemma_id = selected lemma id` does not exist. The only segment→lemma bridge is the **`lemma_buckwalter` string** (FK exists only for `pos`). This does not block the fix, but it changes the join shape and adds caveats.

### 3.3 What *is* available to identify the matching segment

For a given word (`quran_word_id`) and a selected lemma:
- `quran_lemmas.lemma_buckwalter` (the lemma's Buckwalter) ↔ `quran_word_morphology_segments.lemma_buckwalter`
- `quran_word_morphology_segments.pos` → `quran_pos_tags.code` for the Arabic label.

So the matching segment is resolvable by:

```
segment.quran_word_id = word.id
AND segment.lemma_buckwalter = lemma.lemma_buckwalter
```

then read `segment.pos` and join `quran_pos_tags`.

### 3.4 No migration / importer / seed change needed

- **No migration** — `pos` and `lemma_buckwalter` already exist on segments; `pos → quran_pos_tags.code` FK already exists; all 128,219 segments are populated.
- **No importer change** — segment POS and `lemma_buckwalter` are already written by `MorphologyAssembler` / the morphology bulk copier.
- **No POS seed / i‘rab seed change** — this report changes the *source field* the reader reads, not any label text. (Label-text fixes are the separate `pos-segment-type-label-cleanup-plan.md`.)
- **Existing data is structurally sufficient** — with the caveats in §3.5.

### 3.5 Caveats the implementation must handle (verify against live data)

1. **`lemma_buckwalter` is not globally unique** — the capability report records **9 duplicate lemma Buckwalter values**. Matching must be scoped **within a single word** (`quran_word_id`), where collisions are effectively absent, not done globally.
2. **`lemma_buckwalter` can be null on a segment** — `MorphologyAssembler` stores the lemma's Buckwalter from the **STEM** segment's corpus lemma; non-STEM particle segments may carry their own segment `lemma_buckwalter` or null. Define a deterministic **fallback to `head_pos`** when no segment matches.
3. **More than one segment in a word could match** — if two segments share the lemma's Buckwalter, the reader must pick exactly **one** segment per occurrence (suggested tie-break: `kind = 'STEM'` first, then lowest `segment_number`) to avoid **double-counting** a single word occurrence.
4. **Occurrence-set boundary is a product decision (see §4.6)** — keeping the existing head-lemma occurrence set fixes the displayed *type* without changing *which* words/counts appear; widening to a segment-defined occurrence set is a larger, separate change.

---

## 4. Expected correct query shape

### 4.1 Principle

For each occurrence already filed under the selected lemma (head `lemma_id = @id`), classify it by the **POS of the segment in that same word whose `lemma_buckwalter` equals the lemma's `lemma_buckwalter`**, not by `head_pos`.

### 4.2 Type distribution (replaces the `head_pos` join in `LoadWholeSummaryAsync`)

Conceptually, per occurrence resolve one POS:

```
occurrence(word w filed under lemma L)
  -> matchSeg = the segment s where s.quran_word_id = w.id
                 AND s.lemma_buckwalter = L.lemma_buckwalter
                 (tie-break: kind='STEM' then min(segment_number))
  -> typePos = matchSeg?.pos ?? w.head_pos        // deterministic fallback
  -> join quran_pos_tags on typePos for arabicLabel/englishLabel
group by typePos -> { code, arabicLabel, englishLabel, occurrencesCount, first occurrence coords }
```

### 4.3 Ayah filter + highlight (replaces the `head_pos` gate in `GetLemmaAyahMatchesAsync`)

`typeCode` should match against the **matching-segment POS**, not `head_pos`:

```
matched word w  iff  w filed under lemma L
                 AND (typeCode == null OR matchSegPos(w, L) == typeCode)
matched ayah    = DISTINCT ayah_id over matched words
highlight       = matched word ids (same predicate)
```

### 4.4 Labels

Join `quran_pos_tags` on the resolved segment `pos` for `arabic_label` / `english_label`. (Unchanged join target; only the **code source** moves from `head_pos` to matched-segment `pos`.)

### 4.5 Exactly what is counted (keep stable, just correct the type axis)

| Metric | Count rule (proposed) |
| --- | --- |
| Type filter / type card count (`occurrencesCount`) | One contribution **per occurrence word** filed under the lemma, attributed to its matched-segment POS. Exactly one segment per word (tie-broken). No segment fan-out. |
| Ayah count per type | `COUNT(DISTINCT ayah_id)` over words whose matched-segment POS = the type. |
| Occurrence count per type | Same as the type-card count (per occurrence word). |
| Displayed ayah list when a type is selected | Ayahs (deduped by `ayah_id`, ordered by surah/ayah) that contain ≥1 occurrence whose matched-segment POS = the selected type; highlight only those matching words. |

This keeps today's "one occurrence = one word" semantics — only the **type assigned to each occurrence** changes. Totals across all types stay equal to the lemma's occurrence count (no inflation), provided the one-segment-per-word rule in §3.5(3) is enforced.

### 4.6 Occurrence-set boundary — decision flag (not silently widened)

- **Layer 1 (in this fix):** words **already** filed under the lemma via head `lemma_id`. Correct their displayed type to the matching segment's POS. **No count change** for any lemma whose words are all STEM-headed; counts shift only where `head_pos` was a non-matching first-segment particle.
- **Layer 2 (separate decision, NOT in this fix):** words where the selected lemma appears **only as a non-head segment** (e.g. the لا inside أَلَّا when that word's head lemma is `أن`) are **not counted under the lemma at all today**, because the occurrence set is head-lemma. Including them would require a segment-defined occurrence set and would change every count on the page. Recommend treating this as a deliberate follow-up, not bundling it into the type-source fix.

---

## 5. Worked examples (lemma لا)

> The local DB credentials in `appsettings.Development.json` did not authenticate against the running server during this audit, so these examples are derived from the **deterministic code logic + schema** above and the POS facts cited in `pos-segment-type-label-cleanup-plan.md` (which references `2:11:4 لَا` as a `PRO` / حرف نهي location). **Verify exact rows/counts against the live DB during implementation** (read-only).

| Case | Word shape | `head_pos` source | Correct lemma-segment POS | Current label | Correct label |
| --- | --- | --- | --- | --- | --- |
| لا as negation, standalone | single particle segment, no STEM | first segment = the لا segment | the لا segment `pos = NEG` | حرف نفي (coincidentally right) | حرف نفي ✓ |
| لا as prohibition (e.g. `2:11:4`) | particle segment | the لا segment `pos = PRO` | لا segment `pos = PRO` | (right when لا is the head/first segment) | حرف نهي ✓ |
| لا inside a multi-particle word with no STEM (أَلَّا = أَنْ + لا), **if filed under لا** | no STEM ⇒ `head_pos` = **first** segment = أَنْ's POS (`SUB`) | the لا segment `pos` (NEG/PRO) | **حرف نصب/مصدري** (from أَنْ) ✗ | **حرف نفي / حرف نهي** ✓ |

The third row is the defect the brief describes: a لا occurrence mislabeled with the **other** segment's POS (`ACC`/`SUB` → حرف نصب/مصدري) because `head_pos` was lifted from the first segment, not the لا segment. The fix attributes it to the لا segment's own `pos`.

**Second lemma class to check** (prefix/stem/suffix word where head ≠ matching segment): any word whose selected lemma sits on a **non-STEM** segment while the STEM segment carries a different lemma. There, `head_pos` = STEM segment POS ≠ the selected lemma's segment POS. Pick one during implementation from the live data and assert it.

> **Do not** attribute حرف نصب / حرف مصدري to lemma لا unless the matched لا segment itself has that POS — which it does not. Those labels belong to the أَنْ segment.

---

## 6. Required report tables

### Table 1 — Current behavior map

| UI area | Backend endpoint/method | Current source field/query | Uses head POS? | Uses matching lemma segment POS? | Problem | Evidence |
| --- | --- | --- | --- | --- | --- | --- |
| Ayah type filter chips (`qd-lemma-ayah-type-filters`) — labels/counts | `GET /api/words/lemmas/{id}` → `GetLemmaSummaryHandler` → `EfLemmasReader.LoadWholeSummaryAsync` | `join PosTags on m.HeadPos`, grouped per `lemma_id` → `TypeDistribution` | **Yes** | No | Chips show head-POS types; for multi-segment lemmas the type belongs to another segment | `EfLemmasReader.cs` `rawRows` query; `MaterializeTypeDistribution` |
| Ayah list filtered by selected type | `GET /api/words/lemmas/{id}/ayahs?typeCode=` → `GetLemmaAyahsHandler` → `EfLemmasReader.GetLemmaAyahMatchesAsync` | `.Where(m => m.LemmaId == id && m.HeadPos == typeCode)` | **Yes** | No | Filters/keeps ayahs by head POS; wrong set for multi-segment lemmas | `EfLemmasReader.GetLemmaAyahMatchesAsync` matchedAyahIds |
| Ayah word highlight (`IsMatched`) under a type filter | same method (matched-word query) | `where m.LemmaId == id && m.HeadPos == typeCode` | **Yes** | No | Highlights by head POS, not the لا segment | `EfLemmasReader.GetLemmaAyahMatchesAsync` matchedRows |
| (Reference) `قائمة توزيع الأنواع` (`qd-type-distribution-list`) | — | — | — | — | **Stems page only — not rendered on the Lemmas page.** Shared component; out of scope here. | `stems-explorer-page.component.html` (only consumer) |

### Table 2 — Correct behavior proposal

| UI area | Correct data source | Required backend change | Frontend change needed? | Count/dedup rule | Notes |
| --- | --- | --- | --- | --- | --- |
| Type chips labels/counts | matched segment `pos` (segment where `lemma_buckwalter = lemma.lemma_buckwalter`, within word), join `quran_pos_tags`, fallback `head_pos` | Rewrite the type-distribution derivation in `LoadWholeSummaryAsync` to resolve per-occurrence matched-segment POS | **No** (renders API `arabicLabel`/`code`) | One POS per occurrence word (tie-break STEM→min segment_number); group by code | Keep ordering rule (count desc, earliest mushaf coord) |
| Ayah filter set | matched-segment POS predicate | Replace `m.HeadPos == typeCode` with matched-segment-POS predicate in `GetLemmaAyahMatchesAsync` | **No** (passes `code` back as `typeCode`) | `DISTINCT ayah_id`; page after filter | `typeCode` stays a POS code |
| Highlight | matched-segment POS predicate | Same predicate for matched word ids | **No** | Highlight only matching words | `IsMatched` shape unchanged |
| Occurrence set | head `lemma_id` (unchanged for this fix) | None — keep occurrence set; only change type axis | No | One occurrence = one word | Widening to segment-defined set = separate decision (§4.6) |

### Table 3 — Affected response fields

| Response/DTO | Field | Current meaning | Proposed meaning | Breaking change? | Rename recommended? | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `LemmaSummaryDto` | `typeDistribution` | per-lemma distribution of **head POS** | per-lemma distribution of **matching-segment POS** | No (shape identical) | No | Values change, not shape |
| `TypeSummaryDto` | `code` | head POS code | matched-segment POS code | No | No (optional comment clarifying source) | Still a controlled `quran_pos_tags.code` |
| `TypeSummaryDto` | `arabicLabel` / `englishLabel` | label of head POS | label of matched-segment POS | No | No | Same join target |
| `TypeSummaryDto` | `occurrencesCount` | head-POS occurrences | matched-segment-POS occurrences | No | No | Same total across types (one seg/word) |
| `TypeSummaryDto` | `firstSurah/Ayah/WordNumber` | first head-POS occurrence | first matched-segment occurrence | No | No | Recompute on new grouping |
| `GET .../ayahs` query | `typeCode` | filter on head POS | filter on matched-segment POS | No (same param) | No | Semantics tighten; value space unchanged (POS codes) |
| `LemmaAyahMatchDto` / `LemmaAyahWordDto` | `words[].isMatched` | matched by head POS | matched by matched-segment POS | No | No | Highlight follows corrected predicate |

> No DTO/contract shape changes and no renames are required. The change is in the **values** the backend computes. A clarifying code comment that `code` now means "the POS of the segment carrying this lemma" is advisable.

### Table 4 — Test recommendations

| Test area | Scenario | Expected assertion | Backend/frontend | Fixture/data needed |
| --- | --- | --- | --- | --- |
| `LoadWholeSummaryAsync` type distribution | Lemma whose occurrences include a non-STEM/multi-particle word | `typeDistribution` codes come from the matched لا segment (e.g. `NEG`/`PRO`); **no** `SUB`/`ACC` unless the لا segment itself is that POS | Backend | Real morphology import (Testcontainers); a لا-class lemma id |
| `LoadWholeSummaryAsync` count integrity | Any lemma | Sum of `typeDistribution[].occurrencesCount` == lemma `occurrencesCount` (no inflation; one segment/word) | Backend | Same |
| `GetLemmaAyahMatchesAsync` filter | `typeCode = NEG` on لا | Returned ayahs are exactly those with ≥1 لا-segment of POS `NEG`; none filtered by head POS | Backend | Same |
| `GetLemmaAyahMatchesAsync` highlight | filtered ayah with multiple segments | only the لا-segment word(s) are `isMatched`; other-segment words are not | Backend | Same |
| Fallback path | occurrence word with null/absent matching segment `lemma_buckwalter` | falls back to `head_pos` deterministically (no crash, no missing type) | Backend | A word with null segment lemma_buckwalter |
| Tie-break | word with two segments sharing the lemma Buckwalter | counted once; STEM (or lowest segment_number) chosen | Backend | Constructed/identified fixture |
| Frontend regression | type chips + `typeCode` URL round-trip | unchanged behavior; chips render API labels, `typeCode` param still set/cleared correctly | Frontend | Existing `lemmas-explorer-page.spec` + `lemmas-url-sync.spec` (no new logic) |

---

## 7. Frontend flow (Lemmas only)

| Question | Answer |
| --- | --- |
| Which UI elements display the type labels? | Only `qd-lemma-ayah-type-filters` (chips), shown **only when `view === 'ayahs'`**, in `lemmas-explorer-page.component.html` (L121–126). `qd-type-distribution-list` (توزيع الأنواع) is **not** on the Lemmas page (Stems only). |
| Which response fields are used? | `panelState().summary?.typeDistribution` (`TypeSummaryDto[]`) for chip labels/counts; `panelState().ayahTypeCode` for selection; emits `typeCodeChange` → URL `typeCode` → `LemmasApi.getLemmaAyahMatches(..., typeCode)`. |
| Frontend type derivation locally? | **None.** `LemmaAyahTypeFiltersComponent` and `TypeDistributionListComponent` are display-only; they render `arabicLabel`/`code` and pass `code` back verbatim. |
| Does the frontend assume one word type per lemma occurrence? | It assumes a **distribution** of types from the API and one selected `typeCode` at a time. It does not assume a single type, and it does not compute types — so corrected backend values flow through unchanged. |
| Is a backend response fix enough? | **Yes.** No frontend logic change is required. Optional: a comment/label-doc note that `code` is now the lemma-segment POS. |

---

## 8. Final recommendation

### A. Verdict
- **Current Lemma details type behavior: WRONG** for multi-segment occurrences (classifies by `head_pos`, not the selected lemma's segment POS).
- **Issue is backend-only.** Frontend renders API values faithfully; no frontend defect.

### B. Recommended implementation scope (for a later, approved change)
- **Backend methods to change (exactly two):**
  - `EfLemmasReader.LoadWholeSummaryAsync` — replace the `join PosTags on m.HeadPos` type-distribution derivation with a matched-segment-POS resolution (`segment.lemma_buckwalter = lemma.lemma_buckwalter` within `quran_word_id`, tie-broken, `head_pos` fallback).
  - `EfLemmasReader.GetLemmaAyahMatchesAsync` — replace both `m.HeadPos == typeCode` predicates (ayah set + highlight) with the matched-segment-POS predicate.
- **Caching:** `CachedLemmasReader` / `LemmasCacheKeys` keys can stay as-is (same inputs); only the cached *values* change. Flush cache after deploy.
- **Response fields to update/rename:** none structurally. `TypeSummaryDto.code/arabicLabel/englishLabel/occurrencesCount/first*` and `typeCode`/`isMatched` change in **value/semantics only**. Add a clarifying comment; no rename.
- **Frontend components needing only model/name alignment:** none required. Optional doc/comment in `lemmas.models.ts` (`TypeSummaryDto.code` = lemma-segment POS) and in `lemma-ayah-type-filters`.
- **Tests to add:** Table 4 (backend type-distribution source, count integrity, ayah filter/highlight by segment POS, fallback, tie-break; frontend stays regression-only).

### C. Explicit non-scope
- **Do not** fix Stems Explorer here (the shared `qd-type-distribution-list` and `EfStemsReader` likely have the same head-POS pattern — flag as a **separate** follow-up; do not change them in this work).
- **Do not** change POS labels (that is `pos-segment-type-label-cleanup-plan.md`).
- **Do not** change simplified i‘rab labels or `quran_i3rab_rules`.
- **Do not** change database schema, add a `lemma_id` to segments, or add migrations.
- **Do not** change importers or POS/i‘rab seeds.
- **Do not** change Roots or Unique Words pages (referenced only for comparison).
- **Do not** silently widen the occurrence set to segment-defined membership (§4.6) — that is a separate product decision.

---

## 9. Evidence index

- `Backend/infrastructure/.../Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs` — `LoadWholeSummaryAsync` (head-POS type distribution), `GetLemmaAyahMatchesAsync` (head-POS filter + highlight).
- `Backend/infrastructure/.../Persistence/Reads/Quran/Words/Lemmas/LemmasListDerivation.cs` / `LemmasSummaryRow.cs` — `TypeSummaryDto` mapping, `LemmaTypeDistributionRow`.
- `Backend/domain/.../Quran/Words/Morphology/WordMorphology.cs` (`HeadPos`, `LemmaId`), `WordMorphologySegment.cs` (`Pos`, `LemmaBuckwalter`, **no `LemmaId`**), `QuranLemma.cs` (`LemmaBuckwalter`), `PosTag.cs`.
- `Backend/infrastructure/.../Persistence/Configurations/Quran/Words/Morphology/WordMorphologySegmentConfiguration.cs` — confirms segment columns/FKs (`pos`, `lemma_buckwalter`; no `lemma_id`).
- `Backend/infrastructure/.../Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologyAssembler.cs` — `headPos = stemSegment?.Pos ?? segments.First().Pos`; head `lemma_id` from QUL whole-word lemma.
- `Backend/report/database/current-database-tables-and-relationships-report.md` — table/FK inventory (segments have `pos` FK, no `lemma_id`); `lemma_buckwalter` non-uniqueness via capability report.
- `Backend/api/.../Controllers/Words/LemmasController.cs` — endpoints `{id}` and `{id}/ayahs?typeCode=`.
- Frontend: `models/lemmas.models.ts`, `data-access/lemmas.api.ts`, `components/lemma-ayah-type-filters/*`, `pages/lemmas-explorer-page/lemmas-explorer-page.component.html` (L120–127), `state/lemmas-url-sync.ts` (`typeCode`).
- `docs/feature-016-lemmas-stems-explorer/feature-016-lemmas-stems-explorer-capability-linking-report.md` — 4,793 lemmas; 9 duplicate `lemma_buckwalter`; "define النوع precisely" open note (head_pos per occurrence).

*Report only. No code, tests, DB, migrations, importers, seeds, or commits were changed.*
