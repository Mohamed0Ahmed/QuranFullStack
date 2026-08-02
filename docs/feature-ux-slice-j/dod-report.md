# Slice J — §14 DoD report and verification evidence

Branch `feature/ux-slice-j` off `dev`. Recorded 2026-08-02. Frontend-only: no API contract,
no migration, no route-smoke gate.

## What landed

| Phase | Item | Status |
|---|---|---|
| 1 | J9 — tracking-panel deletion | landed (`61168e7c`) |
| 2 | J1 — `.qd-modal--wide` at 52rem | landed (`aece3223`) |
| 3a | J6 — `testIdPrefix` on the primitive | landed (`a34a37f9`) |
| 3b | J6 — five confirm migrations + ride-alongs A3-a/A2-a | landed (`15dc38ed`) |
| 4 | J8 — badge header row (subgrid) | landed |

## §14 DoD fields

- **Global style files changed:** `src/styles/_components.scss` (one new rule,
  `.qd-modal--wide`), `src/styles/_tokens.scss` (comment only — a rotted pointer to a rule
  that no longer exists was dropped).
- **New `qd-` classes:** `.qd-modal--wide`. Nothing else.
- **Theme tokens added or changed:** none.
- **Components affected:** three `--wide` adopters (relations modal, move picker, template-copy
  modal); five confirm sites (abwab page ×2, sections modal, templates page ×2); the
  `qd-confirm-dialog` primitive; the door modal; the door restore modal; the overlays controller.
- **Light/dark impact:** the danger tone's first production render, verified in both themes
  (see below). No token changed, so no other surface moved.
- **RTL impact:** dialog footers and the `--wide` geometry, both measured in RTL (below).
- **Build:** `npm run build` succeeds. Three budget warnings, all pre-existing — verified by
  building `dev` for comparison: initial bundle 573.91 kB on `dev` vs 573.95 kB here (+40 bytes),
  and both mushaf SCSS warnings are byte-identical on `dev`.

## Verification actually run

| Gate | Result |
|---|---|
| Tier A — door modal specs (phase 1) | 17/17 |
| Tier B — full suite after phase 2 | 195 files, 2376 tests |
| Tier B — full suite after phase 3 | 195 files, 2402 tests (+26 new cases) |
| Tier C — full `npm test` | 195 files, **2410** tests |
| Tier C — `npm run build` | succeeds (pre-existing warnings only) |
| E2E — `--project=abwab --workers=1` after phase 3 | 31/31, including the three rewritten specs |
| E2E — `--project=abwab --workers=1` after phase 4 | **35/35** |

The E2E line is the **`abwab` project only**, not the whole suite — `npm run e2e` is two
sequential runs and the `default` project was not executed. That matters slightly more than
usual here because `_components.scss` is global and the `default` project's surfaces compose it;
the one non-abwab surface at risk (a words detail modal) is covered by a case in the new width
spec, but a full `default` run would be stronger evidence. E2E is not a required tier either way.

