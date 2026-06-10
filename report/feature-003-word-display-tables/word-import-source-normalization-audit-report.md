# Word Import / Source Normalization Audit Report

> Audits where Quranic annotation/rendering marks enter the word pipeline (Feature 002 import →
> `quran_words` → Feature 003 display tables) and proposes the safest way to give the four
> display/statistics/linking tables clean **word identity** keys without harming Mushaf rendering
> or source fidelity.
>
> **Report only.** No code, schema, migrations, tests, or source data were changed. All database
> access was read-only `SELECT`s. No secrets are printed.

| | |
|---|---|
| **Audit date** | 2026-06-09 |
| **Database** | `quran_dashboard` @ `localhost:5432` (Postgres 18.4), read-only |
| **Source root** | `resources/import-sources/quran-foundation/` |
| **Pipeline** | Feature 002 `import-foundation` → `quran_words`; Feature 003 `rebuild-words` → 4 display tables |

---

## 1. Verdict

**PASS WITH ACTION REQUIRED** — the import **source is faithful** (the marks genuinely exist in the
QPC / Quran-Foundation source files; nothing in our pipeline invented or corrupted them), **but the
four display tables use raw source strings as identity keys, so word identity is split by
rendering/annotation marks.** This must be fixed with a normalized identity key before the tables
are trusted for statistics, search, or Gate/Topic linking.

The pipeline **can** support reliable identity without re-importing or resetting the database: the
fix lives in the Feature 003 rebuild (derived tables only). It is **not** FAIL (no destructive
re-architecture needed) and **not** PASS (today `الله` exists as 3 separate keys).

---

## 2. Why This Matters

The four tables are the foundation for word **identity, statistics, search, and future linking of
all occurrences of a word to Gates/Topics**. Identity keys must answer "is this the same word?"
Today they answer "is this the same *rendered string*?" — a different question.

Concrete damage (measured, readable words only):

- `الله` is stored as **3** distinct simple keys (`الله`, `الله ۗ`, `۞ الله`) instead of 1. Its
  2,155 occurrences across 1,567 ayahs are split 2103 / 49 / 3.
- `الرحمن` is **2** keys (`الرحمـن`, `الرحمـن ۗ`); `العظيم` is **2** keys (`العظيم`, `العظيم ۩‏`).
- Overall the residual marks inflate the **unique tashkeel** count by ~**1,805** and the **unique
  simple** count by ~**410–1,197** (Section 7).

A Gate that links "the word `الله`" would currently miss 52 occurrences unless it also linked the
two mark-variant keys — exactly the fragility the product decision warns against.

---

## 3. Source Files Inspected

| Source file | Manifest key | → maps to `quran_words` column | Role |
|---|---|---|---|
| `mushaf/qpc-v4.json` | `qpc-glyph` | `qpc_glyph` | Mushaf font glyphs (rendering only) |
| `words/uthmani.json` | `uthmani` | `text_uthmani` | **Tashkeel identity key** (with harakat) |
| `words/uthmani-simple.json` | `uthmani-simple` | `text_uthmani_simple` | **Simple identity key** |
| `words/imlaei-simple.json` | `imlaei-simple` | `text_imlaei_simple` | Imlaei attribute + ayah-marker detection |
| `mushaf/qpc-v4-pages-layout.json` | `layout` | (page/line placement) | Layout |
| `metadata/quran-metadata-ayah.json` | `ayah-meta` | (ayah rows) | Metadata |
| `metadata/quran-metadata-surah-name.json` | `surah-meta` | (surah rows) | Metadata |

Each word file has **83,668** records = **77,432** readable words + **6,236** ayah-number markers
(Arabic-Indic digits such as `١`, flagged `is_ayah_marker = true` and excluded from the display
tables). Records are keyed by `location` (`surah:ayah:word`).

---

## 4. Source Text Symbol Audit

Character-level scan of the four word-text source files. "Records" = source word records containing
≥1 character of that class.

