# Feature: Abwab (الأبواب) — doors & sections management

Planning document. **Plan only — no code was changed and no Git action was taken while
writing it.**

> Path note: workspace convention is `docs/feature-XXX-feature-name/`. This folder omits the
> numeric prefix on the user's explicit instruction.

---

## 0. Guard result — the plan is split into two slices

The requested six-phase spine estimates at **~51 real tasks**, well past the ~30 guard. Per the
hard guard the work is split, and **only Slice A is planned in full below**. Slice B is scoped
and estimated, not detailed; it gets its own plan document when Slice A merges.

| Slice | Phases | Tasks | Ends with |
|---|---|---|---|
| **A** (planned here) | 1 — schema/migration/domain · 2a — smoke-harness `Method` · 2b — write path + endpoints + write smokes · 3 — tree-snapshot read + contract regeneration | **27** | A complete, smoke-tested `/api/abwab` API and a regenerated frontend contract. **Zero frontend change.** |
| **B** (outline in §8) | 4 — FE state/data-access/tree/modal · 5 — cards/bulk/move/archive/sections UI · 6 — e2e flows + docs | ~24 | The page, matching the contract, with e2e flows and doc amendments. |

The seam is phases 3/4, not 4/5: phase 4 cannot begin until phase 3 regenerates
`openapi/swagger.json` → `core/api/generated/`, so the contract artifact is the handoff.
It also means "zero dead controls" is trivially true in Slice A — nothing new is exposed.

---

## 1. Objective

Build the first product-authoring surface in the tree: a doors (أبواب) and sections
management page that lets an admin organize the Quran classification outline — create,
rename, describe, nest, reorder, move, bulk-move, archive and restore doors, grouped into
user-managed sections.

This is also the first **write** surface in the repository. Every write convention it
establishes — service seam, outcome mapping, concurrency, audit-seed columns, smoke
treatment of non-GET routes — becomes the precedent for every later feature.

## 2. Scope (Slice A)

- Three tables: `abwab_sections`, `abwab_doors`, `abwab_door_aliases`, with audit-seed and
  soft-delete columns from migration one.
- Eleven write endpoints and one read endpoint under `/api/abwab`.
- Optimistic concurrency via Postgres `xmin`, surfaced as `409` in the shared envelope.
- Extension of the smoke harness to represent non-GET routes, plus dedicated write smokes.
- Regenerated OpenAPI contract and frontend model types.

## 3. Non-goals

- **No authentication and no authorization** in this slice. The routes are `Open`.
  See §9 (Release) — this must not reach production unprotected.
- **No audit system.** The audit-seed columns plus one write path per aggregate are the
  preparation for one, not one.
- **No protection, relations, or templates.** No dead controls for them (§5).
- No paging on the read — one complete snapshot.
- No frontend change at all in Slice A.

## 4. Locked decisions

