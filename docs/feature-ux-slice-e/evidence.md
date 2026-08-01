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
