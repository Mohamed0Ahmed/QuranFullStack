# UI Drift → Canonical Map

> Maps every ledger row in `UI_DESIGN_HANDOFF.md` to a Golden decision. **Part 1** resolves the 50 drift findings (48 direct removals, plus D36 now locked by owner decision and D38 still gated). **Part 2** records where each of the 24 genuine domain differences lives in the Golden system and how it is constrained. **Part 3** lists the design-problem categories and the mechanism that answers each.
>
> `Family` = owning Golden family (`GOLDEN_UI_COMPONENT_CATALOG.md`). `Ref` = section of `GOLDEN_UI_SYSTEM.md` carrying the rule. Acceptance criterion = what a reviewer checks to declare the drift gone.

---

## Part 1 — Drift resolution (D01–D50)

### Page, shell and responsive drift (D01–D13)

| ID | Canonical decision | Family | Ref | Acceptance criterion |
|---|---|---|---|---|
| D01 | Gutters belong to the page shell alone; no feature, layout partial or explorer stylesheet may add inline padding. | F02 | §3.4 | Computed inline padding from viewport edge to content is exactly one gutter value on every route. |
| D02 | Width is chosen by a named page intent (`capped-reading` / `full-data` / `split-workspace` / `protected-mushaf`), never by composing contradictory classes. | F02 | §3.4 | Access renders `split-workspace`; no element both caps and un-caps width. |
| D03 | Access uses the same page header + block rhythm as every other route; safety actions are a header zone, not a spacing exception. | F02, F03, F19 | §3.3, §3.4 | Access page-header offsets equal Abwab/Words offsets to the pixel. |
| D04 | Placeholder heading and body share one axis inside `capped-reading`; empty page has a designed min height and one message + one action. | F02, F03, F12 | §3.4, §6.4 | Placeholder title and body left/right edges align; page no longer reads as an accidental blank. |
| D05 | All five explorers begin their table/detail layout on the same horizontal axis; Word Types' specialised filters are a **slot**, not a different frame. | F02, F08 | §3.4, §4.2 | Word Types table container edges align with Roots at all three modes. |
| D06 | The responsive shell owns the sole gutter; the explorer layout adds none below desktop. | F02 | §3.4 | No 16px double gutter at ≤1079. |
| D07 | Dashboard destinations: 18–26rem measure, max 3 columns, orphan rule (5 → 3+2, last row stretches). | F04 | §5.2 | No 4+1 with a large blank remainder at 1440. |
| D08 | Abwab cards: 14–20rem measure, **max 4 columns**, ordered rows. | F04, F20 | §5.2 | Column count never exceeds 4 at any width. |
| D09 | Words hub keeps its 2+2+1 teaching order (G05) expressed through canonical bands with a 20–30rem measure and max 2 columns; the final card spans both columns. | F04 | §5.2 | No local 640px rule; order unchanged. |
| D10 | One breakpoint vocabulary shared by CSS and TypeScript, including the wide band. | F02 | §4.1 | Single source of band definitions; TS and CSS agree, wide band present. |
| D11 | Named bands only (Compact / Medium / Wide / Wide-plus). Raw 360/420/640 thresholds are removed; exceptions require a documented reason. | F02 | §4.1 | Grep finds no undocumented raw pixel breakpoints. |
| D12 | One neutral hover surface (`--qd-surface-quiet`) for navigation, menus and cards. Green is never a hover tone. | F01, F04, F15 | §2.3 | Navbar/dropdown/mobile link hover equals card hover. |
| D13 | Mobile navigation is a real dialog: focus trap, inert background, scroll lock, visible "إغلاق", focus return. | F01 | §9 | Keyboard cannot escape the open sheet; background does not scroll. |

### Visual token, motion, form and local-control drift (D14–D22, D50)

