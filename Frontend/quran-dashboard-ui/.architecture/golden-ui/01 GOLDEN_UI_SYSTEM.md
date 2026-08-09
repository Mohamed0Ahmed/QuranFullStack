# Golden UI System — Quran Dashboard

> Status: **Golden design system — direction approved, revision 2 for final lock.** Design phase only. This document does not implement, and does not approve, Plan 7. It resolves the 48 direct drift targets in `UI_DESIGN_HANDOFF.md` §4, preserves G01–G24 in §21, and leaves D36 and D38 as explicit owner decisions.
>
> Input authority: `claude-design-input/UI_DESIGN_HANDOFF.md` (861 lines) and all 44 screenshots under `claude-design-input/screenshots/`. Every value below is either taken from that evidence, or is a **proposed canonical replacement** for a value the evidence proved inconsistent. Proposals are marked `→ NEW` where they change a current number.
>
> Visual companions (annotated frames, real fixtures, all three bands):
> - `Golden UI — Foundation.dc.html`
> - `Golden UI — Families.dc.html`
> - `Golden UI — Workspaces.dc.html`
> - `Golden UI — Responsive Critical States.dc.html`
>
> **Revision 2 (locked-decision pass).** D36 is decided (**disabled + visible reason**), D37 is decided (**non-interactive**), the 1080px Wide threshold is approved, Access route-leave has a canonical *target*, and D38 remains open. Twelve consistency corrections are applied: no gradients anywhere, exact surface-ladder terminology, split Access lifecycle frames, added responsive visual coverage, a proportionate card-nesting rule, preview-vs-project typography separated, mode-scoped Medium width rule, `lifecycle-active` split from `mutation-success`, every referenced token defined, a broader SCSS exception policy, a tab-order-safe truncation contract, and an explicit `qd-state` migration-adapter allowance.

---

## 0. What this system decides

| Question the handoff asked | Golden answer |
|---|---|
| One visual/interaction vocabulary for controls, tabs, menus, state? | §3–§6 + `GOLDEN_UI_COMPONENT_CATALOG.md`. One control geometry scale, one focus contract, one tab behaviour, one floating-layer contract, four separate async concepts. |
| True phone/tablet/desktop structural modes, especially 768? | §4. Three structural modes (Compact / Medium / Wide) with **desktop split and desktop navigation both moved to ≥1080px `→ NEW`**, and a hard "no min-width above the Medium content box" rule. This is the direct fix for the measured 961/866px documents at 768. |
| Which regions reserve geometry, which grow? | §7. Chrome, controls, headers, tabs, toolbars, pagination and status *slots* reserve. Quran, commentary, error text and meaningful lists grow. No invisible permanent reserves. |
| Who owns scrolling? | §8. One named scroller per panel, declared per family. Page never scrolls horizontally in any mode. |
| Which element owns page gutters? | §4.3. Exactly one: the page shell. Four named page intents, no nested gutter owners. |
| Maximum columns for collections? | §9. Every repeated collection declares min measure, max columns and orphan rule. No `auto-fill` without a maximum. |
| Which contracts become shared behaviour? | `GOLDEN_UI_COMPONENT_CATALOG.md` — 20 families, each as base + named variants + optional zones. No boolean-heavy universal components. |
| How do keyboard/touch/AT users get the same information? | §10. One disclosure contract (never pointer-only `title`), 44px hit-area contract independent of visual density, one ARIA vocabulary per family. |
| Density for long research sessions? | §3.6 two density modes (Comfortable / Dense) affecting row/control padding only — never hit area, never type size below the minimums. |
| What may stay different? | `GOLDEN_UI_COMPONENT_CATALOG.md` §"Preserved differences" per family + `UI_DRIFT_TO_CANONICAL_MAP.md` part 2 (G01–G24). |
---

## 1. Design principles

1. **Arabic-first, not mirrored.** Logical properties only (`inline-start/inline-end`, `margin-inline`, `padding-inline`, `border-inline-start`). Direction is a first-class design input, not a post-process. LTR is a *local island* (email, permission code, version hash, subject, source keys) — never a layout direction. (G01)
2. **Scripture is protected content, not UI.** Quran text, fonts, glyphs, markers, measured page geometry and no-animation rules are outside the design system's authority. The system may style the *chrome around* it. (G02, G03, G11)
3. **Same contract, same UI.** If two surfaces share a behaviour contract, they share the visual and interaction vocabulary. Differences must be nameable in domain terms or they are drift.
4. **Calm by default.** Flat parchment/ink/green identity. No gradients, no glass, no resting shadows, no hover lift, no entrance motion. Motion is state feedback only: 120–160ms colour/border transitions; shadow exists only on floating layers.
5. **Green means state, never decoration.** Solid green = one primary action per view. Green tint + 2px inline-start green thread = current/selected. Generic hover is neutral. (D12, D14–D16)
6. **Stable frame, growing content.** Shells, controls, anchors and scroll owners are stable. Content that genuinely varies grows. Stability is never bought with invisible blank bands.
7. **Density serves reading, not compression.** A data-dense dashboard uses width; it does not shrink type or targets. Minimum body 14px, minimum hit area 44px, minimum table row 40px.
8. **Safety is visible.** Destructive and permission-changing actions state their target, their consequence and their diff before they submit. They never look like ordinary saves. (G17, G18)
9. **One primary action per view; one destructive path per object.**
10. **Every constrained text surface declares its overflow contract** (wrap / truncate / scroll / expand) and every truncation has a non-pointer disclosure path. (D35)

