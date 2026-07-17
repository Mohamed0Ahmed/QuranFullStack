# Feature 031 — Words Explainers (page heroes + hub-as-guide) — Implementation Plan

- **Branch:** PLAN ONLY. Implementation targets a **new feature branch off `dev`, opened
  only AFTER `restyle/flat-green-light` merges** — see §9 (Open decision D9, recommended
  and awaiting confirmation).
- **Status:** PLAN ONLY — read-only inspection performed 2026-07-17 on
  `restyle/flat-green-light`. No code changed; this file is the only write.
- **Scope:** frontend-only, presentation-only. **No backend change, no new read, no Quran
  data logic, no URL-state change, no cache-key change.** Every explainer string is static
  approved copy.
- **Content source (approved, authoritative):** `docs/design-preview/words-pages-hero.html`
  (200 lines; intro at `:71-82`, five `<section class="hero">` at `:85`, `:110`, `:134`,
  `:156`, `:174`).
- **Doctrine inputs:** `DESIGN.md` (flat parchment + one scholarly green, the allowed-green
  list, the One Voice Rule, the Flat Rule), `PRODUCT.md` (register, anti-references, WCAG
  2.1 AA, Arabic-first), `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md`
  §9/§10/§11/§12/§13/§16/§17, `Frontend/quran-dashboard-ui/src/app/features/words/README.md`
  (mounted-shell invariants, TDZ getter rule, testid-slug rule),
  `docs/feature-030-explorer-polish/plan.md` (N3 no-shift precedent, N4 stable-slug testids,
  N5 disclosure precedent).

Frontend root shorthand: `FE = Frontend/quran-dashboard-ui/src`. All `file:line` references
below were verified on `restyle/flat-green-light` at commit `b633559`.

---

## 0. Summary verdicts

| # | Item | One-line verdict | Size |
|---|---|---|---|
| 1 | Shared explainer component | New presentational `qd-words-explainer` — **frame from data + `<ng-content>` for the per-page example**; a full block-type union would be a fake abstraction (5 block types, ~1 consumer each) | M |
| 2 | Hero placement | All 5 pages already share an identical `.uw-intro-band > .qd-page-header` mount point — the hero slots in with **zero structural surgery** | S |
| 3 | Hero UX | **Collapsible, per-page memory, synchronous restore, default expanded.** Not dismissible (kills discovery), not info-toggle-only (hides it from first-time admins) | M |
| 4 | No layout shift | Static-from-first-paint = layout, not shift; the only real risk is an **async** storage read that expands-then-collapses. Resolve storage synchronously (ThemeService pattern) and do not animate height | S |
| 5 | Hub redesign | 5 nav cards fed from the same content records + the orientation chain; card order already matches the source; **dead coming-soon scaffolding is removable** | M |
| 6 | Content mapping | Complete 1:1 map in §5 — hub card description = the source's `hero__tagline`, replacing today's invented `descriptionAr` strings | — |
| 7 | Doctrine risk | **Two genuine conflicts flagged**, not waved through: the green-tint benefit callout vs the allowed-green list, and the chain's accent-tint `pos` node vs the green-thread meaning | — |

---

## 1. Current state (evidence)

### 1.1 The five explorer pages share one mount point

All five page templates open with a byte-identical shell — a `.uw-intro-band` wrapping a
`.qd-page-header`, followed by `.uw-toolbar-recess`:

| Page | Template | Intro band | Notes |
|---|---|---|---|
| الكلمات الفريدة | `FE/app/features/words/pages/unique-words-page/unique-words-page.component.html:3-8` | ✓ | Only page with a subtitle `<p>` (`modeLabel()`) already in the band |
| الجذور | `FE/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.html:3-7` | ✓ | |
| الصيغ المعجمية | `FE/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html:3-7` | ✓ | |
| الأصول الصرفية | `FE/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html:3-7` | ✓ | |
| أنواع الكلمات | `FE/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.html:3-7` | ✓ | Tab strip is the split layout's first child (029 U3), untouched by the hero |

`.uw-intro-band` is a 10-line style block — `FE/styles/_words-explorer-layout.scss:3-12`:
`--qd-section-bg` background, `--qd-radius-md`, `--qd-space-4/5` padding, and
`margin-block-end: var(--qd-space-4)`. It is already the **quiet section** rung of the
surface ladder. It has no border today.

