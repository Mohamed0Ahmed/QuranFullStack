# Abwab feature (الأبواب) — category tree, protection, relationships, audit renders

**HOW rules:** `.architecture/FRONTEND_STRUCTURE.md`, `.architecture/API_INTEGRATION_GUIDELINES.md`
(project root). This file is the WHAT (current truth + shared pattern). **Features:**
`029-abwab-core`, `030-abwab-relationships-templates`.

## What this feature does

The domain frontend vertical slice for the category tree: a virtualized, RTL, keyboard-navigable
tree of sections/categories; a category editor (name/description/excerpt/aliases); a protection
panel (view direct/inherited protection, apply/lift/full-preset); and the §6.3 audit-render
components. It is mounted at `/gates` (`app.routes.ts` → `abwab.routes.ts`, unguarded —
public-browse posture, composite-read visibility is in-page and cosmetic, see below).

## Layout

- `data-access/` — the core and relationship contracts, their implementations, caches, and conflict types.
- `state/` — the tree and relationship facades (orchestration) and the composite-read permission mirror.
- `tree/` — the tree page shell and the virtualized tree view.
- `editor/` — the category editor (Reactive Forms).
- `protection/` — the protection panel.
- `relationships/` — the relationship list/panel page (`030`).
- `audit/` — the §6.3 audit-render components (presentational only).

## `data-access/` — the ONE versioned core contract

- **`abwab-core.port.ts`** (`AbwabCorePort`) — every read/write operation the domain needs. Every
  **read** (`getTreeSnapshot`/`search`/`getProtectionProfile`) carries `TimelineGeneration`/
  `TreeRevision` **from the server**; no method may synthesize one. A **direct** manual-protection
  resolution also carries the record's own identity + concurrency token, which the caller sources
  `ExpectedVersion` from for apply/lift/preset — never a manufactured value.
- **`abwab-core.mock.ts`** (`AbwabCoreMock`) — an in-memory implementation for dev/testing. It
  re-derives its own revision/generation counters internally but returns them **only from reads** —
  mutation results never attach one, matching the real HTTP surface exactly. Not `@Injectable()`: it
  takes plain constructor options (actor/clock/id factory) rather than injected services, so callers
  construct it directly or via a DI `useFactory`.
- **`abwab-http.adapter.ts`** (`AbwabHttpAdapter`) — maps the backend's exact `abwab.*` 409 codes
  (`AbwabConflictResponses.cs`) to a typed `AbwabConflictError` (`abwab-conflict.ts`), with the same
  "never fabricates a revision/generation on a mutation" rule.
- **Mock ↔ HTTP parity** (`abwab-core-parity.spec.ts`) is a unit suite that drives the same
  scenarios through both implementations and asserts identical result shapes and identical
  `abwab.*` codes — this is what keeps the mock a safe stand-in for the backend during development.
- **`abwab-mock-*-ops.ts`** — the mock's per-area operation logic (section/category/alias/move/
  delete-restore/protection), kept out of `abwab-core.mock.ts` itself to stay small and focused;
  `abwab-mock-normalize.ts` is a **UI-only mirror** of the backend §5.1 normalization used only for
  the mock's own conflict simulation — the backend `ArabicNameNormalizer` stays the single source of
  truth for real uniqueness decisions.
- **`abwab-cache.ts`** (`AbwabTreeCache`) — reuses the `028` §14.1 primitive
  (`PersistentCache` over an `IndexedDbKeyValueStore`, falling back to an in-memory store when
  `indexedDB` is unavailable). The tree snapshot is cached under **one stable key** (there is only
  one snapshot resource). `invalidate()` is called after every successful mutation and after every
  `abwab.tree_revision_stale`/`abwab.timeline_generation_stale`/`abwab.row_stale` conflict — there is
  no client-side reconciliation of a stale snapshot, only invalidate + re-fetch.

## `data-access/` — the relationship contract (`030`)

`030` gets its **own** port (§14.1) rather than extending `AbwabCorePort` — a mega-port is explicitly
excluded. Same three-part shape and the same rules as the core contract:

