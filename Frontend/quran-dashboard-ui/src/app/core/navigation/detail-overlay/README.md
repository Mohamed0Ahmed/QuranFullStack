# Detail Overlay (app-wide overlay navigation layer)

The floating detail-overlay navigation layer. The **URL is the single source of truth**: overlay
state is whatever the current URL encodes, and every mutation is a router navigation re-parsed on
each `NavigationEnd` (so browser Back/Forward need no separate bookkeeping). This folder owns
navigation semantics only — entity rendering lives in `../../../features/words/entity-detail-overlay/`.

## What lives here

- `detail-overlay.models.ts` — the versioned `v1~…` `DetailFrame` union (`unique`, `root`, `lemma`,
  `stem`, `wordType`), `DetailOverlayUrlState` (`visibility` + ordered `stack`), the query-key
  constants, the eight-frame cap, and complete-identity `detailFramesEqual` / `detailStacksEqual`.
- `detail-overlay-url-codec.ts` — strict parse / serialize / canonicalize for the URL frame grammar.
- `detail-overlay-provenance.ts` — the `history.state` ownership record and stack hashing.
- `detail-overlay-history.service.ts` — the URL-authoritative state machine and navigate/back helpers.
- `detail-overlay-link.directive.ts` (`a[qdDetailLink]`) and `detail-overlay-ayah-link.directive.ts`
  (`a[qdAyahOverlayLink]`) — real copyable anchors that intercept only an unmodified primary click.

## URL frame contract

- Overlay keys: `qdDetail` (repeated, ordered **bottom → top** of the stack) and `qdDetailOpen=1`
  (present only while the dialog is visible). A closed-but-retained stack keeps `qdDetail` values
  without `qdDetailOpen`.
- Frames are `~`-separated, `v1`-versioned fields; `-` is the null sentinel. The codec is
  **fail-closed on input** (any malformed field rejects the whole frame; ids must be safe positive
  integers) and **explicit on output** (all defaults are serialized, so a future default change can
  never re-interpret an old shared URL).
- Canonicalization: an invalid first frame ⇒ no overlay; a malformed later frame truncates the stack
  before it; frames past the eight-frame cap are dropped; `qdDetailOpen=1` with no valid frame
  collapses to closed. A non-canonical URL is rewritten exactly once with replace semantics.
- The frame union is deliberately **decoupled from Words feature models**: the URL grammar is a
  shareable contract (old links must keep meaning), while feature models are free to evolve.

## Provenance ownership model

Each owned history entry stamps a `qdDetailNav` record in `history.state` (`baseSignature`,
`parentStackHash`, `stackHash`, `kind` of `push`/`restore`/`replace`/`seed`). Because Angular
preserves custom `history.state` across initial navigation, reload, and popstate, **the record is
the entry identity**: provenance is trusted only when both its `baseSignature` matches the live base
and its `stackHash` matches the current stack. On sync, an open entry lacking matching provenance is
reconciled fail-closed by `seedChain`, which materializes each stack prefix as its own marked history
entry (a same-URL revisit re-seeds; a reload keeps its preserved marker and is not duplicated).

## History semantics (dialog Back vs browser Back)

- `startStack` / `appendFrame` **push** (append is a no-op on an identical top frame and refused past
  the cap, incrementing `capRejectionCount`); `replaceTopFrame` **replaces** in place for
  tab/sub-view/page changes (no new entry).
- `close` **replaces** into a closed-but-retained state; `restore` **pushes** the retained stack back
  open so browser Back returns to the closed state.
- `back` (dialog Back) uses real browser `Location.back()` only when owned provenance (`push`/`seed`)
  proves the previous entry is the parent card on the same base; otherwise it does a deterministic
  replace with the top frame removed. It never exits the app.
- `navigateBaseWithOverlay` (ayah continuity) navigates the base route *underneath* the overlay:
  open ⇒ replace carrying the whole stack and `qdDetailOpen=1` (re-stamping only the base signature,
  materializing a missing parent prefix first); closed + a `promoteFrame` ⇒ push promoting the source
  detail to a one-frame stack; closed with none ⇒ plain push with overlay keys stripped.

## Related

- Entity rendering / adapters: `../../../features/words/entity-detail-overlay/`
  (and `../../../features/words/README.md`).
- Core index: `../../README.md`.