**Critical structural fact.** On the four "normal" explorers the `.qd-explorer-frame` div
**closes before** the table+panel layout (e.g. `roots-explorer-page.component.html:58`, with
the `@switch` layout starting at `:60`). The hero therefore grows the frame and moves the
grid down — the exact geometry that Feature 030 N3 condemned for page-level state banners.
§4 explains why this case is nevertheless sound, and what would make it unsound.

### 1.2 The hub is thin, and partly dead scaffolding

- `FE/app/features/words/pages/words-hub-page/words-hub-page.component.html:1-38` — title,
  subtitle, and a `.words-hub-grid` holding three separately-rendered card groups.
- `FE/app/features/words/pages/words-hub-page/words-hub-page.component.ts:36-57` — the same
  three groups as view models: `activeCard` (الكلمات الفريدة, hardcoded route string
  `'/dashboard/words/unique'` — the only card not using a `*RoutePath()` helper),
  `additionalActiveCards`, `comingSoonCards`.
- `FE/app/features/words/models/unique-words.labels.ts:21-54` — the label source.
  **`COMING_SOON_HUB_SECTIONS` is `[]`** (`:49`). Every explorer has shipped, so the
  coming-soon group, its `COMING_SOON_BADGE` (`:51`), the `word-section-card` disabled
  branch (`word-section-card.component.html:16-32`) and its `comingSoonLabel` input are
  **dead** — three of the four `word-section-card` specs exercise a branch nothing renders.
- `qd-word-section-card` is consumed **only** by the hub (verified: no other importer), so
  reshaping it has no blast radius outside this feature.
- Card descriptions today are **invented one-liners** ('استكشاف جذور الكلمات القرآنية',
  'استكشاف الصيغ المعجمية للكلمات', …) that predate the approved copy. §5 replaces them.
- **Card order already matches the approved source** (الكلمات الفريدة → الجذور → الصيغ
  المعجمية → الأصول الصرفية → أنواع الكلمات). No reorder needed.
- Grid: `words-hub-page.component.scss` — 1 col, 2 cols ≥640px, **3 cols ≥1024px**. Five
  cards in a 3-col grid leave a ragged 3+2 row; richer cards make that worse (see D6).
- **Hub card testids are derived from the Arabic label** —
  `[attr.data-testid]="'words-hub-card--' + card.labelAr"` (`:19`), producing
  `words-hub-card--الجذور`. This contradicts the feature's own stable-slug rule
  (`words/README.md`: *"Chip testids are stable slugs …, never derived from the Arabic label"*).
  See D7.

### 1.3 Terminology is already locked and consistent

`FE/app/features/words/models/words-shared.labels.ts:4,7` — `lemma: 'الصيغة المعجمية'`,
`stem: 'الأصل الصرفي'`. The approved copy uses these same canonical terms throughout. The
internal اللِمّة / الجذع forms appear **nowhere** in the source HTML and must not be
introduced.

### 1.4 Precedents this plan reuses rather than reinvents

- **Synchronous storage restore:** `FE/app/core/theme/theme.service.ts:37-65` —
  `resolveInitialTheme()` reads `localStorage` in a field initializer behind
  `isPlatformBrowser` + try/catch, so the value is known before first paint. This is the
  pattern §4 requires.
- **Disclosure:** Feature 030 N5 moved the association filter **off** `<details>` to
  field-driven `aria-expanded`; the count-range filter (`explorer-count-range-filter.component.html`)
  is now the only `<details>` left. See D3.
- **Content projection:** `qd-explorer-search-row` projects each page's association filters
  via `<ng-content>` — the same shape §3 proposes for the example region.

---

## 2. Hero placement (per explorer page)

**Where:** inside `.uw-intro-band`, immediately **after** `.qd-page-header`, **above**
`.uw-toolbar-recess`. The band already is the page's intro surface — the hero extends it
rather than adding a competing surface.

```html
<div class="uw-intro-band">
  <div class="qd-page-header">
    <h1 class="qd-page-title" data-testid="roots-explorer-page-title">{{ pageTitle }}</h1>
  </div>

  <qd-words-explainer [content]="explainer" data-testid="words-explainer--roots">
    <!-- page-specific example region projected here -->
  </qd-words-explainer>
</div>

<div class="uw-toolbar-recess"> … unchanged … </div>
```