| Symbol class (code points) | `uthmani.json` | `uthmani-simple.json` | `imlaei-simple.json` | `qpc-v4.json` |
|---|---:|---:|---:|---:|
| Arabic letters (ء–ي, ٱ) | 77,432 | 77,432 | 77,432 | 0 (font glyphs) |
| Harakat / tashkeel (064B–0658, 0670) | 77,431 | **0** | 0 | 0 |
| Waqf / pause marks (06D6–06DC) | 4,364 | 609 | 0 | 0 |
| End-of-ayah (06DD) | 0 | 0 | 0 | 0 |
| Rub-el-hizb `۞` (06DE) | 199 | 199 | 199 | 0 |
| Sajdah `۩` (06E9) | 15 | 15 | 15 | 0 |
| Other small high/low marks (06DF–06ED, excl. above) | 13,516 | 8,881 | 0 | 0 |
| Tatweel / kashida `ـ` (0640) | 6,404 | 6,404 | 0 | 0 |
| Bidi/invisible control (here: `U+200F`) | 1 | 1 | 1 | 0 |
| NBSP (00A0) | 0 | 0 | 0 | 0 |
| Records containing a space | 4,577 | 822 | 571 | 0 |
| Records with a double space | 1 | 0 | 0 | 0 |

Distinct annotation/control code points actually present (top, by char count):

- **`uthmani.json`**: `ـ`0640 (6,736), `ۭ`06ED (4,807), `۟`06DF (3,988), `ۢ`06E2 (2,445), `ۚ`06DA
  (1,972), `ۖ`06D6 (1,682), `ۥ`06E5 small-waw (1,257), `ۦ`06E6 small-yeh (957), `ۗ`06D7 (603),
  `۞`06DE (199), `۩`06E9 (15), `U+200F` (1) … plus rare stop marks 06EA/06EB/06EC.
- **`uthmani-simple.json`**: `ـ`0640 (6,736), `ۭ`06ED (4,807), `۟`06DF (3,988), `ۗ`06D7 (603),
  `۞`06DE (199), `۠`06E0 (66), `ۧ`06E7 (38), `۩`06E9 (15), `ۜ`06DC (7), `U+200F` (1), and single
  occurrences of 06E3/06EA/06EB. **No harakat.**
- **`imlaei-simple.json`**: only `۞`06DE (199), `۩`06E9 (15), `U+200F` (1). **No tatweel, no waqf,
  no tajwid marks** — the cleanest of the three.
- **`qpc-v4.json`**: 100% presentation-form font glyphs (e.g. `ﱁ` `ﱂ` `ﱃ`); not letters — rendering
  only.

Per-file answers to the audit questions:

| Question | uthmani | uthmani-simple | imlaei-simple | qpc-v4 |
|---|---|---|---|---|
| Field used | `text` | `text` | `text` | `text` |
| Quranic annotation marks? | Yes (heavy) | Yes | Minimal (rub/sajdah only) | No |
| Harakat/tashkeel? | **Yes** | **No** | No | No |
| Waqf/pause marks? | Yes (4,364) | Yes (609) | No | No |
| Sajdah / rub / ornament? | Yes (214) | Yes (214) | Yes (214) | No |
| Invisible bidi/control? | Yes (1× `U+200F`) | Yes (1×) | Yes (1×) | No |
| Spaces inside a record? | Yes (4,577) | Yes (822) | Yes (571) | No |

Representative examples (location → raw text):

- waqf: `4:1:17 → "وَنِسَآءًۭ ۚ"`, `5:1:18 → "حُرُمٌ ۗ"`
- rub `۞`: `63:4:1 → "۞ وَإِذَا"`
- sajdah `۩`: `32:15:15 → "يَسْتَكْبِرُونَ ۩"`
- bidi: `27:26:8 → "ٱلْعَظِيمِ ۩‏"` (sajdah **and** an invisible `U+200F`)
- tatweel: `1:1:3 → "ٱلرَّحْمَـٰنِ"` (uthmani) / `"الرحمـن"` (simple)
- genuine multiword (imlaei): `4:1:1 → "يا ايها"`

