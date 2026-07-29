# Abwab tree read model

**Layer:** Infrastructure · read-only queries · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md`

## What this area does

Three readers back the Abwab feature's four read endpoints. `EfAbwabTreeReader` serves
`GET api/abwab/tree` (`GetAbwabTreeHandler`, `AbwabTreeController`) — one complete, versioned
snapshot of the doors/sections outline: no paging, no filtering. `EfAbwabRelationsReader` serves
`GET api/abwab/doors/{doorId}/relations` (`GetDoorRelationsHandler`,
`AbwabDoorRelationsController`) — one door's visible relations, always stated from that door's side.
`EfAbwabTemplatesReader` serves `GET api/abwab/templates` and `GET api/abwab/templates/{templateId}`
(`GetTemplatesHandler` / `GetTemplateHandler`, `AbwabTemplatesController`) — the admin-only door
templates, which live in their own tables and are invisible to the snapshot above.
The write side lives beside it at `../../Writes/Abwab/`; the domain entities are
`Backend/domain/QuranDashboard.Domain/Abwab/` (`AbwabSection`, `AbwabDoor`, `AbwabDoorAlias`,
`AbwabDoorRelation`, `AbwabTemplate`, `AbwabTemplateNode`).

## Key pieces

- `EfAbwabTreeReader.GetTreeAsync` — the snapshot query. `AsNoTracking` throughout.
- `EfAbwabRelationsReader.GetForDoorAsync` — one door's relations. Returns `null` for an unknown
  door so the handler can answer `404`; an **empty list** means the door exists and has nothing
  visible. Those two are not interchangeable (the `IAyahStudyReader` convention).
- `EfAbwabTemplatesReader.GetAllAsync` / `.GetAsync` — the templates list and one template's node
  subtree.

## Shape and invariants (read before changing)

- **Flat, not nested.** `AbwabTreeDto.Doors` is a flat list; each `AbwabTreeDoorDto` carries its own
  `SectionId`/`ParentId` so a consumer assembles the tree at any depth (doors nest without limit —
  feature plan §4). `DirectChildCount` is deliberately present on the flat DTO — it would be a
  redundant, always-derivable field if doors were nested arrays instead.
- **Archived sections are excluded; archived doors are included and flagged.** Sections have no
  restore route in this slice (only `DELETE`, and only when empty of live doors), so an archived
  section is filtered out rather than shown with no way back. Doors DO have a restore route, so every
  door — live or archived — appears, with `IsArchived` set from `DeletedAtUtc != null`.
  **Do not silently start filtering archived doors out** — restore and the archive view both depend on
  them being visible here.
- **`DirectChildCount` and `AbwabTreeSectionDto.DoorsInScopeCount` count LIVE rows only** (own
  documented judgment call, not stated verbatim in the feature plan): they are the "how many doors are
  here right now" badge, not inflated by an archived subtree the main view no longer shows. An archived
  door is still individually visible via `IsArchived`; it simply does not count toward a parent's or a
  section's live total. `DoorsInScopeCount` counts every live door with that `SectionId` regardless of
  nesting depth — this is correct only because every write path inherits a nested door's section from
  its parent, so `SectionId` is never wrong on a non-root door. That is enforced, not assumed:
  `EfAbwabDoorsWriter.ResolveCreateSectionAsync` derives a child's section from its parent (and refuses a
  disagreeing one), and `CascadeSectionToDescendantsAsync` carries a section change down the whole subtree
  on both move paths, archived rows included (`../../Writes/Abwab/README.md`). Restoring a door whose
  section was archived meanwhile detaches the whole restored subtree to `SectionId = null` for the same
  reason — a live door can never point at a section this reader filters out.
- **Aliases are live-only**, matching the write side's own DTO projection (`EfAbwabDoorsWriter.ToDtoAsync`)
  — a soft-deleted alias is gone from every read, not just the write response.
  **Snapshot `Version`** is `max(updated_at, deleted_at)` across `abwab_sections`, `abwab_doors`, and
  `abwab_door_aliases` (one query per table: each row's own greatest of the two columns, then `MAX()`
  across rows) — chosen over a monotonic counter because it needs no new column and no extra write.
  `null` on a fully empty schema.
- **Reads tolerate gaps.** Every write resequences its scope to `1..N`, but this reader does not assume
  or require contiguity — it orders by the raw `OrderValue` (plus `Id` as a pure tie-break hardening,
  never a meaningful ordering signal on its own).
- **`GlobalOrderValue` is `NULL` for nested and archived doors** — it is meaningful for live root
  doors only (`../../Writes/Abwab/README.md`'s invariant). The reader projects it verbatim onto
  `AbwabTreeDoorDto`/`AbwabDoorDto` and does **not** order by it: this reader stays scope-ordered
  (`OrderValue`, `Id`) exactly as before, and the client is the one that sorts the superset by
  `GlobalOrderValue` — consistent with "flat, not nested" above, where shaping the outline is a
  consumer job, not this reader's.
- **Dormancy is a read-time join, never a stored column.** A relation is visible iff its own
  `deleted_at IS NULL` **and both endpoint doors** are live; both readers express that as a join on
  `abwab_doors` for `door_a_id` and `door_b_id`. There is no `is_dormant` column, deliberately: it
  would have to be rewritten by every archive, bulk-archive, restore, and archive-subtree sweep —
  i.e. it would drift on exactly the paths that are hardest to test. Archiving a door therefore
  hides its relations from **both** sides and drops both partners' counts, with no write touching a
  relation row, and restoring brings them straight back. The partial unique index filters on the
  relation's own `deleted_at` only, so a dormant row still occupies its pair, which is what makes
  restore collision-free.
- **A relation's direction is resolved per viewer, never stored twice.** One row carries
  `broader_door_id` (the more comprehensive endpoint); each reader converts it to an
  anchor-relative `AbwabRelationDirection` — `AnchorMoreComprehensive` when `broader_door_id` is the
  door being read. The same row therefore reports opposite directions to its two doors, which is the
  whole point. **Do not put the two-sided "broader"/"narrower" words on the wire** — they read from
  two different perspectives and cannot be disambiguated by a consumer.
- **`RelationCount` counts LIVE-endpoint relations only**, the same judgment call `DirectChildCount`
  and `DoorsInScopeCount` carry above: it is the "how many relations are on this door right now"
  badge. An archived door's count is therefore always 0, and so is a live door's count for a partner
  that is archived. One grouped query per snapshot (`GetLiveRelationCountsAsync`), incrementing
  **both** endpoints of each visible row — never one query per door, which would turn the snapshot
  into an N+1.
- **Snapshot `Version` deliberately ignores `abwab_door_relations`.** `Version` is
  `max(updated_at, deleted_at)` across sections, doors, and aliases only, so a relation write changes
  the snapshot's `RelationCount` values without moving `Version`. That is safe **because `Version` is
  diagnostics-only** — nothing does conflict detection with it (`features/abwab/README.md` says so on
  the client side too). Widening it to a fourth table would imply a guarantee it does not make.
- **Templates are flat too, and a rootless template is not-found.** `AbwabTemplateDto.Nodes` carries
  `ParentNodeId` per node for the same reason the doors list does. Each template's display name is
  its **root node's** name — `abwab_templates` has no name column — so a template with no live root
  has nothing to return for the one field the list renders, and both reads treat it as not-found
  rather than emitting an empty name. Unreachable today (create always writes the root), stated
  because node rows are soft-deleted and this reader is what filters them.
- **The templates list is one query, not one per template.** Root name and live descendant count are
  read in a single join and grouped afterwards — the `GetLiveRelationCountsAsync` rule.
- **Templates never touch the snapshot.** `abwab_templates` / `abwab_template_nodes` are separate
  admin tables, so no `AbwabTreeDoorDto` field, no `Version` term, and no filter here changes because
  of them. An applied template shows up as ordinary doors on the next snapshot read, with nothing
  marking them as template-derived.
- **No caching.** Unlike the Words explorers' readers, this one is not wrapped in a caching decorator:
  Abwab is live admin-authored data with no invalidation story yet, and caching a snapshot an admin is
  actively editing would be a correctness risk, not a convenience.

## Related

- Write side: `../../Writes/Abwab/` (`EfAbwabSectionsWriter`, `EfAbwabDoorsWriter`,
  `EfAbwabRelationsWriter`, `EfAbwabTemplatesWriter`, `EfAbwabTemplateApplyWriter`) and its
  `README.md`.
- Domain entities: `Backend/domain/QuranDashboard.Domain/Abwab/`.
- Handlers: `application/QuranDashboard.Application/Abwab/Queries/GetAbwabTree/`,
  `.../Queries/GetDoorRelations/`, `.../Queries/GetTemplates/`, `.../Queries/GetTemplate/`.
- Controllers: `api/QuranDashboard.Api/Controllers/Abwab/AbwabTreeController.cs`,
  `AbwabDoorRelationsController.cs`, `AbwabTemplatesController.cs`
  (`../../../../api/QuranDashboard.Api/Controllers/README.md`).
- Response DTOs: `application/QuranDashboard.Application.Abstractions/Abwab/Responses/AbwabTreeDto.cs`,
  `AbwabDoorRelationDto.cs`, `AbwabTemplateDto.cs`, `AbwabTemplateSummaryDto.cs`,
  `AbwabTemplateNodeDto.cs`.
- Tests: the relations and templates readers have none — see `docs/TESTING_DEBT.md` for the gaps and
  their triggers.