- **`abwab-relationships.port.ts`** (`AbwabRelationshipsPort`) — `getRelationships` (per category,
  `includeDeleted` widens it to soft-deleted rows) plus add/edit/delete/restore. Reads carry the
  server `TimelineGeneration`; a mutation result never synthesizes one.
- **`abwab-relationships.mock.ts`** / **`abwab-relationships-http.adapter.ts`** — kept in step by
  `abwab-relationships-parity.spec.ts`. Its `conflictScenarios` table drives **each** scenario through
  **both** ports inside **one** test — the mock must actually *raise* the code the server stub
  returns, so the two sides cannot drift into separately-maintained expectations. Codes covered:
  `abwab.relationship_duplicate` (duplicate mutual pair **and** restore collision),
  `abwab.relationship_cycle`, `abwab.manual_protection`, `abwab.category_unavailable`,
  `abwab.row_stale` (a stale `Version` **and** the state-based refusals — deleting an already-deleted
  row, restoring an already-active one — which carry the row's current `Version`, so only the writer's
  state rule can raise them), `abwab.timeline_generation_stale`. The suite also asserts **key-set equality**
  between a mock read and a flushed HTTP read, and exercises add/edit/delete/restore on both sides.
  The mock canonicalizes a mutual pair the way `CategoryRelationship.Canonicalize` does and rejects a
  self-link, and it mirrors **dormancy**: a relationship whose endpoint category is deleted
  disappears from the actionable projection and returns untouched when that endpoint does.
- **`abwab-response-unwrap.ts`** — the shared `ApiResponse` unwrap + `abwab.*` 409 → `AbwabConflictError`
  mapping. The relationship adapter uses it so the mapping is not duplicated; the `029`
  `abwab-http.adapter.ts` still carries its own copy and is untouched by `030`.
- **`abwab-relationships-cache.ts`** — one cache key per `(categoryId, includeDeleted)` projection
  (unlike the single tree snapshot). Both projections of a category invalidate together, because a
  delete moves a row from one to the other (`abwab-relationships-cache.spec.ts`).

## `state/abwab-relationships.facade.ts`

Same single `runMutation()` cache rule as the tree facade: invalidate + reload from the server on
success **and** on conflict, with nothing applied to the rendered list ahead of server confirmation.
Selection survives a reload as long as the relationship is still in the fresh projection. Status is
`idle | loading | ready | empty | error` so the page renders **distinct** loading/empty/error/retry
states rather than a silent blank. `mutationError` carries **every** failed mutation — conflicts and
non-conflicts alike — so a 400/500 can never render as a silent no-op. It also exposes
`endpointCandidates` / `routedCategoryName`, read once from the `029` `allCategoriesProjection`, to
back the endpoint picker; that read is best-effort and never moves the list out of its own state.
Covered by `abwab-relationships.facade.spec.ts`.

## `relationships/` — explicit actions, no drag

`abwab-relationships-page.component.ts` is routed at `/gates/relationships/:categoryId`
(`abwab.routes.ts` mounted under `path: 'gates'` in `app.routes.ts`). The tree page links to it from
the selected category's detail panel. Add/edit/delete/restore are explicit buttons and Reactive Forms
— never drag, never an implicit save. Visibility is filtered by
`AbwabPermissions.canViewRelationship()` etc., which is **cosmetic only**: the backend
`relationship.*` policies are the sole authority.

Three form rules the page must not lose (`abwab-relationships-page.component.spec.ts` pins all
three):

- The `RelationshipType` `<select>` binds **`[ngValue]`**, never `[value]`. `RelationshipType` is an
  **integer** on the wire and no `JsonStringEnumConverter` is registered, so a `[value]` binding
  would hand the control the option's *string* and 400 every non-default type.
- A `BroaderNarrower` edge is stored broader-first and read back in that order, so the form carries
  an explicit **direction** control («هذا الباب هو الأعم / الأخص»). On edit it is seeded from the
  stored `from`/`to`, which is what stops a no-op save from silently inverting the edge; it also
  makes an **inbound** edge (the routed door as the narrower end) authorable.