**Why inside the band, not a sibling:** a sibling would add a second `margin-block-end` and a
second surface, breaking the ladder (parchment page → quiet band → recessed toolbar). Inside
the band, the hero shares one surface with the title it explains — which is also what the
approved comp shows (title and explainer in one `.hero` card).

**What must not move:**

- `.uw-toolbar-recess` and everything in it (search row, result-count stat, sort, range filters).
- The table+panel grid, the details panel, the Word Types tab strip / scope-counts strip.
- Every mounted-shell invariant from `words/README.md` — the hero renders **above and outside**
  every shell the invariants govern and never conditions on `listState()` / `panelState()`.
- Unique Words' band already carries a `modeLabel()` subtitle `<p>`; the hero goes **after**
  it (title → mode → explainer), so the mode line stays adjacent to the title it qualifies.

---

## 3. The shared component

### 3.1 Placement and shape

**Location:** `FE/app/features/words/components/words-explainer/` — words-feature-specific,
so it belongs to the feature's `components/`, not `shared/ui/`. It is a sibling of the other
shared explorer presentationals (`explorer-search-row`, `explorer-result-count`, …).

**Contract:** standalone, `ChangeDetectionStrategy.OnPush`, **presentation-only** — no
`inject()` of any API/facade/cache, no Router, no Quran-data logic. Its one non-visual
concern (collapse memory) is delegated to a tiny service (§3.3) so the component itself stays
a pure function of its inputs.

```ts
readonly content = input.required<WordsExplainerContent>();
readonly expanded = input<boolean>(true);
readonly toggled  = output<boolean>();
```

The component does **not** own the persisted state — it renders `expanded` and emits
`toggled`. The page owns the wiring. This keeps it trivially testable and keeps storage out
of a presentational.

### 3.2 Recommended split: frame from data, example from projection

**Recommendation:** the component renders the **invariant frame** from `content`
(ordinal, eyebrow, title, tagline, body, benefit, the collapse toggle, and all a11y
wiring) and projects the **variable example region** through `<ng-content>`.

*Why not a full block-type union.* The five pages' example regions genuinely differ:

| Page | Example region shape |
|---|---|
| الكلمات الفريدة | 2 mini mode-cards + 2 Amiri words with role notes + 2 prose notes |
| الجذور | 6 Amiri words, each with a grammatical-role note |
| الصيغ المعجمية | 4 Amiri words, no notes |
| الأصول الصرفية | one **segmented** word (prefix + emphasized core + suffix) + 2 notes |
| أنواع الكلمات | 4 type cards (name + Amiri examples + note) + a query-examples note |

Modelling that as data needs ~5 block types with ~1 consumer each — a union invented for an
abstraction that never repeats. That is exactly the AI-generated-code failure mode the
clean-code guard names (speculative generality / YAGNI). Projection keeps each page's example
honest and local while the frame — the part that genuinely repeats 5× — stays shared and
data-fed. D1 records the alternative.

**The example region's *visuals* are still shared**, not forked per page: a new
`FE/styles/_words-explainer.scss` partial owns `.qd-explainer-word` (Amiri chip),
`.qd-explainer-note`, `.qd-explainer-mini`, `.qd-explainer-segments`, `.qd-explainer-benefit`.
Pages compose those classes; no page defines a color. This satisfies §9 (compose primitives)
and §10 (component SCSS stays local layout only).

### 3.3 Content model and collapse memory

**Content:** `FE/app/features/words/models/words-explainer.content.ts`

```ts
export type WordsExplainerKey = 'unique' | 'roots' | 'lemmas' | 'stems' | 'word-types';

export interface WordsExplainerContent {
  readonly key: WordsExplainerKey;
  readonly ordinal: string;   // '٠١' — decorative, aria-hidden
  readonly eyebrow: string;   // 'نقطة البداية'
  readonly title: string;     // 'الكلمات الفريدة'
  readonly tagline: string;   // the one-liner — also the hub card's description
  readonly body: string;      // the descriptive paragraph
  readonly benefit: string;   // the 'الفائدة' callout text
}
```

- **Read via TDZ-safe getters, never `readonly` fields** — the `words/README.md` rule; label
  consts resolve to `undefined` in the bundled test build otherwise. This is a hard,
  previously-bitten invariant, not a style preference.
