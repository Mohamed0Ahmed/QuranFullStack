# Plan — feature-abwab-mandatory-section: every door must belong to a section

- **Status:** planned, not started. Normal implementation plan (explicitly NOT Spec Kit).
- **Base branch:** `dev`. All work on a feature branch off `dev`; PR into `dev` only.
- **Planning basis:** the read-only inspection performed 2026-08-02 in-session; every
  file:line below was re-verified against the working tree on that date.
- **Authoritative decisions:** D1–D10 as locked by the user (restated inline where each
  phase implements them). Planning-time ambiguities are resolved and folded in: the
  bulk-move auto-select rule was approved as written, and the restore section semantics
  were corrected (child derives from the live parent; root re-section cascades to
  archived descendants) — both 2026-08-02.

## 0. Non-goals (locked)

- The archive confirmation redesign (a later slice consumes the same confirm primitive).
- Modal width changes; badge header labels; removing the tracking-data panel;
  relations-with-the-tree; search highlight-instead-of-filter.
- Any section restore route. Any widening of the section-delete predicate (D4).
- Any auth or write-protection work.
- The `EfAbwabDoorsWriter` 816>600-line refactor — **pre-existing debt, logged in §12,
  not done here.**
- Any change to global-order code (`MaintainGlobalOrderAsync`, `ResequenceGlobal`,
  reorder scope logic) — D6; inspection confirmed no global-order code reads section
  (`EfAbwabDoorsWriter.cs:143-144`, `:469-471`).
- Retrofitting the templates-page inline `role="alertdialog"` confirms
  (`abwab-templates-page.component.html:151`, `:180`) onto the new primitive.

## 0b. Release-gate note (record only — no action in this feature)

`main` carries zero abwab migrations (verified: newest migration on `main` is
`20260718142612_AddAccessRoles`; all four abwab migrations exist only on `dev`), so the
NOT NULL migration cannot fail on production data **as far as the repo can prove**. The
repo cannot prove nobody ran dev migrations manually against the production connection
string. **Mandatory release-gate item before any `dev → main`: query production
`__EFMigrationsHistory` and confirm it ends at `20260718142612_AddAccessRoles` (or, if
later, contains no abwab migrations).** Not a blocker for this feature.

## 1. Verified current-state anchors

| Fact | Where (verified) |
|---|---|
| `abwab_doors.section_id` is `integer NULL`, FK Restrict | `Backend/domain/QuranDashboard.Domain/Abwab/AbwabDoor.cs:7-9`; `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Abwab/AbwabDoorConfiguration.cs` (ToTable `:9`, FK `:70-74`) |
| The 7 `SectionId` write sites | `EfAbwabDoorsWriter.cs:26, 127, 280, 443, 464, 562`; `EfAbwabTemplateApplyWriter.cs` `NewDoor` (level-1 call `:127`) |
| Create/section resolution | `EfAbwabDoorsWriter.cs:502` (`ResolveCreateSectionAsync`), `:528` (`EnsureSectionExistsAsync` — no-op on null), `:573` (`ResolveTargetSectionAsync`) |
| Restore detach fallback | `EfAbwabDoorsWriter.cs:440-443` (root), `:462-464` (descendants), `:494` (`AbwabRestoredDoorDto(..., sectionWasArchived)`) |
| Section delete = 409, live-doors-only, soft delete | `EfAbwabSectionsWriter.cs:59-63`; `AbwabSectionsController.cs:72-73`; message `ApiMessages.cs:117` |
| Existing Arabic 400 precedent | `ApiMessages.cs:134` + `AbwabDoorsController` mapping of `SectionParentMismatch` |
| Restore contract today | `RestoreDoorBody.cs:3` (`RestoreDoorBody(uint Version)`); `IAbwabDoorsWriter.cs:71`; `RestoreDoorHandler.cs:12-43`; `AbwabRestoredDoorDto.cs:6` |
| Tree DTO projection | `EfAbwabTreeReader.cs:50-51` (`d.SectionId` into `AbwabTreeDoorDto`); root-count grouping `:38-39` |
| Frontend null-section sites | `abwab-door-modal.component.ts:164`; `abwab.api.ts:33-37`; `abwab-move-picker.component.html:27-28` + `.ts:111-127`; `abwab-tree.builder.ts:99-102, 128, 227`; `abwab-write.controller.ts:157-160`; `abwab-page.component.ts:392` (direct restore call); `abwab.models.ts:154` |
| Labels to remove | `models/abwab.labels.ts:188` (`noSectionOption`), `:204` (`restoreDetachedAnnouncement`) |
| E2E pins | `e2e/abwab-archive.e2e.ts:125` (detach announcement); `e2e/abwab-structure.e2e.ts:31` (section-delete 409 message — **stays**); `e2e/fixtures/abwab.ts:26` (fixture already requires `sectionId: number`) |
| Smoke fixture abwab reset covers 3/6 tables | `SmokeApiFixture.cs:93-94` |
| Smoke helper defaults sectionless | `SmokeAbwabWriteTests.cs:1071` (`CreateDoorAsync(... int? sectionId = null ...)`) |
| Script guard grammar | `Backend/scripts/drop-db:7-9` (`--yes`); `Backend/scripts/create-smoke-dump:34-35, 99` (non-local refusal) |
| CASCADE closure of the six abwab tables | Only the three abwab migrations declare FKs whose `principalTable` is an `abwab_*` table, and every referencing table is itself one of the six (verified by grep over `Migrations/*.cs`). No `quran_*`, `users`, or `roles` table references any abwab table. |
| Confirm-primitive registry | `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` §17 "Component contracts" (line 683); shared primitives live in `src/app/shared/ui/` |
| Planning-folder convention | Sibling folders `docs/feature-abwab-doors/`, `docs/feature-abwab-global-order/`, `docs/feature-abwab-relations/`, `docs/feature-abwab-templates/` each hold a `plan.md` — hence this document's path |

## 2. Design

### 2.1 Contract changes (complete list)

Request DTOs — **all keep `int?` (D3)**:
- `RestoreDoorBody(uint Version)` → `RestoreDoorBody(int? SectionId, uint Version)`.
  `RestoreDoorCommand` gains the same field. This is the only request-shape change;
  `CreateDoorCommand`, `MoveDoorBody`, `BulkMoveDoorsCommand` are unchanged in shape.

Response DTOs:
- `AbwabDoorDto.SectionId`: `int?` → `int`.
- `AbwabTreeDoorDto.SectionId`: `int?` → `int`; **new property `bool SectionRetired`**
  (see §2.3).
- `AbwabRestoredDoorDto` **deleted** (D5). `POST .../restore` returns
  `ApiResponse<AbwabDoorDto>` like every other door write.

