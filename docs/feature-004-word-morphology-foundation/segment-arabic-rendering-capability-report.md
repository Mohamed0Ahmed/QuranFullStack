# Feature 004 — Segment Arabic Rendering Capability (Research Report)

**Date:** 2026-06-10
**Status:** Research/planning report only. **No code, no migrations, no source edits, no build/test were run.**
**Question:** Can we reliably produce an **Arabic display form for every morphology segment** in the
aligned Corpus file — i.e. transliterate each `segments[].form` (Buckwalter) into a normalized Arabic
string for a curator-facing segment table?

**File inspected (read-only):**
`~/Desktop/projects/Dashboard/resources/morphology/corpus/jsonData/quranic-corpus-morphology-qpc-aligned.json`
(77,432 words / **128,219 segments**). All figures below were measured directly by read-only analysis
passes over the full file (no sampling, except where a sample is explicitly named).

> **Scope reminder.** This is **not** about matching a segment as an exact substring of `qpcUthmani`
> (offsets are unsafe in Uthmani script). It is about **transliterating `form` → Arabic**. The actual
> Quran display text stays `quran_words.text_uthmani` / `qpc_glyph` and is **never** touched by this.

---

## 0. Executive summary

- **`form` is consistently QAC *extended* Buckwalter.** Across all 128,219 segments there are exactly
  **61 distinct characters**, and **every one maps** to a defined Arabic codepoint under the Quranic
  Arabic Corpus scheme. **Unknown/garbage characters: 0.**
- **Transliteration is deterministic and 100% character-mappable.** A fixed lookup table renders
  **every** non-empty form. (208 segments have an *empty* form — elided pronouns — with nothing to
  render; see §4.)
- **But the result is a morphological/phonemic spelling, not the Mushaf glyph.** Concatenating a word's
  transliterated segments reproduces `qpcUthmani` **exactly for 79.83 %** of words; the **20.17 %**
  that differ diverge in *systematic, meaningful* ways (ayah-level waqf marks, tanwin-iqlab small-meem,
  kashida carriers, decomposed hamza). So segment Arabic is a **derived reading aid**, **never** an
  Uthmani substring.
- **Confidence tiers (per segment):** ~**94.2 %** high-fidelity (clean letters + harakat, ± maddah),
  ~**5.4 %** carry Quranic annotation marks (correct but Mushaf-specific), ~**0.4 %** are display-fragile
  (kashida-borne hamza / leading combining marks) and need review, plus **1** multi-word token.
- **Recommendation: Option B** — store `form_arabic_normalized` **best-effort for all segments, with a
  confidence tier + source flag**, explicitly **not** claimed as Uthmani. Safest option that still
  serves the Arabic-first product (see §13).

---

## 1. Is `form` consistently Buckwalter? — **Yes (QAC extended)**

Distinct characters in all `form` values, with their QAC meaning:

| Group | Characters | Maps to |
|---|---|---|
| Consonants | `' \| > & < } A b p t v j H x d * r z s $ S D T Z E g f q k l m n h w Y y` | ء آ أ ؤ إ ئ ا ب ة ت ث ج ح خ د ذ ر ز س ش ص ض ط ظ ع غ ف ق ك ل م ن ه و ى ي |
| Harakat / tanwin / shadda / sukun | `a u i F N K ~ o` | َ ُ ِ ً ٌ ٍ ّ ْ |
| Quranic letter specials | `{` (ٱ alef wasla), `` ` `` (ٰ dagger alef), `_` (ـ tatweel), `^` (ٓ maddah), `#` (ٔ hamza-above seat) | — |
| Quranic annotation marks | `@` ۟ small-high rounded zero · `,` ۥ small waw (ṣila) · `.` ۦ small yeh (ṣila) · `[` ۢ small-high meem (iqlab) · `]` ۭ small-low meem · `"` ۠ pausal upright zero · `:` ۜ small-high seen · `;` ۣ small-low seen · `+` ۫ high stop · `!` ۨ small-high noon · `%` ۬ rounded high stop · `-` ۪ low stop | — |
| Token separator | space | multi-word token (`إِلْ يَاسِينَ`) — **1 occurrence** |