---

## 5. Import Mapping Analysis (Feature 002)

**Which source field becomes which column** (manifest + `QuranFoundationAssembler.BuildWords`):

| `quran_words` column | Source | Code |
|---|---|---|
| `qpc_glyph` | `qpc-v4.json.text` | `QpcGlyph = glyph.Text` |
| `text_uthmani` | `uthmani.json.text` | `TextUthmani = uthmani.Text` |
| `text_uthmani_simple` | `uthmani-simple.json.text` | `TextUthmaniSimple = uthmaniSimple.Text` |
| `text_imlaei_simple` | `imlaei-simple.json.text` | `TextImlaeiSimple = imlaeiSimple.Text` |
| `is_ayah_marker` | derived | last word in ayah whose imlaei text is all digits |

**Does Feature 002 clean/normalize any of these? No.**

- `JsonWordSourceReader.ReadRequiredString` returns `property.GetString()` **verbatim** — no trim,
  no Unicode normalization, no mark stripping.
- `QuranFoundationAssembler.BuildWords` assigns each `.Text` **directly** to the entity (no
  transformation). The only computed field is `is_ayah_marker`.
- `RequireAlignedRecord` only validates location/id alignment; it does not alter text.

**Conclusion:** the import is intentionally faithful. Every mark in the source survives byte-for-byte
into `quran_words`. This is correct for fidelity but means `quran_words` carries rendering marks in
its identity-bearing text columns.

---

## 6. Rebuild / Grouping Key Analysis (Feature 003)

`DisplayWordsSql` reads readable words (`is_ayah_marker = false`) and builds the four tables. Grouping
and uniqueness keys are the **raw source text columns**:

| Table | Grouping / uniqueness key | Normalization applied |
|---|---|---|
| `quran_words_ordered_tashkeel` | `text_uthmani` (stats `GROUP BY text_uthmani`) | **None** |
| `quran_words_ordered_simple` | `text_uthmani_simple` (`GROUP BY text_uthmani_simple`) | **None** |
| `quran_words_unique_tashkeel` | `text_uthmani` (`GROUP BY` + `DISTINCT ON`) | **None** |
| `quran_words_unique_simple` | `text_uthmani_simple` (`GROUP BY` + `DISTINCT ON`) | **None** |

- No `TRIM`, `regexp_replace`, or Unicode normalization anywhere in the rebuild.
- The hard checks **enforce faithfulness to the raw string, not to identity**:
  `CheckUnqCountDistinctTashkeelText` / `CheckUnqCountDistinctSimpleText` assert
  `unique_count = COUNT(DISTINCT text_uthmani[_simple])` over `quran_words`, and
  `CheckStatMatchViolations` reconciles against raw `GROUP BY` on those columns. So the current
  green rebuild **proves the tables faithfully mirror the raw source strings** — it cannot detect
  identity splitting, because by its definition `الله` and `الله ۗ` are *correctly* two strings.

**Conclusion:** current unique counts are faithful to **source strings**, not to **word identity**.

---

## 7. Impact on Counts and Linking

Measured over readable words (`is_ayah_marker = false`). Two normalization variants were applied
**for measurement only**:

- **Conservative** — remove only the product-named targets (waqf/pause `06D6–06DC`, end-ayah
  `06DD`, rub `06DE`, sajdah `06E9`, empty-centre stops `06EA–06EC`, bidi/zero-width
  `200B–200F / 2066–2069 / FEFF`, nbsp `00A0`), then trim/collapse spaces. Keeps harakat, tatweel,
  and small tajwid marks.
- **Aggressive** — conservative **plus** tatweel `0640` and all small high/low marks `06DF–06ED`.

