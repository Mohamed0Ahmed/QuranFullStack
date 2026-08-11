# Golden Visual Verification

How to verify a UI-visible change against the Golden system. The canonical visual authority is this
folder — `01 GOLDEN_UI_SYSTEM.md`, `02 GOLDEN_UI_COMPONENT_CATALOG.md`,
`03 UI_DRIFT_TO_CANONICAL_MAP.md`, `04 IMPLEMENTATION_HANDOFF_SUMMARY.md`, and the four HTML boards —
with `UI_DESIGN_HANDOFF.md` as the input authority behind them.

**Golden verification is contract-based, not pixel-perfect screenshot matching.** Board fixtures and
preview typography are references only; production keeps the approved project fonts and every
protected Quran font and rendering boundary.

Which changes require this protocol, and which verification the change selects overall, follow
`TESTING_CONSTITUTION.md`. This document owns the *method*, not the selection.

## Acceptance hierarchy

Use in order:

1. Golden Markdown contract.
2. Matching Golden visual board and state.
3. Actual browser DOM and computed geometry.
4. Responsive transformation behavior.
5. Interaction and state behavior.
6. Screenshot plus measured execution evidence.

A visual verdict must not rest solely on source inspection, a screenshot without measurements, or a
subjective "looks good" judgment.

## A. Computed geometry

Use a real browser and measurable DOM/computed-style evidence when the contract is geometric —
`getBoundingClientRect()`, `getComputedStyle()`, DOM roles and attributes, viewport/document
dimensions, and scroll-owner inspection. Verify, where relevant:

- `document.scrollWidth <= window.innerWidth`, and exactly one route gutter owner;
- Compact/Medium/Wide/Wide-plus gutters of `16/24/32/40px`;
- Templates/Abwab/Access rails of `16/18/20rem` only in their allowed modes;
- Compact interactive hit targets of at least `44px`;
- pagination jump input width of exactly `6rem`;
- Compact modal block size no greater than `94dvh`;
- expected grid-column count and responsive visibility/hiding;
- the declared local overflow/scroller owner, sticky/fixed behavior, focus-ring geometry, and
  modal/floating-layer clipping and placement;
- the actual loaded UI font family and protected Quran font family, never substituting board preview
  faces.

Measure directly when a browser value is available; do not infer computed geometry from screenshots.

## B. Responsive boundaries

Exercise the structural cutovers at `767` (last Compact), `768` (first Medium), `1024` (still
Medium), `1079` (last Medium), `1080` (first Wide), and `1440` (Wide-plus measure enhancement, not a
new structure). At 768–1079 the named Medium composition replaces every squeezed Wide layout; legacy
desktop behavior must not return at 1024. At 1080, Wide navigation, rails, and splits appear only
where their family contract requires them. Board 4 (Responsive Critical States) is the primary visual
reference for these transitions.

## C. Structural comparison

Compare the actual state with the matching board for hierarchy, content axis, spacing rhythm, surface
nesting, page density, card/table proportions, rail geometry, responsive composition,
selected/current treatment, action hierarchy, state placement, modal/sheet anatomy, green semantics,
and the absence of forbidden decorative effects. Fixture text and isolated pixel differences are not
rejection criteria; meaningful structural drift is.

## D. Interaction states

Exercise the states the change implicates: hover, focus-visible, selected/current, disabled, D36
zero-count disabled-with-reason, skeleton, refreshing, empty, error, notice/success, modal open,
picker/menu open, long-text disclosure, dirty review dock, deep hierarchy, pagination, tab switching,
details, and responsive sheet transformation. Do not create product or domain state merely to obtain
visual evidence when no supported fixture exists.

## E. Evidence rules

Screenshots, computed-style dumps, dimensions, and interaction logs are execution evidence only.
Store them under a temporary session location such as `/tmp/golden-ui-evidence/<change>/`; do not add
or commit them to the repository unless separately authorized.

Prefer risk-based representative evidence over every route × every state × every viewport. Cover the
surfaces the change actually reaches, at the breakpoints where its contract changes.

## F. Authenticated and protected-state verification

- Authenticated browser verification may run **only** when the environment already has a valid,
  supported, non-interactive authenticated fixture or session. Running the application and browser
  tooling does not make the executing identity an Owner.
- Never promote the executing identity to Owner, edit database roles, seed or alter product data
  outside an already-supported scoped fixture, bypass a guard, disable authorization, forge a token,
  weaken permissions, change production or domain authorization logic, invent fake authentication, or
  require the human owner to return for an interactive login merely to obtain browser evidence.
- When no supported authenticated fixture or session exists, **record the evidence limitation** and
  fall back to deterministic component, Router, state, request, and permission evidence.
  Authenticated browser evidence is non-blocking unless current policy supplies and requires a
  fixture for that exact flow.
- Access has seven exclusive lifecycle/membership states — pending non-Owner, active non-Owner,
  disabled non-Owner, active Owner, pending Owner, disabled Owner, and unknown. A browser may
  exercise only the subset reachable through an already-authorized fixture; **missing live browser
  data authorizes no Backend, auth, database, or product-data change.**
- The same rule governs Abwab write flows: public and read-only browser verification proceeds
  normally, while write-browser evidence is conditional on an existing supported authenticated
  fixture or session.
