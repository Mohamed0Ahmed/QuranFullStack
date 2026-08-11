# Implementation Handoff Summary

> One-page-per-section summary for the next step: **approve Golden design → revise Plan 7 → implement the shared foundation and the selected pilots.** No code, no Angular, no Tailwind config, no SCSS. Nothing here authorises data, route, auth or Quran-rendering changes.

---

## 1. What was designed

| Deliverable | Contains |
|---|---|
| `GOLDEN_UI_SYSTEM.md` | Foundation: principles, colour, type, spacing, page-width strategy, responsive strategy, control geometry, grids, async language, stable geometry, overflow/scroll ownership, a11y/RTL, fixtures, owner decisions. |
| `GOLDEN_UI_COMPONENT_CATALOG.md` | 20 canonical families × 18 fields (purpose, consumers, anatomy, visual + interaction language, variants, optional zones, removed drift, preserved differences, Wide/Medium/Compact behaviour, states, geometry, spacing, typography, overflow, a11y/RTL, fixtures, downstream consumers). |
| `UI_DRIFT_TO_CANONICAL_MAP.md` | D01–D50 → family + decision + acceptance criterion; G01–G24 → location + constraint; design-problem categories → mechanism. |
| `Golden UI — Foundation.dc.html` | Visual board: five surface levels + footer chrome, ink, green state semantics, the full status/lifecycle token table, type scale, spacing, control geometry, focus, buttons, fields, chips/badges, five async concepts (no gradients), RTL/LTR islands. |
| `Golden UI — Families.dc.html` | Visual board: Golden Table (Wide/Medium/Compact + all states), Details workspace, toolbar/filters, tabs, pagination, modal shell family, floating layers, tree, async states, and the **locked D36** disabled-with-reason treatment (with the rejected alternative marked as such). |
| `Golden UI — Workspaces.dc.html` | Visual board: Access Management as **four separate lifecycle state frames** (Active Owner / Pending non-Owner / Active non-Owner / Disabled non-Owner) + audit + security, Abwab (tree/cards/rail/authoring), Mushaf shell with protected zones and the D38 marker. |
| `Golden UI — Responsive Critical States.dc.html` | Visual board: the structural transformations most at risk of misinterpretation — app chrome (390/768), Access (390), Abwab (768/390), Details (768/390), Modal (390), searchable picker (390), Mushaf (768/390). |

---

## 2. The ten decisions that matter most

1. **One gutter owner, four named page intents** (`capped-reading` / `full-data` / `split-workspace` / `protected-mushaf`). Kills D01–D06 and reclaims workspace width.
2. **Desktop starts at 1080px, and desktop navigation is not exposed below it.** Direct fix for the measured 961px/866px documents at a 768 viewport, plus the 1024-edge clipping. Medium becomes a designed mode, not a squeeze.
3. **Hard ceiling, mode-scoped:** in **Medium**, no rendered surface may require more inline width than the Medium content box (720px) — a surface that cannot fit switches composition instead of widening the page. Wide-only layouts may keep larger legitimate minima. Page-level horizontal scroll is a defect in every mode.
4. **One control geometry scale** (32/40/48) with a **44px hit-area contract independent of visual density**, and one `:focus-visible` ring. Kills D20, D21, D45–D47.
5. **Green is state, never decoration**: solid = the single primary action, tint = current/selected, 2px logical inline-start thread = selection. Neutral hover everywhere. Kills D12, D14–D16, D26.
6. **One Golden Table shell with three row renderers**; Access users/audit and Abwab trees stay out of it. Kills D23–D26 without forcing unlike surfaces together.
7. **One details shell** (identity → metadata → tabs → status slot → single-scroller body) consumed by Words, Access, Abwab and Mushaf. Kills D28, D31, D32 and the blank-panel problem.
8. **One modal shell, four named widths, shell-owned padding, one body scroller**, Compact = 94dvh sheet. Kills D48, D49 and the five-geometry drawer family.
9. **Five separate async concepts** (skeleton / refreshing / empty / error / notice) with declared geometry ownership, **no invisible reserves**, and **no gradients** in any state treatment. Kills D39–D41 and D18. A temporary `qd-state` compatibility adapter is allowed during migration (§6).
10. **Every truncation has a non-pointer disclosure path** via the disclosure ladder (owning control → related details surface → deliberate control → justified exception) — never `title` alone, and never a `tabindex="0"` per truncated text node. Kills D35 without polluting the tab order.