**Verification result:** `chars present: 61 — unmapped by table: NONE (100% mapped)`. There is no
character outside the QAC scheme. The "non-standard" punctuation symbols are **not noise** — each is the
QAC code for a specific Quranic combining mark.

---

## 2. Can we transliterate every `form` into normalized Arabic? — **Yes, deterministically**

A single fixed table (the full map is in §11) renders **every** of the **128,011 non-empty** forms.
There is no ambiguity: each Buckwalter character → exactly one Arabic codepoint. The 208 empty forms
(§4) carry no surface text, so there is nothing to transliterate (correctly `NULL`, not an error).

**Worked verification (concatenated segments vs `qpcUthmani`):**

| Location | `qpcUthmani` | Concatenated transliteration | Match? |
|---|---|---|---|
| `1:1:1` بِسْمِ | بِسْمِ | بِسْمِ | ✅ exact |
| `2:17:9` حَوْلَهُۥ | حَوْلَهُۥ | حَوْلَهُۥ (small-waw ṣila ✓) | ✅ exact |
| `2:10:9` أَلِيمٌۢ | أَلِيمٌۢ | أَلِيمٌۢ (iqlab meem ✓) | ✅ exact |
| `2:258:21` أَنَا۠ | أَنَا۠ | أَنَا۠ (pausal alef ✓) | ✅ exact |
| `11:41:6` مَجْر۪ىٰهَا | مَجْر۪ىٰهَا | مَجْر۪ىٰهَا (rare low-stop ✓) | ✅ exact |
| `1:5:1` إِيَّاكَ | إِيَّاكَ | إِيَّاكَ | ✅ exact |
| **`2:4:10`** وَبِٱلْـَٔاخِرَةِ | وَبِٱلْـَٔاخِرَةِ | وَبِٱلْ**ءَا**خِرَةِ | ❌ hamza decomposed |
| **`2:5:1`** أُو۟لَـٰٓئِكَ | أُو۟لَـٰٓئِكَ | أُو۟لَ**ٰٓ**ئِكَ | ❌ missing kashida carrier |

The exact matches show the transliteration is **high quality**; the mismatches show it is **not the
Uthmani glyph** (§6).

---

## 3. What percentage can be transliterated confidently?

**Character-level:** 100 % (0 unmapped). The meaningful question is **display fidelity**, tiered below
(by **segment occurrence**, over 128,011 non-empty forms):

| Tier | Definition | Segments | % | Distinct forms |
|---|---|---:|---:|---:|
| **T1a — clean core** | letters + harakat + wasla + dagger alef only | 116,447 | **90.97 %** | 11,116 |
| **T1b — + maddah `^`** | adds combining maddah (composable) | 4,177 | **3.26 %** | 571 |
| **T2 — Quranic marks** | contains ṣila/iqlab/pausal small marks (`@ , . [ ] " : ; + ! % -`) | 6,890 | **5.38 %** | 382 |
| **T3 — tatweel / kashida-hamza** | contains `_` or `#` (fragile clusters) | 496 | **0.39 %** | 134 |
| **T4 — multi-word** | contains a space | 1 | 0.00 % | 1 |
| **T0 — unknown char** | unmapped | **0** | **0 %** | 0 |

- **High confidence (T1a + T1b): ~94.2 %** — render as clean normalized Arabic.
- **Medium (T2): ~5.4 %** — transliterate **correctly** but carry Quranic annotation marks; visually
  Mushaf-specific, not a "dictionary" form.
- **Low / review (T3): ~0.4 %** — produce floating/leading combining clusters in isolation.

**By segment kind:**

