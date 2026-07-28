# Abwab tree read model

**Layer:** Infrastructure · read-only queries · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md`

## What this area does

One reader, `EfAbwabTreeReader`, backs the single read endpoint of the Abwab feature —
`GET api/abwab/tree` (`GetAbwabTreeHandler`, `AbwabTreeController`). It returns one complete,
versioned snapshot of the doors/sections outline: no paging, no filtering. The write side lives
beside it at `../../Writes/Abwab/` (`EfAbwabSectionsWriter`, `EfAbwabDoorsWriter`); the domain
entities are `Backend/domain/QuranDashboard.Domain/Abwab/` (`AbwabSection`, `AbwabDoor`,
`AbwabDoorAlias`).

## Key pieces

- `EfAbwabTreeReader.GetTreeAsync` — the one query this area exposes. `AsNoTracking` throughout.

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
  its parent, so `SectionId` is never wrong on a non-root door.
- **Aliases are live-only**, matching the write side's own DTO projection (`EfAbwabDoorsWriter.ToDtoAsync`)
  — a soft-deleted alias is gone from every read, not just the write response.
  **Snapshot `Version`** is `max(updated_at, deleted_at)` across `abwab_sections`, `abwab_doors`, and
  `abwab_door_aliases` (one query per table: each row's own greatest of the two columns, then `MAX()`
  across rows) — chosen over a monotonic counter because it needs no new column and no extra write.
  `null` on a fully empty schema.
- **Reads tolerate gaps.** Every write resequences its scope to `1..N`, but this reader does not assume
  or require contiguity — it orders by the raw `OrderValue` (plus `Id` as a pure tie-break hardening,
  never a meaningful ordering signal on its own).
- **No caching.** Unlike the Words explorers' readers, this one is not wrapped in a caching decorator:
  Abwab is live admin-authored data with no invalidation story yet, and caching a snapshot an admin is
  actively editing would be a correctness risk, not a convenience.

## Related

- Write side: `../../Writes/Abwab/` (`EfAbwabSectionsWriter`, `EfAbwabDoorsWriter`).
- Domain entities: `Backend/domain/QuranDashboard.Domain/Abwab/`.
- Handler: `application/QuranDashboard.Application/Abwab/Queries/GetAbwabTree/`.
- Controller: `api/QuranDashboard.Api/Controllers/Abwab/AbwabTreeController.cs` (`../../../../api/QuranDashboard.Api/Controllers/README.md`).
- Response DTOs: `application/QuranDashboard.Application.Abstractions/Abwab/Responses/AbwabTreeDto.cs`.
