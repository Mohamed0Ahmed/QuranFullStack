# 07 — Frontend Styling Audit (Audit F) + Styling Rule Interactions (Brief §22)

- **Audited branch/commit:** `dev` @ `72792ba9`, audit date 2026-08-08
- **Brief scope:** §15 (Audit F — Frontend Styling Strategy), §22 (rule interactions / policy loops), mandatory questions 32–42
- **Evidence base:** `data/style-inventory.json`, `data/instruction-inventory.json`, `data/markdown-decision-inventory.json`, `data/history-evidence.json`, `data/runtime-measurements.json`
- **Verification:** every headline number below was independently re-measured in the working tree by this author (`find`/`wc`/`grep`/`git rev-list` plus an independent exact-token Tailwind matcher). Where my re-measurement differed from the inventory, both numbers are shown.
- **Mode:** read-only audit. This report proposes and classifies; it does not instruct implementation.

---

## 1. Measured baseline (Q32–Q35)

### 1.1 Totals — CONFIRMED (re-measured)

| Measure | Value | Evidence |
|---|---|---|
| SCSS files under `Frontend/quran-dashboard-ui/src` | **121** | `data/style-inventory.json:10`; re-verified `find src -name '*.scss' \| wc -l` = 121 |
| Total SCSS LOC (physical lines) | **10,346** | `data/style-inventory.json:11`; re-verified = 10,346 |
| Global SCSS (13 files: `src/styles/*` + `styles.scss`) | **2,481 LOC** | `data/style-inventory.json:12`; re-verified = 2,481 |
| Component SCSS (108 files) | **7,865 LOC** | `data/style-inventory.json:13`; re-verified per-feature sums below |
| Approx. token weight of all SCSS (bytes÷4) | ~57,600 tokens | `data/style-inventory.json:16` |
| CSS files | 0 (all styling is SCSS) | `data/style-inventory.json:5` (scan scope) |

LOC counts are physical lines including blanks, matching how `FRONTEND_STRUCTURE.md` expresses its thresholds (`data/style-inventory.json:1761`).

### 1.2 Global SCSS files — CONFIRMED

| File | LOC | Bytes | Character |
|---|---|---|---|
| `src/styles/_components.scss` | 746 | 16,630 | cross-feature primitives (cards, buttons, badges, modal, skeleton, `qd-is-selected`) |
| `src/styles/_explorer-detail-lists.scss` | 407 | 8,028 | **words-feature detail panels** (+4 access-admin class uses) |
| `src/styles/_explorer-tables.scss` | 299 | 7,428 | **words-feature tables** |
| `src/styles/_words-explorer-layout.scss` | 225 | 4,697 | **words-feature layout** |
| `src/styles/_words-explainer.scss` | 160 | 3,443 | **words-feature explainer hero** |
| `src/styles/_tokens.scss` | 154 | 6,089 | design tokens (root custom properties) |
| `src/styles/_typography.scss` | 126 | 3,129 | font faces, Arabic-first type classes |
| `src/styles/_forms.scss` | 101 | 2,389 | `.qd-input` (`_forms.scss:1`), `.qd-select` (`_forms.scss:30`), checkbox family |
| `src/styles/_layout.scss` | 86 | 1,718 | shell/navbar/footer/container/page-frame |
| `src/styles/_themes.scss` | 72 | 2,891 | dark-theme token overrides |
| `src/styles/_utilities.scss` | 63 | 810 | **a hand-built parallel utility vocabulary** (§3.3 below) |
| `src/styles.scss` | 38 | 1,188 | entry: 11 `@use` imports then `@tailwind` ×3 (`src/styles.scss:13-15`) |
| `src/styles/_breakpoints.scss` | 4 | 113 | canonical Sass breakpoints |

(`data/style-inventory.json:53-132`; sizes match the brief §15 byte listing.)

### 1.3 Component SCSS distribution — CONFIRMED

| Statistic | Value |
|---|---|
| Files | 108 |
| Min / median / mean / max LOC | 1 / 52.5 / 72.8 / 306 |
| Histogram | 1–10 LOC: **24** · 11–50: 29 · 51–150: 38 · 151–300: 16 · >300: **1** |

Per feature (files / LOC), re-verified for words and mushaf:

| Feature | Files | LOC |
|---|---|---|
| words | 43 | 2,751 |
| mushaf | 21 | 2,029 |
| abwab | 19 | 1,780 |
| access-admin | 8 | 482 |
| shared | 12 | 432 |
| core | 3 | 337 |
| dashboard | 1 | 44 |
| auth | 1 | 10 |

(`data/style-inventory.json:17-50,133-149`.)

### 1.4 Empty / nearly-empty component SCSS (Q35) — CONFIRMED, with an important nuance

**24 component SCSS files are ≤10 LOC** (re-verified: `awk '$1<=10'` over `wc -l` = 24; full list at `data/style-inventory.json:150-295`). But they are not one uniform kind of waste:

1. **18 files are display-default/near-empty stubs — 16 are exactly the `:host { display: block; }` idiom (28 bytes each, 448 total), 1 is the `inline-block` variant (`chip`, 35 B), and 1 contains only a 3-line comment and zero rules (`explorer-search-row`, 282 B — the one truly rule-empty file, and its content is comments in production SCSS).** Verified sample: `src/app/features/words/components/root-words-list/root-words-list.component.scss` is exactly those 3 lines; `chip.component.scss` is `:host { display: inline-block; }` (verified). 15 are in words, plus `chip`, `tabs`, `placeholder-page` in shared. These exist because a file was minted, then the only thing the component needed was a display default (or nothing at all).
2. **3 files are a deliberate sharing mechanism, not emptiness.** The three mushaf study cards (`full-i3rab-card`, `tafsir-card`, `translation-card`) each contain exactly `@use '../study-card.shared';` (verified), pulling the 27-line `_study-card.shared.scss` partial into each component's encapsulated stylesheet. Cost note: this compiles ~3 copies of the shared CSS into the bundle — small today, but it is duplication-by-mechanism, not sharing.
3. **3 files are small-but-real** (`access-lifecycle-actions` 10 LOC, `auth-callback` 10 LOC, `ayah-card` 10 LOC).

So "24 near-empty files" is true, but the recurring cost is tiny (765 source bytes total for the 18 stub files; ~1.4 kB for all 24). The significance is diagnostic, not economic: the separate-SCSS default mints a file per component whether or not the component has styling to say (§6).

### 1.5 Threshold and budget breaches — CONFIRMED

Doc thresholds (`FRONTEND_STRUCTURE.md:110-112`: ideal <150 / soft 200 / hard 300):

- **17 files over the 150-line ideal** (re-verified: 17), **8 over the 200 soft threshold**, **1 over the 300 hard cap**: `mushaf/selected-ayah-section.component.scss` at 306 LOC (`data/style-inventory.json:296-414`).
- Independently, the Angular build budget (`angular.json` production budgets: `anyComponentStyle` 4kB warn / 8kB error — verified) fired on the same area during the Phase-1b measured build: `selected-word-section.component.scss` compiled to 4.65kB and `selected-ayah-section.component.scss` to 5.85kB (warnings, `data/runtime-measurements.json` failures_observed), alongside an initial-bundle warning (598.75kB vs 500kB budget).
- Read of the 306-line file (first 60 lines verified) shows a genuinely complex reserved-height/loading-baseline system with per-breakpoint floors and an embedded mode — **not** mechanically compressible into utilities. The two enforcement systems (LOC review threshold, byte build budget) agree on where the real complexity lives, which is evidence the thresholds are measuring something real.