No route is added, removed, or retemplated ⇒ **`SmokeRouteCatalog` gets no new entry**
(parity keys on method + template only; `SmokeCoverageParityTests`). The restore entry
at `SmokeRouteCatalog.cs:279` stays as-is.

### 2.2 Writer semantics (D3, D5, D6)

New exception `AbwabSectionRequiredException` in
`Backend/application/QuranDashboard.Application.Abstractions/Abwab/`.

- `ResolveCreateSectionAsync` (`EfAbwabDoorsWriter.cs:502`): root branch
  (`parentId == null`) — null `sectionId` now **throws** `AbwabSectionRequiredException`;
  a stated section is validated live as today. Child branch unchanged: null = "derive
  from parent" (documented meaning stays), stated-mismatch still
  `AbwabSectionParentMismatchException`. Return type becomes `int`.
- `ResolveTargetSectionAsync` (`:573`): root branch (`targetParentId == null`) — null
  `targetSectionId` **throws** `AbwabSectionRequiredException`. Covers `MoveAsync` and
  `BulkMoveAsync` (both call it). Return type becomes `int`.
- **Check ordering, locked to avoid churning 404 tests:** `MoveAsync` keeps its current
  order (door load → 404 first, then resolve → 400), so
  `MoveDoor_WithUnknownId_ReturnsNotFound` (`SmokeAbwabWriteTests.cs:414`) is unchanged.
  `BulkMoveAsync` resolves the target first (`:240`, before door loads) — keep that
  order (request-shape validation before entity checks, consistent with
  `Controllers/README.md` "validation failures map to 400"); the two bulk smoke tests
  that send a null target while probing 404/400-on-doors are rewritten to carry a real
  section (§8).
- `RestoreAsync` (D5) — signature
  `RestoreAsync(int id, int? sectionId, uint expectedVersion, ct)` → `AbwabDoorDto?`:
  1. Door lookup → null (404). Parent-still-archived → throws (409). Version → 409.
     (unchanged order)
  2. **Root-scope restore** (`door.ParentId == null`):
     - `sectionId` stated → `EnsureSectionExistsAsync` (live; else
       `AbwabSectionNotFoundException` → 404, same mapping as create) → assign. May
       differ from the stored section: restore doubles as a legal re-section, which is
       what "prefilled … changeable" requires.
     - `sectionId` null → keep the stored section if its section row is live; if the
       stored section is retired (`IsSectionArchivedAsync`, `:567`) → **throw
       `AbwabSectionRequiredException`** (400, restore-flavored message §5).
  3. **Child restore** (`door.ParentId != null`, parent live per existing rule):
     - `sectionId` null → **derive from the live parent** (fresh read of
       `parent.SectionId`), same semantics as child create. The stored value is
       **never trusted**: a root restored into a different section while this
       descendant sat archived from a separate, earlier archive operation leaves the
       stored value pointing at the old section — present but wrong, invisible to
       NOT NULL.
     - `sectionId` stated and equal to the parent's current section → accepted;
       stated and ≠ parent's → **throw `AbwabSectionParentMismatchException`** (400,
       existing message).
  4. The detach block (`:440-443`) and descendant-detach block (`:462-464`) are
     **deleted**. When a root restore resolves to a section different from the stored
     one, the re-section runs through **`CascadeSectionToDescendantsAsync`
     (`:540-565`)** — the same helper move uses, which deliberately includes ARCHIVED
     descendants — **not** a restore-bounded loop. The restore loop's archive-timestamp
     fingerprint (`:452`, `d.DeletedAtUtc == archivedAt.Value`) restores only the rows
     this archive claimed; the section cascade must also reach the rows it does NOT
     restore, or a separately-archived descendant keeps the old section and resurfaces
     wrong on its own later restore.
  5. Return `AbwabDoorDto` directly; `AbwabRestoredDoorDto` and the
     `DetachedFromArchivedSection` chain die (writer `:494`, `RestoreDoorOutcome.Success`
     shape, controller mapping, `Controllers/README.md` paragraph, frontend
     announcement, e2e assertion).

Outcome/controller additions: `CreateDoorOutcome.SectionRequired`,
`MoveDoorOutcome.SectionRequired`, `BulkMoveDoorsOutcome.SectionRequired`,
`RestoreDoorOutcome.SectionRequired | SectionNotFound | SectionParentMismatch` — each
mapped to `BadRequest`/`NotFound` with the messages in §5.
`InvalidatingAbwabDoorsWriter` mirrors the new `RestoreAsync` signature (decorator rule,
Writes README).

### 2.3 The archived-read state field (D5 bullet 4)

The archive view consumes the same tree snapshot (archived doors are included and
flagged — Reads README). The client must know, per archived door, whether a restore
will demand a destination. **Explicit field:** `AbwabTreeDoorDto.SectionRetired: bool` —
`true` iff the door's section row has `deleted_at != null`. Computed in
`EfAbwabTreeReader` by fetching the retired-section id set
(`db.AbwabSections.Where(s => s.DeletedAtUtc != null).Select(s => s.Id)`) and flagging in
the projection (`:50-51`). Under the new invariant it can only be `true` for archived
doors (a live door can never point at a retired section — section delete requires zero
live doors, D4). Deliberately not inferred client-side from "sectionId absent from the
snapshot's live sections list": explicit beats inference at a contract boundary, and the
inference breaks the day sections gain an archived-but-listed representation.

### 2.4 Migration (D1, D2)

`AbwabDoor.SectionId` becomes `int`; `AbwabDoorConfiguration` adds `.IsRequired()` next
to the existing `HasColumnName("section_id")` (`:16-17`). One migration generated via
`Backend/scripts/add-mig RequireAbwabDoorSection` (tooling-generated per
`Backend/CLAUDE.md` — no hand-written migrations; this plan is the explicit request the
policy requires). Expected content: a single `AlterColumn<int>` on
`abwab_doors.section_id` with `nullable: false` and **no `defaultValue`** — Postgres
`SET NOT NULL` then fails on any existing NULL row, which **is** the fail-closed
behavior (D2). FK, indexes, and the `NULLS NOT DISTINCT` unique index are untouched
(after this change its NULL handling is exercised by `parent_id` only — semantics
narrower, definition identical).

**Inspection gate (stop condition):** if the generated migration contains a
`defaultValue`, any `UPDATE`/backfill SQL, or touches any other column, STOP. If EF
injects `defaultValue: 0` (provider behavior), removing that one argument is the
documented exceptional manual fix per `Backend/CLAUDE.md` — record which file was
edited, why, and the verification run.