| ID | Canonical decision | Family | Ref | Acceptance criterion |
|---|---|---|---|---|
| D14 | Buttons never translate. Active state changes colour/border only. | F05 | §2.6, §7 | No `transform` on any `:active` control. || D15 | Generic card hover is neutral; accent border is reserved for selected/current. | F04 | §2.3 | Hover and selected are visually distinguishable without colour comparison. |
| D16 | Select hover uses a neutral border; green appears only on focus ring and selected option. | F06 | §2.3, §2.7 | Non-selected select hover carries no green. |
| D17 | Select chevron is an icon asset on a flat background — no gradients in the control layer. | F06 | §2.6 | Zero `linear-gradient`/`radial-gradient` in the control layer. |
| D18 | Skeletons use a flat opacity pulse (1 → .62, 1.4s), never a shimmer gradient. Refresh indicators are a solid segment on a flat track. | F12 | §6.4 | **Zero** `linear-gradient`/`radial-gradient`/`conic-gradient` in loading, refresh, controls, or surfaces. The token owner contains only the two fixed multi-door Mushaf gradients, whose consumers are separately restricted to the Mushaf word renderer; motion respects reduced-motion. |
| D19 | No entrance motion for toolbars or feature identity; transitions are state-only (120–160ms). | F08 | §1.4 | `uw-toolbar-rise` equivalent removed. |
| D20 | One control geometry scale: input, select, textarea, button, tab and chip-button share height, radius, border, padding and baseline. | F05, F06, F07 | §3.6 | Measured heights identical within a scale step across all five features. |
| D21 | One focus contract: `:focus-visible` only, 2px green outline, 2px offset, no size change. | F05, F06 | §2.7 | Pointer clicks paint no ring; keyboard always does. |
| D22 | Abwab composes shared field/button primitives; only layout SCSS remains feature-local. | F06, F20 | §3.6 | Abwab controls are pixel-identical to Access controls. |
| D50 | One shared danger menu-item treatment; local overrides removed. | F15 | §6.2 | Template delete hover equals Abwab delete hover. |

### Table, detail, tab, picker, disclosure and inert-affordance drift (D23–D35, D37)

| ID | Canonical decision | Family | Ref | Acceptance criterion |
|---|---|---|---|---|
| D23 | One Golden Table shell with three row renderers (`standard`, `wide-columns`, `grouped-rows`). Columns and density vary; shell behaviour does not. | F09 | §-, catalog F09 | Header/loading/selection/pagination behaviour identical across all five explorers. |
| D24 | `role="table"` + `aria-rowcount`/`aria-colcount` on every explorer table. | F09 | §9 | All five expose row counts, not just Word Types. |
| D25 | `role="list"`/`listitem` on every detail/result list; documented exceptions only. | F10 | §9 | AT enumerates every result list identically. |
| D26 | Selection edge is a logical 2px `border-inline-start` green thread. | F09, F10, F11, F16 | §2.3 | No physical `right` inset anywhere. |
| D27 | One tab behaviour (roving tabindex, logical RTL arrows, Home/End, `aria-controls`) replaces 18 manual tablists. | F07 | §6.1 | Every tablist passes the same keyboard script. |
| D28 | One detail-tab primitive; secondary-button-styled tabs removed. | F07, F11 | §6.1 | Root/Word Type detail tabs look and behave like Lemma/Stem tabs. |
| D29 | Selected-ayah tabs get the full keyboard contract; Quran content is untouched. | F07, F18 | §6.1 | Arrow/Home/End work in the study tabs; rendering unchanged. |
| D30 | Tab layout is chosen by count and mode (segmented ≤3, scrollable ≥4 below Wide) — never accidental CSS wrap. | F07 | §6.1 | Word Type's 3-tab toolbar never forms two columns. |
| D31 | Per-instance IDs for every detail/tab/pagination instance. | F07, F11, F13 | §9 | No duplicate IDs when inline detail and overlay coexist. |
| D32 | One `notFound` rule: stay inside the tabpanel, keep the label, render the `error/notFound` state via F12. | F11, F12 | §6.4 | Root/Lemma/Stem/Word Type behave identically for a deleted deep link. |
| D33 | One floating-layer keyboard contract (open, Escape, Arrow + scroll-into-view, Home/End, type-ahead, Tab-closes, focus return). | F15 | §6.2 | Surah, source and association pickers pass one script. |
| D34 | One anchored-layer geometry utility: `max-block: min(60vh,24rem)`, block-axis flip, inline clamp, no document reflow. | F15 | §6.2 | No picker clips off-viewport at any width. |
| D35 | Truncation always pairs with a disclosure path, applied through the §8.1 **disclosure ladder**: the existing focusable owner carries the full value → else the related details/identity surface already shows it → else a deliberate disclosure control → and only then, with justification, focusable non-interactive text. `title` alone is never sufficient, and `tabindex="0"` on every truncated text node is explicitly prohibited. | F09, F10, F11, F15, F17 | §8, §8.1 | Keyboard-only and touch-only users can read every truncated value, **and** the tab order gains no artificial stops. |
| D37 | **LOCKED: non-interactive.** Morphology segment rows render as content (no button semantics, no `role="button"`, no hover affordance, no pointer cursor, no focus ring) until a real product action exists. | F18 | §11.1 | No control promises an action that does not exist; AT announces content, not a button. |