| Kind | Total | T1a | T1b | T2 | T3 | T4 |
|---|---:|---:|---:|---:|---:|---:|
| **PREFIX** | 28,670 | 99.31 % | 0.69 % | — | — | — |
| **STEM** | 77,915 | 92.84 % | 4.29 % | 2.23 % | 0.64 % | 1 |
| **SUFFIX** | 21,426 | 72.97 % | 2.97 % | 24.06 % | — | — |

The high T2 share in **suffixes** is almost entirely the regular ṣila pronouns (`hu,`=هُۥ ×834,
`hi.`=هِۦ ×497) and the silent-alef plural (`wA@`=وا۟ ×2,853, `w^A@` ×513) — a handful of very
predictable patterns, not scattered noise.

---

## 4. Are there forms that cannot be safely transliterated?

**None are character-unmappable** (T0 = 0). The cases that are **display-fragile** (transliteratable
but not safe to present as a clean standalone Arabic token) are:

1. **Empty forms — 208 segments**, all `(SUFFIX, PRON)`: elided/implicit pronouns the corpus tags with
   features but **no surface form**. → render `NULL`; this is expected, not a failure.
2. **Kashida-borne hamza (`_#`) and embedded tatweel (`_`) — 496 segments (134 distinct forms):** a
   STEM like `>an[bi_#u` → `أَنۢبِـُٔ` ends in a floating kashida+hamza; `_#aAdamu` → `ـَٔادَمُ`
   *begins* with a kashida+hamza. Correct only when concatenated with neighbours; standalone they look
   broken.
3. **Segments beginning with a combining mark** (e.g. a suffix that is pure harakat, or the leading
   marks above): a standalone cluster starting with a combining char renders on a dotted circle in most
   fonts.
4. **Decomposed hamza/alef and missing kashida** (T1/T2 stems): transliteration is *correct Arabic* but
   **not** the Uthmani glyph (§6) — "unsafe" only if mislabelled as Uthmani.

So the honest statement is: **0 % untransliteratable; ~0.4 % display-fragile (manual review); 208
empty (NULL).**

---

## 5. Are prefixes (`wa`, `fa`, `bi`, `ka`, `li`, `{lo`, `sa`, …) safe? — **Yes, completely**

99.31 % of all prefix segments are clean-core (T1a); the rest (0.69 %) only add the composable maddah.
The **entire** distinct prefix inventory (28 forms) is trivially mappable:

```
wa(9572) {lo(5375) fa(3001) {l(2597) bi(2539) la(2085) li(1103) >a(485)
l~i(339) ka(295) lo(275) l~a(244) ya`^(190) ya`(170) l(128) sa(119)
{l~a(73) 'a(28) w~a(22) ta(9) b~i(5) ha`^(4) ha`(4) A^l(2) ha(2) A^lo(2) ya(1) {li(1)
```

