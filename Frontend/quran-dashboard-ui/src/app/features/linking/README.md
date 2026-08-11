# Linking feature

This feature owns the frontend-only Quran Linking workspace. It is available only to a resolved,
authenticated, active System Owner through `LinkingAccessService`; every store command repeats that
gate rather than relying on hidden UI.

`LinkingWorkspaceStore` owns prepared sources, source-wide adaptive ayah selection, source-word
highlight preference, lightweight result counts, and the active workspace/Direct Link intent.
Source descriptors are serializable, feature-owned contracts. They contain every field that defines
the existing read-result scope, but no API DTO graphs, callbacks, Observables, facades, modal state,
loaded ayahs, Quran text, Door selection, workflow step, load state, or mock result.

The workspace session uses the versioned `qd-linking-workspace-v1` `sessionStorage` key. It stores
only the Owner `sub`, ordered prepared items, descriptor keys, selection overrides, last resolved
count, and highlight preference. Invalid, cross-actor, or unavailable storage fails closed; logout
or actor changes clear both storage and in-memory state. Restored descriptors are not resolved until
a later workspace action or Direct Link flow requests them.

There is no write API, request, draft, approval, history, cache mutation, grouped linking path, or
Mushaf grouped descriptor. The future grouped-Mushaf seam remains deliberately unimplemented.

`LinkingWorkspaceHostComponent` is mounted once beside the app shell and the Words detail overlay.
It defers the wide `qd-modal-shell` workspace surface until Linking opens, while app-root owns the
cross-layer inert boundary. The Navbar reads only Owner access and item count. Workspace cards remain
presentational and dispatch remove, edit-selection focus, and one-source Direct Link intent to
`LinkingWorkspaceStore`; the same shell now composes its workspace or Direct Link content without
nested dialogs.

`LinkingWorkflowFacade` owns one transient Direct Link state machine. It starts from either a saved
workspace source or an ephemeral source action, uses the current live Abwab snapshot for the single
Door selection, and returns to the appropriate surface on dismissal. Starting from the global
entity overlay closes that overlay through its existing retained-history behavior before the Linking
shell opens. Door search, selected Door, resolver progress, loaded ayahs, errors, and workflow state
are never persisted.

`QuranSourceLinkingActionsComponent` is the shared Owner-only action seam. Unique Word keeps the
`simple` and `tashkeel` descriptor identities separate. Root, Lemma, and Stem contribute the same
actions through the neutral Words detail-panel action slot after a matching summary resolves; Lemma
and Stem retain their current `typeCode` scope in their descriptors. Every action adds idempotently
to the workspace and starts Direct Link without changing the detail overlay URL stack.

The resolver registry supports Unique Word, selected Mushaf word, Root, Lemma, and Stem descriptors.
It sequentially loads every matching API page, rejects incomplete or inconsistent envelopes, and
de-duplicates only identical repeated `verseKey` rows. Root, Lemma, and Stem preserve exact Uthmani
tokens and backend `isMatched` flags without claiming canonical Quran word IDs; those IDs remain
`null`. A successful workspace-backed load reconciles stored selection and updates the lightweight
result count; loaded ayahs remain workflow memory only.

Direct Link now keeps complete-source selection in its active workflow state, while a workspace-backed
source mirrors the compact selection and highlight preference to its prepared item. Local ayah search
uses the shared Arabic normalization helper only for comparison, so the renderer always displays the
exact returned Uthmani text. The mock command port is an injectable, frontend-only boundary: it
validates the active Owner, live Door, complete source, and nonempty selected ayahs before returning
the one presentation-only success result. It sends no HTTP request and mutates no cache.

The Mushaf source resolves exactly one selected occurrence through its descriptor's existing page
read. It reconstructs only the matching `verseKey`, preserves the selected canonical Quran word ID,
leaves sibling canonical IDs null, and marks the chosen `wordLocation` as the only source match.