---

## 2. Utility and class usage (Q36–Q37)

### 2.1 Tailwind usage share: 0.0% — CONFIRMED (independently re-measured)

| Fact | Value | Evidence |
|---|---|---|
| Installed | `tailwindcss ^3.4.19` | `package.json:77` (verified) |
| Wired | PostCSS (`postcss.config.js`, verified), content glob `./src/**/*.{html,ts}`, empty `theme.extend`, no plugins (`tailwind.config.js`, verified) |
| Directives compiled | `@tailwind base/components/utilities` at `src/styles.scss:13-15`, **after** all 11 custom `@use` imports | verified |
| Tailwind utility tokens in templates | **0** of 3,101 class-attribute tokens | `data/style-inventory.json:422-423`; my independent exact-token matcher over all 115 `*.html`: 0 hits in 2,938 static tokens (difference = `[class.x]` bindings the inventory also scanned) |
| `@apply` in SCSS | **0** | verified `grep -rn "@apply" src --include='*.scss'` = 0 |
| HTML files using any Tailwind utility | 0 of 115 | `data/style-inventory.json:427-429` |

So Tailwind is pure dead weight in its current state: a dependency, a PostCSS pass over every build, and a compiled-in preflight layer — for zero utility usage. Two side effects are live today:

- **Preflight ordering hazard (currently moot):** `@tailwind base` sits after the custom partials, so Tailwind's element resets override equal-specificity custom element rules in source order (`data/style-inventory.json:1757`). Harmless while no utilities are used, but it means the file order is wrong for the doctrine the docs describe.
- **Preflight bytes ship in the bundle.** Size contribution not separately measured (NEEDS_MEASUREMENT — would require a diff build, out of scope for a read-only audit).

### 2.2 `qd-*` usage share — CONFIRMED (inventory LIKELY on parser edge cases; spot checks held)

| Fact | Value | Evidence |
|---|---|---|
| Class tokens in templates that are `qd-*` | 1,584 of 3,101 (51%); the other 1,517 are component-local BEM classes | `data/style-inventory.json:425-426` |
| Distinct `qd-*` classes defined in SCSS | **186** | `data/style-inventory.json:553` |
| Distinct `qd-*` classes used in HTML/TS | 169 | `data/style-inventory.json:554` |
| Defined but unused | **14** | `data/style-inventory.json:555-586`; spot-verified 3 of 3 (`qd-section-title` `_typography.scss:88`, `qd-card--bordered` `_components.scss:39`, `qd-card--feature` `_components.scss:41` — zero usages by grep) |
| Used but undefined | 6 (all benign: host-class or structural hooks, per inventory notes) | `data/style-inventory.json:588-623` |
| Heaviest classes | `qd-skeleton` 109 uses, `qd-btn` 103, `qd-explorer-table__cell` 80, `qd-is-selected` 69, `qd-sr-only` 55 | `data/style-inventory.json:624-640` |

The `qd-*` system is genuinely load-bearing: 51% of every class token on every screen, with a fat head of real primitives. This is not a vanity design system.

### 2.3 The double utility-system paradox — CONFIRMED, flagged head-on

`src/styles/_utilities.scss` (63 LOC, verified in full) hand-defines a **parallel Tailwind vocabulary**: `qd-flex`, `qd-flex-col`, `qd-items-center`, `qd-justify-between`, `qd-gap-2/3/4`, `qd-text-center`, `qd-mt-4`, `qd-mb-4`, plus `qd-sr-only` (≈ Tailwind `sr-only`) and `qd-truncate` (≈ `truncate`).

Usage of that hand-built set (verified by grep + `data/style-inventory.json:555-586,744`):

| Class | Uses |
|---|---|
| `qd-sr-only` | 55 |
| `qd-truncate` | 23 |
| `qd-scroll-stable` | 3 |
| `qd-gap-2` | 1 |
| `qd-flex`, `qd-flex-col`, `qd-items-center`, `qd-justify-between`, `qd-gap-3`, `qd-gap-4`, `qd-text-center`, `qd-mt-4`, `qd-mb-4` | **0 each** |

The project therefore maintains **two utility systems and uses neither for layout**: Tailwind (installed, 0 uses) and a hand-rolled qd- mini-Tailwind (9 of its 13 utilities at 0 uses; 1 at one use). Meanwhile 76 repeated declaration blocks (§4) hand-write the same flex/gap patterns longhand in component SCSS. Whatever direction Q42 resolves to, keeping both systems is the one clearly indefensible state.

---

## 3. Tokens and hardcoded values — CONFIRMED

| Fact | Value | Evidence |
|---|---|---|
| Distinct `--qd-*` custom properties | **114** (+11 component-local `--*` properties) | `data/style-inventory.json:813,930` |
| `var(--…)` uses in component SCSS | **1,349** (re-verified exactly) | `data/style-inventory.json:945` |
| `var(--…)` uses in global SCSS | 469 (my re-count including `styles.scss`: 471) | `data/style-inventory.json:944` |
| Hardcoded hex colors, whole tree | **0** (re-verified: 0 in component SCSS) | `data/style-inventory.json:1022-1051` |
| Hardcoded px font sizes | 0 | same |
| Magic px spacing lines | 17 total (9 global, 8 component) | same, with per-line evidence |
| Top tokens | `--qd-space-2` ×252, `--qd-space-3` ×174, `--qd-space-1` ×141, `--qd-text-muted` ×135, `--qd-border` ×105 | `data/style-inventory.json:946-1006` |

This is an unusually disciplined token layer for a project of this age: effectively zero hardcoded colors or font sizes across 10,346 LOC. The token system (`_tokens.scss`, `_themes.scss`, OKLCH values mirrored in `DESIGN.md:62-83`) is the part of the styling stack that is unambiguously **KEEP** under any future, per brief §15's own ownership model and §29 (RTL/Quran typography protection). The heavy spacing-token usage also means a Tailwind theme could be mapped onto these tokens rather than replacing them.

---

## 4. Duplication (Q38–Q39)

### 4.1 Repeated declaration blocks: what Tailwind could replace vs what must stay semantic

**76 distinct declaration groups (≥3 lines) repeat across ≥3 files** (`data/style-inventory.json:1391`; method at :1054 — textual normalization, so this is a floor: reordered or token-varied duplicates are not counted; confidence LIKELY). Classification of the top 20 (full list `data/style-inventory.json:1055-1389`):