| Area | Decision |
|---|---|
| Naming | `abwab` everywhere: route base `/api/abwab`, nav route `/abwab`, feature folder `features/abwab`, tables `abwab_sections` / `abwab_doors` / `abwab_door_aliases`, testid prefix `abwab-` |
| Audit-seed columns | On all three tables: `created_at`/`created_by`, `updated_at`/`updated_by`, `approved_at`/`approved_by`, `deleted_at`/`deleted_by`. "Who" columns nullable until auth arrives. Dates populated now, in application code |
| Soft delete | `deleted_at IS NOT NULL` = archived. Archive tab lists and restores |
| Aliases | Child table `abwab_door_aliases` (`id`, `door_id`, `value`, audit-seed + soft-delete columns) |
| Column naming | Explicit `HasColumnName` / `ToTable` on every property and entity — **no global snake_case convention exists** in this DbContext |
| Seeds | No `HasData`. Doors are user data |
| Concurrency | `uint xmin` `.IsRowVersion()` on doors **and** sections. Conflicting move/reorder/edit → `409` in the `ApiResponse` failure envelope |
| Write path | One application-service seam per aggregate; transaction per command; exhaustive outcome switch → controller mapping (`LemmasController.cs:65-76` is the shape reference). `400` validation / `404` missing / `409` conflict |
| Name uniqueness | Per-sibling among non-deleted rows |
| Depth | No limit. **Cycle guard on move is mandatory** |
| Ordering | Every write resequences siblings to `1..N`. Reads tolerate gaps |
| Archive semantics | Archiving a door archives its whole subtree in one operation; the UI confirms with a count |
| Section delete | With live doors → `409`. Sections modal = list / add / rename / delete-empty |
| Move picker | Section first, then an expandable door tree (nest anywhere). "as main door" is scoped to the picked section |
| Read model | One complete tree snapshot (sections + doors + aliases + counts), versioned, no paging |
| Smoke | `SmokeRoute` gains `Method`. Write routes are catalogued as **parity-only markers**, NOT dispatched by the generic sweep. Each write endpoint gets a dedicated smoke test. All routes `Open` |
| e2e | Doors flows are required (DoD). The read-only invariant is amended deliberately; flows self-clean inside a sandbox section they create and archive |
| Branch | Feature branch off `dev` → PR into `dev`. Never `main` |

## 5. Design-contract deltas

The implementation matches `docs/design-preview/abwab-tree-concept.html` **with these two
lists applied**.

### 5.1 Deletion list — remove before implementing

| Contract site | Remove |
|---|---|
| `:250-251` | Sidebar `🔗 العلاقات` and `🛡️ الحماية` buttons |
| `:277-278` | The same two entries in the context menu |
| `:110-111` | `.flag.protected` and `.flag.rel` style rules |
| `:436-437` | Per-node rendering of both flags |
| `:598` | The `protected` flag on cards |
| `:344, 346, 350` | Seed data carrying `protected:true` / `rel:true` |
| `:207-211` | The «الأبواب الرئيسية» tab (tabs are «كل الأبواب» + real sections) |

Note `.flag.protected` uses `#f3ede0` / `#8a6d1d` / `#e5d9b8` — a warm-gold family with **no
token equivalent** in `_tokens.scss`. Deleting the flags removes that mapping problem.

### 5.2 Addition list — the contract is short of shippable here

| Gap | Required addition (Slice B, phase 4) |
|---|---|
| Tree has no a11y story: `.node/.row/.chevron/.children` are divs with click handlers — no `role="tree"`/`treeitem`, no `aria-expanded`/`aria-level`/`aria-selected`, no roving tabindex, and `row.ondblclick` (`:446`) as an expand affordance | Full tree ARIA + roving tabindex + keyboard model per `UI_STYLE_SYSTEM.md` §12. Size the phase-4 tree task for this, not for the mock's div soup |
| Two allowed-green violations (`UI_STYLE_SYSTEM.md:628-658`) | `.side-act.toggle.on` solid green fill (`:76`) and `.chip` green fill (`:158-159`) → tint + `--qd-accent-text` + hairline, and compose `qd-chip` |
| No dirty guard, no inline error surface on the add/edit modal | Both added |
| Raw hexes throughout | Bind `--qd-*` tokens only; dark theme is gold-accented (`_themes.scss:25-34`) and hardcoded greens would break it |

**Conflict rule:** where the contract and the locked decisions disagree, the decisions win and
the conflict is reported in the phase's completion note.

---

## 6. Slice A — phases

Every phase is one commit (2b is one commit after 2a). The tree builds and the relevant tier
is green at each commit boundary.

### Phase 1 — schema, migration, domain (5 tasks)

**Files**

- `Backend/domain/QuranDashboard.Domain/Abwab/AbwabSection.cs`,
  `AbwabDoor.cs`, `AbwabDoorAlias.cs` — a new top-level domain area beside `Access/` and
  `Quran/`.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Abwab/`
  — one `IEntityTypeConfiguration<T>` per entity.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs`
  — three `DbSet`s beside `AccessUsers`/`AccessRoles` (`:52-53`).