→ وَ، ٱلْ، فَ، ٱل، بِ، لَ، لِ، أَ، لِّ، كَ، لْ، لَّ، يَٰٓ، يَٰ، ل، سَ، ٱلَّ … The only nuance is that
dagger-alef/maddah carriers (`ya`^`, `A^l`) omit the kashida the Mushaf uses (cosmetic). **Prefixes are
the safest class and can be mapped to Arabic display with full confidence.**

---

## 6. Are STEM forms safe to transliterate, or do they differ from QPC Uthmani? — **Transliterate yes; equal to Uthmani no**

STEM transliteration yields a **morphological/phonemic** Arabic spelling that **systematically differs**
from the Uthmani Mushaf glyph. The whole-word audit makes this precise:

| Whole-word agreement (77,432 words) | Count | % |
|---|---:|---:|
| concatenated translit **== `qpcUthmani`** | 61,815 | **79.83 %** |
| mismatch | 15,617 | **20.17 %** |
| — differ **only by kashida (U+0640)** | 4,859 | (cosmetic) |

**What drives the 20 % mismatch** (characters most involved):

| Mark | Codepoint | Mismatches | Nature |
|---|---|---:|---|
| ۭ small low meem | U+06ED | 4,708 | tanwin-iqlab annotation in Uthmani, often absent in `form` |
| space | U+0020 | 4,576 | trailing **waqf** mark separated by a space in `qpcUthmani` |
| ۚ small high jeem (waqf) | U+06DA | 1,972 | **ayah-level recitation mark — not morphology** |
| ۢ small high meem | U+06E2 | 1,933 | iqlab annotation mismatch |
| ۖ ۗ ۙ ۘ ۛ small high waqf ligatures | U+06D6/D7/D9/D8/DB | ~2,400 | **waqf marks — not part of any segment** |
| ـ tatweel | U+0640 | 1,170 | kashida carriers in Uthmani omitted by `form` |
| ٔ / ء hamza | U+0654 / U+0621 | 277 / 277 | Uthmani `ـَٔا` vs decomposed `ءَا` |
| ۞ rub-el-hizb, ۩ sajdah | U+06DE / U+06E9 | 199 / 15 | **structural Mushaf marks — not morphology** |

**Reading of this:** much of the divergence is **ayah-level waqf / structural marks** that *should not*
be in a morphological rendering at all (the `form` is correctly cleaner than `qpcUthmani`), plus genuine
orthographic differences (iqlab meem, kashida, hamza shape). **Conclusion:** STEM (and any) segment
Arabic is a **derived normalized reading** — useful, ~80 % glyph-identical, but **must never be stored
or presented as a QPC/Uthmani substring.**

---

## 7. Storage naming — **`form_arabic_normalized`, never `qpc_segment_text`**

The evidence in §2/§6 is decisive: `وَبِٱلْءَاخِرَةِ` ≠ `وَبِٱلْـَٔاخِرَةِ`, `أُو۟لَٰٓئِكَ` ≠
`أُو۟لَـٰٓئِكَ`. Calling the column `qpc_segment_text` would **falsely assert** it is an Uthmani Mushaf
substring. Use **`form_arabic_normalized`** (or `form_arabic`) — the name itself states it is a
*normalized transliteration*, not Uthmani. Keep the raw `form_buckwalter` alongside (lossless,
reversible, the actual source value).

---

## 8. Include in Feature 004, or defer? — **Include, as Option B**

Including it is justified: the transliteration is **deterministic, fully reviewed, ~94 % high-fidelity**,
and an Arabic-first curator dashboard should not force reading Buckwalter (`>uw@la`^}ika`). Deferring
removes a genuinely useful reading aid for **no** gain in Quran-text safety (the authoritative Mushaf
text is `quran_words` and is untouched either way). The risk is purely **misrepresentation**, which §7
+ §9 + §10 control. → **Include in 004 under Option B.**

---

## 9. If included: mandatory / best-effort / prefixes-only? — **Best-effort for ALL segments, with flags**

Recommended shape on `quran_word_morphology_segments`:

| Column | Type | Null | Meaning |
|---|---|---|---|
| `form_buckwalter` | `text` | NO | raw source value (already planned) — **always stored, lossless** |
| `form_arabic_normalized` | `text` | YES | transliteration; `NULL` for the 208 empty forms |
| `arabic_render_tier` | `text` | YES | `clean` (T1a/T1b) / `quranic_marks` (T2) / `review` (T3) / `multiword` (T4) |
| `arabic_render_source` | `text` | NO | constant `buckwalter-transliteration` (provenance; not Uthmani) |

- **Not mandatory-uniform (rejects Option A):** a single unflagged column would erase the
  high/medium/low-fidelity distinction and invite misuse.
- **Not prefixes-only:** stems are 97 % clean and are exactly what curators need; restricting to
  prefixes throws away the bulk of the value. Flag the fragile 0.4 % instead.
- **Best-effort = always produced for every non-empty form, stamped with its tier.** The UI can then
  show clean Arabic confidently, badge the `quranic_marks` rows, and route `review` rows to a curator.

---

## 10. Validation checks (if Arabic segment forms are generated)

Add to the morphology import gate (§ companion planning report uses `MORPH-*`):