The fork cap (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`) is baked into `npm test` and was
preserved — every run above went through `npm test`, never a bare `ng test`.

E2E is reported as **supplementary**, not as a Tier C substitute, per `TESTING_STRATEGY.md`.

## The visual checks (2.5, 3.13) — measured rather than eyeballed

The plan wrote tasks 2.5 and 3.13 as manual browser passes. They were instead written as
assertions in `e2e/abwab-slice-j-widths.e2e.ts` and run, because the §17 ladder is a set of
numbers and a number is a better gate than an opinion. All 6 cases pass:

- `--wide` measures exactly **832px** at 1024 / 1184 / 1440px, in **both light and dark**, with
  `direction: rtl` confirmed on the element.
- Gutters are symmetric within 2px and exceed 60px at every viewport — measured against the
  backdrop, which is what centres the modal. (Measuring against the window is wrong here: RTL
  puts the scrollbar on the left, so window-relative gutters differ by ~15px even when centring
  is perfect. That discrepancy was a measurement bug, not a layout defect.)
- The **confirm dialog still measures 448px (28rem)** with `--wide` present in the stylesheet.
- A **words detail modal still computes `width: 672px` (42rem)** — the documented hold-out is
  untouched. It is only measurable below the explorers' desktop breakpoint, where the detail
  surface renders as a modal rather than an inline panel.
- **Task 3.13:** the danger confirm carries `qd-confirm-dialog__confirm--danger` at first
  production render with a non-transparent background — the danger role reached the button
  rather than falling back to the primary style.

**Not measured:** the detail shell's 46rem. It lives behind
`features/words/entity-detail-overlay/`, which the abwab e2e sandbox has no route into. Its
immunity is doubly established without a measurement: `.detail-modal-shell` is declared in a
component stylesheet, so Angular's emulated encapsulation makes it `(0,2,0)` against the global
`(0,1,0)` modifier — and more decisively, its template never receives the `--wide` class at all.
The same specificity mechanism was confirmed empirically by the two modals that *were* measured.

## Divergence from the plan, and why

**§2.2 / §5's "single error owner … no duplicate announcement" was not implemented as written.**

The plan asks that a write dispatched from an open confirm dialog leave the global surfaces —
including the announcer — untouched. That is not achievable by changing the subscriber:
`AbwabWriteController.handleFailure` sets `announcementState` inside the controller, before any
subscriber runs, and the announcer strip is visible (bordered, always mounted), not sr-only.

Suppressing it would have required a non-announcing call path — and would have been an
accessibility regression. `qd-confirm-dialog` is `role="alertdialog"`, and content inserted into
an **already-open** alertdialog is not announced. A screen-reader user would press confirm, the
write would fail, and they would get silence, on exactly the operations most in need of a
result.

**Resolution:** the announcer is retained. Acceptance criterion 5 is read as *one visible error
**element*** — the in-dialog `qd-state variant="error"` — not one visible occurrence of the
string. The `role="status"` announcer is a status region, not an error surface. The page-level
error surface stays empty, which is the part that mattered. Pinned by a spec case that asserts
both halves: exactly one `qd-state[variant="error"]` in the DOM, **and** the announcer carrying
the message.

## Acceptance criteria

Met: 1, 2, 4, 5 (as reinterpreted above, and stated there), 5a, 5b, 6, 7, 8, 9, 11, 12, 13,
14 — 14 covering §17 (three entries plus the retrofit-list replacement), `styles/README.md`,
the `_tokens.scss` pointer, the abwab README (four paragraphs: the shell inventory, the
template-node justification, the stacking rule, the no-audit-columns gotcha), and TESTING_DEBT.

Also met after the phase-4 amendment: **10, 10a, 10b, 10c, 10d** — including the fifth abwab
README paragraph (the badge paragraph), now rewritten.

Partially met: **3** — it names three modals as browser-verified. Two were measured (confirm
dialog 448px, words detail modal 672px); the detail shell's 46rem was reasoned, not measured,
for the reason above.

Criterion 6 re-verified after the migration: `grep -rn 'role="alertdialog"' src/app
--include='*.html'` returns the primitive plus exactly the three dirty-discard strips (door,
sections, template-node modals). The `features/words` and `features/mushaf` trees are clean,
which is what licenses §17's now-unqualified "retrofit complete" sentence.

## Required self-checks (CLAUDE.md)

**Clean-code guard.** The one finding worth recording: four near-parallel `busy`/`error` signal
pairs now exist (overlays controller, sections modal, templates page ×2). Deliberately not
abstracted, per the pack's Rule of 3 and the Metz corollary — they share a *shape*, not a piece
of knowledge. Each belongs to a different dialog in a different component, and the three differ
in substance (the archive pair is shared by two dialogs and carries a context-menu origin flag;
the sections pair keys on a section id; the templates pairs are plain). A shared abstraction
would need per-caller branches on day one, which is the definition of the wrong one. The plan
reached the same conclusion independently ("No wrapper components — each site inlines").

Also checked: no orphaned imports or labels after the deletions (`trackingDataHeading` and its
six siblings have zero remaining references); comments added are WHY-only.

**Test-code self-check.** Two changes came out of it:

- The width spec originally ran 3 viewports × 2 themes. No theme token participates in modal
  geometry, so the three dark cases asserted nothing the light ones did. Collapsed to 3, and the
  theme variant moved to the danger-tone case, where it is the thing under test.
- Added a nested-scroll-lock case to the sections modal. `ScrollLockService` is reference-counted
  and already correct, but abwab is the first feature to actually nest two `qdModalScrollLock`
  holders, so "closing the inner dialog does not unlock the page under the outer modal" is now
  pinned rather than inherited.

One earlier spec was rewritten rather than repaired: the sections modal's focus-trap case
asserted `dialog.hasAttribute('cdkTrapFocus')`, which a property binding necessarily removes. It
now asserts the directive's `enabled` state through both halves of the gate — behavior instead of
attribute presence.

## Phase 4 — what the subgrid conversion cost, measured

The plan's original §2.3 was impossible (a sibling header cannot find the badge columns of a
depth-padded flex row with intrinsic-width trailing clusters). It was rewritten to a
frame → tree → row subgrid chain, validated by a standalone spike, and gated on a measured name
budget rather than a visual check. `phase-4-blocked.md` is **superseded and deleted** — its
findings are now §2.3's rationale; `eb8a1174` is the archive.

**The gate.** Fixed badge tracks take width from the row's only shrinkable item. Measured before
any edit, and again after:

| viewport | name width before (depth 0…5) | after | delta |
|---|---|---|---|
| 1024 | 344 / 320 / 296 / 272 / 248 / 224 px | 325 / 301 / 277 / 253 / 229 / 205 px | −19 px |
| 1184 | 504 / 480 / 456 / 432 / 408 / 384 px | 485 / 461 / 437 / 413 / 389 / 365 px | −19 px |
| 1440 | 760 / 736 / 712 / 688 / 664 / 640 px | 741 / 717 / 693 / 669 / 645 / 621 px | −19 px |

Inside the 20 px ceiling set before the work began. The **indent step is 24 px before and after**,
and chevron, order pill and name still indent together — stop condition 1 measured, not judged.

**Why the labels are abbreviated.** The full words («مباشرين» / «الجميع» / «العمق») measure
33 / 27 / 24 px against badges of 20–25 px, forcing a 2.5 rem column and a **−55 px** name loss —
rejected. «مباشر» / «الكل» / «عمق» measure 21 / 18 / 19 px, fit a 1.75 rem column, and cost 19 px.
Because the header is `aria-hidden` and every badge keeps its **unchanged** full `aria-label`,
screen-reader output is byte-identical to before the slice; only the visible hint got shorter.
Pinned by a spec case that asserts all three `aria-label`s against their label functions.

The one font step the budget allowed was **not spent**: the labels fit at the row's own
0.6875 rem (11 px), and shrinking chrome that did not need shrinking would have cost DESIGN.md's
calm register for nothing.

**Alignment is measured, not eyeballed.** Header label centres and badge centres at 1024 px:
575 / 527 / 479 px for the header, and identically 575 / 527 / 479 px on both a root row and a
depth-1 row.

**A stale contract corrected on the way through.** §17's truncation entry claimed the tree name
"holds ~184 px beside all three badges" at the narrowest viewport. That is reproducible only on a
branch row at depth ≈ 6–7 at 1024 px — a deep-row figure stated as a general one. It now states
the rule (325 px at 1024 px on a root branch row, −24 px per depth level) and requires
re-measurement whenever the row's furniture changes.

## Behavior changes a reviewer should look for

1. Every migrated confirm **stays open during its write** with both buttons disabled; Escape and
   backdrop are inert while busy; failures render inside the dialog and it stays open for retry.
2. **Deleting a section now asks first.** It never did — one click retired a section, with only
   the backend's 409 standing between a misclick and a live-door section.
3. The archive confirms moved from inline cards in the sticky aside into modal overlays with a
   focus trap, Escape, backdrop dismissal, and initial focus on **cancel**.
4. The sections modal **yields its focus trap** while its delete confirm is nested above it.