| # | Block (abbreviated) | Files | 1:1 Tailwind mapping? | Correct disposition |
|---|---|---|---|---|
| 1 | `flex; flex-wrap; gap:var(--qd-space-2)` | 7 | **Yes** — `flex flex-wrap gap-2` (with token-mapped theme) | utility |
| 2 | `flex; flex-col; gap:var(--qd-space-3)` | 6 | **Yes** | utility |
| 3 | `flex; flex-col; gap:var(--qd-space-2)` | 6 | **Yes** | utility |
| 4 | `flex; items-center; justify-center` | 6 | **Yes** | utility |
| 5 | `flex; items-baseline; justify-between; gap-3` | 5 | **Yes** | utility |
| 6 | `flex-col; block-size:100%; min-block-size:0` | 5 | Mostly (`h-full min-h-0`; logical-property nuance) | borderline — it is the recurring "fill-height detail panel" shell of 5 words panels; a shared semantic class fits better |
| 7 | `flex-col; block-size:100%; box-sizing:border-box` | 5 | Mostly (preflight gives `box-border`) | borderline — explorer-table shell |
| 8 | `flex-shrink:0; block-size:var(--qd-explorer-table-header-height,2.75rem); min-block-size:unset` | 5 | **No** — token-driven, fallback value | semantic shared (explorer header geometry) |
| 9 | 8-decl row-button reset (`display:block; width:100%; padding; border transparent; radius; bg transparent; text-align:start; overflow:hidden`) | 4 | **No** — an 8-utility string repeated in 4 tables is exactly what `UI_STYLE_SYSTEM.md:262-263` forbids | semantic shared class |
| 10 | `flex-col; flex:1 1 auto; min-block-size:0; overflow:hidden` | 4 | Borderline | semantic (panel scroll region) |
| 11 | `grid-column/row + align/justify-self` cell placement | 4 | Partial | semantic (explorer mobile grid) |
| 12 | `flex; items-center; gap-2` | 4 | **Yes** | utility |
| 13 | `outline:2px solid var(--qd-focus-ring); outline-offset:-2px; radius-sm` | 4 | **No** — accessibility focus contract on a token | semantic shared |
| 14 | `padding:0; width:100%; text-align:center` | 4 | **Yes** | utility |
| 15 | 10-decl icon-button (3 abwab tree files) | 3 | **No** | missing shared primitive (icon button) |
| 16 | 8-decl input look (3 abwab modal files) | 3 | **No** — and it is a hand-rolled near-copy of the existing `.qd-input` (`_forms.scss:1-11`, verified declaration-by-declaration) | **MERGE into `.qd-input`** |
| 17 | 5-decl form label (same 3 abwab modals) | 3 | **No** | missing shared primitive (field label) |
| 18 | `border-accent; selected-bg; radius-md; padding-3` selected-card | 3 | **No** | semantic |
| 19 | `outline focus-ring; offset 2px; box-shadow:var(--qd-ring)` | 3 | **No** | semantic a11y |
| 20 | `selected-bg; border-color:border-accent; color:accent-text` | 3 | **No** — this is the `.qd-tabs__tab.qd-is-selected` / `.qd-chip.qd-is-selected` variant palette (`_components.scss:205-209`, `:273-277`) re-declared locally; note it differs from the `.qd-is-selected` base at `:147` (`--qd-border-accent` ≠ `--qd-accent`) | **MERGE into the `qd-is-selected` variant palette** |

**Q38 answer:** of the top 20 groups, **7–8 are clean 1:1 Tailwind layout utilities** (rows 1–5, 12, 14, arguably 6–7) — 3–4-line flex/gap/alignment micro-blocks, which is precisely the "layout, flex/grid, spacing" slice the brief's ownership model assigns to Tailwind. Extrapolated over all 76 groups the utility-replaceable share is roughly half (NEEDS_MEASUREMENT for a precise LOC figure; the top-20 sample suggests on the order of 400–900 component-SCSS LOC).

**Q39 answer:** the remaining ~12 of 20 are **semantic patterns that must stay shared, not inlined as utility strings**: token-driven explorer geometry (row 8), focus-ring contracts (rows 13, 19), selected-state palettes (rows 18, 20), form-control looks (rows 16–17), icon buttons (row 15). Turning these into repeated Tailwind strings would violate both the project's own rule (`UI_STYLE_SYSTEM.md:262-265`) and the brief's ownership model ("shared reusable visual blocks … where the repeated pattern is semantic/structural").

### 4.2 The sharpest finding: duplication exists despite — and under — the promotion doctrine

Rows 16 and 20 are not "Tailwind would have prevented this" cases. They are cases where **a shared `qd-` primitive already existed and three-to-four features hand-copied it anyway**: `.qd-input` has 17 template uses (`data/style-inventory.json:645`) and yet three abwab modals re-declare its look locally; `qd-is-selected` has 69 uses and its variant palette (`_components.scss:205-209`, `:273-277`) is still re-declared in two component files (`word-count-chip.component.scss:21-24`, `explorer-count-range-filter.component.scss:83-85`) + twice in `_components.scss` itself. The doctrine ("compose primitives, never re-declare" — `UI_STYLE_SYSTEM.md:322-328`, `FRONTEND_STRUCTURE.md:117`, enforced at `.claude/skills/engineering-review/SKILL.md:311-312`) failed at *enforcement/awareness*, not at *mechanism*. Any Q42 decision that only swaps mechanisms (Tailwind for SCSS) without addressing why review missed 4-file duplication will reproduce the same duplication in utility-string form. CONFIRMED.

### 4.3 Single-feature styles promoted into global files — CONFIRMED

Four "global" partials totaling **1,091 LOC (44% of all global SCSS)** serve essentially one feature (`data/style-inventory.json:1586-1732`):

| Partial | LOC | Owned-class usage |
|---|---|---|
| `_words-explorer-layout.scss` | 225 | 59 uses, all in words (re-verified: `qd-explorer-layout*` appears nowhere outside `features/words`) |
| `_words-explainer.scss` | 160 | 64 uses, all in words |
| `_explorer-tables.scss` | 299 | 255 uses, all in words |
| `_explorer-detail-lists.scss` | 407 | 216 uses words + 4 uses access-admin |

This is the promotion doctrine's second failure mode: `FRONTEND_STRUCTURE.md:117` says repeated visual patterns **must** move to the global style system, and the words feature obeyed — promoting patterns repeated only *within words* into app-wide files that every screen now pays for and every reader must treat as cross-feature contract. The newer boundary rule at `src/styles/README.md:99-103` adds a scoping clause ("If a selector is only meaningful inside one component tree, keep it scoped there") — but the same README affirmatively sanctions exactly these partials as global: `:101` lists "shared explorer scaffolding" among what belongs in `src/styles/`, and the Invariants line `:109` says "Global explorer partials should stay generic across Roots, Lemmas, Stems, Word Types, and related detail panels". Because the partials' classes span dozens of words component trees, the single-tree clause does not cover them; for the case that produced the 1,091 LOC the two documents agree. What is missing is a feature-scoping qualifier: no rule asks whether "repeated" means repeated across features or merely within one, so single-feature styling accumulates as app-wide law with both documents' blessing (§8, loop R5). The cost stands on its own measurement — 44% of global SCSS serves one feature.

### 4.4 Duplicated responsive rules — CONFIRMED