### 2.5 Wipe script (D9)

New `Backend/scripts/wipe-abwab` (bash, `set -euo pipefail`, sources
`_preflight-sandbox.sh` like `drop-db`):

- **`--yes` gate** (`drop-db` grammar, `drop-db:7-9`): without it, print the six tables
  and the target database, exit 1. With it, print a stderr warning naming the six
  tables before executing.
- **Non-local refusal** (`create-smoke-dump` grammar, `:99`): parse `Host=` from the
  resolved connection string; any host other than `localhost`/`127.0.0.1` → refuse,
  exit non-zero. **Deliberately stricter than the borrowed grammar: no
  `--allow-remote` escape exists** — D9 says LOCAL ONLY, and production must not be
  wipeable by flag.
- Connection resolution order identical to the sibling scripts:
  `ConnectionStrings__QuranDashboardDb` env var, else the `api/QuranDashboard.Api` user
  secret.
- SQL (exact, via `psql -v ON_ERROR_STOP=1`):
  `TRUNCATE abwab_sections, abwab_doors, abwab_door_aliases, abwab_door_relations,
  abwab_templates, abwab_template_nodes RESTART IDENTITY CASCADE;`
- **Protection mechanism (named, per D9):** (1) a **literal six-table allowlist** — no
  wildcard, no catalog discovery, the SQL string is fixed; (2) the **CASCADE closure is
  verified closed**: every FK into an abwab table originates from another of the six
  (§1), so CASCADE cannot reach `quran_*`, `users`, or `roles`; (3) the non-local host
  refusal; (4) the `--yes` gate. The script also runs a post-wipe sanity `SELECT`
  asserting `quran_surahs` still has 114 rows and aborts loudly (exit ≠ 0, message)
  if not — a tripwire, not the protection itself.
- `Backend/scripts/README.md` gets a Commands-table row plus a flag table in the same
  change (§6).

### 2.6 Move picker (D7) and door modal (D8)

- Move picker stage one: «بلا قسم» option (`abwab-move-picker.component.html:24-31`) and
  its a11y first-tabbable pin (`spec:178`) are **removed**. Auto-select: single move →
  the moved door's current section; bulk move → the common section when every selected
  door shares one, otherwise no auto-selection and an explicit pick is required
  (common-section-or-explicit-pick — approved 2026-08-02). Auto-selection is
  changeable; stage two only unlocks after a section is (auto- or hand-)picked, as
  today (`ts:72-74`). `confirmed` emits `targetSectionId: number` (never null).
- Door modal (D8): the **shell** (`abwab-door-modal.component.ts`) gains a required
  section `<select>` shown only when `parentId == null && activeSectionId() == null`
  (the «كل الأبواب» root-create case). On a section tab it derives from the tab as
  today; for child creates it stays absent and the wire body keeps omitting the key
  (`abwab.api.ts:35-37` unchanged in behavior). `abwab-door-fields-form` is **not
  touched** — the "form must not acquire a section concept" rule stands (feature README
  line 722); the selector lives in the shell, which is the documented decision layer.
  The modal gains a `sections` input (live sections from the snapshot) bound at
  `abwab-page.component.html`.

### 2.7 Confirm primitive + restore modal (D5, D10)

**`qd-confirm-dialog`** — new shared primitive at
`Frontend/quran-dashboard-ui/src/app/shared/ui/confirm-dialog/` (sibling of `state/`,
`tabs/`, `detail-modal-shell/`), registered as a §17 component contract:

- Inputs: `title: string`, `confirmLabel: string`, `cancelLabel: string`,
  `tone: 'default' | 'danger'` (danger maps confirm to the `--qd-danger` role per
  §16.1), `busy: boolean` (disables both buttons, confirm shows the standard busy
  affordance). Body content is **projected** (`<ng-content>`), so consumers compose
  arbitrary content (the restore modal projects the door path + section selector).
- Outputs: `confirmed`, `cancelled`.
- Behavior: `role="alertdialog"`, `aria-modal="true"`, `aria-labelledby` pointing at the
  title element; focus trapped; **initial focus on the cancel button** (safe default for
  a confirm primitive); `Escape` → `cancelled`; backdrop click → `cancelled`; scroll
  locked via the existing `shared/ui/modal-scroll-lock`. RTL: logical properties only;
  footer button order follows the abwab modal shell's existing footer convention.
- It does NOT replace the abwab authoring-modal shell. The feature README's "all six
  modals share one shell" sentence is **scoped**, not violated: authoring modals share
  the shell; confirmation dialogs use `qd-confirm-dialog` (§6 README updates).

**`abwab-restore-modal`** — new feature component at
`features/abwab/components/abwab-restore-modal/`, composed on `qd-confirm-dialog`
(`tone: 'default'` — restore is not destructive):