---

## 2. Colour system

Derived from the current live parchment/ink/green identity (screenshots `03`, `07`, `17`, `21`, `22`, `27`, `32`). Names are semantic; hex values are the proposed canonical set. Dark theme is explicitly **out of scope** (navy/gold remains deferred — handoff §24.14).

### 2.1 Surface ladder (five light UI levels, plus one specialized chrome surface)

The light UI ladder has **five** levels: `bg-page`, `bg-chrome`, `surface`, `surface-quiet`, `surface-sunken`. `footer-bg` is a **specialized chrome surface outside the ladder** (it is the one inverted band in the product and never participates in the nesting rules below). Dark theme remains deferred and defines no levels in this cycle.

| Token | Value | Use |
|---|---|---|
| `--qd-bg-page` | `#F4F2EC` | Page canvas. |
| `--qd-bg-chrome` | `#FAF9F5` | Navbar, sticky rails' backdrop, table header. |
| `--qd-surface` | `#FFFFFF` | Cards, panels, table body, modal body. |
| `--qd-surface-quiet` | `#FBFAF6` | Nested quiet regions (detail list rows, study cards, empty bodies). |
| `--qd-surface-sunken` | `#EEEBE1` | Filter/toolbar band, segmented-control track, skeleton base. |
| `--qd-footer-bg` | `#16233A` | Footer only (existing ink-navy band). |

Ladder rule: **no surface may sit on the same surface.** A card is `--qd-surface` on `--qd-bg-page`; a row inside it is `--qd-surface-quiet`; a toolbar band is `--qd-surface-sunken`. Gratuitous card-in-card nesting is not permitted; one intentional nested **group surface** is (§5.4).

### 2.2 Ink

| Token | Value | Min size / use |
|---|---|---|
| `--qd-ink` | `#23211C` | Titles, entity identity, table values. |
| `--qd-ink-body` | `#443F37` | Body copy, descriptions. |
| `--qd-ink-muted` | `#6E6759` | Labels, meta, counts, placeholders (≥12px, ≥4.5:1 on all surfaces above). |
| `--qd-ink-on-dark` | `#D5DCE6` | Footer text. |

### 2.3 Green (the only accent with state meaning)

| Token | Value | Allowed uses — exhaustive |
|---|---|---|
| `--qd-green-solid` | `#1C6349` | Background of the single primary action; solid nav "current" pill is **not** allowed (tint only). |
| `--qd-green-text` | `#1B5E46` | Current tab label, eyebrow/step numerals, key counts, links. |
| `--qd-green-tint` | `#E7F0EA` | Current/selected background (nav pill, selected row, selected tab, selected chip). |
| `--qd-green-thread` | `#1C6349` | 2px `border-inline-start` on selected rows/cards/panels (logical, never physical). (D26) |
| `--qd-green-quiet` | `#CFE0D6` | Hairline on green-tinted surfaces only. |

Forbidden: green as generic hover, green as decorative border, green gradients, green for non-state emphasis. (D12, D15, D16)

### 2.4 Status (semantic, never colour-only)

Every status renders **icon/shape + text label**; colour is reinforcement. (F17)

**Role is named separately from colour.** Where one physical value serves more than one legitimate meaning, each meaning gets its own semantic alias. Aliases may resolve to the same value today; they may diverge later without renaming consumers. This table is exhaustive — **no status colour or tint may be invented at implementation time.**

| Semantic token | Resolves to | Meaning |
|---|---|---|
| `--qd-danger` | `#8C2F22` | Destructive action, revoked permission, blocking error. |
| `--qd-danger-tint` | `#F7E9E5` | Surface behind danger content. |
| `--qd-danger-hairline` | `#E4C9C1` | Border on danger-tinted surfaces. |
| `--qd-warning` | `#8A5A12` | Conflict, dirty state, capability-disabled explanation, advisory. |
| `--qd-warning-tint` | `#F7EEDC` | Surface behind warning content. |
| `--qd-warning-hairline` | `#E3D3AE` | Border on warning-tinted surfaces. |
| `--qd-neutral` | `#5B5548` | Read-only, archived, unknown, inert. |
| `--qd-neutral-tint` | `#EDEAE1` | Disabled control/surface fill, neutral badge fill. |
| `--qd-neutral-ink-disabled` | `#9A958A` | Text/icon inside a disabled control. |
| `--qd-lifecycle-pending` | = `--qd-warning` | Access lifecycle: Pending. |
| `--qd-lifecycle-pending-tint` | = `--qd-warning-tint` | Access lifecycle: Pending surface. |
| `--qd-lifecycle-active` | = `#1B5E46` | Access lifecycle: **Active account**. |
| `--qd-lifecycle-active-tint` | = `#E7F0EA` | Active badge surface. |
| `--qd-lifecycle-disabled` | = `--qd-neutral` | Access lifecycle: Disabled. |
| `--qd-lifecycle-disabled-tint` | = `--qd-neutral-tint` | Disabled badge surface. |
| `--qd-lifecycle-unknown` | = `--qd-neutral` | Unknown server status (with literal label). |
| `--qd-mutation-success` | = `#1B5E46` | **A completed mutation.** Not a lifecycle state. |
| `--qd-mutation-success-tint` | = `#E7F0EA` | Success notice surface. |
| `--qd-membership-owner` | = `--qd-ink` | Owner membership outline chip. |

