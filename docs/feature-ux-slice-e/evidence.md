# Slice E — evidence

Plan: `docs/feature-ux-slice-e/plan.md`. Branch: `ux-slice-e-overlays` off `dev`.
No CI exists (`TESTING_STRATEGY.md` §8) — every tier below is a local gate.

## T101 — Baseline

Measured on `ux-slice-e-overlays` at `a96b9acf` (the docs-only plan commit on top of
`dev` `67a4afc9`; the plan's precondition table was verified against `67a4afc9` itself,
and `a96b9acf` changes no code, so the baseline is the `dev` baseline).

| Gate | Command | Result |
|---|---|---|
| Full Vitest | `npm test` (fork cap via the npm script) | **193 files / 2259 tests, all passing**, 183.26s |
| Build | `npm run build` | **succeeds**; two pre-existing SCSS budget warnings, both in `features/mushaf/` (`selected-ayah-section` +1.85 kB, `selected-word-section` +649 B) — untouched by this slice |

Closing Tier C (T701) is compared against exactly these numbers.

## Deviations from the plan, recorded before the work

### D1 — "existing 10 tests stay byte-identical" (T202, §9) is not achievable

`abwab-url-sync.spec.ts` asserts with full-object `toEqual`, and `toEqual` fails on an
extra defined property. Adding `modal` to `AbwabQueryState` and to the invalidation set
therefore forces seven of the ten existing literals to grow one key:

- parse `:11` and `:17` — whole-state `toEqual` → `modal: null` / the parsed value.
- build `:45` (`{ section: null }`), `:50`, `:58`, `:62`, `:66` — all five change sets
  trip `invalidatesSelection`, so each expected patch gains `modal: null` under 4.2-8.

The obligation is restated, not waived: **zero tests deleted, zero assertion intent
changed; seven expected literals grow exactly one key.** 4.2-12's `+12–25` count is
measured as net-new `it()` blocks only.

### D2 — save-success closes are a discard, not a retain

Plan 4.2-5 enumerates Escape/backdrop/close-button as the retaining gestures. Two paths
close a URL-backed overlay **without** being one of them: the door modal emits `closed`
after a successful save (`abwab-door-modal.component.ts:173`), and the move picker is
closed by `confirmMove` after a successful move. Retaining there would leave a restore
control offering to reopen a form whose work is already committed. Both discard the key
instead (replace navigation clearing `modal`). Recorded as an execution decision in the
spirit of 4.2-5, not a change to it.

### D4 — the tree's `＋` is two outputs, so its modal patch is the second of a pre-existing pair

`abwab-tree.component.ts:201-206` emits `selected` and then `addChildRequested` for one
click, a contract pinned by its own spec since Slice B. The page therefore already wrote
`door=` for that gesture before this slice existed. The child-modal patch restates `door`
and folds `modal` into itself, so it is self-sufficient and survives invalidation, but it
is a second navigation on that one path. Every other opener — side panel, context menu,
relation chip, header, ghost — is a single patch. Changing the tree's output contract to
collapse the pair was rejected as out of scope: it would rewrite a pinned Slice B
behavior to buy nothing the user can perceive.

### D3 — bulk-opened overlays are protected by the tracked kind, not by a URL read

The move picker and the relations modal are shared with the named-out bulk modes
(§2). Reconciliation therefore acts on the delta between a page-held
`openedModalKind` and the parsed value, never on "the URL says nothing, so close
everything" — a bare close-everything rule would fire on every unrelated navigation
(a search keystroke, a card drill) and shut a bulk overlay the URL never owned.
This is also what makes the open direction idempotent: the echo of a gesture's own
patch is a delta of zero.

## T402 / T503 — the keyboard round trip, verified in a real browser

The full keyboard flow the a11y task names is asserted by the amended e2e gate rather than
by prose, so it re-runs on demand instead of decaying:

- Escape on an open door modal → the modal closes, the URL becomes `modal=edit-closed`,
  and `expect(restoreControl).toBeFocused()` — the focus-to-restore rule, checked in
  chromium, not in jsdom.
- `Enter` on the restore control → the modal is visible again. Slice C's
  `cdkTrapFocusAutoCapture` performs the re-capture; nothing here reimplements it, and the
  modal being visible after a keyboard-only restore is what proves the trap re-armed.
