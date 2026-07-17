# Feature 029 — Verification record (P7 final guard)

Branch: `restyle/flat-green-light` (work started at `d7e6421`). Date: 2026-07-17.

## Commits

| Phase | Commit | Scope |
|---|---|---|
| P0 | `7c0b483` | Plan committed, defects reproduced with measured baselines |
| P1 | `156273c` | U2 count-range full-width row, U3 Word Types tabs placement |
| P2 | `7030519` | Change A — shared `qdAyahCard` presentation frame + 3 migrations |
| P3 | `9c70edd` | B0–B3 — `v1~…` codec, history service (provenance push/replace/seed), link directive, dialog shell, scroll lock, overlay host |
| P4 | `366d855` | B4–B6 — 5 route-independent detail controllers, 5 overlay adapters, cross-entity append links, Mushaf identity links incl. locked §5.7/§12 word-type mapping (Option A) |
| P5 | `18be718` | B7–B8 — ayah continuity (`navigateBaseWithOverlay`, `qdAyahOverlayLink`), Mushaf session fix, end-to-end + nested-layer specs |
| P6 | `b2e86cd` | U1 — selected-word loading reservation with measured baselines |

## Automated gates (final run, from `Frontend/quran-dashboard-ui`)

- `npm test -- --watch=false` → **150 files / 1407 tests, all pass** (grew from 1189/143 at P0).
- `npm run build -- --output-path=/tmp/qdb-feature-029-verification` → success; 5 per-adapter lazy chunks present; initial bundle ~431 kB.
- `git diff --check` → clean.
- `tsc --noEmit` → clean (P5).

## Measured defect baselines → outcomes

| Defect | Before | After |
|---|---:|---:|
| U2 sort-flip shift (roots/lemmas/stems/unique) | 181.1 / 155.5 / 129.9 / 78.8 px | 0.0 px at desktop/tablet/phone |
| U3 Word Types tabs | spanned 1376 px above both columns | tabs inside layout at table-column width (773.3 px); panel top aligns with table |
| U1 selected-word loading shift (divider top) | −19.3 px desktop; up to −194 px phone | **0.05 px** at 1440, **0.0 px** at 390 (gate ≤1 px) |

U1 measured natural loaded geometry (basis for the responsive baseline):
333 px structure-constant on wide bands (1/3/5-segment all equal); below the
768 px morphology-grid breakpoint: 495.6 px (1-segment floor) to 682.8 px
(5-segment max at 390 px viewport). Loading shell before fix: 313.7 px wide /
488.6 px phone.

## Browser acceptance (Brave, dev servers, 450 ms dev API latency)

- **Overlay open from Mushaf**: morphology/identity anchors carry real `v1~…` hrefs; unmodified click opens the dialog over the unchanged Mushaf base; real entity titles publish to the shell (e.g. «ا ل ه», «خ ت م»).
- **Append + stack**: lemma link inside an open root detail appends a second frame (`qdDetail` repeated bottom→top), dialog Back pops it; browser Back/Forward converge per history-state provenance.
- **Ayah continuity (B7)**: with the overlay open, an ayah card click replaces the base underneath (2:7 → 6:46, history length unchanged, provenance kind `replace`); from a closed side panel the panel's frame is promoted to a one-frame stack over the Mushaf as a push. Two-frame stacks ride along intact.
- **Session restore**: an overlay-only Mushaf URL counts as bare — reader params restored via merge; the retained stack and dialog survive.
- **Canonicalization**: malformed `qdDetail=not-a-frame…` is stripped from the URL; no overlay renders.
- **Keyboard**: focus auto-captures in the dialog; Tab stays trapped; Escape closes retaining the stack (`qdDetailOpen` removed); focus lands on the restore control («استعادة …»); Enter reopens as a push. App shell is `inert` only while open.
- **U1 geometry**: divider coordinate stable during loading at 1440 and 390 (see table); skeleton renders the last successful segment count (5 observed), reservation var applied only while loading and cleared after success.
- **Light/dark**: overlay verified in both; dark (interim navy+gold) renders dialog, tabs, ayah cards, and matched-word underlines correctly; light is the flat parchment+green doctrine.
- **RTL**: `dir="rtl"` throughout; dialog chrome and tabs lay out RTL.

## Quran rendering (identity check)

No Quran-rendering file changed in any commit (`mushaf-word`, `mushaf-line`,
ligatures, `highlighted-ayah`, `segment-rendered-word` internals untouched —
`git log --stat d7e6421..` contains none of them). Change A wraps existing
content in a presentation-only frame; U1 skeletons remain loading chrome that
never approximates Quran text. Live check: overlay ayah cards render Uthmani
text in Amiri (`--qd-font-quran`) with marks and matched-word set intact;
`toStudyAyahDisplayText`, verse-key display, and `ayahNavigate` outputs are
feature-owned and unchanged.

## Docs updated in-change

- `features/words/README.md` — overlay adapters, cross-entity links, ayah continuity.
- `features/mushaf/README.md` — identity links, session/bare-entry rule, U1 reservation.
- `core/README.md` — detail-overlay layer incl. `qdAyahOverlayLink` semantics.
- `shared/ui` / `UI_STYLE_SYSTEM.md` §17 — `qdAyahCard` contract (P2).

## Deviations

- P5 B8 end-to-end spec seeds the first (root) frame via `startStack` — the exact API a Mushaf link invokes — because no explorer anchor starts a root frame; lemma/stem appends use real anchors.
- Word Types grouped root/stem/lemma selections pass `parentFrame: null` (no frame grammar exists for grouped selections; plan defines none).
- The word-types page facade was deliberately not refactored (word-kind-only controller extracted); grouped logic stays page-owned.