| Identity | Current distinct | Conservative | Aggressive |
|---|---:|---:|---:|
| Unique **tashkeel** (`text_uthmani`, harakat kept) | 21,294 | **19,489** (−1,805) | 19,489 (−1,805)¹ |
| Unique **simple** (`text_uthmani_simple`) | 15,826 | **15,416** (−410) | **14,629** (−1,197) |

¹ Removing tatweel additionally changes nothing for tashkeel — tatweel is applied consistently per
word, so it never *independently* splits a key (it does still make keys non-canonical).

**Named words (simple identity):**

| Word | Distinct simple forms today | Forms (occ / ayahs) | Affected? |
|---|---:|---|:---:|
| `الله` | **3** | `الله` (2103/1547) · `الله ۗ` (49/48) · `۞ الله` (3/3) → true 2155/1567 | **Yes** |
| `الرحمن` | **2** | `الرحمـن` (44/44) · `الرحمـن ۗ` (1/1) → true 45/45 | **Yes** (also every form carries a tatweel) |
| `العظيم` | **2** | `العظيم` (35/35) · `العظيم ۩‏` (1/1, sajdah + `U+200F`) → true 36/36 | **Yes** |
| `الرحيم` | **1** | `الرحيم` (34/34) | **No — clean, not affected** |

**Effect on each metric:**

- **Unique counts:** inflated (tashkeel +1,805; simple +410…+1,197).
- **Occurrence counts:** a word's occurrences are split across its mark-variant keys (e.g. `الله`
  shows 2103 on its main key instead of 2155).
- **Ayah counts:** likewise split, and *not* additive (the same ayah can hold both `الله` and
  `الله ۗ`), so per-key `ayahs_count` understates the word's true ayah reach.
- **Gate/Topic linking & search:** a link or search on a clean key silently misses the
  mark-variant keys — the core risk.

**Top simple identity collisions under aggressive normalization** (clean form ← merged keys):
`علم` ←6, and 4-way merges such as `جميعا` ← `جميعۭا | جميعۭا ۗ | جميعا | جميعا ۗ`,
`شيا` ← `شيـۭا | شيـۭا ۗ | شيـا | شيـا ۗ`, `كتب` ← `كتـب | كتـبۭ | كتب | كتبۭ`. These show tatweel
(`ـ`), small-low-meem (`ۭ`), and waqf (`ۗ`) all splitting the same word.

---

## 8. Design Options

### Option 1 — Normalize during Feature 002 import (overwrite `text_uthmani[_simple]`)
- **Pros:** one place; downstream untouched.
- **Cons:** **destroys source fidelity** — `quran_words` is our faithful copy of the source and is
  also the basis for Mushaf-adjacent text; overwriting loses the exact Uthmani string and breaks the
  reconstruction/round-trip guarantees that Feature 002 tests assert. Re-import would be needed to
  recover originals.
- **Verdict:** ❌ Reject — violates "preserve source fidelity / never silently modify source data".

### Option 2 — Keep Feature 002 faithful; normalize only inside Feature 003 (group on normalized text, store normalized text in the four tables)
- **Pros:** source preserved; derived tables only; rebuild-only fix.
- **Cons:** the display tables' `text_*` columns would now hold *normalized* (not raw) text, so they
  can no longer show the exact source form, and the hard checks (which compare to raw
  `COUNT(DISTINCT …)`) must change. It conflates "identity" and "display text" in one column.
- **Verdict:** ⚠️ Workable but muddies the contract.

### Option 3 — Add explicit normalized identity key columns (recommended)
Keep raw `text_uthmani` / `text_uthmani_simple` / `text_imlaei_simple` for display, and add stable
identity keys used for grouping/uniqueness/linking, e.g. `word_key_tashkeel`, `word_key_simple`
(and optionally `word_key_imlaei`), on the display tables (and/or `quran_words`).
- **Pros:** preserves fidelity **and** display text; gives a stable, documented identity for
  statistics, search, and Gate/Topic links; checks can assert *both* "raw faithful" and "identity
  collapses correctly"; future-proof.