Access lifecycle mapping (G17): Pending → `lifecycle-pending`; Active → `lifecycle-active`; Disabled → `lifecycle-disabled`; Unknown → `lifecycle-unknown` + literal "حالة غير معروفة" (never mapped to Disabled). Owner membership is a **separate** badge, never fused with lifecycle. "Active account" and "successful mutation" share a green today but are **different semantic states** and must never be expressed through the same token name.

### 2.5 Domain palette: morphology (preserved)

Mushaf morphology segment cards already use an amber/olive family alongside green (screenshot `32`). Preserved as a **content taxonomy palette**, not a status palette: `--qd-morph-verb #1C6349`, `--qd-morph-particle #9A5B1B`, `--qd-morph-noun #4A4A7A` (proposed third for noun segments `→ NEW`, same chroma/lightness band). Category colour always accompanies the Arabic category label. (G02, F18)

### 2.6 Borders, radius, elevation

| Token | Value |
|---|---|
| `--qd-hairline` | `1px solid #E6E2D7` |
| `--qd-hairline-strong` | `1px solid #D6D1C2` (table header underline, modal footer divider) |
| `--qd-radius-xs` | `4px` (inline count chips) |
| `--qd-radius-sm` | `6px` (all controls: inputs, selects, buttons, tabs) |
| `--qd-radius-md` | `10px` (cards, panels, table container) |
| `--qd-radius-lg` | `14px` (modal/drawer shells) |
| `--qd-radius-pill` | `999px` (status badges, alias chips) |
| `--qd-shadow-layer` | `0 8px 24px -10px rgba(35,33,28,.22)` — **floating layers only** (menus, popovers, modals, drawers) |

Resting elevation is always `0`. Hover never changes elevation or position. (D14)

### 2.7 Focus

One contract, everywhere, on every interactive element including table rows, tree items and chips:

```
:focus-visible → outline: 2px solid var(--qd-green-solid); outline-offset: 2px; border-radius inherits
```

`:focus` (pointer) never paints a ring; `:focus-visible` always does. No control changes size, width or padding on focus. (D21, D42)

---

## 3. Type, spacing, geometry

### 3.1 Families

| Role | Family | Notes |
|---|---|---|
| Display / identity | **Naskh serif** (current wordmark & page-title face) | Page titles, entity identity (root/lemma/stem/word), section headers, ayah metadata labels. |
| UI | **Arabic UI sans** (current project UI face) | All controls, tables, labels, body copy. |
| Protected Quran | **Existing QPC/Uthmani Hafs renderer** | Untouched. Never substituted, never restyled, never animated. (G02) |
| LTR technical | **Mono** (current project mono face) | Email, permission code, version/hash, subject, source key, verse key. `direction:ltr; unicode-bidi:isolate` on the element itself. |

> **Typography authority — read before implementing.** The visual boards load Amiri, IBM Plex Sans Arabic and IBM Plex Mono as **design-preview faces only**, to stand in for the three roles above. **Implementation must use the current approved project fonts** unless the owner separately authorises a font change. This pack does **not** authorise adding Google Fonts, changing any installed face, or touching Quran fonts under any circumstance. Because Arabic faces differ materially in x-height, ascent and advance width, implementation must **re-validate the Golden geometry** (control heights, table row heights, truncation thresholds, `min-inline-size` values, prose measures) against the real project faces and report any value that needs adjusting — the geometry contract is the intent; the exact px may shift by a step.

### 3.2 Scale (px; Arabic needs generous leading)

| Token | Size / line-height | Use |
|---|---|---|
| `t-caption` | 12 / 1.5 | Column labels, chip counts, timestamps. Absolute floor. |
| `t-meta` | 13 / 1.6 | Row meta, helper text, permission codes. |
| `t-body` | 14 / 1.75 | Table cells, controls, lists. Default. |
| `t-body-lg` | 16 / 1.8 | Page description, prose, commentary chrome. |
| `t-h4` | 18 / 1.45 | Card/panel/section titles. |
| `t-h3` | 20 / 1.4 | Modal titles, detail identity. |
| `t-h2` | 24 / 1.35 | Section headers. |
| `t-h1` | 30 / 1.3 | Page title (Naskh serif). |
| `t-identity` | 28–34 / 1.3 | Entity identity display (root `س م و`), letter-spaced. |

Prose measure: commentary/description max `68ch`; page description max `72ch`. Never justify Arabic prose; `text-wrap: pretty` on headings and descriptions.

### 3.3 Spacing scale (4px base, 8px rhythm)

`2, 4, 8, 12, 16, 20, 24, 32, 40, 48, 64`. Nothing else. Vertical page rhythm: page header → 24; block → block 24 (Compact 16); inside card 16 (Dense 12); label → control 6; control → helper/error 4.

### 3.4 Page gutter and width (one owner)

**Only the page shell applies inline gutters.** No feature, layout partial or explorer stylesheet may add one. (D01, D05, D06)

| Mode | Gutter | 
|---|---|
| Compact (≤767) | 16 |
| Medium (768–1079) | 24 |
| Wide (≥1080) | 32 |
| Wide-plus (≥1440) | 40 |

Four named page intents (replacing arbitrary `.qd-page/.qd-container/.qd-page-frame` combinations — D01, D02, D03, D04):