### Async-state, pagination and action-target drift (D39–D47)

| ID | Canonical decision | Family | Ref | Acceptance criterion |
|---|---|---|---|---|
| D39 | Five separate concepts (skeleton / refreshing / empty / error / notice) replace one conflated state component; each declares its geometry ownership. A temporary `qd-state` **compatibility adapter** delegating to these five is permitted during migration, but never as the semantic owner and never with new consumers. | F12 | §6.4 | No single component renders loading, empty, error and success; any surviving adapter only delegates and its call-site count only decreases. |
| D40 | Surfaces with a known final shape use content-shaped skeletons; text loaders only for single-value regions. | F12 | §6.4 | Access list loads as a skeleton list, matching Words and Mushaf. |
| D41 | The mutation feedback slot has **zero** height when idle; staged changes live in a sticky review dock that exists only when dirty. | F12, F19 | §6.4, §7 | No ~6.5rem permanent blank band; nothing shifts when a notice appears. |
| D42 | Pagination jump input has one fixed width in all states. | F13 | §6.5 | No width change on focus. |
| D43 | Go is always mounted (disabled when empty/invalid). | F13 | §6.5 | No control appears or disappears during interaction. |
| D44 | Per-instance IDs for jump input, error and live region. | F13 | §6.5 | Two visible pagers never share IDs. |
| D45 | 44px minimum hit area at Compact (48px controls) regardless of visual density. | F05, F13 | §3.6 | No 28–32px phone control. |
| D46 | Tree/picker actions get 44px hit areas and are never hover-only. | F15, F16 | §3.6, §6.2 | Chevrons and row actions are usable by touch and reachable by keyboard. |
| D47 | Mushaf navigation and page triggers get 44px hit areas around unchanged Quran content. | F18 | §3.6 | Computed hit areas verified post-implementation. |

### Modal geometry and padding/overflow drift (D48–D49)

| ID | Canonical decision | Family | Ref | Acceptance criterion |
|---|---|---|---|---|
| D48 | Four named widths (`confirm` 30rem, `form` 38rem, `wide` 52rem, `overlay` 46rem) on one viewport/scroll contract; Compact is a full-bleed 94dvh sheet. | F14 | §6.3 | Every dialog resolves to a named width; no fifth geometry. |
| D49 | The shell owns padding; the confirm gains the same `max-block-size` + single body scroller as authoring dialogs. | F14 | §6.3 | Long confirmation content scrolls in the body with header and footer visible. |

### Locked by owner (D36, D37) — and the one still open (D38)