- Shows the door name and its path (ancestor chain walked via `parentId` through the
  snapshot's node map, joined with `'، '` like the side panel).
- Root-scope door (`parentId === null`): section `<select>` over the snapshot's live
  sections — prefilled with `node.sectionId` when `!node.sectionRetired`; empty and
  **required** when `node.sectionRetired` (hint string §5). Confirm disabled until a
  section is chosen in the required case. Zero live sections + required → selector
  replaced by the no-sections hint, confirm stays disabled (the user must create a
  section first).
- Child door: no selector; the body states the door returns under its parent.
- States: confirm click → `busy` until the write resolves; failure (409 parent-first /
  stale / duplicate-name, transport error) → inline error line inside the projected
  body using the sections-modal error pattern (`qd-state variant="error"`), modal stays
  open, retry = press confirm again; success → modal closes, snapshot refetch (existing
  write-controller invariant), announcement `'استُرجع الباب'` via the existing
  aria-live announcer.
- Wiring: the archive view's restore button stops calling
  `writeController.restoreDoor` directly (`abwab-page.component.ts:392`) and instead
  opens the modal through `AbwabPageOverlaysController` (new `restoreTarget` signal,
  same pattern as the move picker at `abwab-page-overlays.controller.ts:204-222`).
  `AbwabWriteController.restoreDoor(id, { sectionId, version })` returns the plain
  `AbwabDoorDto` outcome; the `detachedFromArchivedSection` branch (`:157-164`) is
  deleted.

**Archive-view placement of a retired-section archived door (decided, consumed by task
5.9):** the archive view is **tabless** — section controls are hidden in archive mode
(`abwab-toolbar.component.html:2`, `:52`, gated on `hideSectionControls`; rationale at
`abwab-toolbar.component.ts:32-35`: "The archive view has no live section grouping").
An archived door whose section is retired therefore appears in the same flat archive
list as every other archived door; `sectionRetired` drives ONLY the restore modal's
required-destination state, never list membership. Task 5.9's e2e asserts the door is
present in that flat list before opening the restore modal.

## 3. Interaction matrix (standing rule — each cell is a required assertion)

Legend: cells name the expected outcome; **bold** cells are behavior this feature
changes. Owning tests are listed in §4 phase tables; matrix IDs (M-rows) are referenced
there.

| Operation \ state | all live, section live | section stated ≠ derivable | target/stored section retired | no section stated (root scope) | door archived | parent archived | no live sections exist |
|---|---|---|---|---|---|---|---|
| **M1 create root** | 201, door in stated section | n/a (no parent) | 404 `AbwabSectionNotFound` (existing) | **400 `AbwabDoorSectionRequired`** | n/a | n/a | **400** (nothing to state) |
| **M2 create child** | 201, section derived from parent | 400 mismatch (existing, unchanged) | n/a (parent live ⇒ section live) | 201, derives (unchanged) | n/a | 404 parent-not-found (existing) | n/a |
| **M3 move single → root** | 200, section = stated, subtree cascaded | n/a | 404 (existing) | **400 `AbwabDoorSectionRequired`** | 404 (archived door not movable, existing) | n/a | **400** |
| M4 move single → under parent | 200, inherits parent's section; stated value ignored (existing asymmetry, unchanged) | ignored (unchanged) | n/a | 200 inherits | 404 | 404 target parent | 200 (parent's section) |
| **M5 bulk-move → root** | 200 all, all in stated section | n/a | 404 | **400 before door checks** (resolve-first order, §2.2) | all-or-nothing 409/404 unchanged | n/a | **400** |
| M6 archive (single + bulk) | 204/200, `SectionId` untouched on every archived row | n/a | n/a | n/a | idempotence rules unchanged | subtree sweep unchanged | n/a |
| **M7 restore root** | 200, stored section kept when body null | body stated → that section wins (re-section) + **cascades via `CascadeSectionToDescendantsAsync` to ALL descendants, archived included (M-b)** | **400 `AbwabDoorRestoreSectionRequired` when body null; 200 into stated live section otherwise** | body null + stored live → 200 keep | — | n/a | **400 when destination required** |
| **M8 restore child** | 200, **derives the live parent's CURRENT section when body null — stored value never trusted (M-a)** | **400 mismatch when body states a section ≠ parent's current; equal → accepted** | n/a (parent live ⇒ section live) | 200 derives | — | 409 parent-first (existing, unchanged) | n/a |
| **M9 template apply** | 201-batch, every copied node at every depth carries the target's (now non-null) section | n/a | n/a (target is a live door ⇒ live section) | n/a (no section input exists) | 400 target-archived (existing) | n/a | n/a |
| M10 section delete | 409 + «لا يمكن حذف القسم لاحتوائه على أبواب حالية» when live doors exist; 204 soft-delete otherwise — **byte-identical to today (D4)** | n/a | n/a | n/a | archived-only doors do NOT block (unchanged) | n/a | n/a |
| **M11 tree snapshot** | every live door has `sectionId: number`; `sectionRetired: false` | n/a | archived door whose section retired → `sectionRetired: true` | n/a | archived doors included + flagged (unchanged) | n/a | empty sections/doors arrays (unchanged) |

**Required cross-operation assertions (the defect the 2026-08-02 correction closed —
both mandatory):**
- **M-a:** archive a descendant in its own operation → archive the root → restore the
  root into a DIFFERENT section → restore the descendant with body-null section → the
  descendant lands in its parent's **current** section, not the stored one.
- **M-b:** in the same scenario, immediately after the root's re-section and BEFORE the
  descendant is restored, the still-archived descendant's row already carries the new
  section (the cascade covered rows the restore did not claim).

DB tier (defense in depth, D1): raw `INSERT` with `section_id = NULL` →
`PostgresException 23502` (asserted in schema tests, §4 phase 3).

## 4. Phases

