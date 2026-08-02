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
  section was retired meanwhile demands a live destination rather than detaching it, and re-sections the
  subtree the same way. **A live door can never point at a section this reader filters out** — which is
  now guaranteed twice over: a section is only archivable while it holds no live doors, and `section_id`
  is `NOT NULL`, so there is no third "outside every section" state to account for.
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
  the client side too), and nothing validates a cache with it either: the `ETag` is a server-side
  generation counter, not row data (see the caching section). Widening it to a fourth table would
  imply a guarantee it does not make.
- **Templates are flat too, and a rootless template is not-found.** `AbwabTemplateDto.Nodes` carries
  `ParentNodeId` per node for the same reason the doors list does. Each template's display name is
  its **root node's** name — `abwab_templates` has no name column — so a template with no live root
  has nothing to return for the one field the list renders, and both reads treat it as not-found
  rather than emitting an empty name. Unreachable today (create always writes the root), stated
  because node rows are soft-deleted and this reader is what filters them.
- **The templates list is one query, not one per template, and it aggregates in SQL.** Root name and
  live descendant count are correlated subqueries inside one statement — the `GetLiveRelationCountsAsync`
  rule, plus the second half of it: what crosses the wire is one row per template, never one per node.
- **Templates never touch the snapshot.** `abwab_templates` / `abwab_template_nodes` are separate
  admin tables, so no `AbwabTreeDoorDto` field, no `Version` term, and no filter here changes because
  of them. An applied template shows up as ordinary doors on the next snapshot read, with nothing
  marking them as template-derived.
- **Both readers here ARE cached**, behind `Infrastructure/Caching/Abwab/`. The former "no caching"
  rule stood only while there was no invalidation story; there is one now, and it is what makes
  caching admin-authored data safe. See the section below.
- **The relations read is the exception and stays uncached and unconditional.** The client fetches it
  per modal-open with no held prior value, so a `304` would have nothing to render against and a
  per-door validator would be a third cache resource for zero saved bytes.

## Caching and invalidation

`IAbwabTreeReader` and `IAbwabTemplatesReader` are wrapped by `CachedAbwabTreeReader` /
`CachedAbwabTemplatesReader` (`Infrastructure/Caching/Abwab/`), registered in
`AbwabDependencyInjection` the same concrete-Ef + interface→decorator way the `Caching/Quran`
readers are.

- **Entries.** `abwab:tree` holds the whole `AbwabTreeDto` as **one indivisible entry** — a
  root-affecting write moves every row's `xmin`, so a per-section or live-vs-archive split would be
  wrong rather than merely finer, and the archive view is a client-side partition of this same
  snapshot, **not a cacheable resource of its own**. `abwab:templates` holds the list;
  `abwab:template:{id}` holds one template. Both templates entries share one generation, so a node
  edit on template A also invalidates B's cached detail — accepted at admin scale.
- **Eviction is a generation stamp, never `IMemoryCache.Remove`.** `AbwabCacheGeneration` holds one
  counter per resource; every write bumps its counter through an invalidating writer decorator, and a
  cached entry is served only if its stored stamp still equals the current generation. There is **no
  expiration on any entry** — eviction is write-driven and exact, not time-based.
- **Capture before load.** Each reader reads the generation *before* querying and stamps the entry
  with that captured value. A write committing mid-load therefore leaves the new entry already stale:
  the failure direction is one extra query, never a stale hit.
- **`CacheLoadGate` is deliberately not reused.** It cannot express "present but stale" — it returns
  on `TryGetValue` before any generation check — and putting the generation in the key to work around
  that would create the unbounded key space its own comment forbids. There is no single-flight here;
  a single-admin product cannot produce a cold-miss stampede on one key.
- **A miss on `abwab:template:{id}` is never cached.** Template ids come from the caller, so caching
  absences would let an id probe grow the key space without bound.
- **The cache validator ignores `Version` right back.** The `ETag` the API serves is this generation
  counter plus a per-process boot id — server memory, no row data — so snapshot `Version` is neither
  the concurrency currency (that is `xmin`) nor the cache validator. Three separate jobs.
- **CONSTRAINT: this is correct for a single backend instance only.** The generation pair is
  per-process memory. With two instances, a write on instance A leaves B's counter and B's cached
  snapshot untouched, so B serves stale bodies and stale `304`s and the refresh-after-write invariant
  breaks — spurious `409`s follow (`Frontend/quran-dashboard-ui/src/app/features/abwab/README.md`,
  the refresh-after-write section). Production is Railway, currently single-instance; this is the same
  recorded posture as the rate limiter's per-instance paragraph in `API_GUIDELINES.md`.
  **Migration path if a second instance ever runs:** move the generation to shared state bumped inside
  the write transaction — a one-row table or a sequence read by the validator — behind the existing
  `IAbwabCacheInvalidator` / `IAbwabCacheValidators` interfaces, which no caller has to change.

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
- Tests: the relations and templates readers have none. The relations gap and its trigger are in
  `docs/TESTING_DEBT.md`; the templates rows land with that feature's frontend slice.
