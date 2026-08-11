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