- **Cons:** a schema addition (migration) and a normalization function to own and test.
- **Verdict:** ✅ Best long-term; matches the stated product preference.

---

## 9. Recommended Solution

**Adopt Option 3.** The user's preference is **confirmed by the evidence**:

- Source files are faithful and must stay so (Section 5) → do not normalize in import (rejects
  Option 1).
- The damage is entirely in *identity* keys of the four derived tables (Sections 6–7) → fix there.
- Linking/search/statistics need a **stable identity independent of rendering** (Section 7) → a
  dedicated normalized key column is the durable answer (Option 3 over Option 2).

So: **preserve raw text for Mushaf/display; add normalized `word_key_*` identity columns; group and
enforce uniqueness on the keys; point future Gate/Topic links at the key, never at raw display
text.** Annotation/layout/control marks must not affect identity or counts.

One supporting observation: `text_imlaei_simple` is already nearly clean (only rub/sajdah/bidi +
genuine multiword), so it is a useful cross-check/reference when defining the simple identity, even
though uthmani-simple remains the chosen simple orthography.

---

## 10. Proposed Normalization Rule

Define a single, well-documented, **idempotent** normalization function (pure; unit-tested).

**Always remove from *both* identity keys (layout / ornament / control — never semantic):**

- Waqf / pause marks `U+06D6–U+06DC`
- End-of-ayah `U+06DD`, rub-el-hizb `U+06DE`, sajdah `U+06E9`
- Empty-centre stop marks `U+06EA–U+06EC`
- Tatweel / kashida `U+0640` (pure elongation)
- Bidi / zero-width / invisible controls `U+200B–U+200F`, `U+202A–U+202E`, `U+2066–U+2069`,
  `U+FEFF`; NBSP `U+00A0`
- Then **trim** and **collapse internal whitespace** left behind by removed marks.

**Tashkeel identity (`word_key_tashkeel`, "with tashkeel"):**

- **Keep** Arabic letters **and** harakat (`U+064B–U+0658`, superscript alef `U+0670`).
- Remove only the layout/ornament/control set above. → yields **19,489** keys.

**Simple identity (`word_key_simple`):**

- Source already has **no harakat** (remove any if present, for safety).
- Remove the layout/ornament/control set above.
- **Recommended additionally remove** the non-letter tajwid marks `U+06DF` (rounded zero), `U+06E0`,
  `U+06E2` (small high meem), `U+06ED` (small low meem) — they are pronunciation/rendering aids, not
  letters. → moves toward **14,629** keys.
- **Handle with care (do NOT blanket-remove without per-word validation):** the *letter-like* small
  marks `U+06E5` small waw, `U+06E6` small yeh, `U+06E7`, `U+06E8` — these can stand for
  pronounced/silent letters; removing them risks merging genuinely different words. Default: keep,
  or validate case-by-case.

**Harakat summary:** keep in tashkeel identity; absent/removed in simple identity.

**Spaces:**

- Trim leading/trailing; collapse runs to a single space.
- A space that *remains between two real word-tokens* is **not** auto-joined (see edge cases).

**Known cases:** see Section 11.

---

## 11. Edge Cases

| Case | Location | Raw (simple) | Recommended handling |
|---|---|---|---|
| `ال ياسين` (Āl Yāsīn) | `37:130:3` | `ال ياسين` | **Genuine two-token name.** No marks to strip; keep as a stable multiword key (do **not** join to `الياسين`). 1 occurrence. |
| `دائر ةۭ` (دائرة split) | `5:52:12` | `دائر ةۭ` | **Single word split by source segmentation** (`دائر` + `ة`). Mark-stripping alone leaves `دائر ة` (still 2 tokens). Needs an **explicit intra-word join mapping** to reach `دائرة`, or accept as a known 1-occurrence source artifact. Do **not** silently join all spaces. |
| `العظيم ۩‏` | `27:26:8` | `العظيم ۩‏` | Sajdah `۩` + invisible `U+200F` → both removed by the rule → `العظيم`. Merges with the clean key. |
| `الله ۗ`, `۞ الله` | many | — | Waqf `ۗ` / rub `۞` removed → all fold into `الله`. |
| `الرحمـن` (tatweel in every form) | many | `الرحمـن` | Tatweel removed → `الرحمن`; the waqf-split variant folds in too. |
| Ayah-number markers | per ayah | `١`… | Already excluded (`is_ayah_marker = true`); unaffected. |

