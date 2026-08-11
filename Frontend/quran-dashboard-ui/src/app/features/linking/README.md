# Linking feature

This feature owns the frontend-only Quran Linking prototype. It is available only to a resolved,
authenticated, active System Owner through `LinkingAccessService`; every public workspace mutation
rechecks that gate rather than relying on hidden UI.

The selected-Mushaf-word Linking source has been retired. Normal Mushaf word selection, analysis,
identity navigation, URL/session state, renderer boundaries, glyphs, word order, markers, fonts,
spacing, and line metrics remain outside Linking. The current automatic sources are Unique Word,
Root, Lemma, Stem, and Word Type. Manual Mushaf ayah descriptors are V2 contracts only at this
stage; their reader entry and source loading arrive in later phases.

`LinkingWorkspaceStore` owns ordered prepared rows, per-row configuration, transient checked
operation membership, editor targets, a one-item undo snapshot, and presentation-only surface state.
Automatic configuration combines ayah inclusion with `automaticWordMatchesEnabled`; manual
configuration combines ayah inclusion, verse-scoped `wordLocation` coordinates, and the stored
grouped/independent preference. A row's V2 configuration is the persisted truth. Temporary
compatibility selectors keep the existing one-source workspace and Direct Link surfaces compiling
until their later replacement; they are not serialized contracts.

Source descriptors are serializable feature contracts. Automatic source keys retain their previous
identities. A manual Mushaf key is the numerically ordered, deduplicated `verseKey` set only;
display hints, page hints, inclusion overrides, words, and grouping never change it. `verseKey` is
the merge identity, `quranWordId` is canonical only when a read supplied it, `wordLocation` is a
temporary manual coordinate, and a render-position occurrence is presentation-only. Merged review
and ordered source intents are sibling V2 contracts: the display cannot reconstruct or widen an
intent.

`LocalStorageLinkingWorkspaceRepository` implements the replaceable, async-capable persistence port
with `qd-linking-workspace:v2:<encodeURIComponent(actorSub)>`. The V2 envelope repeats its version
and exact actor subject, persists only prepared descriptors/configuration/last-resolved count, and
is strictly decoded. Malformed, cross-actor, or unknown-version payloads invalidate only the active
actor's V2 bucket. Valid envelopes retain independent valid rows, normalize source keys, and keep
the first duplicate. V1 `qd-linking-workspace-v1` data is neither read nor migrated. Hydrated rows
are always unchecked and stale; loaded ayahs, Quran text, DTOs, merged selections, source intents,
Door state, workflow/focus/modal state, errors, and mock results are never serialized.

Storage begins only after resolved Owner identity. Actor changes clear in-memory state and prevent
late previous-actor hydrate/save completions from publishing; they do not delete another actor's
bucket. Same-actor tabs are last-writer-wins, and a local storage failure leaves in-memory Linking
usable with a non-blocking warning.

`LinkingWorkspaceHostComponent` is the one primary Linking shell. Its lightweight wide modal mounts
with the app inert boundary, while inner workspace/editor/flow surfaces defer independently. Wide
and Medium Linking uses the explicit `80vw` by `88dvh` override; Compact continues to use the shared
`94dvh` sheet. The shell body is non-scrolling, each mounted surface owns one body scroller, and
`LinkingFocusCoordinator` owns entry and return focus with the shell's default return disabled.
Focus origins distinguish Navbar, workspace rows, inline source actions, and retained entity
overlays. Surface, focus, and shell readiness are transient only.

`LinkingSourceEditorFacade` owns the automatic-source ayah editor's complete one-source read,
raw API progress, unique verse universe, local search, client page, stale-load generation, and
controlled error state. The editor never persists its loaded ayahs or UI state. It reconciles a
complete universe only when the source row still has the configuration revision captured at load
start; selection changes remain source-row state. Search and pagination constrain visible cards
only, while Select All, Clear All, and checkbox selection continue to operate on the full source
universe. Manual Mushaf source editing remains unavailable until Phase 6.

`QuranSourceLinkingActionsComponent` remains the shared Owner-only explorer seam. Add to Workspace
is idempotent, preserves focus, and announces whether the row was added or already existed; it does
not open the workspace. Direct Link remains a transient one-source shortcut and does not add a row.

The resolver registry supports the automatic source families only. It sequentially loads matching
API pages, rejects incomplete or inconsistent envelopes, and de-duplicates only repeated identical
`verseKey` rows. Root, Lemma, and Stem retain exact Uthmani tokens and backend `isMatched` flags
without claiming canonical Quran word IDs; Word Type preserves returned canonical IDs. A later
source-set coordinator will own multi-source loading, merged review, and intent derivation.

There is still no Linking write API, request, draft, approval, history, cache mutation, durable
group ID, backend entity, or server workspace. The current Direct Link mock remains presentation
only and sends no HTTP request.