Seams follow the inspection's dependency order. One deviation from the literal seam
list, justified in phase 4: the regenerated **TS client** is committed with the
frontend fixes that consume it (phase 5's first commit), because a committed client that
flips `number | null` → `number` breaks `npm run build` (TS2367 on the `=== null`
branches) and no commit may leave a build red.

---

### Phase 1 — wipe script (standalone; unblocks phase 3's local apply)

**Objective:** a deliberate, local-only, guarded reset of all six abwab tables (D9).

| # | Task | Files |
|---|---|---|
| 1.1 | Write `wipe-abwab` per §2.5 (guards, allowlist SQL, tripwire) | `Backend/scripts/wipe-abwab` (new) |
| 1.2 | README row + flag table (grammar of the `create-smoke-dump` section) | `Backend/scripts/README.md` |

**Behavior change:** none in the app; new operator tooling only.
**Tests:** none automated (operator script, consistent with siblings — none of
`drop-db`/`reset-db` have tests). Verification is behavioral:
- `bash -n Backend/scripts/wipe-abwab` (syntax)
- `./Backend/scripts/wipe-abwab` (no flag) → exits non-zero, prints the six tables +
  target DB, wipes nothing.
- `ConnectionStrings__QuranDashboardDb='Host=example.com;...' ./Backend/scripts/wipe-abwab --yes`
  → refused, exits non-zero.
- The destructive path itself is exercised once, deliberately, at phase 3 step 3.6.

**Commit boundary:** one commit — script + README row.

---

### Phase 2 — backend enforcement (writer rejections + restore resolution + tests)

**Objective:** after this phase, **no write path can produce a null section** (D3, D5).
The DB column is still nullable (phase 3); existing local rows are untouched.

| # | Task | Files |
|---|---|---|
| 2.1 | `AbwabSectionRequiredException` (new) | `Backend/application/QuranDashboard.Application.Abstractions/Abwab/AbwabSectionRequiredException.cs` |
| 2.2 | Two new messages (§5) | `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs` (after `:134` block) |
| 2.3 | Root-scope rejection in create | `EfAbwabDoorsWriter.cs` `ResolveCreateSectionAsync:502-526` (return `int`) |
| 2.4 | Root-scope rejection in move/bulk-move | `EfAbwabDoorsWriter.cs` `ResolveTargetSectionAsync:573-588` (return `int`); check-order note §2.2 |
| 2.5 | Restore resolution per §2.2 item 2–5; delete detach blocks `:440-443`, `:462-464`; return `AbwabDoorDto` | `EfAbwabDoorsWriter.cs` `RestoreAsync:406-495`; delete `Responses/AbwabRestoredDoorDto.cs` |
| 2.6 | Restore contract: `RestoreDoorBody(int? SectionId, uint Version)`; command; interface `IAbwabDoorsWriter.cs:71`; decorator `InvalidatingAbwabDoorsWriter` | `.../Commands/Doors/RestoreDoor/*.cs`, `IAbwabDoorsWriter.cs`, `Caching/Abwab/InvalidatingAbwabDoorsWriter.cs` |
| 2.7 | Outcomes + controller mappings (§2.2): `SectionRequired` on create/move/bulk-move/restore; restore `SectionNotFound` → 404, `SectionParentMismatch` → 400 | `CreateDoor/MoveDoor/BulkMoveDoors/RestoreDoor` handlers + outcomes; `AbwabDoorsController.cs` |
| 2.8 | Widen the smoke abwab reset from 3 to all six tables (test infra, aligns with D9's canonical set) | `SmokeApiFixture.cs:93-94` |
| 2.9 | Behavior-test rewrite per §8 strategy + new matrix tests: M1/M3/M5 rejection cases, M7 (`RestoreAsync_RootWhoseSectionRetired_WithoutDestination_Throws`, `RestoreAsync_WithDestination_ResectionsTheRestoredSubtree`, `RestoreAsync_RootKeepsStoredLiveSection_WhenBodyNull`), M8 (`RestoreAsync_Child_WithConflictingSection_Throws`, `RestoreAsync_Child_DerivesLiveParentsSection_WhenBodyNull`), and the two mandatory cross-operation assertions: `RestoreAsync_RootIntoDifferentSection_ResectionsSeparatelyArchivedDescendants` (M-b) and `RestoreAsync_ChildRestoredAfterAncestorResection_DerivesParentsCurrentSection` (M-a) — replacing `:403`/`:430`; rewrite `:283` (section-less root test becomes "rejects null, sectioned roots share the global sequence"), `:198` (move-to-root now with section), `:500` unchanged in spirit (derive still works) | `Backend/tests/QuranDashboard.Tests/Abwab/AbwabDoorWriteBehaviorTests.cs` |
| 2.10 | Smoke-test rewrite per §8 + new HTTP cases: `CreateDoor_RootWithoutSection_ReturnsBadRequest`, `MoveDoor_ToRootWithoutSection_ReturnsBadRequest`, `BulkMoveDoors_ToRootWithoutSection_ReturnsBadRequest`, `RestoreDoor_RootWhoseSectionRetired_WithoutDestination_ReturnsBadRequest`, `RestoreDoor_WithDestinationSection_RestoresIntoIt`, `RestoreDoor_Child_WithConflictingSection_ReturnsBadRequest`; rewrite `:950`/`:973` (detach pair → destination pair); envelope assertions on the new 400s (Arabic message, `errors: []`) | `Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteTests.cs` |
| 2.11 | Tree-read tests: 5 `CreateAsync(null, null, …)` sites get a real section (§8) | `Backend/tests/QuranDashboard.Tests/Abwab/AbwabTreeReadTests.cs` |
| 2.12 | Writes/Reads/Controllers README updates for the semantics that changed in THIS phase (§6 rows 1–3) | three READMEs |

**Verification (Tier A + route gate — contract changed):**
```
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build \
  --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab"
dotnet test ... --filter "FullyQualifiedName~QuranDashboard.Tests.Api"
dotnet test ... --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```
Evidence MUST state smoke counts and whether the data tier ran or skipped
(TESTING_STRATEGY §3 Tier A/C; unqualified "smoke passed" is invalid).

**Known inter-phase degradation (accepted):** until phase 5, root-create from
«كل الأبواب» and «بلا قسم» moves in the local UI receive 400s surfaced as the existing
error states. E2E is not run between phases 2 and 5.

**Commit boundary:** one commit — enforcement + tests + the three backend READMEs.

---

### Phase 3 — domain flip + migration + schema tests + smoke dump

**Objective:** the DB constraint tier of D1, fail-closed per D2.

| # | Task | Files |
|---|---|---|
| 3.1 | `AbwabDoor.SectionId` → `int`; replace the `:7-9` nullability comment with the mandatory invariant («every door belongs to a section; root ≠ section-less», citing D6) | `Backend/domain/QuranDashboard.Domain/Abwab/AbwabDoor.cs` |
| 3.2 | `.IsRequired()` in config (beside `:16-17`) | `AbwabDoorConfiguration.cs` |
| 3.3 | Response DTOs: `AbwabDoorDto.SectionId` int; `AbwabTreeDoorDto.SectionId` int + `SectionRetired` bool | `Responses/AbwabDoorDto.cs`, `Responses/AbwabTreeDto.cs` |
| 3.4 | Reader: project `SectionRetired` per §2.3 | `EfAbwabTreeReader.cs:38-51` area |
| 3.5 | Template-apply plumbing: `NewDoor` + `CopiedNode` take `int` | `EfAbwabTemplateApplyWriter.cs:22, 127, 156, 188-203` |
| 3.6 | **Local sequence (operator steps, in order):** `./Backend/scripts/wipe-abwab --yes` (the ONLY sanctioned wipe — D9; never inside the migration) → `Backend/scripts/add-mig RequireAbwabDoorSection` → inspect per §2.4 gate → `Backend/scripts/update-db` (explicitly authorized by this plan per `Backend/CLAUDE.md`; report migration name, files, build status, and that database update was executed) | `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/` (generated) |
| 3.7 | Schema tests: fix the 5 direct `new AbwabDoor` inserts (`AbwabSchemaTests.cs:173, 182, 201, 215, 297` — each first inserts an `AbwabSection` and sets `SectionId`); **new facts:** `Doors_section_id_is_not_null` (information_schema nullability) and `Doors_insert_with_null_section_is_rejected_by_postgres` (raw INSERT → `PostgresException` 23502) — the DB tier of the matrix | `Backend/tests/QuranDashboard.Tests/Abwab/AbwabSchemaTests.cs` |
| 3.8 | Template-apply minimum assertion (§9): new `ApplyAsync_CopiesCarryTheTargetsSectionAtEveryDepth` — build section + target door + 2-level template via the writers, apply, assert every copied row's `SectionId == target.SectionId` | new `Backend/tests/QuranDashboard.Tests/Abwab/AbwabTemplateApplyBehaviorTests.cs` (uses `AbwabSchemaFixture`) |
| 3.9 | Regenerate the smoke dump — **any migration invalidates the dump, same change, never at the next run's expense** (TESTING_STRATEGY §3 Tier C): `./Backend/scripts/create-smoke-dump --yes`; manifest migration count moves 23 → 24 | `resources/db-dumps/quran-canonical/` (local, gitignored — the *manifest expectations* inside the smoke gate need no code change) |

**Verification:** build; `~QuranDashboard.Tests.Abwab`; full
`~QuranDashboard.Tests.Smoke.` (data tier expected to RUN against the fresh dump —
report "140+N passed, 0 skipped" style counts); then the Tier B no-pipeline filter once
(migration milestone).

**Commit boundary:** one commit — domain flip + migration + schema/apply tests.

---

### Phase 4 — backend-side contract regeneration

**Objective:** committed generated artifacts match the new contract.

| # | Task | Files |
|---|---|---|
| 4.1 | `Backend/scripts/export-swagger` → verify the swagger diff is exactly: `nullable: true` removed from `sectionId` on `AbwabDoorDto`/`AbwabTreeDoorDto`; `sectionRetired` added; `RestoreDoorBody` gains nullable `sectionId`; `AbwabRestoredDoorDto` schema gone; restore path's 200 schema now `AbwabDoorDto` | `Frontend/quran-dashboard-ui/openapi/swagger.json` |
| 4.2 | `npm run docs:api` | `docs/api-reference/index.html` |

**Deviation from the literal seam (justified):** `npm run generate:api` output is NOT
committed here — the flipped client types break `npm run build` until phase 5's
consuming fixes exist, and no commit may leave the build red. `check-api-contract` is
therefore expected RED at this boundary and is run to GREEN at the end of phase 5.

**Verification:** the enumerated swagger diff (`git diff` read), `npm run docs:api`
exit 0.
**Commit boundary:** one commit — swagger + api-reference.

---

### Phase 5 — frontend prevention, restore modal, confirm primitive, e2e

**Objective:** UI tier of D1; D5 UI; D7; D8; D10.

| # | Task | Files |
|---|---|---|
| 5.1 | `npm run generate:api`; flip `AbwabNode.sectionId` to `number`, add `AbwabNode.sectionRetired: boolean`; `AbwabMoveDestination.targetSectionId: number`; builder copies both fields; remove the `:101` null guard + rewrite the `:97-99` and `:184-187` doc comments; `abwab.api.ts` types follow (`sectionId?: number`, omission-for-child behavior unchanged) | `src/app/core/api/generated/**` (generated), `models/abwab.models.ts`, `state/abwab-tree.builder.ts`, `data-access/abwab.api.ts` |
| 5.2 | Door modal shell selector per §2.6/D8; new `sections` input; validation + error string | `components/abwab-door-modal/abwab-door-modal.component.{ts,html}`, `pages/abwab-page/abwab-page.component.html` binding |
| 5.3 | Move picker per §2.6/D7 (remove «بلا قسم», auto-select, emit `number`) | `components/abwab-move-picker/abwab-move-picker.component.{ts,html}` |
| 5.4 | `qd-confirm-dialog` per §2.7 + SCSS + spec (renders/labels; focus trap + initial focus on cancel; `Escape` and backdrop → `cancelled`; `busy` disables both; `role="alertdialog"` + `aria-labelledby`; single-emit) + **§17 entry** | `src/app/shared/ui/confirm-dialog/` (new), `.architecture/UI_STYLE_SYSTEM.md` §17 |
| 5.5 | `abwab-restore-modal` per §2.7 + overlays-controller wiring + write-controller signature/announcement change | `components/abwab-restore-modal/` (new), `state/abwab-page-overlays.controller.ts`, `state/abwab-write.controller.ts:157-164`, `pages/abwab-page/abwab-page.component.ts:387-393` + `.html` |
| 5.6 | Labels: add/remove per §5; `abwab.labels.spec.ts:15` follows | `models/abwab.labels.ts`, `models/abwab.labels.spec.ts` |
| 5.7 | Spec rewrite per §8: move-picker `:55/:111/:178`; door-modal `:101` + new selector cases (hidden on section tab; hidden for child; required error blocks submit; chosen id sent); builder `:232` (Σ root counts now equals live-root count) `:249` (drop the section-less case) `:318` (open-scope count with all-sectioned data); write-controller `:383` (M19 → plain restore announcement + `sectionId` passthrough) `:247`; page `:526/:538/:556`; api `:112` (M33 omission stays) `:151` (`targetSectionId: number`); ~10 factory defaults `sectionId: null → 1` | respective `*.spec.ts` |
| 5.8 | New restore-modal spec: root prefill when `sectionRetired: false`; required-empty + disabled confirm when `true`; no-sections hint; child variant (no selector); 409 inline error keeps modal open; success closes + announces | `components/abwab-restore-modal/abwab-restore-modal.component.spec.ts` |
| 5.9 | E2E: rewrite `abwab-archive.e2e.ts:108-125` — sandbox section A + door, archive door, create section B, delete section A (204 — only archived doors), open archive, restore → modal demands destination, pick B, assert door lands in B's tab and announcer shows `'استُرجع الباب'`; `abwab-operations.e2e.ts:46-66` — assert stage one arrives pre-selected on the sandbox section instead of clicking it | `e2e/abwab-archive.e2e.ts`, `e2e/abwab-operations.e2e.ts` |
| 5.10 | Frontend feature README updates (§6 row 4) | `features/abwab/README.md` |

**Accessibility/RTL requirements (assertable, per instructions):** the confirm
primitive per 5.4's spec list; the shell selector — programmatically associated Arabic
`<label>`, `aria-invalid` + inline error text on validation failure, reachable in the
modal's existing tab order, RTL rendering via logical properties (no `left/right`);
announcements via the existing `role="status"` announcer only.

**Verification (Tier C — frontend changed + contract changed):**
```
npm test -- --include="src/app/features/abwab/**/*.spec.ts"
npm test            # full, fork cap preserved (VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2 baked into npm test)
npm run build
Backend/scripts/check-api-contract   # now GREEN — all three generated outputs committed
npm run e2e         # OPT-IN, supplementary evidence only — never cited as a gate (§3 Tier E)
```
**Commit boundary:** two commits — (a) `qd-confirm-dialog` + §17 entry; (b) abwab
frontend + generated client + specs + e2e + README.

---

### Pre-PR (Tier C, whole feature)

```
dotnet build Backend/QuranDashboard.sln
dotnet test ... --filter "<the §5 no-pipeline chain, incl. &FullyQualifiedName!~QuranDashboard.Tests.Smoke.>"
dotnet test ... --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."   # counts + data-tier statement
npm test && npm run build
```
Tier D is **not** triggered (non-pipeline tables only; no DbContext-wide or shared
persistence change — TESTING_STRATEGY §3/§4). There is no CI (§8): every gate above is
a local gate nobody verifies ran; the PR description must carry the actual outputs.

## 5. Arabic strings (verbatim)

Backend (`ApiMessages.cs`, abwab block after `:134`):

| Constant | Value | Used by |
|---|---|---|
| `AbwabDoorSectionRequired` | `يجب تحديد قسم للباب الرئيسي` | create root / move-to-root / bulk-move-to-root without section → 400 |
| `AbwabDoorRestoreSectionRequired` | `قسم الباب الأصلي محذوف، حدد قسمًا للاسترجاع` | root restore, stored section retired, body null → 400 |

Reused unchanged: `AbwabDoorSectionParentMismatch` (`:134`) for the child-restore
conflict; `AbwabSectionNotFound` (`:115`) for a stated dead destination (404);
`AbwabSectionHasLiveDoors` (`:117`) — untouched (D4).

Frontend (`models/abwab.labels.ts`):

| Key | Value | Surface |
|---|---|---|
| `doorModalSectionLabel` | `القسم` | shell selector label |
| `doorModalSectionRequiredError` | `اختر قسمًا للباب الرئيسي` | shell selector inline error |
| `restoreModalTitle` | `استرجاع الباب` | restore modal title |
| `restoreModalSectionLabel` | `القسم بعد الاسترجاع` | restore modal selector label |
| `restoreModalRetiredHint` | `القسم الأصلي محذوف — اختر قسمًا بديلًا` | required-destination hint |
| `restoreModalNoSectionsHint` | `لا توجد أقسام حالية — أنشئ قسمًا أولًا` | zero-live-sections state |
| `restoreModalConfirm` | `استرجاع` | confirm button |
| `restoreModalCancel` | `إلغاء` | cancel button |
| `restoreAnnouncement` | `استُرجع الباب` | aria-live success announcement |

Removed: `noSectionOption` (`:188`), `restoreDetachedAnnouncement` (`:204`).

## 6. README updates (same change as the behavior they describe)

1. **`Backend/.../Persistence/Writes/Abwab/README.md`** (phase 2): replace the
   restore-detach "first-class state, plan §R8" paragraph with the restore-destination
   contract (§2.2); extend the create/move asymmetry paragraph with the root-scope
   section requirement; state the new invariant: *every door row carries a live-or-once-
   live section; only restore may re-section without a move*; decorator note for the new
   `RestoreAsync` signature.
2. **`Backend/.../Persistence/Reads/Abwab/README.md`** (phases 2–3): update the
   `DoorsInScopeCount` justification (a live door can never point at a retired section —
   now also DB-guaranteed); document `SectionRetired` (§2.3) and that it can be `true`
   only on archived doors.
3. **`Backend/api/QuranDashboard.Api/Controllers/README.md`** (phase 2): remove the
   `AbwabRestoredDoorDto { door, detachedFromArchivedSection }` paragraph; document the
   restore body (`sectionId?`, `version`) and the two new 400s; the "stated section that
   disagrees is a 400" line now also covers restore.
4. **`Frontend/.../features/abwab/README.md`** (phase 5): stats-bar paragraph (line
   ~615) — counts now reconcile (Σ per-section root counts = live roots); keep the "no
   arithmetic-sum test" stance but rewrite its rationale (redundant, not impossible).
   M10/M33 paragraph (line ~722) — the shell now owns a real selector on «كل الأبواب»;
   the "form must not acquire a section" sentence **stands verbatim**. Move-picker
   paragraph (line ~138) — «بلا قسم» removed, auto-select rule. URL-contract row (line
   ~274) — "«كل الأبواب» — every door" (drop "including section-less ones"). Scope the
   "six modals share one shell" sentence to authoring modals; add the restore modal +
   `qd-confirm-dialog` and the new announcement string.
5. **`Backend/scripts/README.md`** (phase 1): `wipe-abwab` row + flags/guards table.
6. **`.architecture/UI_STYLE_SYSTEM.md` §17** (phase 5): `qd-confirm-dialog` entry in
   the house format (Purpose / Inputs-roles / behavior / "supersedes hand-rolled
   `role="alertdialog"` confirms; do not hand-write these again"), noting the
   templates-page inline confirms as retrofit candidates for a later slice.

## 7. Smoke-gate + evidence rules (restated for the implementer)

- Route-smoke REQUIRED at every phase touching contracts (phases 2–5) and pre-PR:
  request/response contracts change (TESTING_STRATEGY §3 Tier A/C, §10, §11 row "API
  route added/removed, or a request/response contract change").
- Evidence format: counts + explicit data-tier statement. `"140+N passed, 0 skipped"` /
  `"…, data tier skipped"` are valid; `"smoke passed"` alone is a defect.
- **No new `SmokeRouteCatalog` entry** (no route added/retemplated); the parity gate is
  untouched. The dot-bounded filter `~QuranDashboard.Tests.Smoke.` is mandatory
  (Smoke is a **namespace inside `QuranDashboard.Tests`**, not a project).
- The phase-3 migration invalidates the dump ⇒ `create-smoke-dump --yes` in that same
  change; the gate fails loud on a stale dump rather than skipping.

## 8. Null-section call-site rewrite strategy (no hand-waving)

- **Smoke suite (~42 omitting call sites + 11 explicit):** change the private helper
  `CreateDoorAsync` (`SmokeAbwabWriteTests.cs:1071`) so that when `sectionId is null`
  **and** `parentId is null` it first creates a fresh uniquely-named section via
  `POST api/abwab/sections` and uses its id. All 42 omitting call sites then compile
  unmodified — but **"compiles" is not "passes": do not assume it.** Every root create
  now brings an extra section row into existence, so after the helper change the whole
  `SmokeAbwabWriteTests` family is run and any test that asserted **section counts,
  section lists, or tree shape** (e.g. snapshot `sections` array length, per-section
  root counts, order-scope expectations) is identified and reported by name in the
  phase-2 evidence, then fixed individually. Child-create sites keep inheriting. Sites
  *asserting* null section are rewritten by hand: `:709`, `:753` (assert the concrete origin section id
  instead of `BeNull`), the `:947` comment case, the `:950/:973` pair (→ destination
  pair, task 2.10), and the two bulk probes that send a null target while testing
  something else (`:713` unknown-door → create a section and pass it, expectation stays
  404; `:676` null-element stays — its 400 fires in the handler's list validation
  before the writer).
- **Behavior tests (~22 `CreateAsync(null, null, …)` sites +
  `MoveAsync(id, null, null, …)` sites):** add one private helper per test class
  (`Task<int> NewSectionAsync(string name)`) inserting an `AbwabSection` row through the
  fixture's context; every root-scope `CreateAsync`/`MoveAsync` call threads a real id.
  Mechanical; the diff is large but single-shaped. Load-bearing tests are rewritten per
  task 2.9, not merely re-plumbed.
- **Frontend factories (~10 spec files defaulting `sectionId: null`):** flip the factory
  default to `sectionId: 1`; only specs asserting null-section behavior are rewritten
  (task 5.7's explicit list).

## 9. Templates apply (TESTING_DEBT row 7)

Once every target door has a non-null section, the apply writer's verbatim copy
(`EfAbwabTemplateApplyWriter.cs:127, 156, 194`) becomes **structurally incapable of
producing a section-less door** — the `int` plumbing (task 3.5) makes it a compile-time
guarantee, and "target is a live door ⇒ its section is live" (D4 predicate) closes the
stale-section hazard the inspection flagged. What is NOT guaranteed and therefore gets
the minimum assertion (task 3.8): that every copied node at **every depth** carries the
*target's* section (the level-≥2 `CopiedNode` relay could regress independently).
Row 7 of `docs/TESTING_DEBT.md` is narrowed accordingly, not deleted (its other
obligations — offsets, aliases, all-or-nothing, 409 collisions — remain unpaid).

## 10. Risks, rollback, stop conditions

**Risks**
- Thinnest-coverage areas are exactly the touched ones: the apply writer has zero
  tests (until 3.8), the caching layer has zero (TESTING_DEBT I1–I4), and there is no
  CI — a skipped gate is silently skipped. Mitigation: the per-phase evidence rules in
  §4/§7 are part of each commit's deliverable.
- `EfAbwabDoorsWriter` grows further past the 600-line threshold (816 today). Logged
  in §12; refactoring here is prohibited (non-goal).
- Inter-phase UI degradation (end of phase 2 → phase 5) is deliberate and local-only.
- EF may emit `defaultValue: 0` on the AlterColumn (§2.4 gate) — handled by the
  documented exceptional-fix protocol, never by accepting a silent backfill.
- The e2e abwab project runs single-worker (TESTING_STRATEGY §6) — the rewritten
  archive spec must stay inside the `abwab` Playwright project.

**Rollback**
- Phases are independent commits; revert in reverse order.
- Migration rollback (local only): `dotnet ef database update AddAbwabTemplates`
  equivalent via `Backend/scripts/update-db` tooling — there is no production abwab
  schema to roll back (§0b).
- The wipe is irreversible by design; abwab content is locally-authored curation data,
  explicitly sacrificed by D3-the-locked-decision (wipe locally). The canonical dump
  restores `quran_*` only and is unaffected.

**Stop conditions (halt, report, do not improvise)**
1. The generated migration contains a default, backfill SQL, or any change beyond the
   single AlterColumn (§2.4).
2. `wipe-abwab`'s post-wipe tripwire fires (any `quran_*` count change), or
   `create-smoke-dump`'s pinned baselines (`quran_roots` 1642 / `quran_lemmas` 4817 /
   `quran_stems` 11843 / `quran_word_morphology` 77432 / segments 128219) shift.
3. `check-api-contract`'s diff shows any change outside the §4.1 enumerated list.
4. Any status-code remap not named in this plan surfaces in smoke output (e.g. a 409→400
   drift on section delete — D4 forbids it).
5. A required README paragraph cannot be updated truthfully (i.e., the code diverged
   from this plan) — reconcile the plan first.

## 11. Acceptance criteria (each independently checkable)

1. `POST api/abwab/doors` with `parentId: null` and null/absent `sectionId` → 400,
   envelope `{isSuccess:false, message:"يجب تحديد قسم للباب الرئيسي", errors:[]}`.
2. Same for `.../move` and `.../bulk-move` with `targetParentId: null` and null target
   section.
3. Child create with null `sectionId` still derives the parent's section (M2 green).
4. `abwab_doors.section_id` is NOT NULL in information_schema; raw NULL insert →
   PostgresException 23502 (schema tests green).
5. The migration file contains exactly one AlterColumn, no default, no data SQL.
6. Restore: root+retired+no-body-section → 400 with the restore message; root with
   stated live section → 200 and the re-section reaches ALL descendants — archived
   included — via `CascadeSectionToDescendantsAsync`; **child restore derives its
   section from the live parent's CURRENT section (the stored value is never
   trusted)**; child with conflicting stated section → 400 mismatch; M-a and M-b
   green. `AbwabRestoredDoorDto` no longer exists in the solution.
7. Section delete behavior byte-identical to today: 409 + «لا يمكن حذف القسم لاحتوائه
   على أبواب حالية» on live doors; 204 soft-delete otherwise;
   `e2e/abwab-structure.e2e.ts` untouched and green.
8. Tree snapshot: every door's `sectionId` is a number; `sectionRetired` true exactly
   for archived doors whose section is retired.
9. `wipe-abwab`: no-flag → preview + exit ≠ 0; non-local host → refused; `--yes` →
   exactly the six abwab tables emptied, `quran_surahs` still 114 (tripwire log).
10. Move picker has no «بلا قسم» option; stage one arrives pre-selected per §2.6; door
    modal on «كل الأبواب» blocks submit until a section is chosen.
11. `qd-confirm-dialog` exists under `shared/ui/`, has a green spec covering focus
    trap/initial focus/Escape/busy/roles, and a §17 entry.
12. `check-api-contract` exits 0 at HEAD; swagger shows non-nullable `sectionId`,
    `sectionRetired`, the new restore body, and no `AbwabRestoredDoorDto`.
13. Tier C evidence recorded with smoke counts + data-tier statement; full `npm test` +
    `npm run build` green.
14. All §6 README updates present in the same commits as the behavior they describe.

## 12. TESTING_DEBT.md updates (part of phase 5's final commit)

- **Row 7** (templates deep-copy): narrow — "`section_id` inheritance at every depth" is
  paid by `AbwabTemplateApplyBehaviorTests.ApplyAsync_CopiesCarryTheTargetsSectionAtEveryDepth`;
  the remaining obligations stay.
- **Add row (pre-existing, surfaced here):** `EfAbwabDoorsWriter` exceeds the 600-line
  threshold (816 before this feature, larger after) — split candidate for a dedicated
  slice; blocked on nothing.
- **Add row:** the sections-modal delete flow still has no confirm dialog; the
  archive-confirmation slice (non-goal here) should consume `qd-confirm-dialog` for
  both.
- Check the A.2c gap note from the inspection: the e2e teardown predicate
  (`e2e/fixtures/abwab.ts:124`) can drop its `!== null` branch once doors are always
  sectioned — fold into row H4's neighborhood or fix inline in phase 5.9 (preferred:
  fix inline, no debt row).
