# Investigation — Ayah 37:130 Word-Count Warning

**Scope:** Data + code inspection only. No source files were modified, no importer or database
command was run, and no Quranic text was altered or invented. All Arabic below is reproduced
**verbatim** from the named staged files (read-only) and must not be edited.

**Question:** the import finished `pass-with-warnings`; the only warning is
`Ayah 37:130 word-count differs (metadata 4 vs records 3); word records treated as canonical.`

**Verdict of this investigation:** **Not a bug, not data corruption.** It is the single, known
QPC word-segmentation special case (`إِلْ يَاسِينَ` stored as one word slot). All four word sources
agree perfectly. **Recommended action: A — keep the warning; word records are canonical.** The
codebase already encodes this as a documented, targeted regression guard (effectively option C).
Do **not** patch the staged metadata (B) and do **not** weaken validation (D).

---

## 1. What the metadata says for 37:130

From `metadata/quran-metadata-ayah.json` (entry id `3918`):

| Field | Value |
|---|---|
| `verse_key` | `37:130` |
| `surah_number` / `ayah_number` | `37` / `130` |
| `words_count` | **4** |
| `text` (verbatim) | `سَلَٰمٌ عَلَىٰٓ إِلۡ يَاسِينَ ١٣٠` |

Whitespace tokens in that text: `سَلَٰمٌ` · `عَلَىٰٓ` · `إِلۡ` · `يَاسِينَ` · `١٣٠`. The trailing `١٣٠`
is the ayah-number glyph, not a word — so `words_count = 4` counts **إِلۡ and يَاسِينَ as two
separate words**.

> Note: `words_count` is stored by the importer as `quran_ayahs.words_count_source` (provenance),
> **not** as the canonical count (`QuranFoundationAssembler.cs:81`).

---

## 2. Every word record for 37:130 (per source)

All four word sources contain exactly `37:130:1‥4`, identical `id`/`surah`/`ayah`/`word` per
location. Only the `text` column differs.

| location | id | word | `uthmani` | `uthmani-simple` | `imlaei-simple` | `qpc-v4` (glyph) |
|---|---|---|---|---|---|---|
| `37:130:1` | 61956 | 1 | `سَلَـٰمٌ` | `سلـم` | `سلام` | `ﱏ` |
| `37:130:2` | 61957 | 2 | `عَلَىٰٓ` | `على` | `على` | `ﱐ` |
| `37:130:3` | 61958 | 3 | `إِلْ يَاسِينَ` | `ال ياسين` | `ال ياسين` | `ﱑﱒ` |
| `37:130:4` | 61959 | 4 | `١٣٠` | `١٣٠` | `١٣٠` | `ﱓ` |

**Do all sources agree on locations? Yes — 100%.** Same four locations, same ids (61956–61959),
contiguous. This is why the hard `source-alignment` check passed with 0 mismatches.

Key observation: **`37:130:3` is a single word record whose text contains an internal space** —
two orthographic words (`إِلْ` + `يَاسِينَ`) occupy **one** word slot. In `uthmani-simple` and
`imlaei-simple` the same slot reads `ال ياسين` (also one record, internal space).

---

## 3. QPC glyph records for 37:130 (`mushaf/qpc-v4.json`)

| location | id | glyph text (verbatim) |
|---|---|---|
| `37:130:1` | 61956 | `ﱏ` |
| `37:130:2` | 61957 | `ﱐ` |
| `37:130:3` | 61958 | `ﱑﱒ` ← **two glyph codepoints in one word slot** |
| `37:130:4` | 61959 | `ﱓ` (ayah-number glyph) |

The glyph file confirms the segmentation from the visual side: word 3 is rendered by **two**
glyphs (`ﱑ` + `ﱒ` = إل + ياسين) but is still a **single** word entry.

---

## 4. Presence of 37:130:1 / :2 / :3 / :4 per source

| source | :1 | :2 | :3 | :4 | :5 |
|---|:--:|:--:|:--:|:--:|:--:|
| `words/uthmani.json` | ✅ | ✅ | ✅ | ✅ | — (none) |
| `words/uthmani-simple.json` | ✅ | ✅ | ✅ | ✅ | — |
| `words/imlaei-simple.json` | ✅ | ✅ | ✅ | ✅ | — |
| `mushaf/qpc-v4.json` | ✅ | ✅ | ✅ | ✅ | — |
| `metadata/quran-metadata-ayah.json` | n/a (ayah-level; keyed by id `3918`, `words_count=4`) ||||| 

There is **no** `37:130:5` in any source. The word files top out at `:4`, where `:4` is the
ayah-number marker.

---

## 5. The exact token responsible

**`37:130:3`.** The mismatch is entirely the well-known **`إل ياسين`** case:

- **Metadata** writes it as two space-separated tokens `إِلۡ` + `يَاسِينَ` → contributes **2** to
  `words_count` → total **4**.
- **Word records** (all four sources) store it as **one** word slot, `37:130:3` =
  `إِلْ يَاسِينَ` (uthmani) / `ال ياسين` (simple/imlaei) / `ﱑﱒ` (glyph) → contributes **1**.

So the difference is exactly 1, located at this single token. It is **not** a missing or dropped
record (no record is absent; ids are contiguous 61956–61959) — it is a **segmentation/counting
convention** difference: metadata counts `إل` and `ياسين` separately; the QPC word list keeps them
as one word slot.