| Intent | Max width | Consumers |
|---|---|---|
| `capped-reading` | `72rem` (1152) | Dashboard, Words hub, placeholders, docs-like content. |
| `full-data` | `100rem` (1600) | Any single full-width data surface. |
| `split-workspace` | `100rem` (1600) | Words explorers, Access, Abwab, Templates. |
| `protected-mushaf` | feature-owned (`90rem`) | Mushaf only — reader measure is content-derived. (G03) |

### 3.5 Split-layout scale (replaces 15.5/18/20rem constants — D-adjacent, §16 of handoff)

| Token | Value | Consumers |
|---|---|---|
| `--qd-rail-s` | `16rem` | Templates list rail. |
| `--qd-rail-m` | `18rem` | Abwab selected-action rail. |
| `--qd-rail-l` | `20rem` | Access user rail. |
| `--qd-split-data` | `1.25fr / 1fr` | Words explorers (preserves current ≈5:4 table/detail). |
| `--qd-split-mushaf` | `40% / 60%` | Mushaf only — not part of the rail scale. (G03) |

Gap between split columns: 24 (Wide), 20 (Wide at 1080–1279).

### 3.6 Control geometry (one scale — D20)

| Token | Height | Padding-inline | Use |
|---|---|---|---|
| `ctl-sm` | 32 | 10 | Dense desktop inline controls **only when a ≥44px hit area is provided by the row/pseudo-element**. |
| `ctl-md` | 40 | 14 | Default: inputs, selects, buttons, tabs, pagination. |
| `ctl-lg` | 48 | 18 | Compact-mode primary actions, modal footer actions on phone. |

Invariants: identical height, radius (`6px`), border (`hairline`), font (`t-body`) and vertical alignment for **input, select, textarea-single-line, button, tab, chip-button**. Icon-only buttons are square at their scale. Busy state never changes width: label persists, the icon slot swaps to a 16px spinner, and a `min-inline-size` is reserved from the resting label. (D20, D21, F05)

**Hit-area contract (44px, independent of visual density — D45, D46, D47):** any control smaller than 44px in either axis must expand its interactive box via padding or an `::after` inset overlay to ≥44×44. This applies to tree chevrons, row overflow actions, pagination controls, Mushaf page/nav triggers and chip removes. The visible icon may stay 16–20px.

### 3.7 Repeated-geometry anchors

| Element | Value |
|---|---|
| Navbar height | `3.5rem` (unchanged) |
| Table header row | 44 |
| Table body row (Comfortable / Dense) | 40 / 36 |
| Mobile card row min-height | per entity: root 5.5rem, lemma 6.5rem, stem 6.75rem, unique 4.25rem, grouped 5rem (G09) — one padding/label/action system |
| Tree row | 44 (Comfortable) / 40 (Dense), hit area always ≥44 |
| Toolbar band | min 56, stable across draft/applied (applied-filter summary uses a reserved single line) |
| Pagination bar | 56, fixed control widths |
| Modal shell heights | see §6.3 |

---

## 4. Responsive strategy

### 4.1 Three structural modes (not four breakpoints)

| Mode | Range | Navigation | Page structure |
|---|---|---|---|
| **Compact** | ≤767 | Accessible sheet navigation (focus-trapped, scroll-locked, visible Close) | Single column; detail as sheet/drawer where the family allows |
| **Medium** | 768–1079 | **Same sheet navigation as Compact `→ NEW`** — desktop link row is not exposed here | Single column with *designed* medium compositions (see §4.2); no split |
| **Wide** | ≥1080 `→ NEW` | Desktop link row | Split workspaces, rails, inline details |
| Wide-plus | ≥1440 | Desktop link row | Same structure, wider measures, larger gutter, more table columns visible |

**Why 1080 and not 1024.** The audit measured document widths of 961px at a 768 viewport (Roots, Lemmas, Abwab, Access) and 866px (Word Types) because desktop navigation and desktop minimum widths engaged before they fit. Splitting at 1024 leaves the same failure at the 1024 edge (screenshots `10`, `41` show clipped columns and awkward wrapping exactly there). Moving the desktop threshold to **1080** and keeping the navigation row desktop-only gives every Wide composition ≥1016px of content box. (D10, D11, RESPONSIVE_FAILURE)

**Shrink guard (the most common way this rule gets broken).** Any control that flexes inside a toolbar or filter row is declared `flex: 1 1 0` with `min-inline-size: 0`, and its non-flexing siblings are `flex: 0 0 auto; white-space: nowrap`. A bare `flex: 1` on a text input keeps its default `min-width: auto` (≈20 characters) and will push a Compact row past the viewport even when the content is short. Where a row still cannot fit, it stacks — it never widens the page.

