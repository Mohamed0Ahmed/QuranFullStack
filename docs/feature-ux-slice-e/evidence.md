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

### D3 — bulk-opened overlays are protected by the tracked kind, not by a URL read

The move picker and the relations modal are shared with the named-out bulk modes
(§2). Reconciliation therefore acts on the delta between a page-held
`openedModalKind` and the parsed value, never on "the URL says nothing, so close
everything" — a bare close-everything rule would fire on every unrelated navigation
(a search keystroke, a card drill) and shut a bulk overlay the URL never owned.
This is also what makes the open direction idempotent: the echo of a gesture's own
patch is a delta of zero.