`data/style-inventory.json:1455-1583`: the canonical Sass breakpoints (`_breakpoints.scss`, mirrored in `breakpoints.ts` per `src/styles/README.md:32`) are used 41 times (`bp.$qd-bp-tablet-max` ×21, `$qd-bp-desktop-min` ×10, `$qd-bp-phone-max` ×9, `$qd-bp-wide-desktop-min` ×1) — but **18 media-query lines hardcode raw px values that bypass them**: `767px` ×5, `1023px` ×5, `1024px` ×3, `420px` ×3, `360px` ×1, `640px` ×1, `768px` ×1 (mushaf-page-area's combined query). The raw values equal the canonical breakpoints (phone-max 767, tablet-max 1023), so this is silent drift risk, not visual divergence today. `prefers-reduced-motion` boilerplate repeats in 15 files — a legitimate shared-pattern candidate.

---

## 5. `UI_STYLE_SYSTEM.md` adjudication (Q40)

### 5.1 Size and growth — CONFIRMED

| Fact | Value | Evidence |
|---|---|---|
| Size | **1,658 lines / 103,970 bytes / ~26,000 tokens** | re-verified `wc`; `data/history-evidence.json` ui_style_system |
| Commits touching it | 43 by `git rev-list --count HEAD` (re-measured); inventory reports 46 (counting method double-counts two merges, per its own caveat) | both confirm sustained churn |
| Born | 333 lines, 2026-06-06 (`7892b9bf`), pre-monorepo | `data/history-evidence.json` |
| Trajectory | 333 → ~1,016 (07-18) → 1,658 (HEAD); July 30–Aug 4 alone ~20 commits appending 5–121 lines each | same |
| Growth mechanism | per-feature/per-slice appends; each shipped primitive adds its own §17 block in its feature commit | same, verdict field |

### 5.2 Live contract vs historical narrative — the line map

| Lines | Section | Status |
|---|---|---|
| 1–253 | §1–§5 (purpose, organization, naming, tokens, themes) | **live contract** |
| 254–266 | §6 Tailwind Usage | live *doctrine*, **dead in practice** — permits utilities for "simple layout and spacing" (`:260`), yet usage is 0.0% (§2.1). A rule that has never once been exercised in 9 weeks is not describing the system |
| 267–398 | §7–§14 (typography, RTL, primitives, component-SCSS rules, states, a11y, Quran safety, DoD) | **live contract** |
| **399–572** | **§15 Prototype-Derived Implementation Contract (Navy+Gold+Parchment — superseded)** | **174 lines of explicitly superseded content** (`:399` says "Status: superseded" — verified), deliberately retained as the sole surviving prototype extraction record (`data/markdown-decision-inventory.json` historical_sections[0]). **Trap:** §15A fragments inside it are declared *still in force* — the 400/700-only font-weight rule (`:429-434`, verified) and the Quran-font protection (`:435-437`, verified). A reader cannot skip §15 wholesale; live law is embedded inside a superseded section |
| 573–689 | §16 Color doctrine | live contract; declares itself a mirror of `DESIGN.md` §2 that "must stay word-identical" (re-verified `:688`; the inventory relays `:654`) — a standing manual-sync obligation |
| **690–1,658** | **§17 Component contracts** | live contract, **969 lines = 58.4% of the file**, accreted one block per shipped primitive, with feature/slice provenance in headings ("Feature 030, N8", "Slice B2 T901/T903" — `data/markdown-decision-inventory.json` historical_sections[1]) |

### 5.3 Conflicts and duplication with DESIGN.md — CONFIRMED

- **Direct conflict:** `UI_STYLE_SYSTEM.md:429-434` mandates weights **400 and 700 only** (no 500/600 faces bundled, mid-weight declarations forbidden); `DESIGN.md:190-194` says mid-weights "carry the nav", softened by "where available" (`data/markdown-decision-inventory.json` decisions — conflict entry). Two canonical docs disagree on a rule agents apply constantly.
- **Deliberate mirror:** the allowed-green list is authoritative in `DESIGN.md:134-168` and mirrored in §16.3 with a keep-in-sync obligation — duplication by design, with recurring sync cost.
- `SKILLS_AND_ARCHITECTURE_GUIDE.md:272` already adjudicates the relationship correctly ("DESIGN.md is the direction/north star; UI_STYLE_SYSTEM.md is the canonical implementation system").

### 5.4 Recurring cost — CONFIRMED

`Frontend/quran-dashboard-ui/CLAUDE.md` (UI Style System section) routes **every** change to "global styles, theme tokens, reusable UI classes, layout shell styles, component visual styles, dark/light theme behavior, or shared UI patterns" through reading this file first. That is a ~26k-token read as a precondition for any styling task — the single largest per-task frontend context item after the instruction files themselves. Because §17 grows with every shipped primitive, this cost grows monotonically under current policy: the growth doctrine (§8, loop R2) guarantees the file gets more expensive every feature.

**Adjudication (Q40):** the document is **all four things the brief asks about at once** — a genuinely useful live contract (§16, §17 content, §1–14), partly historical narrative (§15's 174 superseded lines, slice-provenance framing in §17), duplicated with DESIGN.md (§16 mirror + weight conflict), and too large for routine reads (~26k tokens, growing). Its structure — live law interleaved with superseded eras, contracts framed as rollout history — is directly contributing to the read cost, and its doctrine (§6 + §9) contributed to custom-CSS growth by design.

---

## 6. The separate-SCSS-by-default rule (Q41) — honest analysis

**The rule and its enforcement machine (all verified):**

- `FRONTEND_STRUCTURE.md:40-41` — "Components use separate `.html` and `.scss` files by default" (bold), restated at `:69`.
- `angular.json` schematics: `@schematics/angular:component` → `inlineTemplate: false, inlineStyle: false, style: "scss"` — **the tooling mints a `.scss` file on every `ng generate component`,** which is where the 24 tiny files actually come from.
- `.claude/skills/engineering-review/SKILL.md:253` re-checks it at review time.

**Is 108 files / median 52.5 LOC pathological?** The honest answer is **no — the file count is not the disease**:

- 78% of component SCSS files (84 of 108) have >10 LOC of real content; the median 52-LOC file (e.g. `dashboard-home.component.scss`, read in full) contains a mix of legitimate one-off layout and semantic content. Angular scoping means those 52 lines are encapsulated, greppable, and deletable with their component — this is close to the brief's own "component SCSS where genuinely needed" model.
- The 24 tiny files cost ~1.4 kB of source total and zero meaningful build or context cost. Deleting them saves almost nothing; 16 of them exist only to hold `:host { display: block; }` (one more holds the `inline-block` variant, one only a comment), and 3 are a sharing mechanism (§1.4).
- The measurable pathologies live **elsewhere**: 76 repeated blocks across the *substantive* files (§4.1), 1,091 LOC of single-feature "global" styles (§4.3), and a component-SCSS mass growing ~3:1 against global (§7 history).

**Where the rule does cost something:** it normalizes "every component has a stylesheet", which combined with the absence of any usable layout-utility vocabulary (§2.3 — both utility systems unused) means each new component re-hand-writes its 3-line flex/gap blocks in its own file. The rule is an enabler of the duplication, not its cause. Verdict: **KEEP** the separate-file default (its direct cost is trivial and separate files support the RTL/a11y-heavy SCSS this product genuinely needs), and treat the missing micro-layout vocabulary as the actual problem (Q42). LIKELY.

---

## 7. Growth history — what the trend actually shows (context for Q42)

From `data/history-evidence.json` (curve CONFIRMED; LOC values via byte-ratio, LIKELY):

| Date | Global styles | Component SCSS |
|---|---|---|
| 2026-06-06 | 156 B (scaffold) | 0 |
| 2026-06-25 | 31.2 KB / 12 files | 90.3 KB / 52 files |
| 2026-07-30 | 76.4 KB / 13 files | 166.1 KB / 95 files |
| 2026-08-02 | **82.4 KB peak** | 174.7 KB / 99 files |
| 2026-08-08 (HEAD) | **58.6 KB (−29% in one week)** | 171.9 KB / **108 files** |

Two honest caveats on the headline "global shrank 29%": (1) the shrink coincides with the repo-wide **comment purge** plus words-explorer partial cleanups (`data/history-evidence.json` summary) — it is not pure de-duplication, and byte comparisons across the comment-removal boundary overstate style reduction; (2) component SCSS did **not** shrink — it added 9 files in the same week and now outweighs global ~2.9:1 by bytes. The system's center of gravity has moved to per-component styling and is still growing there. The one-off global cleanup does not change the recurring trajectory.

---

## 8. Policy-loop tracing (Brief §22) — exact blast radius per rule

Method: `data/instruction-inventory.json` duplicated_rules cross-checked by my own repo-wide greps (Tailwind mentions, `qd-` mentions per file, threshold text, separate-file text). A relevant nuance found during verification: **`.agents/skills/engineering-review/SKILL.md` is a pointer, not a copy** (verified by diff — it defers to the `.claude` skill), so the engineering-review styling rules exist once, not twice. `performance-angular-review`, by contrast, has real text in both `.claude` and `.agents` copies.

### R1 — "Separate `.html` + `.scss` files by default"

| # | Location | Form |
|---|---|---|
| 1 | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md:40-41` | canonical, bold |
| 2 | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md:69` | restated in size-rules section |
| 3 | `.claude/skills/engineering-review/SKILL.md:253` | review checklist item |
| 4 | `.cursor/rules/always-read-agents.mdc:86` | adjacent half of the rule (no inline templates) |
| 5 | `Frontend/quran-dashboard-ui/angular.json` schematics (`inlineTemplate/inlineStyle: false`, `style: "scss"`) | **the enforcement machine** — changing the doc without this changes nothing |

Loop: FRONTEND_STRUCTURE says X → angular.json mints X → engineering-review checks X → 108 stylesheets exist matching X. CONFIRMED.

### R2 — `qd-*` growth doctrine ("repeated patterns become qd- classes")

| # | Location | Form |
|---|---|---|
| 1 | `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:261` | "Repeated design patterns should become `qd-` classes" (canonical) |
| 2 | `UI_STYLE_SYSTEM.md:322-328` (§9) | compose primitives; "add it to the style system instead of copying styles" |
| 3 | `UI_STYLE_SYSTEM.md:346-347` (§10) | grown component SCSS → move repeated patterns to global |
| 4 | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md:50` | "compose shared `qd-` classes" |
| 5 | `FRONTEND_STRUCTURE.md:117` | "Repeated visual patterns **must** move to the global style system using `qd-` classes" |
| 6 | `FRONTEND_STRUCTURE.md:467` | presentational components must not duplicate `qd-` primitives |
| 7 | `.cursor/rules/always-read-agents.mdc:87` | "Repeated visual patterns should use centralized style tokens and reusable `qd-*` classes" |
| 8 | `.claude/skills/engineering-review/SKILL.md:311-312` | review: "use of centralized `qd-` classes"; "no repeated one-off card/button/input/table/modal styles" |
| 9 | `SKILLS_AND_ARCHITECTURE_GUIDE.md:269,272,351` | routing summaries naming `qd-*` as the canonical system |
| 10 | `Frontend/quran-dashboard-ui/src/styles/README.md` (47 `qd-` mentions; Boundary `:99-103`) | per-partial ownership prose |
| 11 | `Frontend/quran-dashboard-ui/.architecture/API_INTEGRATION_GUIDELINES.md:27-28,243-245` | consumes vocabulary (`qd-loading/empty/error-state`) |
| 12 | `DESIGN.md` (59 `qd-` mentions, token tables `:62-83`) | tokens rather than class doctrine, but vocabulary-coupled |
| 13 | the six frontend feature READMEs — notably `src/app/features/words/README.md:41` (names `styles/_words-explorer-layout.scss`) and `:108-110` (documents composing `.qd-explorer-table` / `.qd-detail-list__*`), plus shared, mushaf, abwab, access-admin, core | per-feature `qd-` vocabulary documentation; the words README is a **blocking repoint-before-delete dependent** of any partial re-homing |
| 14 | `Frontend/quran-dashboard-ui/e2e/README.md` | `qd-` vocabulary in e2e documentation |
| 15 | `docs/TESTING_DEBT.md` rows E1–E3 | owed tests asserting the `--qd-z-*` scale and the `.qd-modal-backdrop`/scroll-lock consumer sets — invariants this report itself leans on in §9.1 |

Loop (fully closed): UI_STYLE_SYSTEM says X → FRONTEND_STRUCTURE mandates X → cursor rules repeat X → engineering-review enforces X → agents promote patterns into `src/styles/` matching X → `styles/README.md` records X → §17 appends a contract block, growing the canonical doc that restates X. The 1,091-LOC words-only globals (§4.3) and the ~26k-token style guide (§5.4) are this loop's measured output. CONFIRMED — this is the highest-value policy loop to break in the styling area.

### R3 — "Tailwind supports, does not replace"

| # | Location | Form |
|---|---|---|
| 1 | `UI_STYLE_SYSTEM.md:254-266` (§6) | canonical doctrine, incl. `:266` "Tailwind supports the design system; it does not replace it" |
| 2 | `UI_STYLE_SYSTEM.md:97` | styles-entry description (Tailwind layers listed) |
| 3 | `Frontend/quran-dashboard-ui/src/styles/README.md:94` | layer-order step 12 |
| 4 | `.claude/skills/performance-angular-review/SKILL.md:5,26` | descriptive ("SCSS + Tailwind, centralized `qd-` classes") |
| 5 | `.agents/skills/performance-angular-review/SKILL.md:5` | descriptive copy |
| 6 | `Frontend/quran-dashboard-ui/package.json:77`, `tailwind.config.js`, `postcss.config.js`, `src/styles.scss:13-15` | infrastructure that embodies the rule |

Smallest blast radius of the five — the doctrine lives in one canonical section plus one README line plus two descriptive skill mentions. A Tailwind-role change is cheap to propagate **in documents**; the expensive part is §8/R2's ecosystem. CONFIRMED.

### R4 — Component-SCSS size thresholds (150/200/300)

| # | Location | Form |
|---|---|---|
| 1 | `FRONTEND_STRUCTURE.md:108-112` | canonical numbers |
| 2 | `FRONTEND_STRUCTURE.md:152-154` | same numbers for utility files (independent restatement) |
| 3 | `.claude/skills/engineering-review/SKILL.md:261-275` | threshold check — **pointer-style** ("thresholds defined in FRONTEND_STRUCTURE.md"), no numbers duplicated |
| 4 | `.cursor/rules/always-read-agents.mdc:64-69` | "check against documented file-size thresholds" — pointer-style |
| 5 | `SKILLS_AND_ARCHITECTURE_GUIDE.md:97,108,268` | summary mentions, no numbers |
| 6 | `Frontend/quran-dashboard-ui/angular.json` budgets (`anyComponentStyle` 4kB/8kB) | independent, byte-denominated sibling threshold |

Healthy shape: one number source, pointer references elsewhere. Changing the numbers touches one file (+ budgets if the byte analogue should track). CONFIRMED.

### R5 — Shared-pattern promotion (where repeated styles go)

| # | Location | Form |
|---|---|---|
| 1 | `FRONTEND_STRUCTURE.md:117` | "**must** move to the global style system" |
| 2 | `UI_STYLE_SYSTEM.md:326-328` (§9) | add to style system instead of copying |
| 3 | `UI_STYLE_SYSTEM.md:346-347` (§10) | split or promote when SCSS grows |
| 4 | `.claude/skills/engineering-review/SKILL.md:312` | no repeated one-off styles |
| 5 | `src/styles/README.md:99-103,109` | scoping clause (single-tree selectors stay scoped "even if it looks reusable") **plus** affirmative sanction of the explorer partials as global (`:101` "shared explorer scaffolding", `:109` "Global explorer partials should stay generic across Roots, Lemmas, Stems, Word Types…") |
| 6 | `.cursor/rules/always-read-agents.mdc:87` | promotion phrasing |

**Tension / scoping gap, not a confirmed contradiction:** for the case that actually produced the 1,091-LOC words partials the documents agree — the README's `:101`/`:109` affirmatively sanction the explorer partials as global, and because those partials' classes span dozens of words component trees, #5's single-tree clause does not bite. The real gap is that `FRONTEND_STRUCTURE.md:117` carries no feature-scoping qualifier: no rule asks whether "repeated" means across features or merely within one, and no document prices that difference. Any future rule change must edit both files together or the pattern recurs. CONFIRMED (as a scoping gap; the earlier contradiction reading did not survive verification).

---

## 9. A Tailwind-dominant future (Q42) — honest evaluation

### 9.1 What the evidence supports

Evaluated against the brief §15 ownership model (Tailwind for layout/spacing/simple state; small token+theme layer stays; shared semantic blocks stay; component SCSS only where genuinely needed):

**Points in favor (all measured):** roughly half the repeated duplication is 3–4-line layout micro-blocks that map 1:1 to Tailwind utilities (§4.1); component SCSS is the growing 3:1 majority of styling and its growth is exactly these micro-blocks plus semantic patterns; the project *already believes in utilities* enough to have hand-built a mini-Tailwind (§2.3); the token discipline (§3) means a Tailwind theme could be defined *from* `--qd-*` variables, preserving the design system rather than replacing it; Tailwind is already installed, wired, and paid for.

**Points against (equally measured):**

1. **Adoption is 0.0% after 9 weeks of being installed and explicitly permitted** (`UI_STYLE_SYSTEM.md:260` has allowed simple-layout utilities since June). The revealed preference of every contributor and agent, under the current doc regime, is to never write a utility. A "Tailwind-dominant" declaration without retiring the contrary doctrine (§8 R2/R3) would predictably produce the same 0%.
2. **RTL is load-bearing, and the codebase is ahead of Tailwind v3 here.** Component SCSS uses logical properties pervasively (`block-size`, `min-block-size`, `inline-size`, `margin-block-end`, `text-align: start` — §4.1 blocks 6–9; verified in files). Tailwind v3.4 covers inline-axis logical utilities (`ms-/me-/ps-/pe-/start-/end-`) but not the block-size idioms these panels use; naive migration to `w-`/`h-`/`ml-` physical utilities would be an RTL regression risk. This is a brief-§29 protected area (RTL/Quran typography correctness).
3. **The semantic majority must stay.** 51% of all template class tokens are `qd-*` (§2.2); the heavy hitters (skeletons, buttons, tables, selected states, focus rings) are semantic contracts, several enforced by TESTING_DEBT-tracked invariants (z-scale row E1, modal/chrome-inert row E2 — `data/markdown-decision-inventory.json`). Mushaf/Quran rendering SCSS (fonts, ayah markers, reserved-height baselines — §1.5) is exactly the brief's "component SCSS where genuinely needed".
4. **Migration cost is real and mostly not code.** Code side: 115 templates / 3,101 class tokens to re-express selectively; a token-mapped `tailwind.config` theme; preflight-vs-custom ordering fix. Policy side (the expensive part): every entry in §8's R1–R3, R5 tables — 2 architecture docs, the style guide, 2 README docs, the six feature READMEs + e2e README, `docs/TESTING_DEBT.md` rows E1–E3, cursor rules, engineering-review + performance-angular-review skills, SKILLS_AND_ARCHITECTURE_GUIDE — must change *coherently*, plus reviewer retraining, or the policy loop re-imposes the old world. The §22 analysis exists precisely because a partial migration leaves contradictory law active.
5. **Design register is orthogonal but worth stating:** `PRODUCT.md:86-102` anti-references generic SaaS; `DESIGN.md:304-306` bans raw CSS/hex outside the token system. Tailwind with a default theme would violate both in spirit; Tailwind with a `--qd-*`-mapped theme violates neither — the register constraint is on *values and looks*, not on class syntax. But it does rule out the "just use stock Tailwind" shortcut entirely.

### 9.2 The three coherent end-states

| Option | Description | Assessment |
|---|---|---|
| **A. Status quo** | Two utility systems, both unused for layout; doctrine says "supports"; growth continues per §7 | The measured worst case: pays dependency + preflight + doctrine-maintenance cost, receives zero utility. Only defensible as "deferred decision" |
| **B. Tailwind-dominant for layout** (brief's direction) | Token-mapped theme; utilities own layout/spacing/simple state; `qd-*` keeps semantic primitives, tokens, themes, RTL/Quran typography; component SCSS only for genuinely complex cases; `_utilities.scss` layout subset retired | Viable **only** as a package: theme mapped to `--qd-*`, logical-property utility policy for RTL, R2/R3/R5 doctrine rewritten in the same change, `UI_STYLE_SYSTEM.md` §6 rewritten. Benefit is prospective (cheaper future features, less per-component micro-SCSS), not retrospective LOC deletion. Bounded backfill: the 7–8 utility-mappable block groups |
| **C. Remove Tailwind** | Drop dependency + directives; promote the existing `qd-` utility subset as the sanctioned micro-layout vocabulary (it is 63 LOC and already written); keep everything else | Cheapest to reach and honest about revealed behavior; loses the ecosystem option and keeps hand-maintaining a utility dialect nobody has adopted either (9 of 13 at 0 uses). Risk: the same non-adoption repeats and micro-layout stays longhand |

**Assessment (LIKELY):** B and C are both coherent; A is not. The decision hinge is not styling preference but **enforcement capacity**: §4.2 shows the current system already fails to route duplication into existing primitives at review time. Option B adds a second vocabulary for reviewers to police; option C keeps one vocabulary but must actually start enforcing it. Given the brief's operating principle (lower recurring feature cost), B's prospective savings are the larger prize *if* the policy loops are rewired in one coherent change — and close to worthless if they are not. This audit classifies the direction as viable-with-preconditions rather than recommending a timetable; the choice belongs to the cross-cutting priorities report with the other workstreams' costs in view.

---

## 10. Proposed simplifications (brief §4 seven-question format)

Labels use the brief §9 taxonomy verbatim: `KEEP` / `MERGE` / `DELETE_CANDIDATE` / `REWRITE` / `RUN_LESS_OFTEN` / `NEEDS_MEASUREMENT`.

### S1 — Resolve the double-utility paradox (pick option B or C of §9.2) — REWRITE (doctrine) + DELETE_CANDIDATE (the losing utility system)

1. **Value today:** Tailwind provides option value only; `_utilities.scss` layout subset provides nothing measurable (0–1 uses per class).
2. **Dependents:** Tailwind: `package.json:77`, `postcss.config.js`, `tailwind.config.js`, `styles.scss:13-15`, doctrine at `UI_STYLE_SYSTEM.md:254-266`; `_utilities.scss`: `qd-sr-only` (55), `qd-truncate` (23), `qd-scroll-stable` (3) are real dependents and stay regardless — only the 9-class layout subset is orphaned.
3. **Risk:** removing Tailwind loses preflight normalization the bundle currently ships (behavior diff on element defaults — NEEDS_MEASUREMENT via diff build); adopting Tailwind risks RTL regressions via physical-property utilities (§9.1.2).
4. **Equivalent protection elsewhere:** browser-default normalization partially via existing base rules in `styles.scss`; RTL protection exists only as doctrine + review, no automated check (TESTING_DEBT has no RTL-utility row).
5. **Smallest safe step:** a decision record choosing B or C, plus deletion of the 9 unused layout utilities in `_utilities.scss` (safe under either future; verified 0 uses).
6. **Later verification:** grep for the deleted class names (already 0); `npm run build:verify` + visual smoke; if B, a lint/review rule that utility spacing/colors resolve to token-mapped theme values.
7. **Recurring cost removed:** one of two vocabularies to document, review, and explain; Tailwind's build pass and preflight bytes (if C); per-component longhand micro-layout (if B).

### S2 — Split `UI_STYLE_SYSTEM.md` into a small live contract + relocate historical/superseded material — REWRITE; §15's superseded 174 lines DELETE_CANDIDATE after extracting the still-in-force fragments

1. **Value:** §16/§17 are genuinely load-bearing contracts; §15 preserves prototype provenance; the whole is the canonical styling law.
2. **Dependents:** routed by `Frontend/quran-dashboard-ui/CLAUDE.md` (+ AGENTS copy) on every style task; referenced from `styles/README.md` (≥8 pointer mentions, e.g. `:12,18,23,27`), `FRONTEND_STRUCTURE.md:15,50`, `SKILLS_AND_ARCHITECTURE_GUIDE.md:269-272,351`, `API_INTEGRATION_GUIDELINES.md:27-28`, engineering-review skill. All references are to the file, none to §15 line numbers (per inventory routing edges), so restructuring is reference-safe if the filename survives.
3. **Risk:** losing the two live fragments buried in §15 (weights 400/700 `:429-434`; Quran-font protection `:435-437`) — brief §29 protected territory; also losing the §16↔DESIGN.md mirror discipline mid-edit.
4. **Equivalent protection elsewhere:** Quran-font rule also in `DESIGN.md:184-188` and `Frontend/quran-dashboard-ui/README.md:109-110`; the weights rule exists **only** in §15A and conflicts with `DESIGN.md:190-194` — it must be re-homed and the conflict resolved to a single owner, not dropped.
5. **Smallest safe step:** move §15's superseded 174 lines out of the routine read path (the repo's own lifecycle gate applies: facts not provable from code get folded with `file:LINE` proof or kept deliberately); resolve the weight-rule conflict to one owner.
6. **Later verification:** `grep -rn` for inbound references (per the repo's repoint-before-delete rule); token count of the resulting routine read.
7. **Recurring cost removed:** a large slice of ~26k tokens per styling task, and removal of the skip-trap where live law hides inside a superseded section. (Precise post-split size NEEDS_MEASUREMENT — depends on how much of §17 stays routine-read vs indexed.)

### S3 — Re-home the 1,091 LOC of words-only "global" partials to feature scope — MERGE (into feature ownership), not deletion

1. **Value:** the styles themselves are used heavily (255 + 216 + 64 + 59 owned-class uses) and stay.
2. **Dependents:** words feature templates; 4 access-admin uses of `_explorer-detail-lists` classes (`data/style-inventory.json:1700-1718`) — the one genuine cross-feature strand, which needs an explicit decision (promote those 2 classes for real, or decouple access-admin); `src/app/features/words/README.md` (`:41` names `styles/_words-explorer-layout.scss`; `:108-110` document composing `.qd-explorer-table` / `.qd-detail-list__*`) — a **blocking repoint-before-delete dependent** under the workspace rule; and `src/styles/README.md:109`'s explorer-partial invariant, which must move or be rewritten with the partials.
3. **Risk:** Angular global-vs-encapsulated scoping differences during any mechanical move; `_explorer-*` partials reference cross-partial classes (`qd-skeleton`, `qd-page-header`, `qd-select` — `data/style-inventory.json:1760`).
4. **Equivalent protection elsewhere:** none needed — this is relocation of ownership, not removal.
5. **Smallest safe step:** even without moving code, closing the R5 scoping gap (§8) stops the pattern from recurring; the move itself can follow later.
6. **Later verification:** owned-class usage grep per feature (the inventory method is reproducible); `grep -rn` the words README's partial-path and class references as part of the repoint check; visual smoke of explorer pages.
7. **Recurring cost removed:** every non-words styling task stops paying reading/reasoning cost for 44% of "global" styles that cannot affect it; the global layer becomes small enough to hold in one read.

### S4 — Delete the 14 defined-but-unused `qd-*` classes — DELETE_CANDIDATE

1. **Value:** none measurable (0 uses each; 3 spot-verified independently).
2. **Dependents:** none found in HTML/TS (inventory method excludes dynamic template-literal prefixes and lists them separately — `data/style-inventory.json:795-808` — none collide).
3. **Risk:** low; residual risk is dynamically-composed class names, which the inventory explicitly hunted (`possibly_dynamic_matches: []`).
4. **Equivalent protection:** n/a.
5. **Smallest safe step:** delete the 9 `_utilities.scss` layout classes (also covered by S1) + `qd-section-title`, `qd-card--bordered`, `qd-card--feature`, `qd-explorer-controls`, `qd-skeleton--w-25` after one fresh grep each.
6. **Later verification:** grep + `build:verify`.
7. **Recurring cost removed:** small (≈60 LOC) — hygiene, and removes false vocabulary from the style guide's implied surface.

### S5 — Fold the 4-file duplicates into the existing primitives they copy — MERGE

1. **Value:** the duplicated blocks work today; their value is already delivered by `.qd-input` (`_forms.scss:1`), the `qd-is-selected` variant palette (`_components.scss:205-209`, `:273-277`), and a to-be-named icon-button/label primitive (§4.1 rows 9, 15–17, 20).
2. **Dependents:** 3 abwab modals, 4 words tables, 3 abwab tree components (file lists at `data/style-inventory.json:1192-1389`).
3. **Risk:** subtle visual diffs (e.g. the abwab copy uses `font-size: 0.85rem` vs `.qd-input`'s `0.9375rem`; block 20 must fold into the variant palette, not the `.qd-is-selected` base at `:147` — the base uses `--qd-accent` for borders where the copies use `--qd-border-accent`, distinct tokens per `_tokens.scss:25` vs `:31`) — each fold needs a deliberate keep-or-normalize decision, which is design-register-relevant (calm consistency is the product's stated value, `PRODUCT.md` Brand Personality).
4. **Equivalent protection elsewhere:** engineering-review already mandates this (`SKILL.md:311-312`); the finding is that mandate's miss list.
5. **Smallest safe step:** the `.qd-input` near-copy in 3 abwab modals (block 16) — one primitive, one feature, verified overlap.
6. **Later verification:** repeated-block re-measurement with the inventory's method (reproducible hash counting); visual smoke on abwab modals.
7. **Recurring cost removed:** shrinks the 76-group duplication floor and, more importantly, re-establishes that review catches this class of defect before Q42's decision multiplies vocabularies.

### S6 — Close the promotion rule's feature-scoping gap (R5) — REWRITE (one sentence each in two files)

1. **Value:** both rules encode real judgment (share repeated patterns / don't globalize single-tree styles), and for the explorer partials they currently agree (§4.3, §8 R5).
2. **Dependents:** every future feature's styling placement decision; the six locations in §8 R5.
3. **Risk:** none beyond wording.
4. **Equivalent protection:** none — no document asks whether "repeated" means across features or merely within one, so nothing prices single-feature promotion.
5. **Smallest safe step:** add a feature-scoping qualifier for the "repeated within one feature" case to `FRONTEND_STRUCTURE.md:117`, and align `src/styles/README.md:99-109` (whose `:101`/`:109` today affirmatively sanction the explorer partials as global) in the same edit.
6. **Later verification:** the single-feature-globals measurement (§4.3) stops growing.
7. **Recurring cost removed:** the rule gap under which 44% of global SCSS (1,091 LOC) came to serve a single feature with both documents' blessing — the cost case for S3 stands on that measurement, not on an inter-doc contradiction.

### S7 — Component-SCSS size thresholds and the separate-file default — KEEP

Per §1.5 and §6: thresholds fire where real complexity lives (corroborated independently by build budgets), the numbers live in one place with pointer references (healthy loop shape, §8 R4), and the separate-file default's direct cost is ~1.4 kB of trivial files. Changing either buys nearly nothing and spends change-budget the styling area needs elsewhere. The 24 tiny files are **KEEP** (18 stub — 16 idiom, 1 inline-block, 1 comment-only — + 3 sharing mechanism + 3 real); deleting them individually is churn, not simplification.

---

## 11. Mandatory questions answered (brief §25, Q32–42)

| Q | Answer |
|---|---|
| **32. Total SCSS/CSS LOC?** | **10,346 LOC across 121 SCSS files** (0 plain CSS), ~57.6k tokens by bytes÷4. CONFIRMED, independently re-measured (§1.1). |
| **33. Global vs component SCSS?** | Global 2,481 LOC / 13 files; component 7,865 LOC / 108 files — component is 76% and growing (component bytes ~2.9× global at HEAD; global shrank 29% in the final week partly due to the comment purge, component did not shrink). CONFIRMED (§1.1, §7). Caveat: 1,091 LOC of "global" is single-feature in practice (§4.3). |
| **34. Number of component SCSS files?** | **108** (43 words, 21 mushaf, 19 abwab, 8 access-admin, 12 shared, 3 core, 1 dashboard, 1 auth). CONFIRMED (§1.3). |
| **35. Empty/tiny SCSS files?** | **24 files ≤10 LOC**: 18 are display-default stubs (16 exactly the 3-line `:host{display:block}` idiom, 1 `inline-block` variant, 1 comment-only), 3 are 1-line `@use` imports of a shared mushaf study-card partial (a sharing mechanism, with a small bundle-duplication cost), 3 are small-but-real. Economic cost ≈ nil; diagnostic value = the schematic mints a file per component (`angular.json` `inlineStyle:false`). CONFIRMED (§1.4). |
| **36. Tailwind usage share?** | **0.0%** — 0 utility tokens among 3,101 template class tokens, 0 of 115 HTML files, 0 `@apply`, confirmed by two independent matchers. Tailwind is installed (`^3.4.19`), PostCSS-wired, and its directives compile into the bundle. CONFIRMED (§2.1). |
| **37. `qd-*` usage share?** | 1,584 of 3,101 class tokens (51%) are `qd-*`; 186 classes defined, 169 used, **14 unused** (incl. 9 of the 13 hand-built layout utilities in `_utilities.scss` — the parallel mini-Tailwind, §2.3); 6 used-but-undefined, all benign. CONFIRMED (§2.2). |
| **38. Repeated CSS that Tailwind can replace?** | 76 repeated 3+-line groups across ≥3 files (floor, textual method). Of the top 20: **7–8 map 1:1 to Tailwind layout utilities** (flex/wrap/direction + token gap, alignment trios); extrapolated Tailwind-replaceable mass is roughly half the duplication, on the order of 400–900 component LOC (NEEDS_MEASUREMENT for precision). Requires a token-mapped theme; raw Tailwind values would breach `DESIGN.md:304-306`. LIKELY (§4.1). |
| **39. Shared semantic patterns that should remain reusable?** | The other ~12 of the top 20: token-driven explorer geometry, focus-ring contracts, selected-state palettes, form-control looks, icon buttons — plus the entire heavy-use `qd-` head (skeleton/btn/table/modal/state). Two of these repeated blocks are hand-copies of primitives that already exist (`.qd-input`, `qd-is-selected`) — the promotion doctrine failed at enforcement, not mechanism. CONFIRMED (§4.1–4.2). |
| **40. Is `UI_STYLE_SYSTEM.md` too large/historical?** | Yes to both, and it is also genuinely load-bearing: 1,658 lines / 103,970 B / ~26k tokens, 43 commits (46 by the inventory's merge-inclusive count), grown 5× from 333 lines by per-feature appends. §15 = 174 explicitly superseded lines **containing still-in-force fragments** (weights `:429-434`, Quran fonts `:435-437`); §17 = 969 lines (58.4%) of live contracts in historical framing; §16 mirrors DESIGN.md with a manual sync obligation; one confirmed conflict with `DESIGN.md:190-194` on font weights. It is read as a precondition for every styling task and grows every feature. CONFIRMED (§5). |
| **41. Is separate `.scss` by default increasing unnecessary files?** | It mints files — 24 tiny ones exist, and `angular.json`'s schematic (not just the doc) is the minting machine — but the honest verdict is that **108 files at median 52 LOC is not pathological**: 78% carry real, scoped, deletable-with-the-component content, and the measurable disease is duplication across substantive files plus misplaced globals, not file count. KEEP the default; fix the vocabulary gap and the promotion rule's feature-scoping gap instead. LIKELY (§6). |
| **42. What would a Tailwind-dominant future safely look like?** | Only as a package deal: Tailwind theme **generated from `--qd-*` tokens** (preserving the parchment/green register and `DESIGN.md`'s token law), utilities restricted to layout/spacing/simple state with a logical-property policy for RTL (brief-§29 protected), the `qd-` semantic layer (51% of tokens today) and Quran/mushaf SCSS retained, and — decisive — the R1–R3/R5 policy loops rewritten in the same change across the ~15 documents/skills listed in §8 (including the feature READMEs and TESTING_DEBT rows E1–E3), or 9 weeks of evidence says adoption stays at 0%. The alternative (remove Tailwind, sanction the existing 63-LOC `qd-` utility subset) is cheaper and honest about revealed behavior. The current hybrid is the only clearly wrong state. LIKELY, decision deferred to cross-cutting priorities (§9). |

---

## 12. Measurement gaps

| Gap | Why it matters | Tag |
|---|---|---|
| Tailwind preflight's byte contribution to the shipped CSS bundle | Sizes the "dead weight" claim in bytes; needs a diff build (out of scope read-only) | NEEDS_MEASUREMENT |
| Precise LOC replaceable by utilities across all 76 repeated groups | §4.1 classifies only the top 20; extrapolation is sample-based | NEEDS_MEASUREMENT |
| Repeated-block floor vs true duplication | The hash method misses reordered declarations and same-style-different-token spellings; 76 groups is a lower bound | NEEDS_MEASUREMENT |
| Post-split routine-read size of `UI_STYLE_SYSTEM.md` (S2) | The recurring-token saving depends on how much of §17 remains in the routine path | NEEDS_MEASUREMENT |
| Visual/behavioral diff of removing Tailwind preflight (option C) | Preflight element resets are live CSS today; removal is a rendering change | NEEDS_MEASUREMENT |
| Whether agents actually read all 1,658 lines per styling task vs partial reads | The ~26k-token recurring cost assumes the routing instruction is followed literally; actual read behavior is not instrumented | UNKNOWN |
| Bundle-size effect of the 3× compiled copies of `_study-card.shared.scss` | Small in source; compiled multiplication unmeasured | NEEDS_MEASUREMENT |
| qd-class parser completeness | Inventory self-rates the defined/used analysis LIKELY (regex + brace-stack, deep `&`-chains may be missed); my spot checks (3 unused, 2 single-feature claims) all held | LIKELY (spot-verified) |