- Content is a **`models/` constant, not a label file addition.** It does not belong in
  `unique-words.labels.ts` — that file is already overloaded (it holds the hub labels for
  historical reasons) and this is per-page prose, not shared UI vocabulary.

**Collapse memory:** `FE/app/features/words/state/words-explainer-preference.ts` — a tiny
root-provided service, modelled directly on `ThemeService`:

```ts
const STORAGE_KEY = 'qd-words-explainer';   // value: comma-joined collapsed keys

isExpanded(key: WordsExplainerKey): boolean   // synchronous, browser-guarded, try/catch
setExpanded(key: WordsExplainerKey, expanded: boolean): void
```

- `isPlatformBrowser` guard + try/catch on both read and write (ThemeService `:51-65`).
- **Per-page keys, not one global flag** — an admin may know الجذور cold and still need
  أنواع الكلمات explained. One flag would force a wrong answer on four pages.
- Default when nothing is stored: **expanded** (D2).

---

## 4. Hero UX: the collapsible decision, and the real no-shift constraint

### 4.1 Options weighed

| Option | Verdict |
|---|---|
| **Full, always-on** | Rejected. Taxes every visit of a long research session forever; directly against "calm for long focus". |
| **Dismissible with memory** | Rejected. Permanent by default — the explanation becomes unrecoverable exactly when a returning admin wants it back, and there is no obvious restore affordance. |
| **Info-toggle only** (collapsed always, `؟` button) | Rejected as the default. A first-time admin lands on a bare table and never learns what the page is — which is the whole problem this feature exists to solve. |
| **Collapsible, per-page memory, default expanded** | **Recommended.** First visit teaches; one click retires it per page, permanently but reversibly. Discovery cost is paid once; the long-session tax is one line forever after. |

### 4.2 Recommended behavior

- **Default expanded** on a page with nothing stored.
- Collapsed state persists per page key; **the tagline stays visible when collapsed**, so the
  collapsed hero is a **one-line addition** to a band that already renders a muted `<p>` on
  Unique Words. The collapsed footprint is near-zero.
- Toggle is a `<button type="button">` carrying `aria-expanded` + `aria-controls`, labelled
  by visible Arabic text (D3 covers the `<details>` alternative).
- **No height animation.** Instant expand/collapse. Justification: DESIGN.md's motion contract
  admits motion only to *confirm state*; a disclosure that instantly reveals needs no
  confirmation, and an animated height is the one thing that would turn a user-initiated
  reflow into a visible jolt. This also sidesteps `prefers-reduced-motion` handling and any
  `ResizeObserver` measurement entirely — cheaper **and** calmer.
- Collapsed body is **removed via `@if`**, not `hidden` — cheaper, and keeps the collapsed
  DOM out of the a11y tree without relying on `hidden` styling surviving a cascade.

### 4.3 Why this is not a layout shift — and the one way to get it wrong

The requirement is **no layout shift**, and Feature 030 N3 is the precedent that killed
page-level banners for pushing the grid down. The distinction that makes the hero sound:

- **N3's banners shifted the grid *asynchronously***, driven by `listState()` resolving after
  first paint. The user saw a stable page jolt.
- **The hero's height is known at first paint** and changes only on an **explicit user click**.
  A statically-taller page is *layout*, not shift; a user-initiated reflow is excluded from
  CLS by definition (within 500ms of input). Nothing async moves the grid.

**The one way to get this wrong** — and the thing P2 must be tested against: reading the
collapse preference in an `effect()`, an `ngOnInit`, an async guard, or anything that runs
after the first render. That renders expanded, then collapses, and reproduces N3's jolt
exactly. Hence the ThemeService pattern (synchronous field initializer) is a **requirement**,
not a stylistic echo. A regression test must assert the *first* rendered frame already
reflects stored state.

**Also required:** the hero must never condition its own height on `listState()`,
`panelState()`, or any request. It is static prose; it has no loading state and no skeleton.

---

## 5. Content mapping

### 5.1 Source → page hero