---

## 3. Locked architecture direction, expressed as design intent

```
CSS variables / tokens        →  the §2–§3 semantic token set: five surface levels + footer chrome,
                                 ink, green-state, the full status/lifecycle/mutation token table
                                 (§2.4 — exhaustive, so nothing is invented at implementation time),
                                 radius, shadow-layer, spacing scale, control heights, rail scale,
                                 page intents, breakpoint bands
Tailwind                      →  the DEFAULT styling mechanism; the token set is what Tailwind is
                                 configured from, so utilities carry semantics, not raw hex/px
qd-* semantic classes         →  reserved for cross-feature *meaning* that utilities cannot express:
                                 page intents, surface levels, green-state selection thread,
                                 status semantics, skeleton/refresh/empty/error blocks, hit-area
                                 expansion, LTR isolate
Shared Angular components     →  the families with a visual **and** interaction contract:
                                 F05 button, F06 field set, F07 tabs, F09 table shell, F10 list,
                                 F11 details shell, F12 state set, F13 pagination, F14 modal shell,
                                 F15 floating layer, F16 tree, F17 chip/badge
Feature composition           →  F19 Access and F20 Abwab compose the above; explorers compose
                                 F08+F09+F11; Mushaf composes F18 zones
Specialised SCSS/CSS          →  the EXCEPTION, allowed wherever specialised CSS is materially
                                 clearer, safer or more maintainable than utilities (see §3.1)
```

### 3.1 SCSS / CSS exception policy

Tailwind is the default; SCSS is the exception — but **not** a closed whitelist. Specialised CSS is legitimate whenever it is materially clearer, safer or more maintainable than utility composition. Known valid categories:

- complex selectors and state-precedence problems;
- pseudo-elements (`::before`/`::after`, hit-area expansion, indentation guides, edge fades);
- animations, `@keyframes` and `prefers-reduced-motion` handling;
- third-party, projection and view-encapsulation overrides;
- intricate scrolling / sticky / fixed / viewport and `dvh`/safe-area geometry;
- Quran and Arabic rendering (always — protected);
- specialised or inconsistent browser CSS behaviour.

The examples named elsewhere in this pack (Mushaf rendering, tree indentation guides, Quran canvas measure) are **illustrations, not the exhaustive list**. The test is justification, not category membership: if a reviewer can state why utilities are worse here, SCSS is correct.

### 3.2 Typography authority

The visual boards use Amiri / IBM Plex Sans Arabic / IBM Plex Mono as **design-preview faces** standing in for three roles (display-Naskh, UI-sans, LTR-mono). **Implementation must use the current approved project fonts.** This pack does not authorise a font change, does not authorise adding Google Fonts, and never touches Quran fonts. Because Arabic faces differ in metrics, implementation must re-validate the Golden geometry (control heights, row heights, truncation thresholds, `min-inline-size`, prose measures) against the real project faces and report any value needing a step adjustment.

Rule of thumb for reviewers: **visual-only repetition → semantic class; visual + interaction repetition → shared component; single-surface layout → feature composition.**

---

## 4. Suggested build order (design-side sequencing, not a schedule)

1. **Foundation** — tokens (including the exhaustive status/lifecycle/mutation table), breakpoint bands, page intents, control geometry, focus, surface levels, status semantics. Nothing else can be consistent before this.
2. **State set (F12) + button/field (F05/F06)** — every family depends on them; they also remove the largest single source of drift (53 `qd-state` call sites, local control copies). The `qd-state` **compatibility adapter** ships with this step so call sites can migrate incrementally: it keeps existing inputs/outputs/test IDs, delegates to the five primitives, gains no new consumers, and is deleted when its last call site is migrated.
3. **Pilot A — Words Explorer** (F02 split + F08 + F09 + F11 + F13 + F14 sheet). Highest duplication payoff: five shells become one, and it exercises Wide/Medium/Compact plus every async state.
4. **Pilot B — Access Management** (F19 with F07, F10, F17, F12 review dock). Highest safety payoff: removes the blank mutation band, fixes the tablet master/detail mode, and proves the staged-review contract.
5. **Modal + floating layer (F14/F15)** — unlocks Abwab's six dialogs and every picker with one contract.
6. **Abwab workspace (F20 + F16)** — depth budget, 44px targets, bounded cards, shared authoring shell.
7. **Mushaf chrome only (F18)** — tab keyboard, source picker, 44px nav targets. Protected rendering untouched.
8. **Shell + placeholders (F01/F02)** — accessible sheet navigation and the placeholder page axis.