---

## 12. Suggested Implementation Plan (not implemented)

Small, derived-tables-only change. **No re-import and no full DB reset required** — `quran_words`
and Feature 002 are untouched.

**Files likely to change**
- New: a normalization helper (pure function + its unit tests), e.g. in Domain/Application near the
  words feature (`WordIdentity` / `WordKeyNormalizer`).
- `infrastructure/.../Persistence/Repositories/Quran/Words/Display/DisplayWordsSql.cs` — compute and
  group on the normalized key; keep a representative raw form for display.
- The four display entities + EF configurations (`OrderedTashkeelWord`, `OrderedSimpleWord`,
  `UniqueTashkeelWord`, `UniqueSimpleWord` + `*Configuration.cs`) — add `word_key_*` column(s) and
  index/uniqueness on the key.
- `SqlDisplayWordsRebuilder.cs` hard checks — assert identity collapse (e.g. `الله` → 1 key,
  `الرحيم` stays 1) **and** keep a raw-faithfulness check; the existing
  `unique = COUNT(DISTINCT raw)` checks must move to `COUNT(DISTINCT normalized)`.

**Migration?** Yes if `word_key_*` columns are added (Option 3) — generated via EF tooling, not
hand-written. If instead normalization is applied in-place to the existing `text_*` grouping
(Option 2), no migration but a contract change.

**Rebuild only vs reset?** **Rebuild (`rebuild-words --force`) is sufficient** after the code +
(optional) migration. No `import-foundation` re-run; no `drop-db` / `reset-db`.

**Expected tests**
- Normalizer unit tests: mark removal, harakat retention (tashkeel), idempotency, the Section 11
  edge cases, the "do-not-merge" letter-like-mark cases.
- Rebuild identity tests: `الله`/`الرحمن`/`العظيم` collapse to a single key with correct summed
  occurrence/ayah counts; `الرحيم` unchanged; unique counts match the new normalized expectations
  (tashkeel ≈ 19,489; simple per the chosen simple rule).
- Updated hard-check tests reflecting identity-based (not raw-string) reconciliation.

**Expected reports**
- A post-rebuild normalization report (before/after unique counts, collapsed-key list, residual
  multiword tokens).
- Re-run the unique-tables audit (`words-unique-tables-audit-report.md`) to confirm no residual
  marks remain in identity keys.

**DB reset/drop needed?** **No.** Migration (if columns added) + `rebuild-words --force`.

---

## 13. Final Recommendation

**PASS WITH ACTION REQUIRED.** Keep the import faithful; the problem is solely that the four display
tables key on raw rendered strings. Implement **Option 3**: add normalized `word_key_tashkeel` /
`word_key_simple` identity columns, group and enforce uniqueness on them, preserve the raw text for
Mushaf/display, and target all future Gate/Topic links at the normalized key. Use the **conservative
layout/ornament/control removal** as the non-negotiable core (it alone fixes `الله`, `الرحمن`,
`العظيم` and removes the invisible `U+200F`), and decide the secondary tajwid/tatweel and letter-like
small-mark questions explicitly (Section 10) before finalizing the simple rule. The change is a
scoped, derived-tables-only rebuild — no re-import, no database reset.

*Generated read-only against the live local database and the on-disk source files; no changes were
made and nothing was committed.*
