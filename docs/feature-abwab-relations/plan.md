# Feature: abwab-relations — relations between doors (تشابه · تضاد · شمولية)

Planning document. **Plan only — no code was changed and no Git action was taken while
writing it.**

> Path note: workspace convention is `docs/feature-XXX-feature-name/`. This folder omits the
> numeric prefix, following `docs/feature-abwab-doors/` and `docs/feature-abwab-global-order/`
> (the same user instruction).

> Spec Kit note: this feature does **not** populate `specs/`. `specs/` holds only its
> `README.md`; the two preceding abwab features were planned entirely under `docs/`. A later
> implementation review should look for `docs/feature-abwab-relations/plan.md`, not
> `specs/abwab-relations/contracts/`.

---

## 0. Guard result — one slice, 29 tasks

| Phase | Commit | Tasks | Ends with |
|---|---|---|---|
| 1 — housekeeping, schema, migration | 1 | 5 | `abwab_door_relations` exists; smoke dump regenerated |
| 2 — read path | 2 | 4 | `GET .../relations` + relation counts in the tree snapshot |
| 3 — write path | 3 | 5 | multi-add + delete, 409/400 mapped, never a 500 |
| 4 — contract regeneration | 4 | 1 | `openapi/swagger.json` → `core/api/generated/` + `docs/api-reference/` |
| 5 — frontend data & state | 5 | 4 | api, models, relations controller, labels |
| 6 — frontend UI | 6 | 5 | the modal, the revived `.flag.rel`, three entry points |
| 7 — docs, debt, evidence | 7 | 5 | READMEs, `docs/TESTING_DEBT.md`, re-measured counts |
| **Total** | | **29** | Under the ~30 guard — **not split** |

The count is not padded down. The one task that could honestly split is **T601** (the groups +
type segment + direction pill, and the tree picker are two independent pieces of work) — doing
so lands on 30 and trips the guard. That is a conscious call made here, not something to
discover mid-phase-6: **if T601 splits, the feature splits at phase 4** — exactly where
`abwab-global-order` put its own handoff. Slice A = phases 1–4 (backend + contract), Slice B =
phases 5–7 (frontend + docs). Phases 5+ cannot start before phase 4 lands either way.

