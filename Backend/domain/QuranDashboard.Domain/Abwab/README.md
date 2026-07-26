# Abwab domain — Sections, Categories, Protection, Tree (`029`), Relationships + Templates (`030`)

**Layer:** Domain · **Features:** `029-abwab-core`, `030-abwab-relationships-templates` ·
**HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`

The category-tree product model: sections, the category tree itself, search aliases, and manual
protection. The `028` write-kernel primitives (`Audit/`, `Concurrency/`, `Timeline/`, `Persistence/
IAbwabAuditable`, `Notifications/`) are documented in
`Backend/infrastructure/QuranDashboard.Infrastructure/Abwab/README.md` (the infrastructure half of
that kernel) — every entity below is `IAbwabAuditable` and only ever mutates through it.

## What is here

- `Sections/Section.cs` — `SectionId`, `Name`/`NormalizedName`, `SortOrder`, `IsPermanentDefault`,
  soft-delete metadata, `Version` (xmin). Exactly one row has `IsPermanentDefault = true`
  (`أبواب غير مصنفة`, seeded by migration); it can be reordered but never renamed, deleted, or
  duplicated.
- `Categories/Category.cs` — the tree node: `Name`/`NormalizedName`, optional `Description`,
  optional `RepresentativeQuranExcerpt` (**plain string — no Quran FK, no ayah validation**),
  `ParentCategoryId`/`SectionId`, three independent order fields (`SiblingOrder` per parent,
  `SectionOrder` and `GlobalOrder` independent root orders), denormalized `AncestorIds`/`Depth`,
  the ordinary-protection actor/time fields, `CategoryContentRevision`, `DeletionOperationId` (set
  by an atomic subtree delete, cleared on restore), soft-delete metadata, `Version`. Root shape:
  `ParentCategoryId = null`, non-null `SectionId`/`SectionOrder`/`GlobalOrder`, `AncestorIds = []`,
  `Depth = 0`. Descendant shape: non-null `ParentCategoryId`/`SiblingOrder`, `AncestorIds` root→parent,
  `Depth = AncestorIds.Length`.
- `Categories/CategorySearchAlias.cs` — a separate owned row per alternate search name
  (`Value`/`NormalizedValue`, soft-delete, `Version`); uniqueness is per-category, never global, and
  is not part of category-name uniqueness.
- `Protection/ManualProtection.cs` + `ManualProtectionType`/`ManualProtectionScope` — one typed
  protection record per `(CategoryId, ProtectionType)` (`CategoryData`, `InternalStructure`,
  `QuranContent`, `Deletion`, `Relationship`), scoped `CategoryOnly` or `Subtree`, with
  applied/lifted actor+timestamp and soft-delete.
- `Relationships/CategoryRelationship.cs` + `RelationshipType` (`030`) — one typed row per
  relationship between two categories. A row carries **exactly one shape**: the **mutual** pair
  (`Similar`/`Opposite`) in canonical `LowerCategoryId < HigherCategoryId` order, **or** the
  **directional** pair (`BroaderNarrower`, `SourceCategoryId` = broader, `TargetCategoryId` =
  narrower). The shape is **bound to the type** by the `one_shape` CHECK, not merely mutually
  exclusive: a mutual type may not occupy the directional columns, because such a row would join the
  Broader/Narrower graph that cycle validation walks. Soft-delete metadata + `Version` (xmin).
  `Canonicalize` is the single place a submitted pair becomes storable columns, which is what makes a
  *reverse* duplicate collapse onto the same active unique-index key; `EndpointsOf` is the single
  place those columns become an ordered endpoint pair, so no reader can pick the wrong shape's
  columns. `EndpointCategoryIds` (a stored row) and the application-side `RelationshipShape` (a
  not-yet-stored submission) both delegate to it rather than repeating the rule.
  The Broader/Narrower **inverse label is derived for display**, never a second stored row.
- `Templates/DoorTemplate.cs` + `TemplateNode.cs` + `TemplateNodeSearchAlias.cs` (`030`) — the door
  template aggregate. `DoorTemplate` carries identity, `Name`/`NormalizedName`, optional
  `Description`, the `TemplateRevision` aggregate counter, soft-delete metadata and `Version`.
  `TemplateNode` carries template ownership, an optional `ParentTemplateNodeId` (null = template
  root), `Name`/`NormalizedName`, an optional **plain-string** `RepresentativeQuranExcerpt`, optional
  `Description`, an explicit `SiblingOrder`, soft-delete metadata and `Version`.
  `TemplateNodeSearchAlias` mirrors `CategorySearchAlias`'s value/normalization/soft-delete contract
  for a node. Templates are created and edited **only in the template editor** — no entity, command,
  or service reads real categories into a template, and no template or node crosses doors (§7.4).
- `Tree/ArabicNameNormalizer.cs` — the §5.1 normalization used for every Section/Category/alias
  name: NFC (UAX#15, Unicode 16.0), trim + collapse whitespace, strip tatweel and the frozen
  Arabic-mark scalar set, fold `أ/إ/آ/ٱ → ا` and `ى → ي`. **`ة` is never folded to `ه`.** The display
  string is preserved; only the normalized column feeds uniqueness/search.

## Invariants / caveats (read before changing)

- **No Abwab→Quran foreign key.** Both `Category.RepresentativeQuranExcerpt` and
  `TemplateNode.RepresentativeQuranExcerpt` stay plain string columns;
  `Backend/tests/QuranDashboard.Tests/Abwab/_Guards/NoPrematureQuranFkTests.cs` asserts both the FK
  absence and the plain-string type — keep it green.
- **`TemplateRevision` is the template aggregate's own counter**, bumped exactly once per grouped
  **structural** node operation (add/reparent/reorder/remove) and never by a content edit or an alias
  mutation. It is excluded from restore snapshots — it is current technical state, not product state.
  A template carries **no name-uniqueness rule**: §7.4 defines none and §11 owns no conflict code for
  one, so the normalized-name indexes on templates and nodes are lookup indexes, never unique.
- **Two independent revision counters on `Category`, never conflated.** `TreeRevision` (on
  `AbwabRevisionState`, `028`) bumps once per atomic *structural* operation (move/reorder/subtree
  delete-restore). `CategoryContentRevision` (on the row) bumps once per *direct-content* operation
  (name/description/excerpt/alias). A pure move/reorder never bumps `CategoryContentRevision`, and a
  content edit never bumps `TreeRevision`. Content concurrency has no dedicated §11 code — it is
  `xmin` (`abwab.row_stale`) + `ExpectedTimelineGeneration`, same as any other row.
- **`AncestorIds` is the read-time truth for inheritance**, not a stored descendant snapshot — moving
  a category changes what it inherits immediately, with no batch/recompute step for ancestors.
- **`ة` is deliberately excluded** from the alef-maqsura-style folds — do not "complete" the mapping;
  the corpus test in `Backend/tests/QuranDashboard.Tests/Abwab/_Fixtures/NormalizationCorpus.cs`
  pins this.
- **One active `ManualProtection` per `(CategoryId, ProtectionType)`** is a DB-enforced filtered
  unique index (`Infrastructure/Persistence/Configurations/Abwab/ManualProtectionConfiguration.cs`),
  not just an application check.
- **A relationship row is never cascade-deleted by a category delete.** All four endpoint FKs are
  `RESTRICT`, and the row carries no deletion state of its own — **dormancy is derived on read** from
  the endpoints' current deleted state, never a written flag, so a category operation-restore needs
  no relationship-side write to reverse. Shape/canonical-order/no-self are DB CHECK constraints and
  duplicate rejection is a filtered unique index over **active** rows only, so soft-deleted history
  survives.

## Related

- Application handlers: `QuranDashboard.Application/Abwab/README.md`.
- Read ports/DTOs/commands: `QuranDashboard.Application.Abstractions/Abwab/README.md`.
- EF configurations, read ports, restore adapters (infrastructure): the sibling
  `Infrastructure/Abwab/README.md`, `Infrastructure/Persistence/Reads/Abwab/README.md`,
  `Infrastructure/Abwab/Restore/README.md`.
- API surface: `Api/Abwab/README.md`.
- Contracts: `specs/029-abwab-core/contracts/` (`tree-read-contract.md`, `sections-api.md`,
  `categories-api.md`, `manual-protection-contract.md`, `restore-adapters-contract.md`) and
  `specs/030-abwab-relationships-templates/contracts/` (`relationships-api.md`,
  `relationship-dormancy-contract.md`, `templates-api.md`, `template-application-contract.md`,
  `restore-adapters-contract.md`).