| Id | Severity | Assertion |
|---|---|---|
| `MORPH-SEG-CHARSET` | **hard** | every `form` character is in the QAC map; **0 unmapped** (else refuse — a new char means the map is stale) |
| `MORPH-SEG-RENDER-TOTAL` | **hard** | every **non-empty** form yields a non-empty `form_arabic_normalized`; every empty form yields `NULL` (expected 208) |
| `MORPH-SEG-TIER-VALID` | **hard** | every rendered row has a valid `arabic_render_tier`; `arabic_render_source = 'buckwalter-transliteration'` |
| `MORPH-SEG-NOT-UTHMANI` | **hard (guard)** | the column is never written from `qpc_glyph`/`text_uthmani`; raw `form_buckwalter` is always present |
| `MORPH-SEG-WORD-AGREEMENT` | warning | per-word concatenated translit vs `qpcUthmani` exact-match rate ≈ **79.83 %**; report deviation (encoding-drift canary) |
| `MORPH-SEG-TIER-DIST` | warning | tier distribution ≈ 94.2 % / 5.4 % / 0.4 %; deviation → investigate |
| `MORPH-SEG-REVIEW-LIST` | warning | emit the full T3 (134 forms) + T4 (1) + empty (208) lists for manual sign-off |
| `MORPH-SOURCE-UNCHANGED` | hard | source files unchanged (shared with the morphology plan) |

---

## 11. Sample audit across all 128,219 segments

Run a **full pass** (not a sample) once, captured in a report artifact:

1. **Charset coverage:** assert 0 unmapped characters over all 128,219 segments (done here: ✅ 61 chars,
   0 unmapped). Re-run on every source refresh.
2. **Tier bucketing:** count every segment into T1a/T1b/T2/T3/T4 and compare to the baselines in §3.
3. **Whole-word agreement:** transliterate-and-concatenate each of the 77,432 words; record the exact
   match % (baseline **79.83 %**) and bucket mismatches by the differing codepoints (§6 table).
4. **Round-trip spot:** confirm `form_buckwalter` is retained verbatim for every row (re-derivable).
5. **Targeted sampling for human eyes:** ~20 randomly-sampled rows **per tier** + **100 %** of T3/T4 +
   the 208 empties, written to an audit JSON/Markdown for curator review.
6. **The full QAC map** used (single source of truth):

```
' ء   | آ   > أ   & ؤ   < إ   } ئ        A ا   b ب   p ة   t ت   v ث   j ج
H ح   x خ   d د   * ذ   r ر   z ز        s س   $ ش   S ص   D ض   T ط   Z ظ
E ع   g غ   f ف   q ق   k ك   l ل        m م   n ن   h ه   w و   Y ى   y ي
a ◌َ  u ◌ُ  i ◌ِ  F ◌ً  N ◌ٌ  K ◌ٍ        ~ ◌ّ  o ◌ْ  ` ◌ٰ  { ٱ   _ ـ   ^ ◌ٓ
# ◌ٔ  @ ◌۟  , ◌ۥ  . ◌ۦ  [ ◌ۢ  ] ◌ۭ        " ◌۠  : ◌ۜ  ; ◌ۣ  + ◌۫  ! ◌ۨ  % ◌۬   - ◌۪
(space) → multi-word token separator
```

---

## 12. Risky examples to inspect manually

| Case | Example(s) | Why risky |
|---|---|---|
| Kashida-borne hamza `_#` | `2:31:10` `>an[bi_#u` → `أَنۢبِـُٔ`; `2:33:2` `_#aAdamu` → `ـَٔادَمُ` (134 distinct) | floating/leading kashida+hamza; broken standalone |
| Decomposed hamza/alef | `2:4:10` `'aAxirapi` → `ءَاخِرَةِ` (Uthmani `ـَٔاخِرَةِ`); `2:8:8` `ٱلْءَاخِرِ` | `ءَا` vs Uthmani `ـَٔا` |
| Missing kashida carrier | `2:5:1` `>uw@la`^}ika` → `أُو۟لَٰٓئِكَ` (Uthmani `أُو۟لَـٰٓئِكَ`) | dagger-alef without kashida |
| Multi-word token | `37:130:3` `<ilo yaAsiyna` → `إِلْ يَاسِينَ` (1 only) | one token, two words (space) |
| Pausal alef | `2:258:21` `>anaA"` → `أَنَا۠` | small-high upright zero `"` |
| ṣila pronouns | `2:17:9` `hu,` → `هُۥ`; `2:22:13` `hi.` → `هِۦ` | small waw/yeh — verify they are intended for standalone display |
| Silent-alef plural | `2:6:3` `wA@` → `وا۟` | small-high rounded zero on otiose alef |
| Rare marks | `:`,`;`,`+`,`!`,`%`,`-` (`yaboS:uTu`, `muS;ayoTiruwna`, `ta>oma+n~a`, `nu!jiY`, `A%EojamiY~N`, `major-Y`` ) — 1–2 each | low-frequency Quranic marks; confirm codepoint mapping by eye |
| Empty forms | 208 × `(SUFFIX, PRON)` | nothing to render → must be `NULL`, not `""` |