| Source | `words-pages-hero.html` | → Hero slot |
|---|---|---|
| `.hero__num` | `:86`, `:111`, `:135`, `:157`, `:175` (`٠١`–`٠٥`) | `content.ordinal` — decorative, `aria-hidden="true"`; see D5 (green numeral) |
| `.hero__eyebrow` | نقطة البداية / أوسع تجميع صرفي / الصورة المعجمية / الطبقة الصرفية الأدق / التصنيف النحوي | `content.eyebrow` |
| `.hero__title` | `:87`, `:112`, `:136`, `:158`, `:176` | `content.title` — **must equal the page's existing `pageTitle`**; assert in the content spec |
| `.hero__tagline` | `:88`, `:113`, `:137`, `:159`, `:177` | `content.tagline` — **also the hub card description** |
| `p.body` | `:89`, `:114`, `:138`, `:160`, `:178` | `content.body` |
| `.h3` + `.two`/`.example`/`.types` | `:91-104`, `:116-128`, `:140-150`, `:162-168`, `:180-191` | **Projected** per page (§3.2) |
| `.benefit .txt` | `:106`, `:130`, `:152`, `:170`, `:193` | `content.benefit` |

Per page, verbatim:

| Key | Ordinal | Eyebrow | Title | Tagline (→ hub card too) |
|---|---|---|---|---|
| `unique` | ٠١ | نقطة البداية | الكلمات الفريدة | كل كلمة قرآنية فريدة، وأين وردت — وأين لم تَرِد. |
| `roots` | ٠٢ | أوسع تجميع صرفي | الجذور | النواة التي تخرج منها الأسماء والأفعال جميعًا. |
| `lemmas` | ٠٣ | الصورة المعجمية | الصيغ المعجمية | مدخل القاموس: صورة واحدة تجمع تصاريفها الإعرابية. |
| `stems` | ٠٤ | الطبقة الصرفية الأدق | الأصول الصرفية | قلب الكلمة بعد نزع السوابق واللواحق. |
| `word-types` | ٠٥ | التصنيف النحوي | أنواع الكلمات | استعلامات نحوية حقيقية عبر القرآن كله. |

### 5.2 Source → hub

| Source | `words-pages-hero.html` | → Hub slot |
|---|---|---|
| `.intro .eyebrow` | `:72` — المنهج القرآني · دليل الأدمن | Drop the "دليل الأدمن" framing (the whole app is the admin dashboard); see D8 |
| `.intro h1` | `:73` — أقسام دراسة الكلمات القرآنية | Hub title — **replaces** today's `WORDS_HUB_TITLE = 'الكلمات'` / subtitle pair (D8) |
| `.intro p` | `:74` | Hub subtitle |
| `.chain` | `:75-81` | The orientation chain — **recommended to keep** (D4); it is the single clearest artifact explaining why there are five sections |
| `.hero__num` + `.hero__eyebrow` + `.hero__title` | per section | Card ordinal + eyebrow + name |
| `.hero__tagline` | per section | **Card description** — replaces the invented `descriptionAr` strings at `unique-words.labels.ts:21-47` |
| route | — | `uniqueWordsRoutePath('tashkeel')` / `rootsRoutePath()` / `lemmasRoutePath()` / `stemsRoutePath()` / `wordTypesRoutePath()` — the hardcoded `'/dashboard/words/unique'` at `words-hub-page.component.ts:39` is replaced by the helper |

**One content source, two surfaces.** The hub cards and the page heroes read the **same five
`WordsExplainerContent` records**. A card's description and its page's tagline can therefore
never drift — which is the point of §3.3 being a content file rather than duplicated labels.

### 5.3 Hub structure after redesign

```
qd-page > qd-container
  qd-page-header      → title + subtitle (from .intro)
  chain               → orientation diagram (D4)
  words-hub-grid      → 5 × qd-word-section-card, fed from WORDS_EXPLAINER_CONTENT
```

`qd-word-section-card` gains `ordinal` + `eyebrow` inputs and **loses** its dead disabled /
coming-soon branch (§1.2, D6). The active branch stays an `<a class="qd-card qd-card--hover
qd-interactive-surface" [routerLink]>` — unchanged mechanics, richer body.

---

## 6. Constraints and how each is met