**Execution: a short-lived branch `abwab-relations` off `dev`, PR into `dev`.** Not direct on
`dev` — this feature carries a migration and seven commits, and the last three features
(`abwab-doors` #48/#49, `abwab-global-order` #50, the close chore #51) all went through
branches. Never `main` (root `CLAUDE.md`).

---

## 1. Objective

Doors carry no relations today; the tree mockup's `.flag.rel` chip was deliberately deleted in
Slice B as a dead control (`docs/feature-abwab-doors/plan.md:90-95`, and
`features/abwab/README.md`'s "Zero dead controls" line). This feature builds the thing that
makes it live: an admin can state that two doors are **similar** (تشابه), **opposed** (تضاد), or
that one is **more comprehensive** than the other (شمولية), and can see and remove those
relations from one modal anchored on a door.

The approved design contract is `docs/design-preview/abwab-relations-concept.html`. Everything
below is derived from it plus the locked decisions in §4; where the mockup contradicts a locked
decision, §9 names the line and the decision wins.

## 2. Scope

- One new table, `abwab_door_relations`, with the audit-seed columns, `xmin`, and soft-delete
  columns the two abwab tables already carry.
- Three routes: read a door's relations, add N relations in one call, delete one relation.
- Relation counts on the tree snapshot, and the revived `.flag.rel` chip on tree rows with
  `count > 0`.
- One modal (`abwab-relations-modal`) implementing the contract: four display groups, the type
  segment, the direction pill with its live preview, the expandable/searchable tree picker,
  and multi-target add.
- Three entry points: the side panel, the row context menu, and bulk mode.
- **No new tests** (§8). Parity-catalog entries are **not** tests and are mandatory.

## 3. Non-goals

- **No relation editing.** The modal has ✕ and add, no edit affordance; flipping a شمولية
  direction is delete + re-add (§5.3).
- **No archive blocking and no cascade.** Relations never prevent archiving a door and are
  never deleted by one (§6).
- **No relation-type extension point.** Three types, closed enum. A fourth type is a schema
  change, not a config row.
- **No relations in the archive view, and no relations on cards.** Both derived, not decided —
  §6.4 and §7.3 (T603).
- **No auth change.** The routes stay `Open`, like every other `/api/abwab` route; the release
  block in `features/abwab/README.md` («do not include this feature in a `dev → main` release
  until write protection lands») still stands and now covers three more write-capable routes.
- **No relation-aware search or filtering** anywhere in the tree/cards/toolbar.

---

## 4. Locked decisions

| Area | Decision |
|---|---|
| Types | `Similarity = 1` (تشابه), `Opposition = 2` (تضاد), `Comprehensiveness = 3` (شمولية). Enum starts at 1 — the unmapped-zero trap `AbwabReorderScope` already documents |
| Row shape | **One row per pair per type, always canonically ordered** `door_a_id < door_b_id` — for all three types, directional included (§5.2 explains why) |
| Direction | `broader_door_id`: the endpoint that is **more comprehensive**. `NOT NULL` exactly for `Comprehensiveness`, and must equal `door_a_id` or `door_b_id` |
| Mutual display | The other side is **derived at read time**, never stored twice. Deleting from either side deletes the row — which is what the modal's own copy already promises (`abwab-relations-concept.html:119`) |
| User vocabulary | **Comprehensiveness only**: «أبواب أكثر شمولية» / «أبواب أقل شمولية», from the anchor door's perspective. **Never أعم/أخص in UI copy** — §9 lists the two mockup lines that violate this |
| Self-relation | Refused. DB `CHECK (door_a_id < door_b_id)` makes it unrepresentable; handler maps to `400` |
| Duplicates | Refused per (pair, type) **regardless of direction** — A cannot be both more and less comprehensive than B. Partial unique index `(door_a_id, door_b_id, relation_type) WHERE deleted_at IS NULL`; handler maps `23505` to `409`, **never a 500** |
| Multi-target add | **One** endpoint call: anchor door + type [+ direction] + N target ids. **All-or-nothing**, per the bulk precedent (`features/abwab/README.md`, "Bulk is all-or-nothing") |
| Delete | Explicit delete is a **soft** delete (`deleted_at`/`deleted_by`). Re-adding the same pair creates a **new row**; nothing revives the old one |
| Dormancy | **Derived, never stored.** A relation is dormant iff either endpoint has `deleted_at IS NOT NULL`. No `is_dormant` column (§6.1) |
| Archive | Relations never block archiving; archiving makes touching relations dormant; restoring makes them reappear. Rows untouched either way |
| Target liveness | A relation may only be **created** between two live doors. An archived target is refused `400` — it would be born invisible (§6.2) |
| Concurrency | Relation writes carry **no version token** (§5.4). They still trigger the full refresh-after-write, because they change snapshot counts |
| Counts | `AbwabTreeDoorDto.RelationCount`, live-endpoint-only — the same live-only judgment call `Reads/Abwab/README.md` already documents for `DirectChildCount`/`DoorsInScopeCount` |
| Flag | The tree row renders `.flag.rel` («علاقات») only when `relationCount > 0`. Cards render no flag (mockup `abwab-tree-concept.html:598` renders only `protected` there) |
| Bulk entry | Bulk mode's «إضافة علاقة» opens the modal in **anchor-pick mode**: the bulk set is the fixed target list, the picker single-selects the anchor (§7.2, the one item worth confirming) |
| Migration | Authorized. EF tooling only; **local apply only** — plus the standing dump-regen rule (T105) |
| Branch | `abwab-relations` off `dev` → PR into `dev`. Never `main` |

---

## 5. The data model, derived line by line

### 5.1 The one sentence everything derives from

> **A `Comprehensiveness` row states: `broader_door_id` is MORE comprehensive than the other
> endpoint.**

Nothing else in this feature encodes direction. Both display groups, the pill, the preview
string, and every matrix cell below are consequences of that sentence.

### 5.2 Why directional rows are canonically ordered too

The user-locked shape is "mutual = one canonical row; directional = source/target semantics".
The semantics are kept; the **column position** is not the carrier. Reason, from the contract:

`abwab-relations-concept.html:225` disables an already-linked picker row with
`rels.find(r => r.other === node.name && r.t === curType)` — **no direction term**. So once
الجنة–الآخرة carries a شمولية row, the mockup blocks the opposite direction too, correctly: A
cannot be both more and less comprehensive than B.

A `UNIQUE (source_id, target_id, relation_type)` does **not** give that — it happily admits both
`(A,B)` and `(B,A)`. The two implementable ways to get it:

1. **Canonical pair for all three types** + a `broader_door_id` column. One `CHECK
   (door_a_id < door_b_id)`, one partial unique index, and the flip is unrepresentable by
   construction.
2. Keep `source_id`/`target_id` and add an expression unique index on
   `(LEAST(source_id,target_id), GREATEST(...), relation_type)` — which splits the canonical-
   ordering CHECK into "applies to two types, not the third".

**Option 1 is taken.** It keeps one schema rule for all three types, and it is what makes the
"deleting from either side deletes the row" promise structural rather than handler logic.

Consequence to state plainly, because the modal already assumes it: **flipping a direction is
delete + re-add, not an update.** There is no edit affordance anywhere in the contract.

### 5.3 The direction truth table

Anchor = «الجنة». `broader_door_id` is stored; the group is derived per viewer.

| Pill pressed in the modal | Preview shown | Row stored (targets = «الآخرة») | Group on **الجنة**'s modal | Group on **الآخرة**'s modal |
|---|---|---|---|---|
| «المحدد أقل شمولية» | «الأبواب اللي هتختارها **أقل شمولية** من «الجنة»» | pair {الجنة, الآخرة}, type `Comprehensiveness`, `broader_door_id = الجنة` | «أبواب أقل شمولية» (الآخرة listed) | «أبواب أكثر شمولية» (الجنة listed) |
| «المحدد أكثر شمولية» | «الأبواب اللي هتختارها **أكثر شمولية** من «الجنة»» | same pair, `broader_door_id = الآخرة` | «أبواب أكثر شمولية» (الآخرة listed) | «أبواب أقل شمولية» (الجنة listed) |
| (تشابه) | — | pair, type `Similarity`, `broader_door_id = NULL` | «تشابه» | «تشابه» |
| (تضاد) | — | pair, type `Opposition`, `broader_door_id = NULL` | «تضاد» | «تضاد» |

Derivation rule, one line: `group = broader_door_id == anchorId ? "أقل شمولية" : "أكثر شمولية"`.

**Do not put the mockup's `broader`/`narrower` tokens on the wire.** They are consistent inside
the mockup but read from two different perspectives in two places — `data-d="broader"` is
labelled «المحدد أقل شمولية» (the *picked* doors' role, `:136`) while the seeded row's
`dir:'narrower'` means «الجنة أخص من الآخرة» (the *anchor's* role, `:175`). The wire enum is
named from the anchor and cannot be read two ways:
`AbwabRelationDirection { AnchorMoreComprehensive = 1, AnchorLessComprehensive = 2 }`.

### 5.4 No version token on relation writes

Relation writes touch `abwab_door_relations`, not `abwab_doors`, so no door's `xmin` moves and
there is nothing for a stale-token `409` to compare. The add/delete bodies therefore carry **no
`version`**, and the only `409` these routes can produce is the duplicate-pair one.

The relation row still gets its own `xmin` (the user-locked schema shape, and the two abwab
tables' convention), but **nothing consumes it** — delete addresses a row by id, add creates.
It is diagnostics/future-proofing only, exactly as `AbwabTreeDto.Version` is recorded as
diagnostics-only in `features/abwab/README.md`. **Do not add a `version` to the delete body
"for consistency"** — a token nothing checks is a lie in the contract.

Relation writes **do** go through the same full refresh as every other write: they change
`relationCount` on two rows of the snapshot. The refresh-after-write invariant is unchanged and
is not narrowed for this feature.

---

## 6. The interaction matrix

Mandatory section. "Live" = `deleted_at IS NULL`. "Dormant" = the relation row is alive but at
least one endpoint door is archived. Every cell here has a matching manual-check step in §10.

### 6.1 Dormancy is derived

```
relation is VISIBLE  ⟺  relation.deleted_at IS NULL
                        AND door_a.deleted_at IS NULL
                        AND door_b.deleted_at IS NULL
```

Computed at read time by joining both endpoints. **No `is_dormant` column** — it would have to
be rewritten by every archive, bulk-archive, restore, and archive-subtree sweep, i.e. it would
drift on exactly the paths that are hardest to test. The partial unique index filters on the
**relation's** own `deleted_at` only, so a dormant row still occupies its pair — which is what
makes restore collision-free.

### 6.2 Relation ops × door states

| Op | Both endpoints live | Anchor archived | Target archived | Section-less door | Nested door (any depth) | Ancestor ↔ descendant |
|---|---|---|---|---|---|---|
| **Read** a door's relations | rows returned, dormant excluded | route answers `200` with `[]` (every row is dormant); the UI never opens it — no entry point on an archived door (§6.4) | that row is filtered out of the live anchor's list | no difference — sections are irrelevant to relations | no difference — depth is irrelevant | returned like any other row |
| **Add** | created | not reachable from the UI; the route refuses `400` (anchor must be live) | refused `400` — a relation born dormant is a dead write | allowed | allowed | **allowed** — a door may relate to its own ancestor/descendant. No guard, deliberately: nesting is containment, relations are semantics |
| **Delete** | soft-deleted, gone from both sides | allowed at the route level, unreachable in the UI | same | allowed | allowed | allowed |
| **Duplicate add** (same pair+type, either direction) | `409` | — | — | `409` | `409` | `409` |
| **Self add** (a = b) | `400` | — | — | `400` | `400` | `400` |
| **Unknown door id** (anchor or any target) | `404` | — | — | — | — | — |
| **Cross-section pair** | allowed — sections never constrain relations | — | — | allowed | allowed | allowed |

### 6.3 Door lifecycle × relations

| Event on door D | D's relation rows | The **other** endpoint's modal | Counts | Blocking |
|---|---|---|---|---|
| **Create** D | none | — | `0`, no flag | — |
| **Edit** D (name/desc/ayah/aliases) | untouched | the chip's label follows the door's current name (names are read live, never snapshotted into the relation row) | unchanged | — |
| **Move** D (section or parent, single or bulk) | **untouched** | unchanged | unchanged | — |
| **Reorder** D (either scope) | untouched | unchanged | unchanged | — |
| **Archive** D (single) | all go **dormant**; rows untouched | **loses the entry from its own list, and its count drops** — this is the cell most likely to be missed | D → hidden; every partner −1 | archiving is **never** blocked by relations |
| **Archive** D **with a subtree** | every swept-in descendant's relations go dormant too | same, per descendant | each partner −1 | never blocked |
| **Bulk-archive** | same, per door | same | same | never blocked |
| **Restore** D | every row whose *other* endpoint is also live becomes visible again | regains the entry; count +1 | D and its partners recomputed | restore is never blocked by relations either |
| **Restore + detach** (section was archived) | untouched — relations do not know about sections | unchanged | unchanged | — |
| **Restore** D while the partner is **still archived** | stays dormant | — | still 0 for that pair | — |
| **Section create / rename / delete** | untouched | unchanged | unchanged | — |

The invariant those rows encode: **relations are attached to doors, not to structure.** Nothing
about sections, parents, ordering, or either order space touches a relation row.

### 6.4 Derived, not decided

- **The archive view gets no relations flag and no relations entry point.** Every archived
  door's visible relation count is always 0 by §6.1 — a flag there would be permanently absent
  and a menu entry would open an always-empty modal. This is the same derivation
  `features/abwab/README.md` already makes for the archive view's child-count badge ("every
  archived door's live-child count is always 0, so the badge would be meaningless"). Recorded
  as a consequence, not as a new decision, so nobody "adds it back for symmetry".
- **The picker lists live doors only**, and never the anchor itself (`abwab-relations-concept.html:221`).
  That is what makes the archived-target `400` unreachable through the UI — it exists so the
  route is not a hole.
- **Restore does not re-add anything.** The row was never deleted, so there is nothing to
  re-create; a "restore relations" step would be a second, redundant write path.

---

## 7. Phases

Every phase is one commit. The tree builds at each commit boundary, and the phase's tier is
green before the next one starts.

### Phase 1 — housekeeping, schema, migration (5 tasks)

**Files** — `CLAUDE.md`; `Backend/domain/QuranDashboard.Domain/Abwab/AbwabDoorRelation.cs` (new);
`Application.Abstractions/Abwab/AbwabRelationType.cs`, `AbwabRelationDirection.cs` (new);
`Infrastructure/Persistence/Configurations/Abwab/AbwabDoorRelationConfiguration.cs` (new);
`Infrastructure/Migrations/` (generated); `resources/db-dumps/quran-canonical/` (regenerated).

- **T101 — Housekeeping, and an honest correction to the brief.** Set the root `CLAUDE.md`
  Active-Feature line to `abwab-relations` + this plan (it currently reads "None"). **The
  smoke-harness eviction the brief asks for already landed** — commit `8f501828` (merged as
  #51, 2026-07-29) deleted `docs/feature-smoke-harness/plan.md` and cleared the Active-Feature
  line. The buffer today is `abwab-global-order` + `abwab-doors`, both closed; **opening this
  feature evicts nothing.** Record that in the commit body rather than inventing a deletion.
  The pre-existing `docs/feature-032-rate-limiting/` + `docs/feature-033-auth-roles-permissions/`
  drift stays **out of scope** — `docs/feature-abwab-global-order/plan.md` §9 already ruled that
  it be raised separately, and substituting an unrelated deletion is not the housekeeping asked
  for. The eviction that *does* need scoping is at close: §11.
- **T102 — `AbwabDoorRelation`** in `Domain/Abwab/`: `Id`, `DoorAId`, `DoorBId`, `RelationType`,
  `BroaderDoorId` (`int?`), the audit-seed columns (`CreatedAtUtc`/`CreatedBy`/`UpdatedAtUtc`/
  `UpdatedBy`/`ApprovedAtUtc`/`ApprovedBy`), the soft-delete pair (`DeletedAtUtc`/`DeletedBy`),
  and `Version` (`uint`, `xmin`) — the exact column set `AbwabDoor` carries. Two enums, **in two
  different layers** (corrected during implementation — the original draft put both in
  Abstractions, which does not compile: `QuranDashboard.Domain.csproj` has zero
  `ProjectReference` entries, and the entity's own property is typed as the type enum):
  `AbwabRelationType { Similarity = 1, Opposition = 2, Comprehensiveness = 3 }` is **persisted**,
  so it lives in `Domain/Abwab/` beside the entity (precedent: `Domain/Access/UserStatus.cs`);
  `AbwabRelationDirection { AnchorMoreComprehensive = 1, AnchorLessComprehensive = 2 }` is
  request-side only and never stored, so it stays in `Application.Abstractions/Abwab/` beside
  `AbwabReorderScope`. **Both start at 1**, per the unmapped-zero trap
  `AbwabReorderScope` documents (`docs/feature-abwab-global-order/plan.md` §6). One comment
  carrying §5.1's sentence, not a restatement of the types.
- **T103 — `AbwabDoorRelationConfiguration`.** `ToTable("abwab_door_relations")`, **explicit
  `HasColumnName` on every property** (the two existing abwab configurations' convention),
  `Version` as `IsRowVersion()` with **no** `HasColumnName` (giving it one makes EF emit a real
  column — the trap `AbwabDoorConfiguration.cs:65-68` already documents). Two FKs to
  `abwab_doors` with `OnDelete(DeleteBehavior.Restrict)` — archive is soft, so a hard-delete
  cascade would be wrong here for the same reason it is wrong on `AbwabDoor`. Constraints:
  - `CHECK (door_a_id < door_b_id)` — canonical ordering **and** the no-self-relation rule in
    one constraint;
  - `CHECK ((relation_type = 3) = (broader_door_id IS NOT NULL) AND (broader_door_id IS NULL OR
    broader_door_id IN (door_a_id, door_b_id)))` — direction exists exactly for شمولية and
    always names an endpoint. The literal `3` **must** carry a comment tying it to
    `AbwabRelationType.Comprehensiveness`; a reordered enum would otherwise change this
    constraint's meaning silently;
  - partial unique index `(door_a_id, door_b_id, relation_type) WHERE deleted_at IS NULL` —
    same live-scope filter as the doors' unique name index (`AbwabDoorConfiguration.cs:97`);
  - plain indexes on `door_a_id` and `door_b_id` (the per-door read hits both), and on
    `deleted_at` (the doors' precedent).
- **T104 — Migration. STOP CONDITION.** Generate with EF tooling only, on explicit user
  go-ahead (`Backend/CLAUDE.md`). No backfill — this is a new, empty table. Report migration
  name, generated files, build status, and that `database update` ran **locally only**.
- **T105 — Regenerate the canonical smoke dump.** A new table moves the migration head, and
  `TESTING_STRATEGY.md` §3/§5 is explicit that a **stale dump fails loud rather than skipping**
  — exactly what happened on `abwab-global-order`'s own first measurement pass. Under this
  feature's no-new-tests posture, a loud data-tier failure is a merge blocker, so this is a
  named task and not a footnote: run `Backend/scripts/create-smoke-dump --yes` after T104's
  local apply and state the outcome in the phase evidence.

**Verification**

```bash
dotnet build Backend/QuranDashboard.sln
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab."
```

`Tests.Abwab` (46 today) must stay green — the existing `AbwabSchemaTests`/`AbwabSchemaFixture`
run against a Testcontainers schema built from the migrations, so a broken migration fails here
without anything new being written.

---

### Phase 2 — read path (4 tasks)

**Files** — `Application.Abstractions/Abwab/Responses/AbwabDoorRelationDto.cs` (new),
`Responses/AbwabTreeDto.cs`; `Application.Abstractions/Abwab/IAbwabRelationsReader.cs` (new);
`Application/Abwab/Queries/GetDoorRelations/` (new);
`Infrastructure/Persistence/Reads/Abwab/EfAbwabRelationsReader.cs` (new) + `EfAbwabTreeReader.cs`;
`Api/Controllers/Abwab/AbwabDoorRelationsController.cs` (new);
`tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs`.

- **T201 — `AbwabDoorRelationDto`**, resolved **from the anchor's perspective** so the client
  groups without re-deriving direction:
  `(int Id, int OtherDoorId, string OtherDoorName, AbwabRelationType Type,
  AbwabRelationDirection? Direction)` — `Direction` non-null exactly for `Comprehensiveness`,
  computed as §5.3's one-line rule. The name is projected live from `abwab_doors`, never
  snapshotted onto the relation row (§6.3's edit cell).
- **T202 — `EfAbwabRelationsReader.GetForDoorAsync`.** `AsNoTracking` throughout (the read
  area's rule). Matches rows where the anchor is **either** endpoint, excludes soft-deleted
  relations, and excludes dormant ones by joining both endpoint doors on
  `deleted_at IS NULL` (§6.1). Orders deterministically (type, then other door's name) so the
  modal's groups are stable across refreshes.
- **T203 — Relation counts on the tree snapshot.** `AbwabTreeDoorDto` gains
  `int RelationCount`; `EfAbwabTreeReader` computes it with one grouped query over visible
  relations — **not** one query per door. Live-endpoint-only, and the reader's README gets the
  same live-only rationale it already carries for `DirectChildCount`/`DoorsInScopeCount`
  (T701). **Deliberately not added to `AbwabDoorDto`**: no door write can change a relation
  count, and adding it there would also ripple into
  `abwab-page-overlays.controller.ts:44-55`'s hand-built DTO for no gain.
- **T204 — Query handler + `GET api/abwab/doors/{doorId:int}/relations` + catalog entries.**
  New `AbwabDoorRelationsController` (`[Route("api/abwab")]`, per-action templates) rather than
  a twelfth handler on the 216-line `AbwabDoorsController`. Outcomes: `Success` → `200` with the
  list; `NotFound` (unknown door id) → `404`. An **archived** door answers `200` with `[]` — it
  exists, and dormancy is a filter, not a 404 (§6.2). Catalog entry:
  `new("api/abwab/doors/{doorId:int}/relations", "/api/abwab/doors/1/relations",
  HttpStatusCode.NotFound) { ParityOnly = true }`, with the siblings' rationale comment.
  **`ParityOnly` even though it is a safe read** — the mushaf `{verseKey}` precedent does not
  transfer: nothing in the suite creates ayahs, but `SmokeAbwabWriteTests` creates **doors** in
  the same shared schema, so a dispatched `/api/abwab/doors/1/relations` would answer `200`
  instead of `404` whenever a created door happens to hold id 1. That is order-dependence of
  exactly the kind the catalog's own comments call out, and under §8's posture a flaky smoke
  tier is a merge blocker. This entry is a **gate, not debt**: `SmokeCoverageParityTests` fails
  by name without it.
  **Also in this task:** the existing `api/abwab/tree` entry needs **no new row** (path
  unchanged) but its comment must record that the response contract gained `RelationCount`, and
  its `DerivedStatus` (`200`) must be **re-checked and confirmed, not assumed** — the precedent
  `abwab-global-order`'s T208 set for a changed contract on an unchanged path.
  **Snapshot `Version` decision, to be made here and not left open:** `Version` is
  `max(updated_at, deleted_at)` over `abwab_sections` / `abwab_doors` / `abwab_door_aliases`, so
  a relation write changes `RelationCount` **without** moving it. `abwab_door_relations` is
  **deliberately excluded** from that query — `Version` is documented diagnostics-only, nothing
  derives freshness from it, and the refresh-after-write path never consults it, so a fourth
  query would be cost with no consumer. Stated in the README (T701) so it reads as a decision,
  not an oversight.

**Verification** — a new route means the smoke filter is required alongside the API families
(`Backend/CLAUDE.md`):

```bash
dotnet build Backend/QuranDashboard.sln
dotnet test … --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab."
dotnet test … --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api"
dotnet test … --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

Evidence **must state whether `Tests.Smoke.Data` ran or skipped**.

---

### Phase 3 — write path (5 tasks)

**Files** — `Application.Abstractions/Abwab/IAbwabRelationsWriter.cs` + four exception types
(new); `Application/Abwab/Commands/Relations/AddDoorRelations/`, `DeleteDoorRelation/` (new);
`Infrastructure/Persistence/Writes/Abwab/EfAbwabRelationsWriter.cs` (new);
`Api/Controllers/Abwab/AbwabDoorRelationsController.cs` + `ApiMessages`;
`tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs`.

- **T301 — Abstractions and Arabic messages.** `IAbwabRelationsWriter` (`AddAsync`,
  `DeleteAsync`) plus the plain exception types the seam is allowed to throw:
  `AbwabRelationDuplicateException`, `AbwabRelationSelfException`,
  `AbwabRelationTargetArchivedException`, `AbwabRelationNotFoundException` — all in
  `Application.Abstractions/Abwab/`, beside the eight the doors seam already defines. New
  `ApiMessages` entries: created (singular/plural — see the Arabic-number rule in T504),
  duplicate, self, archived-target, relation-not-found, door-not-found.
- **T302 — `EfAbwabRelationsWriter.AddAsync`.** One call, N targets, **all-or-nothing** in one
  transaction (the bulk precedent). Steps: load anchor + all targets in **one** query; refuse
  unknown ids (`404`), a non-live anchor or target (`400`), and an anchor present in its own
  target list (`400`); canonicalize each pair to `(min, max)` and set `broader_door_id` from the
  request's anchor-perspective `direction`; insert all rows; save. **Its own `23505`
  translation** — the existing `SaveTranslatingWriteExceptionsAsync` is keyed to a *door name*
  for the duplicate message and is the wrong helper here; a bare `SaveChangesAsync` is how a raw
  `PostgresException` reaches the global handler as a **500**, which the locked decisions
  forbid. The new helper maps `23505` → `AbwabRelationDuplicateException` naming the
  conflicting target door(s).
- **T303 — `EfAbwabRelationsWriter.DeleteAsync`.** Soft delete by relation id
  (`deleted_at`/`deleted_by`), `AbwabRelationNotFoundException` when the id is unknown or
  already soft-deleted. **No side lookup** — the row is the relation, so "deleting from either
  side deletes it" needs no code, only this shape (§5.2). No revive-on-conflict anywhere: a
  re-add after a delete inserts a new row.
- **T304 — Handlers.** `AddDoorRelationsHandler` / `DeleteDoorRelationHandler` with exhaustive
  outcome enums, matching the doors commands' shape 1:1. `AddDoorRelationsCommand`:
  `(int DoorId, AbwabRelationType Type, AbwabRelationDirection? Direction, IReadOnlyList<int>
  TargetDoorIds)`. Guard both enums with `Enum.IsDefined` at the controller edge → `400`, and
  refuse a `Direction` that is absent for `Comprehensiveness` **or present for the other two**
  — a direction on a mutual type is a caller bug, not something to silently drop.
- **T305 — Routes, status mapping, catalog.**
  `POST api/abwab/doors/{doorId:int}/relations` → `201` (`Created`, mirroring door create) with
  the created rows in the anchor's perspective. A multi-create has no single resource URI, so the
  location is the **collection**, `api/abwab/doors/{doorId}/relations` — named here so it is not
  invented during implementation. `DELETE api/abwab/relations/{relationId:int}` →
  `204 No Content`, matching the two existing 204 routes — and therefore matching the
  `null`-envelope handling the frontend already has (§7 T503). Mapping: `400` self/archived
  target/invalid enum/empty target list, `404` unknown door or relation, `409` duplicate.
  **Two `ParityOnly = true` catalog entries** with the same rationale comment the sibling write
  routes carry ("these write, so the generic sweep must not dispatch them"), each carrying a
  `DerivedStatus` like every sibling — documentation of what a well-formed call answers against
  the empty schema (`404` for both: door 1 / relation 1 do not exist), never an assertion the
  sweep runs. Mandatory: the parity gate fails by name otherwise.

**Verification** — same four commands as phase 2; the smoke tier is **required** (new routes +
new request contracts). State the `Tests.Smoke.Data` ran/skipped line.

---

### Phase 4 — contract regeneration (1 task)

- **T401** — `npm run generate:api` (`ng-openapi-gen` + `scripts/prune-generated-api.mjs`) and
  `npm run docs:api`. Confirm the generated models carry `AbwabDoorRelationDto`, both enums, the
  add body, and `relationCount` on `abwab-tree-door-dto.ts`. This is the phases 3→5 handoff
  artifact; phase 5 cannot start before it.

**Verification** — `npm run build`: the generated models must typecheck before anything consumes
them. Nothing is expected to break here (`AbwabDoorDto` is untouched by design — T203).

---

### Phase 5 — frontend data and state (4 tasks)

**Files** — `features/abwab/data-access/abwab.api.ts`; `models/abwab.models.ts`;
`models/abwab.labels.ts`; `state/abwab-relations.controller.ts` (new);
`state/abwab-write.controller.ts`.

- **T501 — `AbwabApi`**: `getDoorRelations(doorId)`, `addDoorRelations(doorId, body)`,
  `deleteRelation(relationId)`. The delete answers `204`, so its return type is
  `Observable<ApiResponse<unknown> | null>` — the **null-envelope** shape the file already uses
  for `deleteSection`/`archiveDoor`, and the exact bug class `features/abwab/README.md`
  documents at length. Do not dereference `response.isSuccess` first.
- **T502 — Models.** `AbwabRelationType` / `AbwabRelationDirection` as readable domain unions
  (`'similarity' | 'opposition' | 'comprehensiveness'`, `'anchor-more' | 'anchor-less'`) with
  `*_TO_WIRE` maps mapping to the generated numeric enums **only at the dispatch boundary** —
  the pattern `ABWAB_ORDER_SCOPE_TO_WIRE` established. `AbwabNode` gains
  `readonly relationCount: number`, carried through `abwab-tree.builder.ts`. One view model,
  `AbwabRelationGroupVm`, holding the four display groups in contract order (تشابه · تضاد ·
  أكثر شمولية · أقل شمولية) — the grouping is §5.3's one-line rule, in one place.
- **T503 — `state/abwab-relations.controller.ts`** — the relations-facing write surface, built
  on the `abwab-sections.controller.ts` precedent (37 lines, forwards): it holds the per-door
  relations fetch and **forwards every write to `AbwabWriteController`**, which owns the 409
  policy, the outcome→message mapping, and the refresh-after-write invariant. Do **not**
  duplicate the 409 policy — one policy for all aggregates is a documented invariant of that
  file. The write controller gains the two dispatch methods and their message mapping only.
- **T504 — `abwab.labels.ts`.** Every Arabic string: the modal title/description, the four group
  headings, the three type-segment labels, the two pill labels, the two preview strings, the
  «مرتبط بالفعل بهذا النوع» disabled hint, the empty state, the delete tooltip, and the add
  button's **counted** label. The count goes through the existing Arabic number-forms helper —
  «أضف علاقتين», not «أضف 2 علاقات» (the README's counted-labels rule; this product is
  Arabic-first). **Check before authoring any conflict copy**: the write controller prefers the
  backend's own Arabic message when one is present, so a plan-authored duplicate string would be
  dead code — the mistake `features/abwab/README.md` records for the section-delete copy.

**Verification** — `npm test -- --include="src/app/features/abwab/**/*.spec.ts"` (Tier A).
Existing specs must stay green; see T605 on repair-vs-new.

---

### Phase 6 — frontend UI (5 tasks)

**Files** — `components/abwab-relations-modal/` (new); `components/abwab-tree/`;
`components/abwab-side-panel/`; `pages/abwab-page/`; `state/abwab-page-overlays.controller.ts`.

- **T601 — `abwab-relations-modal`**, the contract, end to end. It owns its own type /
  direction / picks / search / expansion state and takes its **write functions as inputs**, the
  `abwab-sections-modal` precedent the README calls deliberate. Composes
  `.qd-modal`/`.qd-modal-backdrop` + `qdModalScrollLock` like the door modal. Contract details
  that are easy to lose:
  - four groups with their colored dots, rendered **only when non-empty**
    (`abwab-relations-concept.html:201`), and one empty state when the door has none;
  - switching the type segment **clears the picks** (`:268`) and re-renders the disabled rows —
    "already linked" is per type;
  - the tree picker expands/collapses at any depth, allows picking at any depth, and a search
    **auto-expands matching paths** (`:216-223`: a node is shown if it or any descendant
    matches, and `hasKids && q` forces it open);
  - already-linked rows are disabled with «مرتبط بالفعل بهذا النوع» — per (pair, type), no
    direction term (§5.2);
  - the anchor door never appears in the picker (`:221`);
  - the add button's label counts the picks and is disabled at zero.
- **T602 — Anchor-pick mode** (the bulk entry, §4). Same component, one input flag: the N bulk
  doors are a fixed target list rendered as read-only chips, the picker **single-selects the
  anchor**, the existing-relations groups are hidden (there is no single door to show them for),
  the header reads «إضافة علاقة لـ N أبواب», and the preview reads «الأبواب المحددة (N) هتبقى
  أقل/أكثر شمولية من «X»» once an anchor is picked. **No per-row "already linked" disabling in
  this mode** — the flag is per pair and there are N pairs, so a row would have to be disabled
  only when *all* N pairs exist, which is a rule the user cannot see. The all-or-nothing `409`
  naming the conflicting door is the honest surface instead.
- **T603 — The `.flag.rel` chip comes alive.** `abwab-tree.component.html` renders
  `<span class="flags"><span class="flag rel">علاقات</span></span>` after the `.count` badge and
  before `.actions`, **only when `node.relationCount > 0`** — placement, size, and palette taken
  from the approved tree contract (`abwab-tree-concept.html:108-111` for the styles, `:435-437`
  for the placement), not reinvented. It is a chip, not a button: no tab stop, no click handler,
  so the roving-tabindex invariant is untouched. **Cards render no flag** (`:598` renders only
  `protected` there) and the **archive view renders none** (§6.4).
- **T604 — Three entry points + overlay state.** Side panel: «العلاقات» in the operations list,
  disabled on `selectedDoor() === null || bulkMode()`, matching its siblings. Row context menu:
  «العلاقات» through the existing `runContextAction` path (which selects the row first). Bulk
  bar: «إضافة علاقة» beside bulk move/archive. `abwab-page-overlays.controller.ts` gets **only**
  open/closed + anchor id + mode — it is already 281 lines and was split out of the page for
  threshold reasons; the modal's own state stays in the modal.
- **T605 — Existing-spec repair pass (not new tests).** `AbwabNode` gaining a required field
  breaks every spec fixture that builds a node literal, and the side-panel/page/tree specs count
  rendered buttons and rows. Repairing those is keeping the existing suite green — the posture
  in §8 forbids **new** test coverage for this feature, not fixing the fixtures this feature
  breaks. Do not quietly add relation assertions here; that coverage is debt, recorded in T703.

**Verification**

```bash
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/features/abwab/**/*.spec.ts"   # Tier A, focused
npm test                                                       # Tier B/C, full suite
npm run build                                                  # required before the PR
```

---

### Phase 7 — docs, debt, evidence (5 tasks)

- **T701 — Backend READMEs, in the same change.** `Persistence/Writes/Abwab/README.md`: the new
  writer, its **own** `23505` translation and why the door-name helper is wrong here,
  all-or-nothing add, soft delete with no revive, and "relations never block archive". Its
  "**one seam per aggregate, no EF types cross it**" rule now covers three writers, not two.
  `Persistence/Reads/Abwab/README.md`: the dormancy filter as a read-time join, `RelationCount`
  as live-endpoint-only with the same rationale the two other counts carry, the one-grouped-
  query rule, and **the snapshot-`Version` exclusion from T204** — a relation write changes the
  snapshot without moving `Version`, deliberately, because `Version` is diagnostics-only.
- **T702 — `features/abwab/README.md`, including a line this feature falsifies.** Its **"Zero
  dead controls. Nothing for relations…"** gotcha is now wrong for relations specifically —
  rewrite it to say the relations flag and entries are live and *why* (they are backed by a real
  read), keeping the rule intact for protection/templates/per-node flags and the «الأبواب
  الرئيسية» tab. Add: the modal and its three entry points, the archive-view derivation (§6.4),
  the no-version-token decision (§5.4), and the render-chain entries for the new component and
  controller. Also update the endpoint count in "What this feature does" (eleven writes →
  thirteen, twelve endpoints → fifteen) — a stale count is exactly the kind of drift this README
  is otherwise careful about.
- **T703 — `docs/TESTING_DEBT.md`** — new file, this feature is its first entry. One line per
  skipped test area, each naming a **concrete future trigger**, not "later":
  - backend write behavior (canonicalization, direction storage, all-or-nothing, soft delete) —
    pay when the relations writer is next touched, or when a second relation type is added;
  - backend read behavior (dormancy join, counts) — pay when the archive/restore paths are next
    changed, since that is what dormancy rides on;
  - relations smoke (`201`/`204`/`400`/`404`/`409` bodies) — pay when write protection lands and
    these routes stop being `Open`, which forces the auth cases anyway;
  - frontend modal/grouping/anchor-pick specs — pay when the modal next changes shape;
  - one e2e flow (add → see chips both sides → archive one endpoint → chip and flag vanish →
    restore → they return) — pay at the same time as the frontend specs.
  Add the pointer line to `docs/README.md`. **Catalog entries are explicitly *not* debt-able**
  and must say so in the file, so nobody reads the posture as covering the parity gate.
- **T704 — `TESTING_STRATEGY.md` §5/§6 counts re-measured, not arithmetic.** The smoke tier
  grows (three new catalog entries → the parity theory grows, and the one non-`ParityOnly`
  entry adds a dispatched sweep case); `Tests.Abwab` and the pipeline row should not move.
  Re-verify the three-way partition identity (`1,086 + 617 + 140 = 1,843` today) rather than
  adjusting one number. Frontend counts move only if T605's repair changes case counts — state
  the measured value either way. Record the T105 dump regeneration beside the existing note.
- **T705 — Evidence + the user's manual pass.** The verification runs from phases 3 and 6
  re-run at the PR boundary (there is no CI — every tier is a local gate), plus §10's checklist
  walked by the user. This feature's acceptance is the existing suites green + that walk; do not
  present the opt-in e2e run as a tier.

---

## 8. Testing posture (user decision, in effect)

- **No new tests are written in this feature** — not backend, not Vitest, not e2e.
- **Parity catalog one-liners are mandatory for every new route** and are *not* tests:
  `SmokeCoverageParityTests` fails by name when a registered route has no entry, so the three
  entries in T204/T305 are a build-level gate, not coverage. Debt-ing them would fail the suite.
- **Existing suites must run green before merge** — backend (`Tests.Abwab`, `Tests.Api`,
  `Tests.Smoke.` with the ran/skipped statement), the full Frontend suite, and `npm run build`.
  Fixing fixtures this feature breaks is maintenance, not new coverage (T605).
- **One line per skipped test area in `docs/TESTING_DEBT.md`** (T703), each with the trigger
  that pays it.
- Verification for behavior is therefore: **existing suites + the §10 manual pass.** No phase in
  §7 contains a test task, and no evidence in this feature may claim behavioral coverage it does
  not have.

---

## 9. Traps and contract conflicts — do not "fix" these in review

- **The mockup violates the locked vocabulary in two places.**
  `abwab-relations-concept.html:183` sets `TYPE_META.hier.label = 'أعم / أخص'` and `:155`'s
  hint paragraph says «جرّب تبديل النوع (أعم/أخص بيظهر الاتجاه)». The locked decision is
  comprehensiveness vocabulary only, and the *rendered* group headings and type segment (`:130`,
  `:196-197`) already comply — `:183` is unreachable in the mockup's own rendering because
  `:202` takes the label from the `groups` array instead, which is why it survived. Recorded the
  way `features/abwab/README.md` records the section-delete copy conflict: named by line,
  decision wins, **do not copy either string into `abwab.labels.ts`**.
- **`broader`/`narrower` never reach the wire** (§5.3). Two perspectives share those tokens in
  the mockup; the wire enum is anchor-relative.
- **The direction pill needs two copies, one per mode — and the trap is in the copy layer, not the
  wire.** «المحدد» means *the doors the picker selects*, and the two modes select opposite sides:
  targets in door mode, the anchor in anchor-pick mode. The door-mode pair
  («المحدد أقل/أكثر شمولية») therefore states the **opposite** of what the row stores when reused
  in anchor-pick mode, where `anchor-more` makes the picked door the more comprehensive endpoint.
  Anchor-pick mode uses its own pair («الباب المختار أكثر/أقل شمولية»). Recorded because §5.3
  eliminated this exact ambiguity on the wire and in the grouping, and it still survived into the
  labels file — killing it in one layer does not kill it in the next.
- **Direction is not editable.** No PATCH, no flip endpoint. Delete + re-add, because the
  contract has no edit affordance and a flip is a different row under the canonical shape.
- **`23505` must be translated in the new writer.** The existing helpers are door-name-keyed;
  reusing one produces a wrong Arabic message, and skipping translation produces a **500** where
  the locked decision demands a `409`.
- **No `is_dormant` column, ever** (§6.1). It is a derived predicate; storing it means every
  archive/restore path owes it an update.
- **Do not add relations to the archive view** (§6.4) — permanently-empty by construction.
- **Do not add `relationCount` to `AbwabDoorDto`** (T203) — no write can change it, and it
  ripples into the overlays controller's hand-built DTO for nothing.
- **Do not add a `version` to the relation delete body** (§5.4) — nothing checks it.
- **The 204 null-envelope trap applies to the delete route** — `HttpClient` yields `null`, not
  an envelope. This already cost this feature area one production-path bug; the handling exists,
  just do not bypass it.
- **Ancestor↔descendant relations are legal.** A guard "because it's already nested" would be a
  new rule nobody asked for (§6.2).

---

## 10. The user's manual-test checklist

Given §8, this is the behavioral acceptance. Each item maps to a §6 matrix cell. Run against the
local dev DB with two sandbox sections so cross-section cases are reachable.

**Add / read**

1. Open «العلاقات» from the side panel on a live door → modal opens, empty state shows.
2. تشابه + pick **one** door → button reads «أضف العلاقة» → add → chip appears under «تشابه».
3. Open the **other** door's modal → the same chip appears under «تشابه». *(mutual derivation)*
4. تضاد + pick **three** doors → button reads «أضف 3 علاقات» → add → three chips under «تضاد».
5. شمولية + «المحدد أقل شمولية» + pick one → it lands under «أبواب أقل شمولية» here, and under
   «أبواب أكثر شمولية» on the other door. *(§5.3 row 1)*
6. شمولية + «المحدد أكثر شمولية» + pick one → the mirror of step 5. *(§5.3 row 2)*
7. The direction preview text updates the moment the pill is switched, and names the anchor.

**Picker behavior**

8. A door already linked **by the selected type** is disabled with «مرتبط بالفعل بهذا النوع»;
   switching the type segment re-enables it and clears the picks.
9. Search matching a deep child auto-expands its ancestors; clearing search restores manual
   expansion state.
10. The anchor door never appears in the picker.
11. Pick at depth 0 and at depth 2+ — both add normally. *(nesting is irrelevant)*
12. Relate a **section-less** door (one whose section was archived, or one created with no
    section) → succeeds like any other. *(§6.2's "Section-less door" column — sections are
    irrelevant to relations in both directions)*

**Refusals**

13. Add the same pair+type twice → `409`, Arabic message names the door, nothing is created.
14. Add the **opposite** شمولية direction for a pair that already has one → `409` too. *(§5.2)*
15. Add three targets where one is a duplicate → **nothing** is created. *(all-or-nothing)*
16. Relate a door to its own child, and to its own parent → both succeed. *(§6.2)*
17. Relate doors in two different sections → succeeds.

**Delete**

18. ✕ on a chip → gone here **and** on the other door's modal.
19. Re-add the same pair+type after deleting → succeeds. *(soft delete does not block)*

**Flag and counts**

20. A door with ≥1 relation shows the «علاقات» chip in the tree; a door with none does not.
21. The chip appears in the tree only — **not** on cards, **not** in the archive view.
22. After an add and after a delete, the flag/count update without a manual page reload.
    *(refresh-after-write)*

**Archive / restore — the dormancy cells**

23. Archive door A (which has relations) → the archive **succeeds**, never blocked.
24. Open partner B's modal → A's chip is **gone**, and B's tree flag drops (disappears if A was
    B's only relation). *(§6.3, the most-missed cell)*
25. Restore A → the chip and the flag **come back on both sides**, with no re-adding.
26. Archive a door **with a subtree** where a descendant has relations → the descendant's
    partners lose their chips too; restore brings them all back.
27. Bulk-archive two related doors, then restore only one → its relation stays dormant until the
    partner is restored too.
28. Archive a door, restore it into a **detached** state (its section was archived meanwhile) →
    relations are unaffected by the detach.

**Structure ops never touch relations**

29. Move a related door to another section / under another parent (single and bulk) → chips and
    counts unchanged on both sides.
30. Reorder a related door in both order spaces → unchanged.
31. Rename a related door → the chip on the partner's modal shows the **new** name.
32. Create a section, rename it, and delete an empty one while related doors exist → every chip
    and count is unchanged on both sides. *(§6.3's section row — the cell most easily assumed
    rather than checked, because no relation code runs on any of these paths)*

**Bulk entry (T602 — confirm this reading)**

33. Enter bulk mode, select three doors, press «إضافة علاقة» → the modal opens in anchor-pick
    mode: the three doors are shown as fixed targets, the picker single-selects the anchor, the
    existing-relations groups are hidden.
34. Pick an anchor + شمولية + a direction → three relations created in one call; a duplicate
    among them fails the whole batch with one `409`.
35. Switch the direction pill in anchor-pick mode → it names the **picked anchor**'s side
    («الباب المختار أكثر/أقل شمولية»), not «المحدد», and the preview below it agrees. Add, then
    open the anchor's own modal and confirm the targets landed in the group the pill promised.
    *(the pill reads from the opposite side in each mode — §5.3's two-perspectives trap, which
    survived in the copy layer until review)*

---

## 11. Close checklist — planning-artifact sweep

- **Opening this feature evicts nothing.** `docs/feature-smoke-harness/` was already swept in
  `8f501828` (#51, 2026-07-29), which also cleared the Active-Feature line. The buffer is
  `abwab-global-order` (merged #50) + `abwab-doors` (#48/#49), both closed; this feature is the
  only open one.
- **Closing this feature makes the buffer `abwab-relations` + `abwab-global-order`, which evicts
  `docs/feature-abwab-doors/`** — and that is a **real repoint pass into code**, not a doc
  deletion. Known inbound references, to be re-grepped at close, not trusted from this list:
  `features/abwab/README.md` (`plan-slice-b.md` §4.1/§2/§6.4-R12/§7-T407/T503, `plan.md` §5.1/
  §4/§10, `plan-slice-b2.md`), `models/abwab.models.ts:55,59` (§7 T407, §4.4),
  `data-access/abwab.api.ts:26` (§4, input 5), `docs/feature-abwab-global-order/plan.md` §1/§4.
  Each must be repointed to code + the nearest `README.md`, or the fact folded into that README,
  **before** the deletion. Dangling links are a defect (root `CLAUDE.md`).
- **Pre-existing drift, flagged not fixed:** `docs/feature-032-rate-limiting/` and
  `docs/feature-033-auth-roles-permissions/` are still past the buffer, as
  `docs/feature-abwab-global-order/plan.md` §9 already recorded. Raise separately.
- **Not deletable by this rule:** `Backend/report/feature-008-*` and `feature-009-*` are import
  **evidence** (canonical counts/provenance), which the lifecycle rule protects per file.

## 12. Obligations checklist

- [x] Migration by EF tooling only, on explicit go-ahead; **local apply only**; name/files/build
      reported (T104) — `20260729135714_AddAbwabDoorRelations`
- [x] Canonical smoke dump regenerated after the migration — a stale dump **fails loud** (T105)
- [x] Three `SmokeRouteCatalog` entries, **all `ParityOnly`** (T204, T305), **plus** the
      `api/abwab/tree` entry's contract-change comment and re-checked `DerivedStatus` (T204).
      **Mandatory gate, not debt**
- [x] `Tests.Abwab` + `Tests.Api` + `Tests.Smoke.` run at phases 2, 3 and at the PR boundary,
      each with the `Tests.Smoke.Data` **ran/skipped** statement — PR boundary (2026-07-29):
      full suite 1,843 passed / 0 skipped, no-pipeline 1,086, smoke **140 passed, 0 skipped
      (data tier RAN)**, `Tests.Abwab` 46
- [x] Full Frontend suite + `npm run build` before the PR (T605, phase 6) — 190 files / 2,158
      tests, build clean; e2e re-run too: 28 + 20 = 48 passed
- [x] `docs/TESTING_DEBT.md` created, each line naming its paying trigger; pointer added to
      `docs/README.md` (T703)
- [x] `Writes/Abwab/README.md` + `Reads/Abwab/README.md` + `features/abwab/README.md` updated in
      the same change, including the **"Zero dead controls"** correction and the endpoint counts
      (T701, T702) — 13 writes / 15 endpoints, measured off the controllers
- [x] `TESTING_STRATEGY.md` counts re-measured and the partition identity re-verified (T704) —
      `1,086 + 617 + 140 = 1,843`, every term unchanged, which is itself the finding
- [x] Root `CLAUDE.md` Active-Feature line → `abwab-relations` (T101)
- [x] Engineering review run against this plan at the PR boundary (2026-07-29). One MAJOR — the
      anchor-pick direction pill reused the door-mode copy and inverted the write's meaning — plus
      three MINORs (mouse-only picker expansion, missing `aria-labelledby`, two §6 cells with no
      §10 step). All fixed in the same branch; §9 gained the copy-layer trap, §10 gained steps 12,
      32 and 35, and `docs/TESTING_DEBT.md` rows 2–4 were widened to cover them
- [ ] The §10 manual pass walked by the user before merge (T705) — **the one open item**
- [x] Clean-code self-check (`.claude/skills/engineering-review/references/clean-code-guard/`)
      before delivery, per the root `CLAUDE.md`. The test-code self-check applies **narrowly to
      T605's fixture repair and one e2e assertion** — test files are edited, so it is not out of
      scope, but there is no new coverage for it to judge. Three places pinned the old "no
      relations control" truth and all three were repaired, not extended:
      `abwab-side-panel.component.spec.ts`, `abwab-page.component.spec.ts`, and
      `e2e/abwab-operations.e2e.ts` (the last was not anticipated by the plan)
- [ ] Branch `abwab-relations` off `dev`; PR into `dev`. **Never `main`** — branch done, PR open