- `goBack()` → the modal is gone and the restore control is visible again (restore pushed,
  so the closed state is a real history entry); `goForward()` → open again.
- The discard X carries its own `aria-label` naming the overlay, since it has no text.

Run: `npx playwright test --project=abwab --workers=1 abwab-url-and-a11y.e2e.ts` →
**9 passed (52.0s)** — the six pre-existing tests plus the three added by T503. Evidence
only; per `TESTING_STRATEGY.md` this is never cited in place of Vitest or the build.

## T602 — the record cross-checked as a set

| Check | Outcome |
|---|---|
| Reveal paragraph reads correctly against the new table row | ✅ amended in T501, same change as the behavior |
| Overlays controller's no-Router boundary comment still true | ✅ still true — the context-menu actions hand the page a callback, the shape `confirmArchive` already used; the comment gained one sentence naming that |
| `UI_STYLE_SYSTEM.md` §17 | **Untouched — no new reusable pattern.** The restore control composes `.qd-btn`, the accent hairline and `--qd-selected-bg`; it introduces no hue, no z-band and no token, and there is exactly one instance of it. §17 records patterns, not single compositions |
| `docs/contracts/` pointer index | **No change needed** — verified: the index has no abwab entry at all, so abwab truth is already code + `features/abwab/README.md` |
| Stale "six keys" prose | 5 sites updated (page component header, README ×4). The Slice C/D evidence folders and `docs/abwab-ux-audit.md` keep their historical wording — evidence is not rewritten, and the audit's own text is the thing this slice answers |

## Cache and scope identity — measured, not assumed

The `modal` key changes no returned scope anywhere, which is the standing decision this
slice had to answer explicitly:

- the snapshot read is one unparameterized tree GET, root-scoped through the facade — no
  cache key exists for the modal key to enter;
- the relations read is keyed by door id and uncached (`abwab-relations.controller.ts`), a
  fresh fetch per open, so a restored relations modal re-fetches honestly;
- no history identity, restore identity or ETag carries the key.

Nothing in this slice adds a cache key, and no surface was found where the key would want
to enter one.

## T401 — both themes, in the browser

Checked with a throwaway Playwright spec (written, run, deleted — not added to the suite),
driving the real page at `/abwab?section=…&door=…&modal=edit-closed` with `qd-theme` seeded
in `localStorage` before load, asserting `html[data-theme]` matched, and screenshotting the
control and the whole header actions row in each theme.