---

## 5. Review gates before implementation

- [x] **D36 locked** — zero-count triggers are **disabled with a visible, accessible reason**, uniformly across all five explorers, grouped rows and metric chips. The enabled→empty alternative is out of the contract.
- [x] **D37 locked** — morphology segments are non-interactive content.
- [x] **Wide threshold 1080px approved**; Medium (768–1079) is a designed mode.
- [x] **Access route-leave** — canonical confirm is the design target; adopt only if the current dirty-state/navigation protection is not weakened, otherwise keep native `window.confirm` and keep the canonical shell as a target.
- [ ] **D38 still open** — Mushaf `panel`/`wordTab`/`segment`: implement approved behaviour or retire from the URL contract. No behaviour may be inferred from this pack.
- [ ] Confirm the **rail scale** (16/18/20rem) replacing 15.5/18/20rem constants.
- [ ] Confirm **fonts**: implementation uses the current approved project faces; any change is a separate owner authorisation. Geometry re-validated against those faces.
- [ ] Confirm dark theme stays **out of scope** for this cycle.
- [ ] Confirm that `Load more` (audit) remains a separate capability from numeric pagination.

---

## 6. Verification work that must follow implementation (not claimed here)

> Method: `GOLDEN_VISUAL_VERIFICATION.md` in this folder — acceptance hierarchy, computed-geometry
> and responsive checks, evidence rules, and the authenticated/protected-state constraints.

Keyboard and screen-reader passes per family script; computed 44px hit-area audit (pagination, tree, Mushaf); 320px, short-landscape, 200% zoom and forced-colors checks; reduced-motion comparison for skeleton pulse and state transitions; a re-measure of document width at 768 and 1024 to confirm zero page-level horizontal scroll; contrast verification of `--qd-ink-muted` on all four surface steps; RTL arrow-key behaviour in trees and tablists; populated-archive and deep-hierarchy density review (the current archive is empty, so real density was never observed).

---

## 7. Guardrails carried forward

No implementation in this phase · Plan 7 is not revised or approved by this pack · no product data changed · **no Quran text authored anywhere** (all bodies are `[actual Quran text from API]` placeholders; the only literal Quran token used is the verified word `وَبَشِّرِ` at `2:25:1` from the handoff fixtures) · Quran rendering and Quran fonts untouched · **no font change authorised** (boards use preview faces; implementation uses the approved project faces) · auth/authorization untouched · Access is not generic role CRUD and no frame implies an Active Owner editing direct permissions · Abwab semantics not flattened · no Approve/Reject modal families invented · no protected/locked Abwab door state invented · no gradients, glass, resting shadows, hover lifts, decorative imagery or gamification · green never decorative · dark theme deferred · screenshots not committed (the temporary copies used during design were deleted).

---

## 8. Counts

| Metric | Value |
|---|---|
| Canonical families fully designed | **20** (F01–F20) |
| Drift findings resolved | **50** mapped — **49** normalized (48 direct + D36 locked), **1** gated (D38) |
| Genuine domain differences preserved | **24** (G01–G24) |
| Light UI surface levels | 5 (+ footer chrome, outside the ladder) |
| Named page intents | 4 |
| Structural responsive modes | 3 (+ wide-plus measure band) |
| Named modal widths | 4 (+ Compact full-bleed) |
| Async concepts separated | **5** (from 1) |
| Table shells | 1 (from 5) with 3 row renderers |
| Tab implementations | 1 (from 18 manual tablists) |
| Visual boards | 4 |
| Outstanding owner decisions | **1** (D38) |