- The other endpoint is a **picker** over the `029` category projection, never a hand-typed GUID. It
  excludes the routed door, so a self-link cannot be submitted; a group validator rejects one anyway.

`includeDeleted` lives in the URL as `?includeDeleted=true` (`FRONTEND_STRUCTURE.md` "Tabs and URL
State"), so refresh and sharing preserve it. `data-access/relationship-type-labels.ts` holds the
frozen Arabic type labels — keyed by the wire constants from `abwab-relationships.port.ts`, so a
label and its integer cannot drift — and derives the Broader/Narrower endpoint labels; the inverse is
a **display derivation**, never a second stored row. It is the one label source: the audit
relationship payload carries the wire `RelationshipType` and `relationship-render.component.ts`
derives its label from this module, so no caller can introduce a second wording.

One narrow build rule applies to it, and only to it: **a class-FIELD initialiser must never read an
imported binding.** Under the unit-test builder (`@angular/build:unit-test`) esbuild emits shared
modules as lazily-initialised chunks, and vite-node's SSR transform hoists an import referenced in
class-field position into a module-top `const` snapshot that evaluates *before* that chunk's body —
capturing `undefined`, which renders the type `<select>` with zero options and no error. The page
component therefore exposes `typeLabels`/`typeOptions` as **getters**. Everything else stays as
written: reads from a method body (`buildRelationshipForm`'s `RELATIONSHIP_TYPE_SIMILAR` default,
pinned by a spec assertion), from a getter, and from the `@Component` metadata (the cache provider)
are live property accesses and are unaffected. Add/edit forms are **separate `FormGroup`s** so
opening the editor cannot discard an unsaved add draft.

## `state/abwab-tree.facade.ts` — orchestration, cache rule, and "rollback"

Owns the loaded tree snapshot, selection/expansion, and every mutation, all funneled through one
`runMutation()` that is the single place the cache rule above lives: on success, invalidate the
cached snapshot and reload from the server (the only source of a fresh revision/generation); on a
conflict, do the **same** invalidate + reload. Because nothing is ever applied to the rendered
snapshot ahead of server confirmation, "rollback" is just this re-sync — there is no separate
optimistic-undo path to get wrong. Selection/expansion **survive** a reload as long as the category
still exists in the fresh snapshot (post-mutation context preservation, SC-015); only ids that
genuinely disappeared (e.g. a subtree delete) are pruned.

## Composite-read visibility (`state/abwab-permissions.ts`) — cosmetic only

`AbwabPermissions` mirrors the backend's composite-read redaction table (`tree-read-contract.md`)
from `/me` permissions: tree/search visibility requires **both** `category.view` and `section.view`;
full protection detail additionally requires `protection.view`. **This is entirely non-authoritative
UI polish** — the backend DTO projection (`AbwabCompositeReadRedactor`) is the sole authority, and a
hidden action invoked directly is still rejected server-side. The protection panel enforces the same
rule from the other direction: when `canView` is false it renders **nothing** protection-specific,
which is safe by construction because a caller without `protection.view` is only ever handed a
redacted profile with no type/scope/actor/source-ancestor to leak in the first place.

## `tree/` — no drag, RTL, virtualized

`abwab-tree-view.component.ts` renders a virtualized (`cdk-virtual-scroll-viewport`), RTL,
keyboard-navigable category tree. Every mutation is an **explicit action** (a button), never drag:
expand/collapse, select, move-up/move-down (sibling reorder), "move to…" (opens the destination
picker in the page shell), and delete are all discrete emitted events — see
`e2e/abwab/core-slice.spec.ts` for the no-drag browser proof and `check:no-drag` for the static source
gate. RTL keyboard follows the WAI-ARIA treeview pattern's mirrored assignment: `ArrowLeft` expands
("inward"), `ArrowRight` collapses. `abwab-tree-node.ts` flattens the snapshot into only the
currently-**visible** rows (a node's descendants show iff every ancestor up to the root is
expanded), which is what makes the tree cheap to virtualize regardless of total size.
`abwab-tree-page.component.ts` is the page shell composing the tree view, editor, and protection
panel over the facade.

## `editor/category-editor.component.ts` — reused Reactive Forms, no edit-session lock

Reuses the `028` `@angular/forms` Reactive Forms package. **Explicit save only** — there is no
autosave and no "start editing session" call, so opening (or switching) the editor never itself
emits a mutation or locks the category against another editor; the only thing that can conflict is
the save itself, via the ordinary `abwab.row_stale`/`abwab.timeline_generation_stale` mutation
conflict. Existing-alias edit/remove each carry the **alias's own** `Version` (from
`CategorySnapshotDto.aliases`) as `expectedVersion` — never the category's.