| Constraint | How |
|---|---|
| **Flat-green** | No new color. Hero = the existing `.uw-intro-band` `--qd-section-bg` surface; example blocks step to `--qd-surface-recessed`; borders are `--qd-border` hairlines. **No shadow** (the hero does not float), no gradient, no lift, no blur. |
| **Token mapping** | The preview's raw hex must **not** be pasted (DESIGN.md: *"Don't paste raw CSS, inline styles, or hex values into Angular"*). Map: `.mini`/`.type` `#f3f1ea` → `--qd-surface-recessed`; `.example` `#f6f4ee` → `--qd-bg`; `.benefit` bg `#eaf2ee` → `--qd-accent-tint`; `.benefit` border `#cfe6d8` → `--qd-border-accent` (`#bcd6cc`); `.benefit .txt` `#2a5347` → **`--qd-accent-text`** (`#275c50`, DESIGN.md-certified AA); eyebrow/`.h3` green → `--qd-accent-text` (never raw `--qd-accent` as small text — allowed-green item 4). |
| **Arabic-first / RTL** | Logical properties only (`margin-inline`, `padding-inline`, `border-inline-start`). The chain's `←` arrows are **RTL-directional glyphs in RTL prose** — they must be verified as rendered, not assumed (D4). Amiri sets its own direction. |
| **WCAG 2.1 AA** | Toggle is a real `<button>` with `aria-expanded`/`aria-controls`; hero is a `<section aria-labelledby>` pointing at its title; ordinal is `aria-hidden`. All pairs are DESIGN.md-certified AA tokens. Focus = the standard `:focus-visible` ring. Nothing conveys meaning by color alone (§12) — the chain's grammatical/morphological split carries the textual marker "(نحوي)", not just a tint (D4). |
| **No layout shift** | §4.3 — synchronous restore, no height animation, no async condition. |
| **Quran rendering faithful** | Example words use `--qd-font-quran` (Amiri, `FE/styles/_tokens.scss:68`) and are **copied verbatim with tashkeel intact** from the source. They are static illustrative morphology, **not** rendered Quran data and not ayah text — see **D10**, which flags this against §13 rather than assuming it. Never animated. |
| **Follow existing patterns** | Composes `qd-card` / `qd-btn` / the surface ladder (§9); component SCSS stays local layout (§10); shared visuals go to a style partial, not per-page forks; content projection mirrors `qd-explorer-search-row`; storage mirrors `ThemeService`; testids are stable slugs (D7). |
| **Dark theme** | Dark still runs interim navy+gold (`DESIGN.md` §2). The hero inherits it free via token remaps — the hero will read gold-accented in dark, consistent with the rest of dark. **Contrast must be verified in dark, not assumed** (P5): `--qd-accent-text` on `--qd-accent-tint` is certified in light; the dark remap is a different pair. |
| **One Voice Rule (green ≤10%)** | **At risk** — an expanded hero adds a green eyebrow + a green sub-heading + a green-tint callout above an otherwise quiet page, ×5 pages. **D5** makes this an explicit decision rather than a silent regression. |

---

## 7. Affected files

**New (8)**

```
FE/app/features/words/components/words-explainer/words-explainer.component.ts
FE/app/features/words/components/words-explainer/words-explainer.component.html
FE/app/features/words/components/words-explainer/words-explainer.component.scss
FE/app/features/words/components/words-explainer/words-explainer.component.spec.ts
FE/app/features/words/models/words-explainer.content.ts
FE/app/features/words/models/words-explainer.content.spec.ts
FE/app/features/words/state/words-explainer-preference.ts
FE/app/features/words/state/words-explainer-preference.spec.ts
FE/styles/_words-explainer.scss                        (+ import in styles.scss)
```

**Modified (16)**

```
FE/app/features/words/pages/unique-words-page/unique-words-page.component.{html,ts,spec.ts}
FE/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.{html,ts,spec.ts}
FE/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.{html,ts,spec.ts}
FE/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.{html,ts,spec.ts}
FE/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.{html,ts,spec.ts}
FE/app/features/words/pages/words-hub-page/words-hub-page.component.{html,ts,scss,spec.ts}
FE/app/features/words/components/word-section-card/word-section-card.component.{ts,html,scss,spec.ts}
FE/app/features/words/models/unique-words.labels.ts   (hub labels → content; drop dead COMING_SOON_*)
FE/app/features/words/README.md                        (REQUIRED — see below)
```

