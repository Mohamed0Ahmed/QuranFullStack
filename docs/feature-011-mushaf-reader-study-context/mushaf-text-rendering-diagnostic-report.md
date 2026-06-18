# Mushaf Text Rendering — Diagnostic Report

**Feature:** 011 — Mushaf Reader Study Context
**Scope:** Diagnostic / report only. **No code was modified. No data was changed.**
**Date:** 2026-06-18
**Author:** Claude (diagnostic pass)

---

## 0. TL;DR Verdict

Two independent rendering issues, both in the **frontend rendering path** — not in the
stored Quran text, not in the database, and not (primarily) in the font choice:

1. **"Black dot-like marks between words" are real characters in `quran_words.text_uthmani`** —
   specifically **Quranic waqf (pause) signs and small-high annotation marks** (e.g.
   `U+06DB ARABIC SMALL HIGH THREE DOTS`, `U+06DA SMALL HIGH JEEM`, `U+06D6/06D7` pause
   ligatures, and the pervasive `U+06DF SMALL HIGH ROUNDED ZERO`). They are **not** CSS
   bullets, **not** `::before`/`::after`, **not** list markers, and **not** a missing font.
   They look like detached dots because **the data stores many of them as a trailing
   `SPACE + isolated combining mark`** (4,376 word entries), and the reader renders **each
   word as its own inline `<button>`** with `white-space: pre-wrap`. An isolated combining
   mark sitting after a space, at an inline-box boundary, has no base letter to attach to,
   so the text engine renders it on a **dotted-circle placeholder** (`U+25CC`, which this
   font even ships a glyph for). The three-dots / rounded-zero marks are themselves
   dot/circle-shaped. **These are legitimate Quranic marks and must be preserved.**

2. **The page looks "right-biased / not centered" because of layout CSS, plus glued words.**
   `.mushaf-line { text-align: start }` resolves to **right** under `dir="rtl"`, so each line
   is right-aligned with a ragged left edge (a real Madani Mushaf page is *justified*
   edge-to-edge). Compounding this: **94.5% of word entries (79,091 / 83,668) contain no
   space**, and adjacent per-word inline buttons carry no inter-element whitespace, so most
   words render **glued together** with no gap, while the ~5% waqf-bearing words have an
   internal space — producing uneven, cramped, non-Mushaf-like spacing.

> **⚠ SUPERSEDED:** the original draft concluded "the font is not the root cause." That was
> wrong for the *dominant* dots. A later live HarfBuzz investigation (see **§10 Addendum**)
> proved the current font file **is** the root cause of the prominent `U+06DF` dots. The
> sub-sections below that say "font is fine" (§1.2, §3.2, finding F4) are corrected by §10.

---

## 0.1 Addendum summary (verified — read §10 for detail)

Live shaping with HarfBuzz (the same engine browsers use) plus the running-app screenshot
proved:

- The dominant black dots are **`U+06DF ARABIC SMALL HIGH ROUNDED ZERO`** (e.g. on the silent
  و in **أُو۟لَـٰٓئِكَ**). The current font file `UthmanicHafs_V22.ttf` ("KFGQPC HAFS Uthmanic
  Script") shapes `U+06DF` to a **wide baseline glyph** (advance 1442, outline `y:[-210,1045]`)
  — a circle sitting on the line — and ships **no OpenType feature** that repositions it.
- **Amiri** (already bundled, already the fallback) shapes the same `U+06DF` to a correct
  **zero-advance high mark** (outline `y:[1690,2114]`, width 352).
- **Fix applied:** `--qd-font-quran` now resolves to `'Amiri', serif` (the broken
  `'Uthmanic Hafs'` `@font-face` was removed). This is complementary to the in-progress
  presentation fixes (`toMushafWordDisplayText` waqf-space trim, `margin-inline-end` word
  spacing, `text-align: center` lines), which fix the layout + isolated-waqf-mark items but
  not the font-glyph issue.

---

## 1. Source of the black dot-like marks

### 1.1 They are NOT CSS-generated separators / bullets / list markers

Searched the entire Mushaf feature for `::before`, `::after`, `content:`, and `list-style`:

```
$ grep -rn "::before|::after|content:|list-style" src/app/features/mushaf
# (no matches in the page/line/word/marker rendering path)
```

- `mushaf-page-view`, `mushaf-line`, `mushaf-word`, `mushaf-marker` SCSS contain **no
  pseudo-element or list-marker rules**.
- The page text is rendered with a **plain interpolation binding**, not `innerHTML`:
  `mushaf-word.component.html` → `{{ word().textUthmani }}` inside a `<button>`.
- The only `[innerHTML]` / `safeHtml` usages in the feature are in the **study cards**
  (`tafsir-card`, `translation-card`, `full-i3rab-card`) — **not** the Mushaf page text.

➡ **Conclusion:** the dots are not injected by CSS or by sanitized HTML.

### 1.2 They are NOT a font-fallback / unsupported-glyph (tofu) problem

- `--qd-font-quran: 'Uthmanic Hafs', 'Amiri', serif;` (`src/styles/_tokens.scss:17`).
- `@font-face` for `Uthmanic Hafs` → `url('/assets/fonts/quran/UthmanicHafs_V22.ttf')`
  (`src/styles/_typography.scss:1-7`).
- The file **exists** at `src/assets/fonts/quran/UthmanicHafs_V22.ttf` (~290 KB, valid TTF),
  `angular.json` maps `src/assets` → `/assets`, and it appears in the build output
  (`dist/quran-dashboard-ui/browser/assets/fonts/quran/UthmanicHafs_V22.ttf`). The font URL
  resolves.
- Glyph-coverage check of the font (`fc-query` charset) confirms **all relevant codepoints
  are present** in the font (see §3.2), including the dotted-circle placeholder `U+25CC`.

➡ **Conclusion:** the intended font loads and has the glyphs; this is not tofu / fallback.

### 1.3 They ARE actual Quranic marks in `text_uthmani` (root cause)

The backend maps the stored value verbatim — no reconstruction, no trimming:

> `EfMushafPageReader.MapWord(...)` → `word.TextUthmani` → `MushafWordDto.textUthmani`
> (`Backend/.../EfMushafPageReader.cs:180-186`). Contract: *"`textUthmani` is authoritative;
> never reconstructed from segments."* (`contracts/mushaf-page.api.md:63`).

Analysis of the staged source data
(`resources/import-sources/quran-foundation/words/uthmani.json`, 83,668 word entries — the
same shape that feeds `quran_words.text_uthmani`) shows the dot-like marks are stored Quranic
annotation signs. See §2 for the exact codepoints and examples.

**Why they look like *detached* dots *between* words:** 4,376 entries store a waqf mark as a
**trailing `U+0020` + a single combining mark**, e.g. `'رَيْبَ ۛ'` = `…U+0628 U+064E U+0020
U+06DB`. The reader renders **one inline `<button>` per word** (`mushaf-line.component.html`
loops `qd-mushaf-word`), each with `white-space: pre-wrap` (`mushaf-word.component.scss:6`),
so the trailing space is preserved and the combining mark ends up isolated at the trailing
edge of its own inline box — with no preceding base glyph in that run. Isolated combining
marks are rendered by the text engine on a **dotted-circle base (`U+25CC`)**, which this font
supplies a glyph for → a small ring/dot in the inter-word gap. Several of the marks are
inherently dot/circle-shaped even when attached (three dots, rounded zero, filled-centre
stop), reinforcing the effect.

---

## 2. Unicode evidence

All examples below are from real Uthmani word data and were extracted with `python3` +
`unicodedata` (codepoints + official names). Display text is preserved; nothing was mutated.

### 2.1 Marks present in the data (by frequency)

| Codepoint | Unicode name | Category | Count | Shape / note |
|---|---|---|---|---|
| `U+064E` | ARABIC FATHA | Mn | 122,948 | normal vowel (not a dot) |
| `U+0650` | ARABIC KASRA | Mn | 45,970 | normal vowel |
| `U+064F` | ARABIC DAMMA | Mn | 37,320 | normal vowel |
| `U+0652` | ARABIC SUKUN | Mn | 37,148 | normal mark |
| `U+0651` | ARABIC SHADDA | Mn | 22,678 | normal mark |
| `U+0670` | ARABIC LETTER SUPERSCRIPT ALEF | Mn | 9,725 | dagger alef |
| `U+06ED` | ARABIC SMALL LOW MEEM | Mn | 4,807 | small low mark |
| **`U+06DF`** | **ARABIC SMALL HIGH ROUNDED ZERO** | Mn | **3,988** | **small circle above** |
| `U+06E2` | ARABIC SMALL HIGH MEEM ISOLATED FORM | Mn | 2,445 | small high mark |
| **`U+06DA`** | **ARABIC SMALL HIGH JEEM** | Mn | **1,972** | **waqf pause (looks dot-like)** |
| **`U+06D6`** | ARABIC SMALL HIGH LIGATURE SAD…ALEF MAKSURA | Mn | 1,682 | waqf pause sign |
| `U+06E5` | ARABIC SMALL WAW | Lm | 1,257 | small superscript waw |
| `U+06E6` | ARABIC SMALL YEH | Lm | 957 | small superscript yeh |
| **`U+06D7`** | ARABIC SMALL HIGH LIGATURE QAF…ALEF MAKSURA | Mn | 603 | waqf pause sign |
| `U+06DE` | ARABIC START OF RUB EL HIZB | So | 199 | ۞ ornament |
| **`U+06E0`** | **ARABIC SMALL HIGH UPRIGHT RECTANGULAR ZERO** | Mn | **66** | **small rectangle/dot** |
| **`U+06DB`** | **ARABIC SMALL HIGH THREE DOTS** | Mn | **12** | **literally three dots** |
| `U+06D8` | ARABIC SMALL HIGH MEEM INITIAL FORM | Mn | 22 | waqf-related |
| `U+06DC` | ARABIC SMALL HIGH SEEN | Mn | 7 | small mark |
| **`U+06EC`** | **ARABIC ROUNDED HIGH STOP WITH FILLED CENTRE** | Mn | **1** | **filled black dot** |
| `U+06EB` | ARABIC EMPTY CENTRE HIGH STOP | Mn | 1 | hollow dot |
| `U+06E9` | ARABIC PLACE OF SAJDAH | So | 15 | sajdah ornament |

(Full list captured during analysis; the **bold** rows are the inherently dot/circle-shaped
marks most consistent with the user's report.)

### 2.2 Examples with surrounding words (verbatim, with codepoints)

Standalone waqf marks stored as `… SPACE + mark` (these float in the inter-word gap):

```
'رَيْبَ ۛ'        -> [U+0631 U+064E U+064A U+0652 U+0628 U+064E  U+0020  U+06DB]   (SMALL HIGH THREE DOTS)
'فِيهِ ۛ'         -> [U+0641 U+0650 U+064A U+0647 U+0650        U+0020  U+06DB]
'حُرُمٌ ۗ'        -> [... U+0645 U+064C                          U+0020  U+06D7]   (waqf QAF-LAM-ALEF)
'وَٱلنُّورَ ۖ'     -> [... U+0631 U+064E                         U+0020  U+06D6]   (waqf SAD-LAM-ALEF)
'وَنِسَآءًۭ ۚ'     -> [... U+064B U+06ED U+0020 U+06DA]                            (SMALL HIGH JEEM)
```

Inherently circular marks attached to a base letter (small "zeros" above word-final letters):

```
'ٱتَّقُوا۟'        -> [U+0671 U+062A U+0651 U+064E U+0642 U+064F U+0648 U+0627  U+06DF]  (rounded zero)
'وَأَنَا۠'         -> [U+0648 U+064E U+0623 U+064E U+0646 U+064E U+0627        U+06E0]   (rectangular zero)
'ءَا۬عْجَمِىٌّۭ'    -> [U+0621 U+064E U+0627 U+06EC ...]                                 (filled-centre stop)
```

### 2.3 Classification

- **What they are:** Quranic **waqf (pause) signs** and **small-high recitation marks** —
  legitimate, source-correct content of the Uthmani text.
- **What they are NOT:** punctuation, bullets, CSS content, or unknown/fallback glyphs.
- **Secondary observation — ayah markers:** the ayah-end "marker" words are stored as a
  **bare Arabic-Indic digit** (e.g. `'١'` = `U+0661`), flagged `isAyahMarker=true`
  (6,236 such entries). They render as small muted digits via
  `.mushaf-word--marker { color: var(--qd-text-muted) }`. They are **not** the black dots,
  and per the constraints they should **not** be removed if intentionally displayed.

---

## 3. Font application diagnosis

### 3.1 Where the Quran font is applied

| Element | `font-family` | Source |
|---|---|---|
| Reader shell (`.mushaf-reader`) / body | `'IBM Plex Sans Arabic', system-ui, sans-serif` | `src/styles.scss:20` |
| Mushaf page container (`.mushaf-page-view`) | *inherits* body sans (no override) | `mushaf-page-view.component.scss` |
| Mushaf line (`.mushaf-line`) | *inherits* (no override) | `mushaf-line.component.scss` |
| **Mushaf word (`.mushaf-word`)** | **`var(--qd-font-quran)` = `'Uthmanic Hafs','Amiri',serif`** | `mushaf-word.component.scss:3` |
| Mushaf marker pill (`.mushaf-marker`, juz/hizb/rub/sajda) | *inherits* sans | `mushaf-marker.component.scss` |

**Observation:** the Quran font is applied **only at the word `<button>` level**, not on the
line or page container. That is sufficient for the visible Quran glyphs (which all live inside
word buttons), but it means the **font boundary coincides with the per-word inline-box
boundary** — exactly where the isolated trailing waqf mark sits. The division-marker pills
(juz/hizb/rub/sajda) intentionally use the sans UI font; that is correct and unrelated to the
dots.

### 3.2 Is the intended font actually applied? — Yes, and it covers the glyphs

`fc-query` charset membership test against `UthmanicHafs_V22.ttf` (≈608 codepoints total):

```
U+0020 SPACE                 -> present
U+06D6 waqf SLA              -> present
U+06D7 waqf QLA              -> present
U+06D8 high meem init        -> present
U+06DA high jeem             -> present
U+06DB high THREE DOTS       -> present
U+06DF high rounded zero     -> present
U+06E0 upright rect zero     -> present
U+06EC filled-centre stop    -> present
U+06ED small low meem        -> present
U+0661 arabic-indic 1        -> present
U+06DD end of ayah           -> present
U+25CC DOTTED CIRCLE         -> present   <-- placeholder glyph for isolated combining marks
```

➡ The font is **the correct intended font, loaded, and complete** for this data. No mixed-font
risk inside the Quran text (all word glyphs use one family). The font ships a `U+25CC` glyph,
which is *why* an isolated combining mark renders as a visible dotted ring rather than
silently vanishing.

### 3.3 Would testing without the custom font help?

**Yes — as a confirmation toggle, not a fix.** Temporarily forcing the Mushaf word font to a
generic family (or removing `--qd-font-quran`) and re-checking the dots would:
- distinguish "font draws these as dots" from "the *characters* are dot-shaped regardless of
  font" (expected result: the dot-like marks **persist** with any Arabic-capable font, because
  they are real codepoints — confirming §1.3); and
- confirm the marks are not a font-specific artifact.

This is a diagnostic toggle only. **Do not ship it** — the Uthmanic Hafs font is the intended,
correct face for this text.

---

## 4. Line layout diagnosis

### 4.1 Is rendering based on `quran_mushaf_lines`? — Yes

`EfMushafPageReader` reads `QuranMushafLines` ordered by `LineNumber`, groups words by
`LineNumber` (ordered by `LineWordOrder`), and emits one `MushafLineDto` per stored line
(`EfMushafPageReader.cs:28-102`). The frontend renders one `<qd-mushaf-line>` per line:

```
mushaf-page-view.component.html:  @for (line of page().lines; ...) { <qd-mushaf-line .../> }
```

➡ Line structure is faithful to `quran_mushaf_lines`.

### 4.2 Is each line an independent container? — Yes (this part is correct)

- `.mushaf-page-view { display: flex; flex-direction: column }` stacks lines vertically.
- `.mushaf-line { display: block }` — each line is its own block box.

➡ **Words from different lines cannot wrap into one paragraph.** That behavior is correct and
is *not* a bug. (Within a single line, words *can* still wrap if the line overflows its width,
because words are `display:inline` with `white-space:pre-wrap` and there is no
no-wrap/justify-to-fit constraint — a real fixed-width Mushaf line should fit on one row.)

### 4.3 Centered vs right-aligned — this is the layout bug

```
.mushaf-line {
  display: block;
  text-align: start;     /* under dir="rtl" -> RIGHT */
  ...
  &--centered, &--surah-name { text-align: center; }
}
```

- The page is RTL (`<html dir="rtl">`, reader `dir="rtl"`), so `text-align: start` =
  **right-aligned**. Ordinary ayah lines therefore hug the **right** edge with a **ragged left
  edge** → the "pushed / right-biased, not centered" appearance.
- Only `surah_name` and `isCentered` lines are centered. A traditional Madani Mushaf page is
  **fully justified** (`text-align: justify` with the last line handled), so each line spans
  the full column width edge-to-edge. The current code never justifies.

### 4.4 Inter-word spacing — compounding cause of the "not a readable Mushaf page" look

- **94.5% of word entries (79,091 / 83,668) contain no space character at all.**
- Each word is a **separate inline `<button>`**, and there is **no inter-element whitespace**
  rendered between adjacent `<qd-mushaf-word>` buttons.
- Therefore most adjacent words render **glued together with no gap**, while the ~4,400
  waqf-bearing words carry an *internal* space — yielding **inconsistent, cramped spacing**
  that does not read like a Mushaf line. This interacts with the right-alignment to make the
  block look dense and off-center.

### 4.5 List semantics / browser default markers? — No

The page uses `div` / `button` elements (flex + block), **not** `ul`/`ol`/`li`. There are no
list markers, and no default browser list spacing is involved. The only "markers" are the
labeled division pills (juz/hizb/rub/sajda), which are inline-flex `span`s, not list bullets.

---

## 5. Recommended minimal fix plan (recommendation only — not implemented)

> Hard constraints respected throughout: **preserve** clickable word buttons, selected-word
> behavior, selected-ayah behavior, URL-state behavior, and sanitized-HTML policy; **do not**
> strip Quranic stop marks from stored text; **do not** remove intentional ayah markers; **do
> not** mutate `quran_words.text_uthmani`.

### 5.1 Must-fix rendering bugs

1. **Stop isolated waqf marks from rendering as detached dotted-circles.** The data stores
   `… SPACE + combining mark` at word ends. The fix is **at the rendering layer, not the
   data**. Options, in order of safety:
   - **(a) Shaping continuity:** render the words of a line within a **single shaping run**
     (e.g. keep the inline buttons but ensure they are direct inline siblings with no block
     boundary, or render line text as one shaped string and overlay clickable word spans),
     so a trailing mark attaches to its proper base instead of being isolated. This removes
     the dotted-circle placeholder while keeping the mark visible and correct.
   - **(b) Keep the mark glued to its base inside the same inline box** (no isolating space at
     an inline-box edge) — a *presentation-time* grouping, **without editing stored text**.
   - Verify with the §5.3 toggle that the dots are placeholder artifacts, not the mark glyphs
     themselves, before choosing (a) vs (b).
2. **Restore inter-word spacing.** Because 94.5% of words have no trailing space and buttons
   are glued, add **presentation-level word spacing** (e.g. a real space/`word-spacing`
   between word buttons, or `gap` on an inline-flex line) so words are visually separated —
   **without** writing spaces into stored data. Keep `isAyahMarker` digits spaced consistently.

### 5.2 Safe CSS / layout improvements

- **Justify Mushaf lines** for a true Mushaf page feel: `text-align: justify` on ayah lines
  (with `text-align-last` handling for the final line), keeping `center` for `surah_name` /
  `isCentered`. At minimum, change ayah lines away from ragged right-alignment.
- Consider preventing intra-line wrapping so a stored Mushaf line stays on one visual row
  (e.g. constrain/scale the line rather than wrapping mid-line), since lines are already
  authoritative from `quran_mushaf_lines`.
- Optionally apply `--qd-font-quran` higher up (line/page container) so the font boundary no
  longer coincides with the per-word inline boxes — only if it helps the shaping fix in 5.1.

### 5.3 Optional diagnostic toggles (temporary, do not ship)

- **Font toggle:** temporarily set the Mushaf word `font-family` to a generic Arabic font and
  confirm the dot-like marks **persist** (expected) → proves the marks are real characters,
  not a font artifact (§3.3).
- **Reveal-marks toggle:** in DevTools, inspect a dot in the live DOM and confirm it is a
  combining codepoint (e.g. `U+06DB`/`U+06DA`/`U+06DF`) plus possibly an inserted `U+25CC`,
  rather than a pseudo-element — confirms §1.1/§1.3 on the running app.
- **Outline toggle:** add a temporary `outline` on `.mushaf-word` / `.mushaf-line` to visualize
  the glued-word boxes and right-alignment described in §4.

### 5.4 Test coverage to add / update

- A rendering test asserting **no dotted-circle / isolated-mark artifact** for a word whose
  `textUthmani` ends with `SPACE + waqf mark` (use a **source-safe synthetic** fixture, e.g.
  `"اختبار "` + a single waqf codepoint, per the workspace Quranic-test-data-safety rule).
- A test asserting **visible separation between adjacent word buttons** (inter-word spacing
  present), and that **clickable/selected behavior is unchanged** after the spacing fix.
- A layout test asserting ayah lines are **justified/centered as intended** and that
  `surah_name` / `isCentered` lines remain centered.
- Guard test: rendering must **not** mutate `textUthmani` (the displayed string equals the DTO
  string, marks intact) — protects the "never strip stop marks" constraint.

---

## 6. Files inspected

**Frontend (rendering path):**
- `src/app/features/mushaf/components/mushaf-page-view/{html,scss,ts}`
- `src/app/features/mushaf/components/mushaf-line/{html,scss,ts}`
- `src/app/features/mushaf/components/mushaf-word/{html,scss,ts}`
- `src/app/features/mushaf/components/mushaf-marker/{html,scss,ts}`
- `src/app/features/mushaf/components/mushaf-page-area/{html,scss}`
- `src/app/features/mushaf/pages/mushaf-reader-page/{html,scss}`
- `src/app/features/mushaf/models/mushaf.models.ts`
- `src/styles.scss`, `src/styles/_tokens.scss`, `src/styles/_typography.scss`, `src/index.html`
- `angular.json` (asset mapping), `src/assets/fonts/quran/UthmanicHafs_V22.ttf` (font coverage)

**Backend (data-shape verification only):**
- `Backend/.../Persistence/Reads/Quran/MushafReader/EfMushafPageReader.cs`
- `specs/011-mushaf-reader-study-context/contracts/mushaf-page.api.md`

**Data (read-only, for Unicode evidence):**
- `resources/import-sources/quran-foundation/words/uthmani.json` (83,668 entries)

**Tooling used:** `grep`, `find`, `fc-query` (font charset), `python3` + `unicodedata`
(codepoint/name analysis). All read-only.

---

## 7. Findings by severity

| # | Severity | Finding | Where |
|---|---|---|---|
| F1 | **High** | "Black dots" are real Quranic waqf/annotation marks; isolated trailing `SPACE+mark` at per-word inline-box edges renders on a dotted-circle placeholder | data + `mushaf-word` rendering (§1.3, §2, §3.2) |
| F2 | **High** | 94.5% of words have no space + glued inline buttons → most words render with no inter-word gap; spacing is uneven | `uthmani.json` + `mushaf-line`/`mushaf-word` (§4.4) |
| F3 | **Medium** | Ayah lines are right-aligned (`text-align: start` under RTL), not justified/centered → "right-biased" look | `mushaf-line.component.scss` (§4.3) |
| F4 | **High (corrected)** | ~~Font is fine~~ → The font file `UthmanicHafs_V22.ttf` shapes `U+06DF` (and treats it) as a **wide baseline glyph**, producing the dominant dots; **this is the root cause of the prominent dots**. Amiri shapes it correctly. See §10. | HarfBuzz shaping (§10) |
| F5 | **Low/Info** | Ayah markers are bare Arabic-Indic digits (muted); intentional, not the dots — keep | data + `mushaf-word--marker` (§2.3) |
| F6 | **Low/Info** | Line stacking is correct (independent block lines; no cross-line wrap); no list semantics involved | `mushaf-page-view`/`mushaf-line` (§4.2, §4.5) |

---

## 8. Recommended next steps

1. Confirm F1 live with the §5.3 toggles (font-off persistence + DevTools codepoint inspection).
2. Implement the §5.1 must-fixes (shaping continuity / mark grouping + inter-word spacing) as a
   **rendering-only** change — no edits to stored `text_uthmani`.
3. Apply §5.2 line justification.
4. Add §5.4 tests (source-safe synthetic fixtures) and re-run the reader rendering specs.
5. Route any implementation through the normal review (`engineering-review` / `test-guard`)
   before merge.

---

## 9. Confirmation

**Diagnostic pass (sections 1–8):** read-only — no code, styles, Quran text, or database data
changed. Evidence came from source files, the staged source data package, and the bundled font.

**Follow-up fix (section 10), explicitly authorized by the user after the live investigation:**
the only code change is the **Quran font for the Mushaf text** (`--qd-font-quran` → `'Amiri',
serif`, and removal of the unused `'Uthmanic Hafs'` `@font-face`). **No Quran text, no database
data, and no stored `quran_words.text_uthmani` were touched**; the marks remain fully displayed.
A separate, already-staged presentation fix (`toMushafWordDisplayText`, word spacing, line
centering) is in-progress work by the user, not authored here.

---

## 10. Addendum — live HarfBuzz investigation & resolution

**Why this addendum exists:** after the report's layout/waqf fixes were applied, the dots
persisted in the running app (user screenshot of Al-Baqarah 1–5). A deeper, browser-faithful
investigation was run.

### 10.1 Method

Standard browser engines (Chrome/Firefox) shape Arabic with HarfBuzz. The same engine was run
offline via `harfbuzzjs` against the actual font files, shaping the exact on-screen clusters and
reading glyph advances/offsets/outline bounds. The font's OpenType feature table was also dumped.

### 10.2 Evidence

- **Data (exact codepoints, verse 2:5):** `أُو۟لَـٰٓئِكَ` = `… و(U+0648) ۟(U+06DF) ل(U+0644) …`.
  `U+06DF` is **mid-word, attached to a base letter** (it never follows a space — 0/3988
  occurrences), so it is **not** an isolated-mark/dotted-circle case.
- **Font shaping of `U+06DF`:**

  | Font | advance | outline `y` range | width | result |
  |---|---|---|---|---|
  | `UthmanicHafs_V22.ttf` (current) | **1442** | `[-210, 1045]` | **1255** | large circle on the baseline → the visible dot |
  | `Amiri` (bundled fallback) | **0** | `[1690, 2114]` | 352 | small high mark above the letter → correct |

- **Font features:** GSUB `calt, fina, init, liga, medi, rlig`; GPOS `curs, kern, mark, mkmk`.
  All are default-on; there is **no stylistic set or non-default feature** to toggle via
  `font-feature-settings` that would reposition `U+06DF`. So the baseline rendering is intrinsic
  to this file under web shaping.
- The waqf marks `U+06D6`/`U+06DB` (the smaller scattered dots) **are** small high glyphs in this
  font; they float only when isolated after a space at a word/button boundary — handled by the
  staged `toMushafWordDisplayText` helper.

### 10.3 Verdict (corrected)

The **dominant dots are a font-glyph defect**: `UthmanicHafs_V22.ttf` renders `U+06DF` (and
similar small-high "zero" marks) as a wide baseline circle. The marks themselves are authentic
Quranic content and remain in the data. The original report's "font is fine" was wrong; §1.2/§3.2
are corrected here.

### 10.4 Fix applied

- `src/styles/_tokens.scss`: `--qd-font-quran: 'Amiri', serif;` (was `'Uthmanic Hafs', 'Amiri',
  serif`).
- `src/styles/_typography.scss`: removed the now-unused `'Uthmanic Hafs'` `@font-face`; `$font-quran`
  → `'Amiri', serif`.
- Build verified (`ng build` succeeds; built CSS emits `--qd-font-quran:"Amiri", serif` and no
  `'Uthmanic Hafs'` `@font-face`).
- The `UthmanicHafs_V22.ttf` asset is left in place (unreferenced; not downloaded by the browser).
  It can be re-wired if a *corrected* Uthmani font is later sourced (the alternative the user
  declined for now).

### 10.5 Verification still required

In-browser confirmation on the running app that `أُو۟لَـٰٓئِكَ` and the waqf marks now render as
proper small high marks (the HarfBuzz probe predicts they will). jsdom unit tests cannot verify
font shaping, so no shaping unit test was added.