---

## 13. Recommendation

| Option | Shape | Verdict |
|---|---|---|
| **A** | Add `form_arabic_normalized` for **all** segments, mandatory, single column, no flags | ❌ **Reject** — erases the fidelity tiers; an unflagged column invites treating Mushaf-divergent text (20 % differ from `qpcUthmani`) as authoritative. |
| **B** | Add `form_arabic_normalized` **best-effort for all**, with `arabic_render_tier` + `arabic_render_source`, **never** claimed as Uthmani; keep raw `form_buckwalter` | ✅ **Recommended.** |
| **C** | Do **not** render Arabic in 004; store only raw `form` + labels; defer rendering to a later feature | ➖ Safe but over-conservative — discards a deterministic, reviewed, ~94 %-high-fidelity reading aid for no gain in Quran-text safety. Acceptable only as a fallback. |

### Recommended: **Option B** — and why it is the safest *correct* choice

1. **The authoritative Quran text is never involved.** Mushaf display stays `quran_words.text_uthmani` /
   `qpc_glyph`. `form_arabic_normalized` is a **derived analysis aid** in the segments table — generating
   it cannot alter or endanger the Quran text.
2. **It is deterministic and lossless.** 100 % character coverage, 0 unknown; raw `form_buckwalter` is
   always kept, so the rendering is fully reproducible and reversible.
3. **The only real risk is misrepresentation — and B closes it.** The name `form_arabic_normalized`, the
   `arabic_render_source = buckwalter-transliteration` flag, the `arabic_render_tier`, and the
   `MORPH-SEG-NOT-UTHMANI` guard make it impossible to honestly mistake this for Uthmani. The ~80 %
   exact-match figure and the 20 % systematic divergence are documented, not hidden.
4. **It serves the Arabic-first product** without forcing curators to read Buckwalter, while honestly
   badging the ~5.4 % mark-bearing and ~0.4 % fragile rows.

**Option A** over-claims uniform fidelity; **Option C** sacrifices real, safe value for caution that
buys nothing (the Mushaf text is untouched under all options). **Option B is the safest option that is
also faithful and useful.**

### Quranic Data Safety guardrails (conditions of Option B)

- **Mushaf/display text is always `quran_words` (Uthmani/QPC)** — `form_arabic_normalized` is **never**
  used as Mushaf display and **never** presented as Uthmani.
- **Raw `form_buckwalter` is always stored** verbatim; the Arabic column is clearly *derived*.
- **No invention:** empty forms → `NULL`; fragile/`review` rows are flagged, not "fixed" by guessing.
- **The transliteration table is the single source of truth**, re-validated (`MORPH-SEG-CHARSET`) on
  every source refresh; a new character refuses the import rather than rendering a `�`.
- **Original Corpus and QUL source files remain read-only** (`MORPH-SOURCE-UNCHANGED`).