## `protection/protection-panel.component.ts`

Views direct/inherited protection (type/scope, the resolving source ancestor, the server-derived
expiry) and drives apply/lift/full-preset, gated by `protection.view` as described above. Each
changed-scope preset type needs its **own** active record's `Version`; a type with no active record
(newly inserted by the preset) is never version-checked by the write path, so `0` there is an inert
placeholder, not a manufactured expectation (`manual-protection-contract.md`).

## `audit/` — §6.3 render components (presentational only, no fetch)

Pure view models + presentational components for the five §6.3 payloads
(`audit-render-contract.md`): category **create** (complete new-state, empty fields shown as
`غير محدد`, order fields included), category **edit** (non-color field-diff marker, order fields
included), bulk **move** (nested descendants, sibling-order side effects grouped by affected
parent/order scope), subtree **delete/restore** (dormant-dependent labels/counts), and
**manual-protection** (changed direct/inherited effects). `030` adds the **relationship** payload
(`relationship-render.component.ts`): type/shape, the type label **and** the Broader/Narrower inverse
label **derived for display** from `data-access/relationship-type-labels.ts` — the payload carries the
wire `RelationshipType`, never Arabic text, so a `033` caller cannot pass a free-form label — and one
diff row per field laid out `label | previous | current` — previous state
right, current/result left in RTL. Both endpoints render their historical section/path **plus** the
live current name/path/deleted state **on whichever side the payload carries**, so a `deleted`
payload (before only) still shows live current state. A changed value carries colour **and** a
non-colour «▲ تغيير» marker **inside the value itself**, following `field-diff-row.component.*` —
never a detached marker block. The payload carries **no protection-blocker list** — applicable
`Relationship` protection aborts the mutation before a ChangeSet exists, so a blocked attempt is an
`abwab.manual_protection` conflict, never an audit row — and reviewer is
«غير مطلوب». `relationship-dormant-counts.ts` is `030`'s **data contribution** to the `029`-owned
subtree delete/restore payload: relationship counts feed the generic `dormantDependentCounts` seam.
It takes the label map as an **argument** rather than importing it, so the mapper stays a pure
contribution to the `029`-owned seam with no wording of its own. It contributes the relationship **name only** (`علاقات (مشابه)`) — the `029` row stamps its own
«خامل» badge, and the count is never labelled "deleted" (a subtree delete writes no relationship row
at all). **There
is no standalone "ordering" render component** — ordering is folded into bulk-move and
category-edit, per §6.3. `029` defines
and renders only the *shape* of these payloads over whatever changeset a caller supplies; it does
not build the main audit page, pagination, or fetch real audit records — that read model is `033`'s.
Fixture data in tests is synthetic Arabic only (source-safe) — never real Quran text.

## Related

- Backend: `Backend/api/QuranDashboard.Api/Abwab/README.md`,
  `Backend/application/QuranDashboard.Application/Abwab/README.md`.
- Contracts: `specs/029-abwab-core/contracts/tree-read-contract.md`,
  `audit-render-contract.md`, `manual-protection-contract.md`.
- Reused `028` primitives: `../../core/README.md` (§14.1 cache/foundation), `../../shared/README.md`.
- Playwright source suite: `e2e/abwab/core-slice.spec.ts`.