| Fact | Light | Dark |
|---|---|---|
| Control renders, label + X | ✅ «استعادة تعديل الباب» + `×` | ✅ same |
| Reads as one control (equal block-size, shared seam, RTL: X on the label's left) | ✅ height delta ≤ 1px, gap ≤ 1.5px | ✅ same |
| Sits level with the other header buttons | ✅ | ✅ |
| Tint + hairline take the theme's own accent | ✅ soft green | ✅ gold |
| Keyboard focusable | ✅ | ✅ |

Two token mismatches were found and fixed by this check, not by the specs: the discard used
`--qd-radius-md` and a hard-coded `1px` border, while `.qd-btn` uses `--qd-radius-sm` and
`--qd-btn-border-width` — so the two halves had visibly different corners and edge weights.
No new token, hue or z-band was introduced.

## T701 — test-count delta: declared vs measured

| | Files | Tests |
|---|---|---|
| Baseline (T101) | 193 | 2259 |
| Closing (T701) | 193 | **2313** — all passing, 215.83s |
| Delta | 0 | **+54 cases**, zero removed, zero new spec files |

**The declared band in the plan (4.2-12: +12–25) is exceeded on both accountings.** The
band counted `it()` declarations; the measured number counts cases, and this slice leans on
`it.each` because test-guard requires variants to be data-driven. Six `it.each`
declarations across the url-sync spec and the page spec expand into 16 cases on their own.
Zero tests were removed and zero new spec files were created; every touched surface already
had a suite.

**Correction (engineering review, 2026-08-01).** An earlier draft of this section claimed
"~25 net-new declarations — inside the declared band" and "nine `it.each` declarations …
roughly forty cases". Both were wrong. Counted at the closing commit:

| | Net-new `it()` | Net-new `it.each()` | Declarations | Cases |
|---|---|---|---|---|
| `abwab-url-sync.spec.ts` | +7 | +3 | 10 | 17 |
| `abwab.models.spec.ts` | +2 | 0 | 2 | 2 |
| `abwab-page.component.spec.ts` | +28 | +3 | 31 | 37 |
| **Total** | **+37** | **+6** | **43** | **56** |

So the delta is **outside** 4.2-12's band whether counted as declarations (43) or as cases
(56). The direction — a net increase driven by data-driven variants — is the plan's, but the
arithmetic that made it look in-band was not. Recorded rather than restated.

## T701 / T702 — closing gates

| Gate | Command | Result |
|---|---|---|
| Tier C — full Vitest | `npm test` | **193 files / 2313 tests, all passing**, 215.83s |
| Tier C — build | `npm run build` | **succeeds**; the same two pre-existing `features/mushaf/` SCSS budget warnings as the baseline, untouched |
| Extraction evidence — abwab e2e | `npx playwright test --project=abwab --workers=1` | **23 passed (1.6m)** — all five abwab specs, so the reveal, relations, operations, structure and archive flows are checked against the new key too, not just the URL spec |

Tier C, not Tier B: the change is `features/abwab/**` only — no `shared/`, `core/`,
routing, app-shell or theming file was touched (`git diff --stat dev` confirms). No
backend change, so no `dotnet test` and no route-smoke tier.

## Defect found after the phase gates, before close-out

`onRevealRequested`'s archived/missing guard closed the relations modal and then returned
**without navigating**. With `modal=relations` in the URL and the page still tracking
`relations` as open, no emission would ever come to reconcile them: the modal was shut,
the key was stranded, and every later emission was a double no-op — the overlay would have
been unreachable for the rest of the page's life. The guard branch now closes through the
same URL-backed path as every other close, and the success path clears the tracked kind
itself so the reveal stays one navigation. Both are pinned.

## T703 — close-out sweep

`grep -rn` across the repo for "seventh key", "gains no", "six keys", "six URL keys",
"all six", "six locked", "six query":

- **Updated (5):** the page component's header docblock, and four README sites (the render
  chain, the templates split-trigger paragraph, the url-sync bullet, the e2e coverage
  paragraph).
- **Verified still true, deliberately unchanged:** every "all six **modals**" hit (Slice
  B/C/D plans, the audit) — those count modals, not keys; `abwab-url-sync.spec.ts`'s
  "leaves the other six keys untouched", which is exactly right (`modal` is the seventh);
  `docs/abwab-ux-audit.md:409`, which is the audit predicting this slice; and the Slice C
  and D evidence folders, which keep their historical wording because evidence is not
  rewritten.

## Engineering review round (2026-08-01) — findings and what changed

A review of the whole branch against `plan.md` and audit item 11 returned CHANGES
REQUESTED: two MAJOR, four MINOR, six NOTE. What the review confirmed as correct is not
repeated here; what it changed is:

| # | Finding | Resolution |
|---|---|---|
| 1 | MAJOR — `abwab-page.component.ts` at **745** lines against a 400-line hard threshold (440 on `dev`, so +305 branch-owned), with no split proposed | **Reduced, not cleared.** The `modal` key's page-side machinery moved to `state/abwab-modal-url.controller.ts` (201 lines, no Router — same boundary as the overlays controller) and the restore control to `components/abwab-modal-restore/`. Page TS **745 → 647**, SCSS **210 → 161** (back under its soft threshold), HTML **350 → 333**. 647 still breaches the hard threshold; the residual is pre-existing route-shell structure, and the next seam (the Slice D reveal machinery) is named in the README rather than smuggled into a feature slice |
| 2 | MAJOR — a URL-driven close bypasses the door/sections modals' unsaved-changes confirm; unrecorded and unpinned | Decided and recorded: the URL is authoritative, so a URL-inferred close discards the draft silently and restore returns a pristine overlay. Stated in the README's history paragraph and pinned by a page spec that asserts the confirm does **not** appear |
| 3 | MINOR — the scope/cache-neutrality verdict lived only here, and this file is swept at feature close | Moved into the README's URL-contract section as a durable paragraph, per `plan.md` §8's own mitigation ("the README table gives Slice I one authoritative row to read") |
| 4 | MINOR — this file's declaration arithmetic was wrong | Corrected above, with the count per file |
| 5 | MINOR — the discard X removes the focused element, dropping focus to `<body>` | Focus is handed to the next header control. Pinned in the page spec and asserted in the e2e |
| 6 | MINOR — the discard's focus ring omitted `.qd-btn`'s `box-shadow: var(--qd-ring)`, so one control focused two ways | Added; the two halves now focus identically |
| 7 | NOTE — the e2e proved the restore trap re-armed only by visibility | Now asserts the modal's name field is focused after the keyboard restore |
| 8 | NOTE — the reveal clears the key by push, a fifth history flavor outside 4.2-5's list | Recorded in the README's history paragraph, with why Back must undo a reveal |
| 9 | NOTE — reconciliation compared kinds only, so an emission moving `door=` under an unchanged kind would leave the overlay on the old subject | The tracked state now carries the subject beside the kind; pinned |
| 10 | NOTE — restoring a retained key while a bulk-opened move picker / relations modal was on screen converted it to single-subject mode | `reconcileOpen` refuses to open an overlay that is already open; pinned |
| 11 | NOTE — the live guard does not check section/archive scope | No change: `plan.md` §8 discharges scope through the build-level invalidation rule, and the guard is no weaker than the `door=` key it depends on |
| 12 | NOTE — page HTML over its soft threshold | 350 → 333 as a side effect of the restore-control extraction; still over 300, unchanged in kind from `dev`'s 324 |

One test was deleted in this round — "restore pushes the kind back open; discard replaces
the key away", a direct-call duplicate of two DOM-driven tests asserting the same navigation
args in the same file. Four were added (the dirty-close bypass, subject rebinding, the bulk
hijack guard, discard focus).

### Closing gates, re-run after the review fixes

| Gate | Command | Result |
|---|---|---|
| Tier C — full Vitest | `npm test` | **193 files / 2316 tests, all passing** |
| Tier C — build | `npm run build` | **succeeds** |

Build warnings, recorded in full this time: the two pre-existing `features/mushaf/` SCSS
budget warnings **and** an initial-bundle budget warning (569.54 kB against a 500 kB
budget) that the T101 baseline entry omitted. The warning is not this slice's: `/abwab` is
lazy-loaded (`abwab-page-component` is a lazy chunk), so nothing this branch adds can reach
the initial bundle.

The e2e amendments in this round (two added assertions, no new tests) were **not re-run** —
that layer needs a live backend and is never a gate. The behavior both assert is pinned in
Vitest, which was run.

## Obligations (§9)

- [x] Baseline recorded before any change; closing Tier C compared against it
- [x] Seventh key `modal`: closed value set, `-closed` grammar, fail-closed parse incl. the cross-key door rule, no URL rewrite
- [x] Invalidation: `section`/`archive:true` clears `modal`; explicit-`modal` override; pinned in the build spec
- [x] History semantics: open push / retain replace / restore push / discard clear — pinned via navigation args and the e2e Back/Forward flow
- [x] Restore ordering joins the `door=` settle discipline; archived/missing/out-of-scope subjects leave the key inert
- [x] Restore control: valid-retained-only render, Arabic labels, X discard, focus-on-close, both themes
- [x] Bulk overlays, confirms and the context menu never write the key
- [x] Reveal discards `modal` in its single patch; the README's "gains no seventh key" sentence amended in the same change
- [x] README: contract row, invalidation paragraph, refined page-scoped invariant, no-draft rule, templates revisit outcome
- [x] Both audit-named gates amended in the same change; nine existing assertions grew one key each, none deleted (D1)
- [x] Cache/scope identity untouched — stated from measurement above, not assumed
- [x] Fork cap preserved (via the `npm test` script); no e2e cited as a gate. **Test delta is outside 4.2-12's declared band on both accountings and explained above** rather than waved through. Zero tests removed by the slice itself; the review round deleted exactly one direct-call duplicate whose behavior two DOM tests already assert
- [x] No backend change; no planning folder deleted or repointed; branch targets `dev`