| ID | Status | Canonical decision |
|---|---|---|
| **D36** | **LOCKED — disabled + visible reason** | A zero-count detail/category trigger is **not** openable. It renders disabled (`--qd-neutral-tint` fill, `--qd-neutral-ink-disabled` ink, no hover, no pointer cursor) with `aria-disabled="true"` and a visible reason associated via `aria-describedby` — "لا كلمات مرتبطة بهذا النوع، لذا لا تفاصيل لعرضها." One rule for all five explorers, grouped rows and zero-count metric chips. The "enabled → empty detail" alternative is **removed from the contract** and survives only as clearly marked rejected evidence in `Golden UI — Families.dc.html`. Acceptance: no explorer opens a detail whose only content is "nothing here", and every disabled trigger exposes its reason to AT. |
| **D37** | **LOCKED — non-interactive** | See the D37 row above. |
| **D38** | **OWNER DECISION REQUIRED — IMPLEMENT OR RETIRE** | Mushaf `panel`, `wordTab` and `segment` serialize and hydrate with no visible consumer. No control is invented. The study chrome is designed from current visible behaviour only; the Workspaces and Responsive boards carry the marker. Blocked until the owner decides to give them approved behaviour or retire them from the URL contract. |

Related, now a **design target rather than an open question**: Access route-leave with a dirty draft should use the canonical confirm contract (F14 `confirm`, target named), adopted **only if** the current router/dirty-state architecture supports it without weakening today's protection — otherwise the native `window.confirm` protection is preserved and the canonical shell stays a documented target. Dark-theme navy/gold reconciliation remains explicitly deferred and untouched.

---

## Part 2 — Genuine differences preserved (G01–G24)

| ID | Where it lives in the Golden system | How it is constrained so it cannot become drift |
|---|---|---|
| G01 | Every family: logical properties only; LTR is a value-level isolate. | One direction contract (§9); Latin islands may not set layout direction. |
| G02 | F18 `page-canvas`, `word-study`, `ayah-study`, `commentary`, `ayah-result-card`. | Protected renderer is out of the system's authority; the system styles only surrounding chrome; no truncation, compression or animation may reach it. |
| G03 | F18 + F02 `protected-mushaf` intent; `--qd-split-mushaf` 40/60 kept **outside** the rail scale. | Mushaf is the only surface allowed a feature-owned measure and a page-shaped 52rem reservation. |
| G04 | F02's four named page intents. | Purpose-named variants only; no page may compose its own width. |
| G05 | F04 `navigation` (Dashboard destinations) vs the Words-hub curriculum grid. | Same card base, different grid rule and eyebrow semantics (step numerals); both bounded by max columns. |
| G06 | F09 `standard` + F11 `entity`: Unique simple/vocalized as route modes with a mode identity slot. | Mode changes identity presentation only; shell, filters and states are shared. |
| G07 | F07 tab counts per entity (5/4/4/3/2–3) and F11 tab zone. | Counts and labels vary; tab behaviour, geometry and panel states do not. |
| G08 | F08 `explorer`/`taxonomy` filter slots. | Fields differ; submit/clear/draft/applied/count/wrap order are shared. |
| G09 | F09 Compact card min-heights (root 5.5 / lemma 6.5 / stem 6.75 / unique 4.25 / grouped 5rem). | Only the min-height differs; padding, label-value pattern, selection, actions and targets are one system. |
| G10 | F09 `wide-columns` + `grouped-rows` renderers; F08 `taxonomy`. | Default ordering, taxonomy filters and display-only members preserved inside the shared table shell. |
| G11 | F10 `quran-result` frame delegating to F18 `ayah-result-card`. | Quran rows never adopt table cell geometry; non-Quran rows never adopt ayah geometry. |
| G12 | F18 `commentary` source picker: language-first grouped hierarchy for Tafsir/translation, flat list for full i'rab. | Both use one F15 `searchable-picker` behaviour; only the option hierarchy differs. |
| G13 | F18 `similar-results` (score/coverage/matched) vs `mutashabihat-groups` (group key, phrase, occurrences). | Distinct renderers, shared F10 frame, shared async and disclosure rules. |
| G14 | F14 `overlay` adapters: word identity only; grouped root/stem/lemma details stay local to the page. | Overlay adapter registry is explicit; adding grouped identities requires a product decision. |
| G15 | F14 `overlay` history zone: Back, retained-closed Restore, base-route preservation, 8-frame cap + cap-rejection state. | History controls are fixed shell furniture across all adapters. |
| G16 | F19 Owner guard, stated in copy as well as enforced; Abwab reads public, writes permission-gated in F20. | Authorization is never expressed by hiding UI alone; capability disclosure rules are per-family and explicit. |
| G17 | F17 `status` (Pending/Active/Disabled/Unknown) + separate `membership` (Owner) badge. | Lifecycle and Owner never merge into one chip; unknown never maps to Disabled; no role palette is invented. |
| G18 | F19 inline staged review + review dock vs F20 modal authoring/confirmation. | Permission decisions stay inline (diff needs page context); hierarchy authoring stays contained (form needs focus). |
| G19 | F08 `workspace` search + F16 mode variants: live tree marks and retains hierarchy (zero-match reports a count), cards filter the level, archive prunes to matching paths, pickers filter their own hierarchy. | Each mode's search meaning is stated in the UI, so a shared control cannot flatten the semantics. |
| G20 | F16 `live-tree`/`archive-tree` (`role="tree"`) vs `template-list` (`role="list"`). | Role follows implemented keyboard behaviour; a list may not claim tree semantics. |
| G21 | F16 `destination-picker` (single, cycle/subtree exclusion, pinned root) vs `set-picker` (multi-select). | Two named variants of one hierarchy component; exclusion reasons are always visible. |
| G22 | F16 `archive-tree`: Restore may be **visible-disabled** with an explanation. | The single documented exception to "missing writes are hidden"; requires the reason text. |
| G23 | F20 `cards`: no context menu, drill-down + selection only. | Card mode may not grow a second action surface. |
| G24 | F14 `wide` template-copy body: rules stated in the confirmation (direct children only, root never copied, copies detached). | Rule text is part of the shell's required content, not optional helper copy. |

