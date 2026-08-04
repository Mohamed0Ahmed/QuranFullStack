# API controllers

HTTP entry points for `QuranDashboard.Api`. This folder owns route groups, HTTP status mapping,
and the `ApiResponse<T>` envelope; application handlers own use-case logic.

## Route families

- `Abwab/` — `api/abwab/sections`, `api/abwab/doors`, `api/abwab/relations`, `api/abwab/templates`,
  and `api/abwab/template-nodes` (twenty-one write
  routes: create/rename/reorder/delete on sections; create/edit/move/reorder/bulk-move/bulk-archive/delete/
  restore on doors; add-N and delete-one on relations; create/delete plus apply on templates; and
  add/edit/reorder/delete on template nodes) plus four reads — `api/abwab/tree` (one
  versioned snapshot: sections + doors, archived doors included and flagged, aliases, per-door
  direct-child and relation counts, per-section live-doors count, no paging),
  `api/abwab/doors/{doorId}/relations` (one door's visible relations, `404` for an unknown door,
  `200` with `[]` for a door with none), and `api/abwab/templates` + `api/abwab/templates/{templateId}`
  (the admin-authored door templates and one template's flat node list). Twenty-five routes in all. All
  routes are `Open` — this is the repository's first write surface, and it shipped to production still
  unauthenticated; the 2026-08-04 abwab note in [`docs/TESTING_DEBT.md`](../../../../docs/TESTING_DEBT.md)
  records that state and the feature that must close it. Optimistic
  concurrency is `uint xmin`, surfaced as `409` in the shared envelope. Section **delete** is the one
  door/section write that carries **no version token** — `DELETE api/abwab/sections/{id}` takes no
  body, because the server re-derives its only precondition (no live doors) itself. Its stale-version
  `409` is therefore never a stale-token comparison: it is the writer's translated answer to a lost
  interior race — a concurrent rename, reorder, or delete of the same section between the delete's own
  load and save (`Persistence/Writes/Abwab/EfAbwabSectionsWriter.cs:67`) — and the reload-and-retry
  message is accurate for every one of those races. Creating a door under a parent
  derives its section from that parent; a stated section that disagrees is a `400`, not a silent
  overwrite — on create and on restore alike. Creating or moving a door at **root** scope must name its
  section: there is no parent to derive one from, so an omitted section is a `400`
  («يجب تحديد قسم للباب الرئيسي»). Restore takes an optional destination — the body is
  `{ sectionId?, version }` — and returns the plain `AbwabDoorDto` like every other door write. Omitting
  `sectionId` means "back where it came from"; a root whose section was retired meanwhile has no such
  place and is a `400` («قسم الباب الأصلي محذوف، حدد قسمًا للاسترجاع»), while a stated section that no
  longer exists is a `404`. Restore also has a `409` of its own: a door whose **parent is still
  archived** cannot come back before it does («لا يمكن استعادة الباب لأن الباب الأب ما زال مؤرشفًا»
  — restore the parent first). Reorder carries a required `scope` in its body
  (`AbwabReorderScope`: `1` = Section, `2` = Global); an omitted or unrecognised value is a `400`
  («نطاق الترتيب غير صالح»), and so is `Global` on a nested door — `globalOrderValue` exists only
  for live roots, so there is no superset position for a child to take. The relation routes are the one write
  family that carries **no version token**: they touch no door row, so no `xmin` moves and the only
  `409` they can produce is the duplicate pair (same two doors + same type, either direction) —
  mapped by `AbwabDoorRelationsController`, which owns its own status mapping exactly as the other
  Abwab controllers own theirs. Self-relation and an archived endpoint are `400`, and so are an empty
  target list and an unrecognised `type`; a **recognised `direction` is required** for a
  `Comprehensiveness` relation and **forbidden** for the other two types, and a violation either
  way is a `400`. An unknown
  door id is `404`; the multi-target add is all-or-nothing. The template routes carry **no version
  token** either, and split across two controllers because nine actions on one would sit at the
  200-line threshold: `AbwabTemplatesController` owns the template list/detail/create/delete plus
  the apply, `AbwabTemplateNodesController` owns the four node writes — edit, reorder and delete
  under `api/abwab/template-nodes/{nodeId}`, plus the add, which hangs off its parent template at
  `POST api/abwab/templates/{templateId}/nodes` because that is where the new node's owner is
  named. A template's name is its root node's name, so there is no
  rename route — editing the root through the node edit **is** the rename. The root refuses
  reordering and deletion alike (`400`); deleting the template is the way. The apply copies the
  template root's **direct children** as new children of each target door — never the root itself
  (the ux-slice-g reversal; `Persistence/Writes/Abwab/README.md` holds the axiom) — so each target
  gains N doors, one per direct child, each with its own subtree beneath it. It is all-or-nothing:
  an empty target list is `400` (which is also how "never a root door" is enforced at the wire), an
  **empty-root template** (no live children) is a distinct `400` raised before any target row is
  read, an archived target is `400`, an unknown template or target is `404`, and a target that
  already has a live child named like any of the root's direct children fails the whole batch with
  one `409` naming every colliding **(target, child)** pair.
  None of the Abwab controllers
  carries `///` XML docs (root `CLAUDE.md` comment policy — see "Generated
  contract artifacts" below for what that means for the exported spec).
- `Access/` — `api/access/me`; the authenticated caller's provisioned user. Carries `[Authorize]`
  (authenticated-only) and get-or-create provisions the local user on first login (email verified
  server-side via the Logto Management API). The response includes `roleName` (null when no role);
  the configured owner email is bootstrapped to `Owner`/`Active`. This is the only endpoint that
  requires authentication — role-based named policies are registered but applied to nothing, so every
  other route stays publicly browsable. See `../README.md` (Authentication / Roles).
- `Dashboard/` — `api/dashboard/info` for app/version/environment metadata.
- `MushafReader/Ayahs/` — `api/mushaf/ayahs/{verseKey}/study`, `/similar-ayahs`, and `/mutashabihat`.
- `MushafReader/Catalogs/` — `api/mushaf/surahs` and `api/mushaf/study-sources` catalogs.
- `MushafReader/Pages/` — `api/mushaf/pages/{pageNumber}` page-reader endpoint.
- `MushafReader/Words/` — `api/mushaf/words/{wordLocation}/analysis`.
- `System/` — `api/health` health-check endpoint.
- `Words/` — `api/words/unique`, `api/words/roots`, `api/words/lemmas`,
  `api/words/stems`, and `api/words/word-types` explorer endpoints. Word-types grouped detail reads
  (`api/words/word-types/table/{kind}/{dimensionId}[/words|/ayahs|/surahs]`, Feature 023) live in the
  separate `WordTypeGroupedDetailsController`, which shares the `…/word-types/table` route base without
  growing `WordTypesController`. Route `{kind}` is the plural key `roots|stems|lemmas`; an unknown value
  is a `400`. All four actions carry the identical five-field scope (`type`, `childCode`, `case`, `tense`,
  `voice`); `words` and `ayahs` are paged (`page`/`pageSize`), while summary and `surahs` are single-shot
  and expose no paging parameter. Invalid kind/id/filter/paging → `400`, an absent scoped group → `404`,
  and an out-of-range page → `200` with an empty page.

## Splitting an oversized controller

Controllers have a 300-line hard limit (`../../../.architecture/BACKEND_STRUCTURE.md`). Two shapes
are in use, and they are not interchangeable:

`AbwabDoorsController` sits over the 200-line soft threshold and under the hard
limit, and stays whole deliberately: its eight actions are one resource's write surface (create,
edit, move, reorder, restore, delete, and the two bulk writes), every one of them a thin
outcome-to-status map with no logic to extract. Splitting it would divide one resource across two
classes — the failure the template split above was careful to avoid, since that split followed a
route family, not a line count. The trigger here is the 300-line hard limit, and the shape would
be a bulk-writes controller, because the bulk pair is the only subset with its own route segment.

- **A new route family → a new controller class.** `WordTypeGroupedDetailsController` is the
  precedent: it shares the `…/word-types/table` route base without growing `WordTypesController`.
- **An existing endpoint group → a `partial` part of the same class.** `RootsController` (list) +
  `RootsController.Details.cs` (per-root detail/drilldown) and `WordTypesController`
  (tree/words/table/scope-counts) + `WordTypesController.Details.cs` (per-word detail) follow this.
  Swashbuckle derives each operation's OpenAPI `tags` from the controller **class name**, so moving
  *existing* actions to a *new class* would retag them and change the exported spec. Keep the class
  name and the split is invisible to `swagger.json`. The part carrying the primary constructor owns
  the shared handlers, the `[ApiController]`/`[Route]` attributes, and the paging defaults; the other
  parts declare only `public sealed partial class <Name>` and their actions.

## Boundary

- Controllers delegate to Application handlers under `../../../application/`; they do not
  query EF Core, read files, or own business rules.
- API envelope contract lives in `../Contracts/ApiResponse.cs`; middleware and controllers
  should keep returning that shape consistently.
- API-local contracts live in `../Contracts/`; feature response DTOs returned today are also
  shaped by `../../../application/QuranDashboard.Application.Abstractions/**/Responses/`.
- Per-action work here is HTTP-only: route binding, query parsing, status-code selection,
  and mapping handler outcomes to `ApiResponse<T>`.

## Invariants

- Route bases here are public API surface; renaming a path segment is a contract change.
- Validation failures map to `400`, missing resources to `404`, and successful reads to `200`.
- Unhandled exceptions should stay outside controllers and flow through the global exception
  handler so the API still returns the shared envelope.
- Rate-limited requests are rejected by middleware **before** reaching a controller and return
  `429` with the same `ApiResponse` failure envelope plus a `Retry-After` header. The limiter is
  per-client-IP with separate general and health profiles; see `../README.md` (Rate Limiting) and
  `../../../.architecture/API_GUIDELINES.md` §14.

## Generated contract artifacts

- The OpenAPI spec for this API is exported offline to
  `Frontend/quran-dashboard-ui/openapi/swagger.json` by `Backend/scripts/export-swagger`
  (Swashbuckle CLI; no running server). Controller (endpoint) XML docs, where present, are the source of
  the endpoint descriptions in that spec; response DTO schemas are intentionally undocumented (bare typed
  schemas). Keep the controller docs accurate where they exist. **Resolved conflict:** the root `CLAUDE.md`
  comment policy (no `///` XML docs on controllers) wins over this convention where the two disagree. As of
  the Abwab slice **no controller in the tree carries `///` at all** — `78d70f04` stripped the last of them
  — so every exported `summary`/`description` is blank, not just `Abwab/`'s. This is accepted, not a defect:
  there is no external contract consumer, and the frontend generates payload types from the spec, never
  descriptions. Note the committed spec stayed stale for several commits after that strip, because
  `check-api-contract` compares regenerated-against-committed and cannot see a spec that nothing has
  regenerated; run it after any change that alters what the exporter reads.
- Frontend payload types are generated from that spec into
  `Frontend/quran-dashboard-ui/src/app/core/api/generated/` (models-only consumption).
  `Backend/scripts/check-api-contract` detects stale generated output. A static human-browsable
  reference can be built on demand with `npm run docs:api`; it is not committed.
- Typed non-200 response schemas (`[ProducesResponseType]` for 400/404/500) are a recorded
  follow-up. Until they land, **the exported spec documents no error codes at all** — there are
  no XML `<response>` tags either, since no controller in the tree carries XML docs (see above).
  This file and the nearest area README are the only description of a route's failure statuses.
  All error bodies use the shared `ApiResponse<T>` envelope. The five Abwab DELETE actions are the
  one place `[ProducesResponseType]` already exists: each declares its `204` success
  (`Abwab/AbwabDoorsController.cs:182`), so the spec documents the no-body `204` they actually send
  rather than the inferred `200`-with-`ObjectApiResponse` they never did; their error codes stay
  undocumented like everyone else's.

## Related

- API root: `../README.md`
- Contract envelope: `../Contracts/ApiResponse.cs`
- Read-model counterparts: `../../../infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/`