**README update is mandatory, not optional.** `words/README.md` documents the intro-band /
toolbar / mounted-shell geometry this feature extends. Per the root CLAUDE.md rule (*"If your
change alters behavior … described in a README, UPDATE that README in the SAME change"*), P3
adds a bullet recording: the hero's mount point, that it is static-height and never
state-driven, the synchronous-restore requirement, and the storage key.

**Not touched:** any `*-url-sync.ts`, `*.api.ts`, `*-cache.ts`, facade, controller, overlay
adapter, or backend file. Zero URL keys, zero cache keys, zero requests.

---

## 8. Phase order

| Phase | Work | Gate |
|---|---|---|
| **P0** | Content model + the 5 `WordsExplainerContent` records transcribed verbatim from the source + content spec | Copy matches source; titles equal each page's `pageTitle`; canonical terms only |
| **P1** | `qd-words-explainer` component + `_words-explainer.scss` + spec | Renders frame from inputs; projects example; toggle emits; no storage inside it |
| **P2** | `words-explainer-preference` service + spec | **Synchronous** restore proven; storage failure → default expanded; per-key isolation |
| **P3** | Wire the 5 pages (hero markup + example regions) + page specs + **README update** | Hero above toolbar on all 5; toolbar/table/panel untouched; mounted-shell specs still green |
| **P4** | Hub redesign: cards from content, chain, `word-section-card` reshape, dead scaffolding removal + specs | 5 cards, copy from the one content source, routes via helpers |
| **P5** | Verification pass: **light + dark**, RTL, AA contrast, keyboard, first-paint no-shift, Amiri fidelity | Record in `docs/feature-031-words-explainers/verification.md` (029/030 format) |

P0 → P1 → P2 are strictly ordered. **P3 and P4 are independent** once P0–P2 land and may run
in parallel. P5 gates the PR.

**Test-guard notes** (applies to every phase): test behavior, not implementation — assert
rendered copy and `aria-expanded`, not signal internals. **Do not mock the content
constants** — they are real data, construct them. No tests for Angular's `input()`/`@if`
(framework guarantees). Storage-failure variants are **data-driven**, not copy-pasted specs.
The only sanctioned mock boundary here is `localStorage` (a real boundary). Obey the
`VITEST_MAX_FORKS` cap (`Frontend/quran-dashboard-ui/README.md`) and the jsdom
`matchMedia`/`ResizeObserver` guards — the hero needs neither, which is one more reason §4.2
rejects measured animation.

---

## 9. Open decisions

Ordered by how much they change the work. **D9 gates the start; D5 and D10 are doctrine
questions I am not authorized to settle silently.**

| # | Decision | Recommendation |
|---|---|---|
| **D1** | **Component split:** frame-from-data + `<ng-content>` example (recommended), **or** a full block-type union rendering examples from data? | **Projection.** The union needs ~5 block types with ~1 consumer each — speculative generality. Revisit only if a 6th explorer appears. |
| **D2** | **Default state:** expanded, or collapsed-with-toggle? | **Expanded.** A first-time admin must not land on a bare table. One click retires it per page. |
| **D3** | **Toggle mechanics:** `<button aria-expanded>` (recommended) or `<details>/<summary>`? | **Button.** 030 N5 moved the association filter off `<details>`; `<details>` also drags RTL marker styling. Only the count-range filter still uses it. |
| **D4** | **Chain diagram on the hub:** keep it? | **Keep** — it is the clearest single artifact explaining why five sections exist. Two riders: the `←` arrows must be **verified as rendered in RTL** (a mirrored arrow is worse than none — consider `·`), and the `pos` node's accent-tint + green border must go (see D5). |
| **D5** | **Green budget (doctrine).** The comp puts green on the eyebrow, the `.h3`, the `.benefit` tint + border, the `.hero__num`, and the chain's `pos` node. The **allowed-green list is locked** and enumerates 7 uses; a **soft informational callout on accent-tint is not among them**, and the chain's `pos` node reads as *selected* when nothing is selected — directly against the Green Thread ("a 2px green edge means *current*"). Five expanded heroes may also breach the One Voice Rule (≤10%). | **Trim to what the list already permits:** keep the eyebrow and `.h3` as `--qd-accent-text` (item 4 — "section eyebrows" is named explicitly). **Render `.benefit` on `--qd-surface-recessed` with a `--qd-border` hairline** and keep only its "الفائدة" label in `--qd-accent-text`. **Ordinal → `--qd-text-muted`** (a green numeral is decoration). **Chain nodes all neutral** — the نحوي/صرفي split is already carried textually. If you want the green callout, it needs an explicit amendment to the allowed-green list in `DESIGN.md` §2 **and** `UI_STYLE_SYSTEM.md` §16.3 (they are kept in sync by contract) — **your call.** |
| **D6** | **Hub grid + dead scaffolding:** 3 cols ≥1024px leaves a ragged 3+2 with five richer cards. And `COMING_SOON_*` + the card's disabled branch are provably dead. | **2 cols ≥1024px** (richer cards want the width; 2+2+1 reads calmer than 3+2), and **delete the coming-soon scaffolding** — it is dead code with three specs guarding a branch nothing renders. Confirm the deletion: it is the one *removal* in this plan. |
| **D7** | **Hub testids** are Arabic-label-derived (`words-hub-card--الجذور`), contradicting the feature's own stable-slug rule. Fix now or leave? | **Fix now** — migrate to `words-hub-card--<key>` (`roots`, `lemmas`, …) while the hub is already open. It is test-visible churn, so it needs your yes; doing it later means touching the hub twice. |
| **D8** | **Hub title:** replace `'الكلمات'` / `'أقسام دراسة الكلمات القرآنية'` with the source's `أقسام دراسة الكلمات القرآنية` + its longer subtitle? Note the current **subtitle already equals the source's title**. | **Adopt the source pair**, and **drop the "دليل الأدمن" eyebrow** — the whole app is the admin dashboard, so the eyebrow is noise inside it (it existed to frame a standalone preview page). Check the sidebar/nav label for "الكلمات" stays consistent. |
| **D9** | **Branch.** Implement on a **new feature branch off `dev`, opened only after `restyle/flat-green-light` merges** — **confirm.** | **Confirmed as recommended, with a hard technical reason beyond hygiene:** this hero is specified entirely in flat-green tokens (`--qd-accent-text`, `--qd-surface-recessed`, `--qd-radius-md`) that **exist today only on `restyle/flat-green-light`**. Branching off `dev` *before* that merge would build against navy+gold tokens and mis-render. Combined with CLAUDE.md (*"ALL new work branches off `dev`"*) and keeping the restyle branch reviewable, the order is: **merge `restyle/flat-green-light` → sync `dev` → branch `feature/031-words-explainers` off `dev` → PR into `dev`.** Proposed name follows the 029/030 docs-plan pattern (this is **not** a Spec Kit feature — no `specs/031-*` folder; 026 remains the active Spec Kit feature). |
| **D10** | **Quran-safety framing (§13).** The example words (`عَلِمَ`/`عُلِمَ`, `الحَمْد`/`حامِد`/`محمود`/`أحمَد`/`حَمِدَ`/`يَحمَد`, `كِتَابٌ`/`الكِتَابُ`/`كِتَابًا`/`كِتَابِهِ`, `وَالْمُؤْمِنُونَ`, `الم`/`الر`/`يس`/`طه`) are approved copy, but §13 says *"Do not invent Quranic text or labels in the UI."* Some forms (e.g. `يَحمَد`, `أحمَد`) are **illustrative derivations**, not necessarily attested Quranic tokens — and they will render in Amiri, the same face as real Quran text. | **Confirm they are approved as *illustrative morphology*, and make that legible in the UI**: the example region carries its "مثال" label (already in the source), sits in a recessed example block, and is never presented as ayah text or as a queryable count. That is my reading — but §13 is a data-safety rule and this is **approved copy rendered in the Quran face**, so I want your explicit yes rather than inferring one. If you prefer zero ambiguity, the alternative is restricting examples to attested Quranic forms only, which would require re-cutting the الجذور example. |

---

## 10. What this plan deliberately does not do

- **No new endpoint, no read, no aggregation.** Every number in the approved copy is prose,
  not a live count. If a future ask wants live counts in the hero, that is a different
  feature with a different risk profile (and would reintroduce the async-height problem §4.3
  exists to prevent).
- **No change to any explorer's toolbar, table, panel, overlay, URL state, or cache keys.**
- **No dark-theme reconciliation.** Dark stays interim navy+gold per `DESIGN.md` §2; the hero
  inherits it and is verified, not redesigned.
- **No new shared `shared/ui/` primitive.** The hero is words-specific; promoting it would be
  speculative until a second feature wants it.