**Hard rule (mode-scoped).** **In Medium mode**, no rendered surface may require more inline width than the available Medium content box (`768 − 2×24 = 720px`). A surface that cannot fit must switch to its Medium composition rather than widening the page. Wide-only layouts may declare larger legitimate minimum geometry where their own contract permits it (a Wide split, a Wide table's column budget, the Mushaf reader measure). Page-level horizontal scroll is a defect in **every** mode.

### 4.2 Medium is a designed mode, not a squeezed desktop

| Family | Medium composition |
|---|---|
| Explorer (F09/F11) | Full-width table with a **column budget** (identity + 3 counts + overflow disclosure), detail as a right-side drawer sheet at 88dvh; no split. |
| Access (F19) | User rail becomes a horizontal **selected-context bar** (search + status filter + selected user summary, pinned) above the detail; list opens as a sheet. Solves "traverse the whole rail to reach detail". |
| Abwab (F20) | Tree full width with depth budget; selected-action rail becomes a **sticky bottom action bar** carrying the selected door name + permitted actions. |
| Word Types (F09 specialised) | Taxonomy becomes a two-row scrollable segmented band (main types row, child chips row) with a stable selected summary; table below. Fixes the 866px overflow and the 1024 wrap. |
| Mushaf (F18) | Reader first, study second, both full width; reader keeps its measured page geometry and its own reservation. (G03) |
| Words hub / Dashboard | 2-column grid, max 2 columns, cards keep `min 18rem / max 26rem` measure. |

### 4.3 Structural change map (what moves at each threshold)

```
≤767   nav→sheet · 1 col · table→semantic cards · detail→sheet · pagination 48px targets · gutter 16
768    nav stays sheet · medium compositions · toolbar collapses to 2 rows max · gutter 24
1080   desktop nav row · split workspaces + rails · inline detail · table columns full · gutter 32
1440   wider measures · more count columns visible · gutter 40 · rails unchanged
```

---

## 5. Surfaces, grids, density

### 5.1 Card contract (F04)

One base: `--qd-surface`, `hairline`, `radius-md`, padding 16 (Dense 12), optional header (title + optional meta) / body / footer zones. States: rest, hover (`background:--qd-surface-quiet` only), focus-visible ring, **selected** (green tint + 2px inline-start thread), disabled (`--qd-neutral-tint`, 60% ink, explanation text), loading (skeleton at final geometry), error (inline error row inside the card).

### 5.2 Grid discipline (D07, D08, §16)

Every collection declares: item min measure, item max measure, max columns, orphan rule.

| Collection | Min / max item | Max columns | Orphan rule |
|---|---|---|---|
| Dashboard destinations | 18rem / 26rem | 3 | Last row stretches to fill; never a single orphan beside 3 (5 items → 3+2). |
| Words hub curriculum (G05) | 20rem / 30rem | 2 | Preserves 2+2+1 teaching order; final card spans both columns. |
| Abwab door cards | 14rem / 20rem | 4 | Ordered rows; no auto-fill maximum-less growth. |
| Access permission groups (5 real groups) | 15rem / 22rem | 3 | 3+2; group heights equalised per row. |
| Word Type child chips | content / 18rem | 4 (Wide) / 3 (Medium) / 2 (Compact) | Overflow beyond 12 chips → "المزيد" disclosure inside the taxonomy band. |
| Audit events | 24rem / 34rem | 2 | Newest first, reading order preserved; never converted to a table (LIST_THAT_ONLY_LOOKS_TABULAR). |

### 5.3 Density modes

`Comfortable` (default) and `Dense` (user-persisted, Wide only) change **row padding and table row height only** (40→36, card padding 16→12). They never change type size, hit area, borders or colour.

### 5.4 Nesting rules

Do not use gratuitous card-within-card nesting. **One intentional nested semantic group level is permitted** when it represents a real grouping contract — and it is expressed as a **group surface** (quiet fill, hairline, `radius-md`, `<fieldset>`/`role="group"` semantics with a legend), not as an independent card. Access permission groups are the canonical example: the permission editor is one card, and the five server-labelled groups inside it are **permission group surfaces**. Everything below that level is a hairline-separated row on `--qd-surface-quiet`. Keep the hierarchy quiet and flat: a group surface never carries a shadow, a hover lift, or its own accent border.

---

## 6. Shared behaviour contracts

### 6.1 Tabs (F07 — D27, D28, D29, D30)

One behaviour for all 18 current manual tablists: `role="tablist"`, roving `tabindex`, **logical** Arrow mapping in RTL (ArrowLeft = next in visual order = logical next), Home/End, `aria-controls`/`aria-labelledby`, per-instance IDs. One visual primitive: pill tab on `--qd-surface-sunken` track; current = green tint + `--qd-green-text` + 2px thread on the **block-end** edge for horizontal tabs. Secondary-button-styled detail tabs are removed (D28).

Layout by count, decided per mode — never accidental CSS wrap (D30):
- 2–3 tabs → segmented, equal widths.
- 4–5 tabs → segmented Wide; horizontally scrollable single row (with edge fade + keyboard scroll-into-view) in Medium/Compact.
- >5 or unknown → scrollable row in all modes.

Panel geometry is independent of the tablist: a panel error or empty state never destroys or reflows the tab row.

### 6.2 Floating layers (F15 — D33, D34, D50)

One anchored-layer utility: `position: fixed` layer, `--qd-shadow-layer`, `radius-md`, max-height `min(60vh, 24rem)`, own scroller, viewport flip on the block axis, inline-axis collision clamp, never affects document flow. Keyboard: Enter/Space/ArrowDown opens, Escape closes and returns focus to the trigger, Arrow navigation with scroll-into-view, Home/End, type-ahead in searchable pickers, Tab closes and moves on. Danger items use the shared danger-item treatment with **no local override** (D50). Row actions are never hover-only: they are always present at Compact/Medium and appear on hover **or** focus **or** selection at Wide, with a persistent overflow trigger (D46).

### 6.3 Modal / drawer shell (F14 — D48, D49)

One shell, four named widths, one geometry contract:

| Variant | Inline size | Block size |
|---|---|---|
| `confirm` | `min(30rem, 100%)` | content, `max-block-size: min(88dvh, 32rem)`, body scroller |
| `form` | `min(38rem, 100%)` | `min(92dvh, 44rem)` fixed (authoring stability, preserved) |
| `wide` | `min(52rem, 100%)` | `min(92dvh, 44rem)` fixed |
| `overlay` (entity detail) | `min(46rem, 100%)` | `min(92dvh, 44rem)` fixed |
| Compact override | `100% − 16` inline, `94dvh` block, safe-area padding | header/footer always visible |

Contract: exactly one body scroller; header (title + optional identity/count + Close) and footer (actions) are sticky and never scroll; **padding is owned by the shell only** (sections add none); focus trap + focus return; `aria-labelledby` on the title; Escape closes unless dirty; dirty close raises the nested `alertdialog` strip **inside the footer** (not a second modal); server errors render above the footer, near the submitting action; busy disables the footer without moving it.

### 6.4 Async and feedback (F12 — D39, D40, D41)

Five separate concepts replace one `qd-state`:

| Concept | Visual | Geometry ownership |
|---|---|---|
| **Skeleton (initial)** | Flat pulse (opacity 1→.62, 1.4s, no gradient — D18), content-shaped: table rows, panel header+tabs+rows, Mushaf 15-line page canvas | Reserves the *final* geometry of the surface it replaces |
| **Refreshing** | A **flat** 2px indicator on the surface's block-start edge: a solid `--qd-green-solid` segment (≈40% width) translating along a `--qd-hairline` track — no gradient, no shimmer, no colour blending. Plus `aria-busy` on the region; existing content stays and stays readable. Under `prefers-reduced-motion` the segment stops moving and the track holds a static solid segment. | Zero added geometry |
| **Empty** | Icon-free, one short line + optional single action. Two distinct copies: *initial empty* ("لا توجد بيانات بعد") vs *filtered no-match* ("لا نتائج مطابقة للمرشحات") + "مسح المرشحات" | Min-block-size `min(40vh, 20rem)` inside a mounted shell; never collapses the split |
| **Error** | Danger-tinted inline block, message (wrapping, unbounded), Retry; rendered **at the origin** of the failure (panel, tab body, toolbar, footer) | Same reserve as Empty for read errors; write errors are inline near the action, adding height only when present |
| **Notice / success** | Single-line inline card with `aria-live="polite"`, dismissible, auto-clears on next mutation | **No permanent reserve** (removes the Access 6.5rem blank band — D41). It is placed at the block-start of the body region so appearing never moves the toolbar or the header above it. |

Button busy is not a state family member: it is the action's own state (§3.6).

Loading vocabulary rule (D40): if a surface has a known final shape (table, list, panel, card grid, Quran page) it uses a **content-shaped skeleton**. Text loaders are permitted only for single-value regions (a count, a badge) and use a fixed-width shimmerless placeholder.

**Migration compatibility (does not weaken the target).** The five concepts above are the final semantic owners. Implementation *may* temporarily keep the existing `qd-state` component/selector as a **compatibility adapter** during incremental migration: it may preserve its current public inputs, outputs and test IDs and delegate rendering to the new primitives. It must **not** remain the semantic owner, must **not** gain new consumers, and must **not** keep implementing the state meanings itself. Each adapter call site is retired as its family migrates; the adapter is deleted when the last one is gone.

### 6.5 Pagination and counts (F13 — D42, D43, D44, D45)

One bar: `السابق` / page numerals with ellipsis / `التالي`, plus optional jump. Fixed reserved widths in **every** state: jump input `6rem` always (no widen-on-focus), Go button always mounted (disabled when input is empty/invalid), previous/next fixed width. Per-instance IDs for input, error and live region. Compact targets 48px, Wide 40px with 44px hit area. Result metadata ("عدد الجذور: 1,642") sits in the toolbar, uses tabular numerals and a reserved min-width sized for 7 digits so 0→1,000,000 never reflows. `Load more` (audit) is a separate capability with its own busy state and appended-count announcement — never numeric pagination. (G-adjacent)

---

## 7. Stable geometry rules

**Reserve (fixed or min-reserved):** navbar, page header block, toolbar band, tab row, table header, table body min-height during load, panel header + tab row + status slot, pagination bar, modal header/footer, button widths across busy, count metrics (tabular + reserved width), floating-layer anchors.

**Grow naturally:** Quran page and ayah text, commentary/Tafsir/translation/i'rab bodies, error and validation messages, alias chip rows, description fields, audit event lists, footer health copy.

**Never:** invisible permanent reserves (D41); conditional mounting of controls that shift neighbours (D43); focus-driven size changes (D42); active-state translation (D14); hover-driven elevation or position change; tab rows that wrap into a different column count than designed (D30).

**Route-level:** keep the delayed 2px shell progress line (200ms) exactly as-is — it is already correct and never moves content.

---

## 8. Overflow and scroll ownership

| Content | Contract | Disclosure |
|---|---|---|
| Quran page/ayah/word text | **EXPAND** — protected wrap; never truncate, never animate, never compress into cells (G02, G11) | n/a |
| Tafsir / translation / i'rab | **EXPAND** inside the study panel's single scroller; measure ≤68ch; no nested horizontal scroll | n/a |
| Table cells (non-Quran) | **TRUNCATE** single line | Full value per the disclosure ladder below + `aria-label`/`aria-describedby` carrying the complete value on the **owning interactive element** (the row). Never `title` alone (D35) |
| Mobile card rows | **WRAP** with label→value pairs preserved | n/a |
| Entity identity in headers/pickers | **TRUNCATE** with the same disclosure contract; identity is never the only place the full value exists | Full value in the detail header's first metadata row |
| Access name / email | **TRUNCATE**, email as an LTR isolate | Full identity always visible in the detail header before any safety decision |
| Abwab tree name | **TRUNCATE** at the row, with a **depth budget** (§below) | Full name in the selected panel + long-press/focus popover |
| Aliases / descriptions | **WRAP** (chips) / **EXPAND** (textarea, 3→8 rows) | n/a |
| Version / hash / codes | **WRAP** as LTR mono with `overflow-wrap:anywhere` + copy affordance | n/a |
| Desktop table | **SCROLL** inside the table container (block + inline), sticky header, selected row scrolled into view | Never the document (§4.1) |
| Panels / drawers / modals | Exactly **one** named scroller per panel (D-Word-Type nested scroller removed) | n/a |
| Floating layers | **SCROLL** inside the layer, `max-block-size: min(60vh, 24rem)` | n/a |

**Abwab depth budget (fixes the "name is the only shrinking item" defect):** indentation is `min(depth, 6) × 16px`; beyond depth 6 the row shows a `⌐ +N` depth marker chip instead of more indentation, and the ancestor path is available in the row's disclosure popover and in the selected panel breadcrumb. Name gets `min-inline-size: 12rem` before truncation begins.

### 8.1 Disclosure ladder — accessible truncation without tab-order noise

D35 stands: a truncated value must be discoverable without relying on pointer-only `title`. It must **not** be solved by adding `tabindex="0"` to every truncated text node — that manufactures hundreds of artificial tab stops and makes keyboard navigation worse than the problem it solves. Apply in order, and stop at the first that fits:

1. **An existing focusable owner carries the disclosure.** If the truncated text sits inside a focusable row, cell button, chip, tab, menu item or tree item, that control owns the full value (accessible name or `aria-describedby`) and reveals it on hover, `:focus-visible` and long-press. This covers the large majority of cases: table rows, tree rows, list rows, picker options.
2. **The full value already exists on a related surface.** Entity identity, Access name/email, source labels and door names appear in full in the details/identity surface that the selection drives — no extra affordance is added.
3. **A deliberate disclosure control** (a small "القيمة الكاملة" button/popover trigger) is added only where keyboard discovery is materially required and neither 1 nor 2 applies — for example a long value in a static header with no owning control.
4. **Non-interactive text becomes focusable only under a justified accessibility contract**, documented per instance. This is the exception, never the pattern.

Hover / focus / long-press disclosure remains valid wherever the owning interaction supports it.

---

## 9. Accessibility and RTL expectations

- **Direction:** all layout logical. Latin islands use `dir="ltr"` + `unicode-bidi: isolate` on the *value element only*.
- **One ARIA vocabulary per family:** `role="table"` with `aria-rowcount`/`aria-colcount` on **all** explorer tables (D24); `role="list"/"listitem"` on **all** detail result lists (D25); `role="tree"/"treeitem"` with `aria-level`, `aria-expanded`, `aria-selected` on live and archive Abwab only (G20); `role="list"` for template hierarchy (G20); `role="tablist"` everywhere tabs appear (D27).
- **Names:** every icon-only control has a text name; every truncated value's full text is reachable through the §8.1 disclosure ladder (on the owning control, not on a synthetic tab stop); every count chip's name includes its dimension ("المواضع: 381").
- **State announcement:** one `aria-live="polite"` region per workspace for filter results, mutation outcomes and pagination changes; `aria-busy` on refreshing regions; error blocks are `role="alert"` only for write failures.
- **Focus:** visible ring everywhere (§2.7); focus never trapped except in modals/sheets; focus returns to trigger on close; sheet navigation gets trap + scroll lock + inert background + visible Close (D13).
- **Targets:** 44px minimum hit area everywhere (§3.6), verified after implementation — this design states intent only (handoff §24.18).
- **Not claimed:** WCAG conformance, screen-reader behaviour, forced-colors, 200% zoom and 320px verification remain post-implementation work.

---

## 10. Fixture data used across all Golden frames

Sanitized, contract-shaped, from handoff §5–§6. Used verbatim in the four visual boards.

- **Roots:** `س م و` (المواضع 381 · الآيات 352 · السور 81 · بدون تشكيل 26 · بالتشكيل 52 · الصيغ 6 · الأصول 29), `ا ل ه` (2,851 · 1,879 · 86), `ر ح م` (339 · 313 · 62), total 1,642, page size 1000 stress case.
- **Lemmas / Stems:** long vocalized Arabic identities with nullable root/lemma (`—` + "غير مرتبط" label), type distribution chips.
- **Unique words:** simple/vocalized modes (G06), `kind` + nullable primary type/root.
- **Word Types:** اسم 12,364 · فعل 8,544 · حرف وأداة 800 · حروف مقطعة 14; children اسم 10,459, اسم علم 412, صفة 847, ضمير 215, اسم موصول 100, اسم إشارة 75, ظرف زمان 106, ظرف مكان 148, اسم فعل أمر 2 — including the **count = 0** case that drives D36.
- **Access:** `900001` null display name / `curator.with.a.long.address@example.test` / pending / 0 permissions; `900002` "مديرة مراجعة المحتوى ذات الاسم الطويل لاختبار الالتفاف" / owner / active; `900003` "مراجع" / disabled. Permission catalogue 19 codes in 5 groups (Doors 6, Sections 4, Relations 2, Templates 3, Template nodes 4), group indeterminate case shown.
- **Audit:** `PermissionGranted` · `2026-08-09T10:15:00Z` · target 900001 · actor System · `abwab.doors.create` · long Arabic reason.
- **Abwab:** section "قسم بحثي طويل لاختبار العنوان في شريط الأقسام" (128 doors in scope); door `930007` name "باب طويل متعدد المستويات لاختبار الالتفاف والاقتطاع داخل الشجرة", depth 7, liveChildCount 24, liveDescendantCount 1,284, maxRelativeDepth 5, relationCount 0 (dashed zero), aliases ["اسم بديل", "اسم بديل طويل لاختبار الشريحة"]; template "قالب بحثي متعدد الفروع" (47 nodes).
- **Mushaf:** page 5, verse key `2:25`, selected word location `2:25:1`, verified word `وَبَشِّرِ`; **all full ayah / commentary bodies render as `[actual Quran text from API]` placeholders** (handoff §24.4). No Quran text is authored anywhere in this pack.
- **Counts stress:** `0`, `114`, `21,294`, `1,000,000`.

---

## 11. Locked decisions and the one that remains open

### 11.1 Now locked

| ID | Decision | What the Golden system does |
|---|---|---|
| **D36** | **Disabled + visible reason.** A zero-count detail/category trigger is **not** openable; it does not open an empty detail merely to state that nothing exists. | One rule across the whole explorer family: the trigger renders disabled (`--qd-neutral-tint` fill, `--qd-neutral-ink-disabled` text, no hover, no focus ring on a non-actionable control) with `aria-disabled="true"` and a **visible, accessible reason** — "لا كلمات مرتبطة بهذا النوع، لذا لا تفاصيل لعرضها." — associated via `aria-describedby`, not a tooltip. Applies identically to all five explorers, to grouped rows, and to zero-count metric chips. The alternative (enabled → empty detail) is **removed from the contract** and appears in the boards only as clearly marked rejected evidence. |
| **D37** | **Non-interactive.** Morphology segment rows stay content, not controls. | No `<button>`, no `role="button"`, no hover affordance, no focus ring, no pointer cursor. They are labelled content cards inside the word-study panel until a real product action is approved. |
| Wide threshold | **1080px approved.** | Wide/desktop structure begins at 1080; Medium remains a true designed mode from 768 to 1079 (§4). |
| Access route-leave | **Canonical confirm is the design target**, not an authorisation to change behaviour now. | The Golden contract expresses route-leave with a dirty Access draft as the canonical confirm shell (F14 `confirm`, target named, Discard/Continue). Implementation adopts it **only if** the existing router/dirty-state architecture can support it without weakening the current protection; if canonical replacement carries engineering risk, the current native `window.confirm` protection is preserved and the canonical shell remains a documented target. Never remove the protection to gain the visual. |

### 11.2 Still open — OWNER DECISION REQUIRED

| ID | Open question | What the Golden system does | What it needs |
|---|---|---|---|
| **D38** | Mushaf `panel`, `wordTab`, `segment` URL state serializes and hydrates with no visible consumer. | Designs the Mushaf study chrome from **current visible behaviour only**. No control is invented for these keys. The Workspaces and Responsive boards mark the area `OWNER DECISION REQUIRED — IMPLEMENT OR RETIRE`. | A decision to give them approved visible behaviour, or to retire them from the URL contract. |

Also deferred by explicit instruction: dark theme navy/gold reconciliation (untouched by this pack).

---

## 12. How to read the rest of the pack

| File | Contains |
|---|---|
| `GOLDEN_UI_COMPONENT_CATALOG.md` | All 20 families with the required 18 fields each: purpose, consumers, anatomy, visual/interaction language, variants, optional zones, removed drift, preserved differences, Desktop/Tablet/Mobile behaviour, state coverage, geometry, spacing, typography, overflow, a11y/RTL, fixtures, downstream consumers. |
| `UI_DRIFT_TO_CANONICAL_MAP.md` | D01–D50 → family + canonical decision + acceptance criterion; G01–G24 → where the difference lives and how it is constrained. |
| `IMPLEMENTATION_HANDOFF_SUMMARY.md` | Token → Tailwind → `qd-*` → shared component → feature composition guidance, build order, pilots, review gates. |
| `Golden UI — Foundation.dc.html` | Surface levels, type, spacing, control geometry, states, badges, the five async concepts, focus, RTL islands, semantic token table. |
| `Golden UI — Families.dc.html` | Table, details, toolbar, tabs, modal, async, pagination, tree, floating layers, chips — realistic fixtures at Wide/Medium/Compact, plus the locked D36 treatment. |
| `Golden UI — Workspaces.dc.html` | Access Management as **four separate lifecycle state frames**, Abwab workspace, Words Explorer, Mushaf shell — annotated compositions. |
| `Golden UI — Responsive Critical States.dc.html` | The structural transformations an implementer could otherwise misread: app chrome, Access, Abwab, details, modal, searchable picker, Mushaf at Medium and Compact. |