- `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/` — generated only.
- `Backend/tests/QuranDashboard.Tests/Abwab/AbwabSchemaTests.cs`.

**Tasks**

- **T101** — Three entities. `AbwabDoor` carries `Id`, `SectionId` (nullable — a door may sit
  outside every section, which is what makes «كل الأبواب» coherent), `ParentId` (nullable),
  `Name`, `Description`, `RepresentativeAyahText` (**free text, never an FK or a verified
  Quran reference**), `OrderValue`, the eight audit-seed columns, and `uint Version` for xmin.
  `AbwabSection`: `Id`, `Name`, `OrderValue`, audit-seed, `Version`.
  `AbwabDoorAlias`: `Id`, `DoorId`, `Value`, audit-seed (no xmin — aliases are replaced
  wholesale under the door's own token).
- **T102** — Three configurations. `ToTable` + explicit `HasColumnName` on **every** property.
  Self-referencing FK `ParentId → abwab_doors.Id` with `DeleteBehavior.Restrict` (archive is
  soft, so a cascade would be wrong), `SectionId → abwab_sections.Id` `Restrict`,
  `DoorId → abwab_doors.Id` `Cascade` (an alias has no life without its door).
  `builder.Property(x => x.Version).IsRowVersion()` bound to the Postgres `xmin` system column.
  Indexes: `(section_id, parent_id, order_value)`; `parent_id`; `deleted_at`; the per-sibling
  uniqueness index (below); `door_id` on aliases.

  **Uniqueness index — the likeliest defect in this phase.** The naive form
  `UNIQUE (parent_id, name) WHERE deleted_at IS NULL` **does not constrain root doors**: their
  `parent_id` is `NULL`, and in Postgres NULLs do not collide in a unique index, so two root
  doors named «العلم بالله» both insert cleanly — the constraint silently fails exactly where
  the outline is most visible. Use `UNIQUE NULLS NOT DISTINCT (section_id, parent_id, name)
  WHERE deleted_at IS NULL` (PostgreSQL 15+; the tree targets `postgres:16-alpine`, so this is
  available). If that form cannot be expressed through the EF fluent API, fall back to two
  partial indexes — one for `parent_id IS NOT NULL`, one for `parent_id IS NULL` keyed on
  `(section_id, name)`. A test that inserts two same-named root doors is mandatory (T105/T215).
- **T103** — `DbSet<AbwabSection> AbwabSections`, `DbSet<AbwabDoor> AbwabDoors`,
  `DbSet<AbwabDoorAlias> AbwabDoorAliases`.
- **T104** — **STOP CONDITION.** Generate the migration with EF tooling only, and only on
  explicit user go-ahead (`Backend/CLAUDE.md`: do not hand-write migrations, do not create
  `.cs`/`.Designer.cs`/snapshot files manually, only add when explicitly requested). Report
  migration name, generated files, build status, and whether `database update` ran.
  Pull the exact Npgsql `xmin` configuration from official docs during this task rather than
  writing it from memory.
- **T105** — Schema tests: every expected column name exists; the four indexes exist; **the
  generated migration adds no column for the concurrency token** (that absence is the proof it
  is bound to Postgres's system column and not a real one); a stale-token update raises
  `DbUpdateConcurrencyException`.

**Verification**

```bash
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab."
```

**Budget** — build ~40 s; focused namespace ~2 s (Testcontainers-backed schema tests: ~30 s
first run for the image pull).

---

### Phase 2a — smoke harness learns non-GET (3 tasks)

Isolated on purpose: the parity gate fails the moment a non-GET route registers without a
`Method`-aware catalog, so the harness change lands **before** any route exists and is
reviewable on its own.

**Files** — `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs`,
`SmokeCoverageParityTests.cs`, `SmokeRoutePipelineTests.cs`.

**Tasks**

- **T201** — `SmokeRoute` gains `HttpMethod Method` (defaulting to GET so all 48 existing
  entries are untouched) and a `ParityOnly` marker. Document what `ParityOnly` means directly
  on the field: *the route is catalogued so the parity gate sees it, and is deliberately not
  dispatched by the generic sweep, because the sweep's premise is that it never writes* — so a
  later reader does not "fix" it by dispatching it.
- **T202** — `CatalogRouteKeys()` (`SmokeCoverageParityTests.cs:41`) keys by the entry's own
  method instead of the hardcoded `HttpMethod.Get.Method`; update the two comment blocks
  (`:38-40`, `:31`) that anticipate exactly this change. `SmokeRoutePipelineTests` skips
  `ParityOnly` entries and dispatches the entry's method for the rest.
- **T203** — Confirm all 48 existing entries still resolve identically and the smoke tier is
  green with no count change.

**Verification**

```bash
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

**Budget** — smoke tier ~48 s, still 74 tests at this commit.

---

### Phase 2b — write path, endpoints, write smokes (12 tasks)

**Route surface (11 writes)**

| Method | Route | Outcomes |
|---|---|---|
| POST | `api/abwab/sections` | 201 / 400 / 409 duplicate name |
| PUT | `api/abwab/sections/{id:int}` | 200 / 400 / 404 / 409 stale, duplicate |
| DELETE | `api/abwab/sections/{id:int}` | 204 / 404 / 409 section holds live doors, stale (see §13.3) |
| POST | `api/abwab/doors` | 201 / 400 / 404 parent or section / 409 duplicate sibling name |
| PUT | `api/abwab/doors/{id:int}` | 200 / 400 / 404 / 409 stale, duplicate |
| POST | `api/abwab/doors/{id:int}/move` | 200 / 400 / 404 / 409 stale, cycle, duplicate at target |
| POST | `api/abwab/doors/{id:int}/order` | 200 / 400 / 404 / 409 stale |
| POST | `api/abwab/doors/bulk-move` | 200 / 400 / 404 / 409 |
| POST | `api/abwab/doors/bulk-archive` | 200 / 400 / 404 / 409 |
| DELETE | `api/abwab/doors/{id:int}` | 204 / 404 / 409 stale — archives the subtree |
| POST | `api/abwab/doors/{id:int}/restore` | 200 / 404 / 409 stale, parent still archived |

**Files** — `Backend/application/QuranDashboard.Application/Abwab/Commands/**`,
`Backend/application/QuranDashboard.Application.Abstractions/Abwab/**`,
`Backend/infrastructure/.../Persistence/Writes/Abwab/**` (a new sibling to `Persistence/Reads/`),
`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs` +
`AbwabDoorsController.cs`, `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs`,
`Backend/application/QuranDashboard.Application/DependencyInjection.cs`,
`Backend/tests/QuranDashboard.Tests/Abwab/**`,
`Backend/tests/QuranDashboard.Tests/Smoke/{SmokeRouteCatalog,SmokeApiFixture}.cs`,
`Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteTests.cs`.

**Tasks**

- **T204** — Request/response contracts in `Application.Abstractions/Abwab/` (never Domain
  entities on the wire, `API_GUIDELINES.md` §8) + Arabic `ApiMessages` constants for every
  outcome, grouped with the existing families in `ApiMessages.cs`.
- **T205** — Sections write service + three handlers (create / rename / delete-empty) with
  closed outcome hierarchies.
- **T206** — Doors create + edit. Edit replaces the alias set wholesale under the door's own
  concurrency token; removed aliases are soft-deleted, not hard-deleted.
- **T207** — Move (with the cycle guard: a door may not become its own descendant → `WouldCycle`)
  and reorder (resequence siblings `1..N` in the same `SaveChanges`).
- **T208** — Bulk move + bulk archive. **All-or-nothing, intended:** every touched row's
  concurrency token is checked, so one stale row fails the whole operation with `409`. State
  this in the handler comment — it reads as a bug otherwise.
- **T209** — Archive subtree (one operation, one `SaveChanges`) + restore. Restoring a door
  whose parent is still archived is a `409`, not a silent re-parent.
- **T210** — Two controllers, sealed, primary-constructor handler injection, exhaustive outcome
  switch with a throwing default (the shape at `LemmasController.cs:65-76`; that throwing
  default is what the smoke "no 500" assertion protects). Status mapping 400/404/409; `201`
  carries the created resource; `204` on the two deletes. **No `///` XML docs** (root
  `CLAUDE.md`). 300-line hard limit per controller — split by aggregate, which the two-class
  shape already does.
- **T211** — `AddScoped<...Handler>()` registrations in `Application/DependencyInjection.cs`,
  following the existing one-line-per-handler style.
- **T212** — `SmokeApiFixture.ResetAbwabAsync()` — `TRUNCATE abwab_door_aliases, abwab_doors,
  abwab_sections RESTART IDENTITY CASCADE`. **A separate method, not an extension of
  `ResetAsync`**: `ResetAsync` currently truncates `users` only (`SmokeApiFixture.cs:73-77`)
  and is called by `SmokeAuthPipelineTests` and the two `Api/Access` files; widening it would
  change behavior for every existing smoke and muddy the "never written to" premise.
- **T213** — Eleven catalog entries, `Access = SmokeRouteAccess.Open`, `ParityOnly = true`,
  in controller declaration order per the catalog's own convention
  (`SmokeRouteCatalog.cs:88-90`). No `Seeded` — there is no canonical dump behind user data.
- **T214** — `SmokeAbwabWriteTests`: real POST/PUT/DELETE with bodies, each test cleaning up
  after itself via `ResetAbwabAsync`. Per endpoint: the success status + envelope shape, a
  malformed body → `400` in the failure envelope, a stale `xmin` → `409`, and a missing id →
  `404`. Plus the two conflict rules: section-delete-with-doors → `409`, move-into-descendant
  → `409`.
- **T215** — Behavior tests for the write rules independent of HTTP: per-sibling uniqueness,
  cycle guard, `1..N` resequencing (including the gap-tolerant read), subtree archive count,
  restore-under-archived-parent rejection, alias replace semantics.

**Verification**

```bash
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab."
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
# run the smoke filter TWICE in a row and diff the results — a single green run
# does not prove the write smokes left the schema clean for the generic sweep
```

**Budget** — smoke tier ~48 s → ~75-90 s with the write smokes; Api slice ~10 s; focused
Abwab namespace ~30-45 s (Testcontainers).

---

### Phase 3 — tree snapshot read + contract regeneration (7 tasks)

**Files** — `Backend/application/QuranDashboard.Application/Abwab/Queries/GetAbwabTree/`,
`Backend/application/QuranDashboard.Application.Abstractions/Abwab/Responses/`,
`Backend/infrastructure/.../Persistence/Reads/Abwab/` (+ its `README.md`),
`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTreeController.cs`,
`Backend/tests/QuranDashboard.Tests/Abwab/AbwabTreeReadTests.cs`,
`Frontend/quran-dashboard-ui/openapi/swagger.json`,
`Frontend/quran-dashboard-ui/src/app/core/api/generated/**`,
`docs/api-reference/index.html`,
`Backend/api/QuranDashboard.Api/Controllers/README.md`, `TESTING_STRATEGY.md`.

**Tasks**

- **T301** — Read model + `GetAbwabTreeHandler`. One snapshot: all sections, all doors
  (archived included, flagged), all aliases, per-door **direct-child count** and per-section
  **doors-in-scope count**, plus a snapshot `version` = **`max(updated_at, deleted_at)` across
  the three tables** — chosen over a monotonic counter because it needs no new column and no
  extra write. `AsNoTracking`.
- **T302** — Response DTOs in `Application.Abstractions/Abwab/Responses/`.
- **T303** — `GET api/abwab/tree` (single action, no paging) + its catalog entry. This one **is**
  swept: it derives `200` with an empty snapshot against an empty schema, whether or not rows
  exist, so it is order-independent by construction. No `Seeded`.
- **T304** — Read tests: empty snapshot, nested tree shape, archived rows flagged not omitted,
  counts correct, gap-tolerant ordering.
- **T305** — `Backend/scripts/export-swagger` → regenerate `openapi/swagger.json`, the frontend
  models under `core/api/generated/`, and `docs/api-reference/index.html`; then
  `Backend/scripts/check-api-contract` to prove nothing is stale
  (`Controllers/README.md:68-79`). **This is the Slice A → Slice B handoff artifact** — phase 4
  cannot start without it.
- **T306** — READMEs in the same change (root `CLAUDE.md` rule): the new route family in
  `Controllers/README.md`, a new `Persistence/Reads/Abwab/README.md`, and the domain-area note.
- **T307** — `TESTING_STRATEGY.md` §5: add `Tests.Abwab` to the namespace list and **re-measure**
  the three-way partition identity (currently `1,040 + 617 + 74 = 1,731`). A new namespace lands
  in the no-pipeline set by default and the smoke count rises; the doc says re-verify whenever a
  namespace is added, and arithmetic is not a measurement.

**Verification**

```bash
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab."
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build   # full, for §5 re-measure
Backend/scripts/check-api-contract
```

**Budget** — full backend suite ~5 m 19 s today, rising with the new families; contract check
< 30 s.

---

## 7. Slice A tier placement

`TESTING_STRATEGY.md` §4 fires two rows; the stricter governs:

- "API endpoint added/changed, or auth/middleware/binding/contract change" → **phase tier A +
  `Tests.Api.*` + Smoke; pre-PR C + `Tests.Api.*` + Smoke**.
- "EF migration affecting only non-pipeline tables" → A + affected schema tests; pre-PR C.

**Tier D does not fire** — the three tables are non-pipeline, so no Quran pipeline family runs.

Evidence rules: **there is no CI** (§8) — every tier is a local gate and "CI is green" is never
available. Every phase's evidence must state whether `Tests.Smoke.Data` ran or skipped (it
self-skips when `resources/db-dumps/quran-canonical/` is absent; a stale dump fails loud).

---

## 8. Slice B — outline only (~24 tasks, separate plan document)

### Phase 4 — frontend state, data-access, tree view, modal (~9)

Nav rename `gates` → `abwab` in `nav-items.ts:14` (key, route, `labelEn`; `labelAr` is already
«الأبواب»). **This is free and safe:** `app.routes.ts:11-19` maps every non-excluded nav item to
a root placeholder path, `route-paths.ts` exports no `GATES_ROUTE_PATH`, `app.routes.spec.ts`
asserts only "no activation guard anywhere" (`:55-56`), `shell-nav.e2e.ts` uses
`nav-link--mushaf`, and `placeholder-routes.e2e.ts` probes `/mutashabihat` — **no test asserts
the gates key**. Then: add `abwab` to the placeholder exclusion filter, declare the real
lazy root route, `features/abwab/` per `FRONTEND_STRUCTURE.md:365-373`, `abwab.api.ts`
(plain `@Injectable({providedIn:'root'})` + `HttpClient`, the `stems.api.ts:25-55` shape) with
its `setupApiTestBed` spec, snapshot facade + cache + url-sync, the tree component **with the
full ARIA/roving-tabindex model from §5.2**, and the add/edit modal with dirty guard and inline
errors. Labels via TDZ getters; testids `abwab-*` English slugs.

### Phase 5 — cards, bulk, move, archive, sections UI (~8)

Cards drill-down + breadcrumbs; bulk-select mode with bulk move/archive; the two-stage move
picker; the archive view state; the sections modal; alias chips composing `qd-chip`; the two
allowed-green fixes; URL state carrying section / view / archive / selection.

### Phase 6 — e2e flows and docs (~7)

Flows: add section · add root + child door via modal incl. alias chips · rename ·
number-reorder · move single + bulk · archive + restore · search by alias · cards drill-down ·
bulk-select. **Two** doc amendments, not one (§9). Plus `TESTING_STRATEGY.md` §6 frontend counts
(today 169 files / 1,938 tests) and the frontend README updates.

---

## 9. Risks and stop conditions

**R1 — The e2e write decision deviates from a documented invariant in two files, not one.**
`e2e/README.md:39-41` says "read-only flows and loose count assertions only". But
`TESTING_STRATEGY.md` §6 goes further: *"do not add write flows to it **without first moving it
onto an isolated database**."* The locked decision is a self-cleaning sandbox against the **local
dev DB**, which fails §6's stated precondition, not just the README's sentence. `TESTING_STRATEGY.md`
is the declared single source of truth for test selection (root and both project `CLAUDE.md`s).
Decisions win — this is recorded as a deliberate, named deviation — but phase 6 carries **an
amendment task for `TESTING_STRATEGY.md` §6 alongside the `e2e/README.md` one**. §6 must not be
amended by implication.

**R2 — Order dependence between write smokes and the generic sweep.** `SmokeRoutePipelineTests`
does not call any reset and relies on the schema never being written
(`SmokeRoutePipelineTests.cs:5-7`). Write smokes share `SmokeCollection`. `GET api/abwab/tree`
is safe either way, but any future id-scoped read would derive `404` empty and `200` if a write
smoke left row id 1 behind. Mitigations: `ResetAbwabAsync` per write smoke (T212/T214), no
id-scoped read entry in the catalog for now, and **running the smoke filter twice in a row** as
the verification — a single green run does not prove order-independence.

**R3 — Parity gate must be green at every commit.** This is why 2a is separated from 2b. Any
commit that registers a route without its catalog entry fails
`EveryRegisteredRoute_HasACatalogEntry` and is not a valid commit boundary.

**R4 — Contract/decision conflicts.** Decisions win; the conflict is reported, never silently
resolved. The known set is §5.

**R5 — STOP: migration generation.** `Backend/CLAUDE.md` forbids hand-written migrations and
requires explicit user request before adding one. T104 halts for go-ahead.

**R6 — STOP: `dotnet ef database update`.** Never run without explicit request. Phase 1's
verification assumes a migrated local database already exists or that the user has approved
applying it.

**R7 — Concurrency token wiring is easy to get subtly wrong.** The proof is the *absence* of a
new column in the generated migration (T105), not a passing test.

**R8 — Assumption I made, not a locked decision: `SectionId` is nullable.** §4 says the move
picker scopes "as main door" to the picked section, which reads as *every* door belonging to a
section. T101 instead makes `SectionId` nullable, so a door can sit outside every section — that
is what makes the «كل الأبواب» tab a real superset rather than a synonym for "all sections", and
it avoids forcing a synthetic default section at migration time. **If you want every door to
belong to a section, say so and this changes:** `SectionId` becomes required, the migration needs
a seeded default section (contradicting the no-`HasData` decision), section-delete-with-doors
semantics get sharper, and the uniqueness index drops its `section_id` component. Decide before
T104 generates the migration — it is expensive to reverse afterwards.

**R9 — Unprotected writes.** See §10.

---

## 10. Release posture

The routes are `Open`. `app.routes.ts:26-29` records the deliberate public-browse posture and a
`roleGuard` exists at `core/auth/role.guard.ts` attached to nothing. **This feature must not
reach production before write protection lands.** Concretely: `dev → main` is the release
boundary and happens only on explicit request (root `CLAUDE.md`), so the gate is procedural —
do not include this feature in a release merge until the auth slice attaches a policy to the
eleven write routes. Record the same in the PR description.

---

## 11. Acceptance criteria

**Slice A**

- All eleven write routes and the tree read exist with the outcome mapping in §6, phase 2b.
- Catalog is 48 → 60 entries; `SmokeCoverageParityTests` green in both directions; the smoke
  filter produces **identical results on two consecutive runs**.
- Every write endpoint has a dedicated smoke asserting envelope + status contract, including a
  malformed body → `400` and a stale `xmin` → `409`.
- The generated migration adds **no** column for the concurrency token.
- `check-api-contract` reports no staleness; `core/api/generated/` and
  `docs/api-reference/index.html` are regenerated.
- `TESTING_STRATEGY.md` §5 partition identity re-measured, not computed.
- Tier C + `Tests.Api.*` + Smoke green pre-PR, with the `Tests.Smoke.Data` ran/skipped statement
  in the evidence.
- READMEs updated in the same change.
- Root `CLAUDE.md` "Active Spec Kit Feature" records the open feature pointing at
  `docs/feature-abwab-doors/plan.md`.

**Slice B** (recorded here so the bar is set now)

- Full Vitest suite + `npm run build` green; `TESTING_STRATEGY.md` §6 counts updated.
- `npm run e2e` green with the doors flows, self-cleaning, both doc amendments landed.
- **Zero dead controls** — nothing from the §5.1 deletion list ships.
- URL restore works: section, view mode, archive view and selection survive refresh and
  Back/Forward.
- The approved contract is visually matched — **the user's own headed run is the final gate**,
  not any automated assertion.

---

## 12. Task-count summary

| Phase | Tasks |
|---|---|
| 1 — schema, migration, domain | 5 |
| 2a — smoke harness `Method` | 3 |
| 2b — write path, endpoints, write smokes | 12 |
| 3 — tree read + contract regeneration | 7 |
| **Slice A total** | **27** |
| 4 — FE state, data-access, tree, modal | ~9 |
| 5 — cards, bulk, move, archive, sections UI | ~8 |
| 6 — e2e flows + docs | ~7 |
| **Slice B estimate** | **~24** |
| **Full feature** | **~51** |

---

## 13. Post-review amendments (whole-branch engineering review)

Three points the phases left open or got wrong, resolved at the pre-merge review. Recorded here because
§5's conflict rule requires a conflict to be reported, never silently resolved.

### 13.1 Restore returns only what the matching archive claimed

§4 locks "archiving a door archives its whole subtree", and the implementation correctly archives only
**live** descendants — a descendant archived earlier on its own was never part of that claim. Restore
originally gave back *every* archived descendant, resurrecting rows the user had archived deliberately.
Restore now matches descendants on the archive's own `deleted_at` timestamp, captured before the door's is
cleared. Symmetry is per-operation, not per-subtree.

### 13.2 Restore renumbers, and detaches from an archived section

Two consequences of restore being the only write that moves a row back **into** a scope:

- It renumbers that scope to `1..N`, like every other write (§4). Without this the restored door collided
  with whichever sibling inherited its `OrderValue` when archive renumbered the scope to `1..N-1`.
- If the door's section was archived meanwhile — legal, since a section is deletable once it holds no
  *live* doors — the door and everything restored with it are detached to `SectionId = null` rather than
  refused. **This is a decision, not a mechanical fix:** sections have no restore route in Slice A, so a
  `409` would strand the door permanently, and "outside every section" is already a first-class state
  (§R8). Revisit if Slice B adds section restore.

### 13.3 Section delete answers 409 on a lost concurrency check

`AbwabSection.Version` is rowversion-mapped, so the soft-delete UPDATE carries `AND xmin = @original` and a
concurrent rename makes it affect zero rows. That surfaced as an untranslated `DbUpdateConcurrencyException`
→ `500` — the one write of eleven leaking an EF type past the Infrastructure seam. It now answers `409`
with `AbwabSectionStaleVersion` like every other conflict. The route still takes **no** client token: the
race is between the writer's own read and its save, so there is nothing for a caller to send.