How the two numbers are derived in code (`QuranFoundationAssembler.BuildAyahs`):

| Stored field | Formula | 37:130 value |
|---|---|---|
| `WordsCountSource` | `= ayah.WordsCount` (metadata) — `:81` | **4** |
| `WordsCountReal` | `= ayahWords.Count - 1` (total records minus the one trailing number marker) — `:82` | `4 - 1 =` **3** |

`FlagAyahMarkers` (`:241`) marks `37:130:4` (`١٣٠`) as the ayah marker because it is the last word
and its imlaei text is digits-only; the remaining **3** records are the readable words.

---

## 6. Is this a known segmentation difference? — Yes, and it is unique in this corpus

This is the recognized `إل ياسين` (Hafs/QPC orthography `إِلۡ يَاسِينَ`) tokenization edge case. To
confirm it is genuinely isolated, every word record whose `uthmani` text contains a space followed
by an Arabic **letter** (i.e. a candidate two-word slot) was enumerated across all 83,668 records:

- **197** candidate records, of which:
  - **195** are the rub‑el‑ḥizb ornament `۞` (U+06DE) prefixed to word 1 of an ayah
    (e.g. `63:4:1 = "۞ وَإِذَا"`) — a decorative division mark glued to the first word, **not** a
    word merge.
  - **1** is `5:52:12 = "دَآئِرَ ةٌۭ ۚ"` — an intra-word spacing artifact of the single word *dāʾira*
    plus a trailing pause mark, **not** two words.
  - **1** is `37:130:3 = "إِلْ يَاسِينَ"` — the **only** record in the entire muṣḥaf where two
    distinct words share one word slot.

(The broader “4,577 records contain a space” figure is dominated by trailing **waqf/pause marks**
like `ۚ ۖ ۗ` spaced after a word — also not merges.)

**Conclusion:** `37:130` is the single ayah where the QPC word segmentation and the ayah-level
`words_count` legitimately disagree. The discrepancy is inherent to the upstream data convention,
reproduced faithfully; nothing in the staging tree is wrong.

---

## 7. Recommendation

### ✅ A — Keep the warning; word records are canonical (already implemented)

This is the correct and lowest-risk choice, and the code already does it well:

- The importer treats the **word records as canonical** (`WordsCountReal = records − 1`) and **also
  preserves** the metadata value as `WordsCountSource`. Both `4` and `3` are stored on
  `quran_ayahs` for 37:130, so the discrepancy is auditable in the DB — nothing is lost.
- `QuranImportValidator.BuildAyah37130Check` (`:175`) is a **targeted regression guard**: it asserts
  the discrepancy is *exactly* `source 4 / real 3` and surfaces it as a **warning**, not an error
  (`ImportValidationCheckIds.Ayah37130Count` → `Warning`). If the upstream data ever changed (e.g. a
  re-segmentation, a dropped record, or a different count), this check would no longer pass and the
  anomaly would be caught immediately. That is exactly the behaviour you want.
- This is, in effect, **option C (an explicit documented exception)** already in place — the warning
  message and the dedicated check *are* the documentation. No additional change is needed.

### ❌ B — Patch staged metadata `words_count` to 3 — **reject**

- It edits a staged Quran source file (violates the task/provenance rule “source bytes must not be
  modified”) and would misrepresent the upstream metadata.
- It is also pointless for correctness: the DB already stores the canonical `WordsCountReal = 3`. The
  only effect would be to **destroy** the `source 4 / real 3` provenance signal and silence a useful
  guard.

### ❌ D — Change importer validation logic — **reject**

- The validation is already correct (hard checks all pass; this is the lone, expected warning).
  Removing or loosening the check would discard the regression guard and the audit trail for a known
  data quirk, for no benefit.

### Optional, non-code follow-up (nice-to-have, not required)

If you want the rationale discoverable outside the code, add a one-line note to
`specs/002-mushaf-words-foundation/source-provenance.md` (a tracked doc) recording that `37:130` is
a known `إل ياسين` segmentation exception (`source 4 / real 3`). This is documentation only — it
does **not** touch source data or code.

---

## Evidence index (read-only)

| Claim | Source |
|---|---|
| metadata `words_count=4`, text | `resources/import-sources/quran-foundation/metadata/quran-metadata-ayah.json` (id `3918`) |
| word records `37:130:1‥4` | `.../words/{uthmani,uthmani-simple,imlaei-simple}.json`, `.../mushaf/qpc-v4.json` |
| glyph `ﱑﱒ` for word 3 | `.../mushaf/qpc-v4.json` |
| `WordsCountSource` / `WordsCountReal` derivation | `Backend/application/.../ImportQuranFoundation/QuranFoundationAssembler.cs:81-82` |
| ayah-marker flagging (digits-only last word) | `QuranFoundationAssembler.cs:241-255` |
| 37:130 regression guard (warning, source 4 / real 3) | `Backend/application/.../Validation/QuranImportValidator.cs:175-190`; `ImportValidationCheckIds.cs:21,43,65`; `ImportValidationWarnings.cs:7-16` |
| uniqueness of the merged slot across the corpus | scan of all `uthmani` records (space + Arabic letter) → only `37:130:3` is a true two-word slot |