Uncounted but respected: relation direction/grouping, root creation requiring a live section, fixed authoring-dialog height, natural footer height, deferred dark theme.

---

## Part 3 — Design-problem categories → answering mechanism

| Category (handoff §18) | Answering mechanism |
|---|---|
| `VISUAL_INCONSISTENCY` | One control geometry scale (§3.6), one focus contract (§2.7), one tab behaviour (§6.1), one floating-layer contract (§6.2), one hover token (§2.3). |
| `RESPONSIVE_FAILURE` | Three structural modes with the desktop threshold at 1080 and desktop navigation excluded from Medium; the **mode-scoped** Medium width rule (no surface may need more than the Medium content box *in Medium*; Wide-only layouts may keep larger legitimate minima); designed Medium compositions per workspace (§4), visually demonstrated in `Golden UI — Responsive Critical States.dc.html`. |
| `LAYOUT_SHIFT` | Reserve/grow/never lists (§7); fixed pagination and busy-button geometry (§6.5, §3.6); zero-height notice slot (§6.4). |
| `OVERFLOW` | Per-content overflow contracts and one named scroller per panel (§8); Abwab depth budget; canonical confirm viewport rules. |
| `EXCESSIVE_GUTTER` | Single gutter owner + four page intents (§3.4). |
| `UNBOUNDED_GRID` | Per-collection min/max measure, max columns and orphan rule (§5.2). |
| `DUPLICATED_UI_CONTRACT` | 20 families as base + variants + zones; five table shells → one; 18 tablists → one; local Abwab controls → shared primitives. |
| `ACCESSIBILITY_DISCLOSURE` | Disclosure contract (§8), 44px hit-area contract (§3.6), one ARIA vocabulary per family (§9). |
| `DENSITY_PROBLEM` | Two density modes affecting padding only; designed no-selection states instead of blank panels; Medium compositions that remove tall empty filter regions. |
| `SPECIALIZED_VALID_DIFFERENCE` | F19/F20 as compositions that name every primitive they consume (no giant universal components). |
| `QURAN_PROTECTED_DIFFERENCE` | F18 protected zones with canonical chrome only; no generic overflow, motion or compression rule may reach Quran content. |

---

## Counts

- Drift findings mapped: **50** (49 normalized outright — 48 direct targets plus D36, now locked; 1 gated: D38).
- Genuine differences preserved: **24**.
- Families carrying the resolutions: **20**.
- Owner decisions outstanding: **1** (D38). D36 and D37 are locked; Access route-leave is a design target constrained by engineering risk; dark theme remains deferred.
