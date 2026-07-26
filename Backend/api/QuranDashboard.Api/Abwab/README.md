# Abwab API — Sections, Categories, Tree, Protection (`029`), Relationships + Templates (`030`)

**Layer:** Api · **Features:** `029-abwab-core`, `030-abwab-relationships-templates` ·
**HOW rules:** `Backend/.architecture/API_GUIDELINES.md`

Thin controllers over the `029` Application ports (`IAbwabCoreReadPort`/`IAbwabCoreWritePort`,
`ProtectionResolver`) — no business logic here. Every endpoint returns `ApiResponse<T>`.

## Controllers

- **`Tree/AbwabTreeController`** — `GET /api/abwab/tree` (snapshot) and search: read-only, no
  mutation action here. Composite-read redaction is enforced **here**, as a backend DTO projection
  over the caller's resolved permissions (`AbwabCompositeReadRedactor`) — never frontend hiding. A
  caller missing `category.view`/`section.view` gets `403` on the whole read, not a partially
  redacted body.
- **`Sections/SectionsController`** — explicit section actions only (`add`/`edit`/`reorder`/
  `delete`) — **no drag semantics**. Every mutation carries `ExpectedTimelineGeneration` (and the
  expected `xmin` where a specific row is targeted). Policies: `SectionAdd`/`SectionEdit`/
  `SectionReorder`/`SectionDelete`.
- **`Categories/CategoriesController`** — explicit category actions only — **no drag semantics**.
  Move and reorder are distinct verbs; subtree-delete/operation-restore is the atomic pair. Every
  mutation carries `ExpectedTimelineGeneration` plus expected `xmin`/`TreeRevision` where structural.
  Policies: `CategoryAdd`/`CategoryEdit`/`CategoryMove`/`CategoryReorder`/`CategoryDelete` (alias
  add/edit/remove and the `RepresentativeQuranExcerpt`/description edits are `CategoryEdit` — never
  a borrowed child verb).
- **`Relationships/RelationshipsController`** (`030`) — explicit relationship actions only
  (`add`/`edit`/`delete`/`restore`) plus the authorized per-category read — **no drag semantics**.
  Every mutation carries `ExpectedTimelineGeneration`; edit/delete/restore also carry the
  relationship row's expected `xmin` (add carries none — no row exists yet, and endpoint validity +
  protection are revalidated under the transaction). Policies: `RelationshipView`/`RelationshipAdd`/
  `RelationshipEdit`/`RelationshipDelete`/`RelationshipRestore` — no synonym, no borrowed verb.
  A **self-link** is malformed input and fails as the framework `400` produced by the
  `[ApiController]` validation convention (`RelationshipContracts.cs` implements
  `IValidatableObject`); no `abwab.*` body code exists for 400/403 and none is introduced.
- **`Templates/TemplatesController`** (`030`) — the template editor and application surface:
  aggregate CRUD, the node internals (`add`/`edit`/`reparent`/`reorder`/`remove`), the alias
  internals (`add`/`edit`/`remove`/`restore`), the authorized reads (list/detail/history), and
  `apply`-to-one-category. **Explicit action endpoints — no drag semantics:** a reparent names its
  destination parent and a reorder posts the whole ordered sibling list. Every mutation carries
  `ExpectedTimelineGeneration`, structural node operations also carry the expected
  `TemplateRevision`, and row-targeted operations carry the row's expected `xmin`; `apply`
  additionally carries the expected `TreeRevision` and the target category's `xmin`. Policies are the
  §5.2-frozen set with **no borrowed verb**: `TemplateView` for reads, `TemplateAdd` for the
  aggregate **only**, `TemplateEdit` for every node/alias internal, `TemplateDelete`/`TemplateRestore`
  for aggregate lifecycle, and `TemplateApply` **alone** for application. There is **no**
  create-from-real-door endpoint and **no** cross-door copy endpoint (§7.4).
- **`Protection/ProtectionController`** — manual protection apply/lift/full-preset
  (`ProtectionApply`/`ProtectionLift`) plus the dedicated effective-protection read
  (`ProtectionView` — the composite-read redaction table in `tree-read-contract.md`).

## Conflict mapping

`AbwabConflictResponses.TryMap` maps every `abwab.*` exception to `409 Conflict` with the shared
`ApiResponse` failure envelope, the Arabic message, and the **exact** code echoed in `Errors` — the
same fixed set from `specs/029-abwab-core/tasks.md` §5 / `contracts/`
(`abwab.section_name_conflict`, `abwab.category_cycle`, `abwab.manual_protection_scope_conflict`,
`abwab.tree_revision_stale`, `abwab.timeline_generation_stale`, `abwab.row_stale`, …) plus `030`'s
`abwab.relationship_duplicate` / `abwab.relationship_cycle` / `abwab.template_cycle` /
`abwab.template_revision_stale`. Never
invent, rename, or remap a code; `Backend/tests/QuranDashboard.Tests/Abwab/_Support/ConflictCodeParityTests.cs`
asserts this mapping identically across the core, the HTTP layer, and the contract fixture list.

## Related

- Application handlers: `Application/Abwab/README.md`.
- Ports/commands: `Application.Abstractions/Abwab/README.md`.
- Contracts: `specs/029-abwab-core/contracts/sections-api.md`, `categories-api.md`,
  `manual-protection-contract.md`, `tree-read-contract.md`.
- Permission policies: `Domain/Security/Permissions/PermissionCatalogue.cs`.
