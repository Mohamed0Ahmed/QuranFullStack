# Feature Specification: Abwab Preflight — Documentation-Only Freeze

**Feature Branch**: `027-abwab-preflight`

**Created**: 2026-07-22

**Status**: Draft

**Input**: User description: "Create only 027-abwab-preflight in specs/027-abwab-preflight using docs/feature-abwab-management/MASTER_PLAN.md as the sole canonical source; preserve its exact scope, terminology, catalogues, matrices, ownership, acceptance criteria, and DAG, introduce no new decisions, include nothing owned by 028–034, and do not implement code."

> **Canonical source**: `docs/feature-abwab-management/MASTER_PLAN.md` is the sole
> canonical product and architecture source for Abwab Spec Kits `027`–`034`. This
> specification records and freezes that source in Spec-Kit-ready form. It introduces no
> new product or architecture decision, reinterprets nothing, and owns none of the
> implementation reserved for `028`–`034`. Section references (e.g. §5.2) point to the
> Master Plan. Where a conflict is perceived, the Master Plan governs and a genuine
> change returns to an independent amendment/re-review of that document, never a local
> decision here (Master Plan §2, §17, §20.1).

## Overview *(context)*

`027-abwab-preflight` is the first of eight top-level Abwab Spec Kits and is
**documentation-only**. It produces no code, package, migration, seed, database,
runtime, mock, or implementation task (Master Plan §1, §18.1). Its deliverable is a
frozen, traceable copy of the canonical vocabulary, normalization contract, permission
catalogue, cross-cutting invariants, domain/persistence model, registries, matrices,
API/conflict contract, source contracts, and dependency DAG, plus a
requirement-to-owner traceability catalogue that hands `028`–`034` an unambiguous,
decision-free starting point. It exits with no open product or architecture choice.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Freeze canonical vocabulary, normalization, and permission catalogue (Priority: P1)

A downstream Spec Kit author for `028`–`034` opens `027` and finds the frozen Arabic/
English entity vocabulary, visible labels, no-drag rule, the exact Arabic normalization
algorithm, and the exact canonical permission catalogue recorded byte-for-code, so every
later Kit inherits identical strings for backend authorization, DB seed/storage, `/me`,
frontend visibility, generated contracts, and tests (Master Plan §5, §5.1, §5.2, §18.1
step 1).

**Why this priority**: These strings and the normalization algorithm are the shared
identity keys that every later Kit depends on. Any drift here silently corrupts
authorization, uniqueness, search, and contract parity across all seven downstream Kits.

**Independent Test**: Compare each recorded vocabulary entry, normalization step, and
permission code in the Spec Kit against the Master Plan; the freeze is valid when every
entry matches byte-for-code with zero synonyms and zero additions or omissions.

**Acceptance Scenarios**:

1. **Given** the frozen vocabulary table, **When** an author checks the entity term,
   **Then** it is `باب`/`أبواب` and never `تصنيف`, with the `/gates` route key and
   `الأبواب` page title preserved (§5).
2. **Given** the normalization contract, **When** an author checks mark removal, **Then**
   only scalars in the frozen Unicode-16 Arabic-mark set are removed, `ة` is not
   normalized to `ه`, and no runtime "all marks" predicate is used (§5.1).
3. **Given** the permission catalogue, **When** an author looks for `category.copy`, an
   Owner-bypass code, or a SystemOwner-direct-link code, **Then** none exist, and
   `permission.*`, `audit.restore`, and `safetyPoint.*` carry `SystemOwnerOnly`
   metadata while `attribution.view` carries `DashboardAdminBaseline` metadata (§5.2).

---

### User Story 2 - Copy registries, matrices, contracts, and the DAG without reinterpretation (Priority: P1)

A downstream Spec Kit author finds the aggregate/audit/concurrency/protection/restore
registry, the action-and-protection matrix, the notification event/recipient matrix, the
API and HTTP-409 conflict-code contract, the attribution-source and note contracts, and
the single authoritative dependency DAG copied exactly, so ownership, restore classes,
conflict codes, and predecessors are unambiguous before any implementation begins
(Master Plan §8, §9, §10, §11, §13, §16, §18.1 step 2).

**Why this priority**: These matrices assign every mutable state, action, notification,
conflict, and dependency exactly once. Reinterpreting any cell reopens a locked decision
and breaks the restore barrier and DAG the whole portfolio relies on.

**Independent Test**: Each registry/matrix/contract/DAG entry in the Spec Kit is checked
against its Master Plan source; the copy is valid when restore classes, conflict codes,
recipients, source contracts, and all DAG edges reproduce the source with no changed,
added, or dropped entry.

**Acceptance Scenarios**:

1. **Given** the restore registry, **When** an author checks any mutable state, **Then**
   it has exactly one restore class ("No adapter" is an explicit class) and its owner/
   adapter prerequisite matches §8.
2. **Given** the conflict catalogue, **When** an author looks up a 409 case, **Then** the
   exact `abwab.*` string and its condition match §11 and no code is added, renamed, or
   remapped.
3. **Given** the DAG, **When** an author reads predecessors, **Then** `027` has none and
   its only successor edge is `027 → 028`, and all 17 edges plus the direct-dependency
   table and safe-parallelism rules reproduce §16.

---

### User Story 3 - Produce the requirement-to-owner traceability catalogue for 028–034 (Priority: P1)

A portfolio reviewer opens the traceability catalogue and sees every locked invariant
group mapped to its canonical Master Plan clauses, exactly one implementation owner set,
and at least one primary acceptance owner, so no invariant is orphaned and no two Kits
silently claim the same implementation (Master Plan §18.1 step 4, §19).

**Why this priority**: The traceability catalogue is `027`'s primary original product
and the acceptance gate for the freeze; without a complete owner mapping, downstream
sequencing and acceptance cannot be verified.

**Independent Test**: Enumerate every locked invariant group; the catalogue is valid when
each maps to exactly one implementation owner and at least one acceptance owner, with
zero unassigned or double-implemented invariants.

**Acceptance Scenarios**:

1. **Given** the traceability catalogue, **When** a reviewer scans implementation owners,
   **Then** every invariant group has exactly one implementation owner set (§18.1 exit).
2. **Given** the same catalogue, **When** a reviewer scans acceptance owners, **Then**
   every invariant group has at least one primary acceptance owner (§18.1 exit).

---

### User Story 4 - Record purely-visual tokens within the locked presentation (Priority: P2)

A design/spec author records remaining purely-visual labels and tokens — such as the
non-color changed-value diff indicator — strictly inside the already-locked scholarly/
RTL presentation, so visual detail is captured without changing any behavior, ownership,
scope, or data contract (Master Plan §5, §6.3, §18.1 step 3).

**Why this priority**: Visual tokens must be captured for downstream UI Kits, but they
are subordinate to behavior; recording them incorrectly must never silently alter a
locked contract.

**Independent Test**: Each recorded visual token is checked for behavioral neutrality;
valid when it changes only presentation and touches no behavior, ownership, scope, or
data contract.

**Acceptance Scenarios**:

1. **Given** the changed-value diff indicator, **When** an author records it, **Then** it
   is `--qd-accent-text`/allowed green plus a textual or icon marker, never color alone
   (§5, §6.3).

---

### User Story 5 - Pass automated documentation-consistency checks (Priority: P2)

A reviewer runs the automated documentation checks that compare all copied direct
dependency sets and catalogue codes against the Master Plan, confirming the freeze is
faithful before `027` is accepted and `028` is authorized (Master Plan §18.1 exit).

**Why this priority**: The freeze's value is only as good as its fidelity; the automated
comparison is the objective acceptance evidence that no drift entered the copy.

**Independent Test**: Run the documentation-consistency comparison; valid when copied
direct-dependency sets and catalogue codes match the Master Plan with zero mismatches.

**Acceptance Scenarios**:

1. **Given** the copied dependency sets and catalogue codes, **When** the checks run,
   **Then** they report zero mismatches against the canonical Master Plan (§18.1 exit).

---

### Edge Cases

- A downstream Kit attempts to reopen a locked product/architecture decision → rejected;
  it returns to an independent amendment/re-review of the Master Plan, never a local
  decision gate (§2, §17, §20.1).
- A recorded normalization step or permission code does not match the Master Plan
  byte-for-code → the freeze fails acceptance until it matches exactly (§18.1 exit).
- An invariant maps to zero or more than one implementation owner → the traceability
  catalogue fails acceptance (§18.1 exit).
- Any "provisional", "if needed", or future product/architecture decision appears in the
  Spec Kit → rejected; there are no open Decision Gates (§5, §20.1).
- Any code, package, migration, seed, database change, runtime, mock, or implementation
  task owned by `028`–`034` is introduced in `027` → out of scope and rejected (§1,
  §18.1 exit).
- A perceived conflict between a copied item and repository reality → repository source/
  config/tests are authoritative only for verified current-implementation facts (§4);
  the Master Plan governs product/architecture decisions and changes go to amendment.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Spec Kit MUST record the frozen Arabic/English entity vocabulary and
  visible labels exactly as Master Plan §5, including `باب`/`أبواب` (never `تصنيف`),
  backend terms `Category`/`Section`, the `/gates` route key with `الأبواب` title, the
  permanent default section `أبواب غير مصنفة`, the global view `كل الأبواب` (a view,
  never a persisted Section), and the UI labels `قوالب الأبواب`, `أسماء البحث`,
  `ملاحظات الرابط`, and `مساحة إعداد الطلبات`.
- **FR-002**: The Spec Kit MUST record the no-drag-and-drop rule as a frozen product
  constraint applying everywhere in the application (§3.2, §9, §14.2).
- **FR-003**: The Spec Kit MUST record the exact Arabic normalization algorithm of §5.1
  byte-for-code — NFC (Unicode 16.0/UAX #15), whitespace collapse, tatweel removal, the
  frozen Unicode-16 Arabic-mark removal set, `أ/إ/آ/ٱ → ا`, `ى → ي`, `ة` NOT normalized
  to `ه`, display-string preservation, and the single shared input/output fixture-corpus
  requirement — with no runtime "all marks" predicate.
- **FR-004**: The Spec Kit MUST record the exact canonical permission catalogue of §5.2
  using only the listed codes, including assignability metadata (`SystemOwnerOnly` for
  `permission.*`/`audit.restore`/`safetyPoint.*`; `DashboardAdminBaseline` for
  `attribution.view`), the non-existent codes (`category.copy`, a grantable Owner-bypass
  permission, a SystemOwner-direct-link permission), the SystemOwner automatic-policy
  rule, and the aggregate-subresource mapping rules — forbidding synonyms such as
  create/add or remove/delete.
- **FR-005**: The Spec Kit MUST record the frozen supersessions of §2.1 so they cannot
  leak downstream, including the plain-string `RepresentativeQuranExcerpt`, relationship/
  reorder operations being outside the ordinary 24-hour gate, the grouped-link ≥2-member
  and delete-whole rule, permission assignments and active System Owner membership being
  current security state outside Product Restore, canonical highlights using
  `QuranWord.Id`, and ordered notes replacing stale source-description wording.
- **FR-006**: The Spec Kit MUST copy the aggregate/audit/concurrency/protection/restore
  registry of §8 without reinterpretation, preserving each mutable state's single restore
  class ("No adapter" as an explicit class), owner/writer, and adapter/planner
  prerequisite, and the `033` restore-barrier prerequisite set.
- **FR-007**: The Spec Kit MUST copy the action-and-protection matrix of §9 without
  reinterpretation, preserving the ordinary-24h/manual/two-hour-stabilization columns and
  the rule that "last editor/Owner allowed" never overrides manual protection or
  stabilization.
- **FR-008**: The Spec Kit MUST copy the durable notification event and recipient matrix
  of §10 without reinterpretation, including exclusions/navigation and the no-Outbox,
  in-producing-transaction creation rule.
- **FR-009**: The Spec Kit MUST copy the exact HTTP-409 conflict-code catalogue and the
  400/403/404/503 mappings of §11 without reinterpretation, recording that no Spec Kit
  may add, rename, or remap an Abwab error code without amending the canonical plan.
- **FR-010**: The Spec Kit MUST copy the attribution-source, note, and current-door
  contracts of §13 and §13.1 without reinterpretation, including the mutashabihat
  word-extraction deferral and the current-door no-copy / no link-block-reorder rules.
- **FR-011**: The Spec Kit MUST copy the single authoritative dependency DAG of §16 — the
  renderer-independent edge list, the direct-dependency table, and the safe-parallelism
  rules — exactly, with `027` having no predecessor and `027 → 028` as its only
  successor edge.
- **FR-012**: The Spec Kit MUST record the cross-cutting architecture invariants (§6) and
  the canonical domain/persistence model (§7) as frozen reference and MUST assign each to
  its implementation and acceptance owner through the traceability catalogue, without
  implementing or reinterpreting them.
- **FR-013**: The Spec Kit MUST record the in-scope and out-of-scope boundaries (§3.1,
  §3.2) and the operational-fluency invariant (§3.3) unchanged.
- **FR-014**: The Spec Kit MUST record the verified repository-reality constraints (§4)
  as current-implementation facts that bound downstream Spec Kits, not as new decisions.
- **FR-015**: The Spec Kit MUST produce a requirement-to-task/test traceability catalogue
  for `028`–`034` (§18.1 step 4, §19) that assigns every locked invariant group to
  exactly one implementation owner and at least one primary acceptance owner.
- **FR-016**: Recorded purely-visual labels/tokens (e.g., the non-color changed-value
  diff indicator using `--qd-accent-text`/allowed green plus a textual or icon marker;
  §5, §6.3) MUST remain within the already-locked scholarly/RTL presentation and MUST NOT
  change behavior, ownership, scope, or data contracts (§18.1 step 3).
- **FR-017**: The Spec Kit MUST NOT introduce any provisional, if-needed, or future
  product or architecture decision; there are no open Decision Gates (§5, §20.1).
- **FR-018**: `027` MUST create documentation only — no code, package, migration, seed,
  database, runtime, mock, or implementation task is performed in `027` itself (§1,
  §18.1 exit).
- **FR-019**: The Spec Kit's normalization and permission lists MUST match the Master
  Plan byte-for-code (§18.1 exit).
- **FR-020**: Automated documentation checks MUST compare all copied direct-dependency
  sets and catalogue codes against the canonical Master Plan (§18.1 exit).
- **FR-021**: The Spec Kit MUST NOT include any content owned by `028`–`034` — the
  fail-closed substrate, core sections/categories/tree/protection, relationships/
  templates, attribution links, workspace/review/notifications, audit/restore, or
  realtime/hardening/release — per the ownership fixed in §17 and §18.2–§18.8. It records
  ownership; it does not perform it.
- **FR-022**: The Spec Kit MUST record its entry preconditions: the Master Plan has
  received an independent adversarial PASS and superseded planning sources have been
  removed, so it is the sole canonical source (§1, §16.2, §18.1 entry).

### Key Entities *(frozen documentation artifacts)*

- **Frozen Vocabulary & Labels**: The Arabic/English entity terms and visible UI labels
  of §5, plus the no-drag rule; identity strings reused verbatim by every later Kit.
- **Arabic Normalization Contract**: The single algorithm of §5.1 and its shared
  input/output fixture corpus; governs all "normalized" name/alias/search projections.
- **Canonical Permission Catalogue**: The exact code set and assignability metadata of
  §5.2; the sole source of authorization codes for `028`–`034`.
- **Aggregate/Restore Registry**: The §8 mapping of every mutable state to owner, audit
  capture, concurrency token, protection, single restore class, and adapter prerequisite.
- **Action & Protection Matrix**: The §9 mapping of each action to ordinary-24h, manual,
  and two-hour stabilization behavior.
- **Notification Event & Recipient Matrix**: The §10 mapping of each event to recipients
  and exclusions/navigation, created in the producing transaction with no Outbox.
- **API & Conflict-Code Contract**: The §11 endpoint families, HTTP-409 `abwab.*` code
  catalogue, and 400/403/404/503 mappings.
- **Attribution-Source & Note Contracts**: The §13/§13.1 per-source selection/validation
  contracts, ordered-note editing rules, and current-door no-copy rules.
- **Dependency DAG**: The §16 edge list, direct-dependency table, and safe-parallelism
  rules for the eight Spec Kits.
- **Frozen Supersessions**: The §2.1 supersessions that must not leak downstream.
- **Requirement-to-Owner Traceability Catalogue**: `027`'s primary product (§18.1 step 4,
  §19) mapping every locked invariant group to clauses, one implementation owner, and at
  least one acceptance owner (reproduced in Appendix A).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of recorded vocabulary, label, normalization, and permission-catalogue
  entries match the Master Plan byte-for-code (0 diffs) (§18.1 exit).
- **SC-002**: Every locked invariant group is assigned exactly one implementation owner
  and at least one acceptance owner — 0 invariants with zero or more than one
  implementation owner (§18.1 exit, §19).
- **SC-003**: Automated documentation checks over copied direct-dependency sets and
  catalogue codes report 0 mismatches against the Master Plan (§18.1 exit).
- **SC-004**: 0 provisional, if-needed, or future product/architecture decisions appear
  anywhere in the Spec Kit (§5, §20.1).
- **SC-005**: 0 lines of code, 0 packages, 0 migrations, 0 seeds, 0 database changes, 0
  runtime artifacts, 0 mocks, and 0 implementation tasks are produced by `027` (§1,
  §18.1 exit).
- **SC-006**: 0 items owned by `028`–`034` are implemented in `027` (scope-leak count = 0)
  (§17, §18.2–§18.8).
- **SC-007**: The copied DAG reproduces all 17 edges and the direct-dependency table with
  `027` having no predecessor and `027 → 028` as its only successor edge (§16).

## Assumptions

- The Master Plan has passed its independent adversarial review and is canonical, and
  superseded planning/decision sources have been removed, so `docs/feature-abwab-management/MASTER_PLAN.md`
  is the sole canonical source (§1, §2, §16.2, §18.1 entry).
- No other planning, remediation, or review document is a normative input; the plan's own
  matrices/registries/checklists are traceability aids that restate decisions already
  made in the same document, not external dependencies (§2).
- Repository source, configuration, and tests are authoritative only for verified
  current-implementation facts (§4); they do not override the plan's locked product/
  architecture decisions.
- Spec Kits `028`–`034` are generated later from the Master Plan strictly per the DAG and
  internal checkpoints; `027` neither generates them nor performs their work (§1, §16).
- The post-`034` implementation review is review-only and is not a ninth Spec Kit (§1,
  §18.8, §20.2).
- The project constitution file is an unfilled template; no additional local project
  principles constrain this documentation-only freeze.

---

## Appendix A — Frozen Reference (copied from the Master Plan, no reinterpretation)

The tables below are copied verbatim in scope from the canonical Master Plan for
traceability. The Master Plan remains authoritative; on any perceived discrepancy the
Master Plan governs (§2).

### A.1 Frozen vocabulary and labels (§5)

| Concern | Frozen value |
|---|---|
| Arabic entity term | `باب` / `أبواب`; never `تصنيف` for this entity |
| Backend entity terms | `Category`, `Section` |
| Existing route key | `/gates`; the visible page title is `الأبواب` |
| Permanent default section | `أبواب غير مصنفة` |
| Global root view | `كل الأبواب`; a view, never a persisted Section |
| Template UI label | `قوالب الأبواب` |
| Search-alias UI label | `أسماء البحث` |
| Link-note UI label | `ملاحظات الرابط` |
| Personal preparation page | `مساحة إعداد الطلبات` |
| Audit changed-value treatment | `--qd-accent-text`/allowed green plus a textual or icon indicator; never color alone |
| Complex editable forms | Angular Reactive Forms; Signals own page/UI state, not the same form-field values |

### A.2 Arabic normalization contract (§5.1)

One algorithm for category names, category aliases, section names, template names, and
any database uniqueness/search projection that says "normalized":

1. Unicode normalize to NFC using Unicode 16.0 / UAX #15 semantics. A future Unicode
   data-version change that alters any accepted normalization vector requires an
   independently reviewed Master Plan amendment.
2. Trim leading/trailing whitespace and collapse internal Unicode whitespace runs to one
   ASCII space.
3. Remove tatweel (`ـ`).
4. Remove a scalar exactly when it is in this frozen Unicode-16 Arabic-mark set:
   `U+0610–U+061A`, `U+064B–U+065F`, `U+0670`, `U+06D6–U+06DC`, `U+06DF–U+06E4`,
   `U+06E7–U+06E8`, `U+06EA–U+06ED`, `U+0897–U+089F`, `U+08CA–U+08E1`, `U+08E3–U+08FF`,
   or `U+10EFC–U+10EFF`. Do not use a runtime-dependent "all marks" predicate and do not
   remove adjacent format characters or letters outside this set.
5. Normalize `أ`, `إ`, `آ`, and `ٱ` to `ا`.
6. Normalize `ى` to `ي`.
7. Do **not** normalize `ة` to `ه`.
8. Preserve the original display string; only comparison/search uses the normalized value.

One canonical input/output fixture corpus is shared by backend domain, database/index,
API, and frontend search/parity tests; the database stores normalized values and
uniqueness constraints are the final race-safe guard.

### A.3 Canonical permission catalogue (§5.2)

| Domain | Codes |
|---|---|
| Category | `category.view`, `category.add`, `category.edit`, `category.move`, `category.reorder`, `category.delete`, `category.restore` |
| Section | `section.view`, `section.add`, `section.edit`, `section.reorder`, `section.delete` |
| Manual protection | `protection.view`, `protection.apply`, `protection.lift` |
| Relationships | `relationship.view`, `relationship.add`, `relationship.edit`, `relationship.delete`, `relationship.restore` |
| Templates | `template.view`, `template.add`, `template.edit`, `template.delete`, `template.restore`, `template.apply` |
| Attribution | `attribution.view`, `attribution.request.create`, `attribution.request.withdraw`, `attribution.request.approve`, `attribution.request.reject`, `attribution.request.requestChanges` |
| Audit/restore | `audit.view`, `audit.restore`, `safetyPoint.view`, `safetyPoint.create`, `safetyPoint.edit` |
| Notifications | `notification.view`, `notification.markRead` |
| Permission administration | `permission.view`, `permission.grant`, `permission.revoke` |

Frozen rules (§5.2): `category.copy`, a grantable Owner-bypass permission, and a
SystemOwner-direct-link permission do not exist; a current enabled System Owner satisfies
every ordinary catalogue permission through the `SystemOwner` policy without persisted
grant rows and cannot bypass manual protection or stabilization; `permission.*`,
`audit.restore`, and `safetyPoint.*` are `SystemOwnerOnly`; `attribution.view` has
`DashboardAdminBaseline`; identical strings/metadata are used by backend authorization,
DB seed/storage, `/me`, frontend visibility, generated contracts, and tests; every
protected endpoint uses an existing code or amends this table first; and aggregate
subresources do not invent child-CRUD permissions (CategorySearchAlias edits require
`category.edit`; `template.add`/`template.edit`/`template.delete`/`template.restore`/
`template.apply` own their exact scopes). Backend handler enforcement is authoritative;
frontend visibility is UX only.

### A.4 HTTP-409 conflict-code catalogue (§11)

| Code | Exact conflict |
|---|---|
| `abwab.row_stale` | an expected `xmin` fails and no more-specific revision code below applies |
| `abwab.timeline_generation_stale` | command `ExpectedTimelineGeneration` differs from the locked current generation |
| `abwab.tree_revision_stale` | expected TreeRevision fails |
| `abwab.template_revision_stale` | expected TemplateRevision fails |
| `abwab.link_revision_stale` | expected LinkRevision fails |
| `abwab.workspace_revision_stale` | expected WorkspaceRevision fails |
| `abwab.request_revision_stale` | expected AttributionRequestRevision fails |
| `abwab.pending_exists` | another Pending request owns the category reservation |
| `abwab.invalid_request_transition` | command/request/workspace status pair is not a legal §7.6 edge |
| `abwab.workspace_state_conflict` | a personal edit/delete/wait action is invalid for its current workspace state |
| `abwab.category_name_conflict` | normalized sibling/root name uniqueness fails, including move/template/restore |
| `abwab.category_alias_conflict` | duplicate active normalized alias exists in one category |
| `abwab.section_name_conflict` | active normalized Section name uniqueness fails |
| `abwab.section_not_empty` | delete is attempted while a non-default Section still has an active root |
| `abwab.category_cycle` | category structural operation would create a cycle |
| `abwab.category_overlapping_move` | one bulk move selects an ancestor and its descendant |
| `abwab.category_unavailable` | required active category/parent no longer exists |
| `abwab.category_reserved_by_pending` | deletion intersects a Pending request |
| `abwab.permanent_default_section` | an operation would rename/delete/duplicate the permanent default section |
| `abwab.manual_protection` | applicable direct/inherited manual protection blocks the mutation or restore |
| `abwab.manual_protection_scope_conflict` | same active category/protection type is found with a different scope during apply |
| `abwab.ordinary_protection` | another administrator is inside the category's ordinary 24-hour window |
| `abwab.stabilization_active` | any mutation is attempted before the exact two-hour end |
| `abwab.relationship_duplicate` | canonical mutual/directional relationship already exists |
| `abwab.relationship_cycle` | Broader/Narrower edge would create a cycle |
| `abwab.template_cycle` | template node create/reparent would create a cycle |
| `abwab.link_check_stale` | submitted confirmation no longer matches authoritative links/proposal/source revisions |
| `abwab.request_no_changes` | authoritative link-check contains no actual non-no-op change |
| `abwab.link_kind_immutable` | an operation attempts to convert Single/Grouped/Surah kind |
| `abwab.link_duplicate` | the same active Surah, SingleAyah, or exact GroupedAyah member-set link already exists in the category |
| `abwab.group_minimum_members` | GroupedAyah would contain fewer than two distinct ayahs |
| `abwab.group_member_duplicate` | the same AyahId would occur twice in one group |
| `abwab.group_delete_confirmation_stale` | the two-to-one delete-whole confirmation no longer matches current group revision/members |
| `abwab.permission_assignment_stale` | expected retained role/subject assignment state or Version changed |
| `abwab.permission_baseline_locked` | command tries to revoke baseline access or assign a non-assignable catalogue code |
| `abwab.last_system_owner` | removal would leave zero active enabled System Owners |
| `abwab.restore_target_ineligible` | target coordinate/SafetyPoint is outside current lineage or ineligible |
| `abwab.restore_preview_stale` | observed head/generation/lineage/hash no longer matches |
| `abwab.restore_preview_invalid` | preview is expired, terminal, wrong-state, or otherwise not executable/cancellable |
| `abwab.restore_schema_unsupported` | planner, snapshot schema, or adapter version is unsupported |
| `abwab.safety_point_immutable` | command attempts to edit immutable SafetyPoint identity/target/generation/eligibility fields |

Malformed input uses HTTP 400 `abwab.validation_failed`; authorization failures use HTTP
403 `abwab.permission_denied`/`abwab.system_owner_required`/`abwab.ownership_denied`;
existence is redacted with HTTP 404 `abwab.not_found`; active RestoreExecuting
maintenance uses HTTP 503 `abwab.restore_executing`. No Spec Kit may add, rename, or
remap an Abwab error code without amending the canonical plan (§11).

### A.5 Dependency DAG (§16)

Renderer-independent edge list (§16.1):

1. `027 -> 028`
2. `028 -> 029`
3. `028 -> 030`
4. `029 -> 030`
5. `028 -> 031`
6. `029 -> 031`
7. `029 -> 032`
8. `031 -> 032`
9. `030 -> 033`
10. `031 -> 033`
11. `032 -> 033`
12. `028 -> 033`
13. `033 -> 034`
14. `032 -> 034`
15. `031 -> 034`
16. `030 -> 034`
17. `029 -> 034`

Direct dependency table (§16.2):

| Spec Kit | Exact direct predecessors |
|---|---|
| `027` | None; it can be selected only after an independent PASS and separate removal of superseded decision files |
| `028` | `027` |
| `029` | `028` |
| `030` | `028`, `029` |
| `031` | `028`, `029` |
| `032` | `029`, `031` |
| `033` | `028`, `030`, `031`, `032` |
| `034` | `029`, `030`, `031`, `032`, `033` |

Safe parallelism (§16.3): `027` completes before `028`; `029` begins only after `028` is
accepted; `030` and `031` may run in parallel once `028` and `029` are accepted; `032`
may begin once `031` and `029` are accepted while `030` runs; `033` cannot begin until
`028`, `030`, `031`, and `032` are all accepted (the exhaustive restore barrier); `034`
cannot begin until `029`–`033` are all accepted. The final implementation review is an
authorization gate outside this DAG and is not a Spec Kit.

### A.6 Frozen supersessions that must not leak downstream (§2.1)

- `RepresentativeQuranExcerpt` is an optional, user-entered plain string with no
  representative-ayah identity, no Quran foreign key, and no whole-ayah requirement.
- Relationship mutations and reorder-only operations do not activate or fall under the
  ordinary 24-hour gate.
- A grouped link always contains at least two ayahs; removing a member from a two-member
  group offers delete-the-whole-block confirmation (confirmation deletes the whole
  aggregate, cancellation changes nothing); there is no one-member grouped link and no
  conversion to a single link.
- Permission assignments and active System Owner membership are current security state
  outside Product Restore; Product Restore never re-grants, revokes, adds, or removes
  them.
- Canonical highlights use `QuranWord.Id`; `MushafWordId` is not a new identity type.
- Ordered notes replace stale source-description wording; notes never own or embed
  word/highlight identifiers.

---

## Appendix B — Requirement-to-Owner Traceability Catalogue (§18.1 step 4, §19)

Every locked invariant group maps to canonical Master Plan clauses, exactly one
implementation owner set, and at least one primary acceptance owner. This matrix is a
navigation/traceability aid, not evidence of review (§19).

| Locked invariant group | Canonical plan clauses | Implementation owner(s) | Primary acceptance owner |
|---|---|---|---|
| No drag-and-drop; explicit permission-filtered actions | 3.2, 9, 11, 14.2 | `028`, domain UIs `029`–`033` | `029`, final browser/source sweep `034` |
| Permanent default section, editable section order, sibling order, independent root section/global order, global view only | 5, 7.1, 9, 14.2 | `029` | `029`; load/browser rerun `034` |
| Category content, noncanonical plain excerpt, aliases, Unicode-range normalization, uniqueness/ancestry, subtree deletion/restore | 5.1, 7.1, 15.1, 18.3 | `027`, `029`; reservation seam `032` | `029` real-PG/API/parity plus `032` deletion races |
| Relationship canonical/directional storage, constraints/cycles, current+proposed endpoint protection, no ordinary 24h | 7.3, 8, 9 | `030` | `030` real-PG/race/negative tests |
| Templates manual-only; one target; basic structure/data only | 7.4, 9 | `030` | `030` negative-copy/atomicity tests |
| Surah/Single/Grouped aggregates, stable IDs, minimum two, delete-whole 2→1 behavior | 7.5, 13, 18.5 | `031` | `031` invariant/race/browser tests |
| Unlimited ordered pure-string link notes; group-level ownership; quotation/highlight independence | 7.5, 13.1 | `031` | `031` unit/API/browser tests |
| Source-specific attribution, near-ayah anchor, current-door no-copy/no link-block reorder, mutashabihat deferral, 031 contract versus 032 persistence | 7.6, 13, 15.1 | validation/ports `031`; persistence `032` | `031` source parity/negative; `032` snapshot tests |
| Personal workspace aggregate ownership/hard-delete/SubmittedReadOnly, canonical per-item waiting, outside-restore behavior | 6.1, 7.6, 8, 18.6 | `032` | `032` ownership/lifecycle/restore-class tests |
| Submit/resubmit fresh link-check, exact request/workspace state machine, actual-change gate, reservation/revision/status-safe wait clearing, notifications in one slice | 7.6, 10, 18.6 | `032` | table-driven transition/activation/concurrent-submit/delete-race tests `032` |
| Non-owner workflow; exact reviewer recipients; whole-request review; permanent decisions versus reversible applied effects | 7.6, 8, 10 | `032`; apply service `031` | `032`, restore proof `033` |
| SystemOwnerDirect link command with no request/fake approval | 6.3, 7.6, 11, 18.5 | `031`; identity `028` | `031` negative-row/audit tests |
| Immutable issuer+subject owner membership, explicit zero-to-one bootstrap, serialized last-owner, live removal/release readiness | 6.5, 7.7, 18.2/18.8 | `028`; live/release `034` | `028` concurrency/security; `034` live/bootstrap readiness |
| One permission catalogue; exact aggregate/subresource/attribution/notification mapping; admin baseline; retained unique serialized grants outside restore/permanently audited | 5.2, 6.5, 7.6–7.8, 8, 11 | `027`, `028`, `029`–`032` | catalogue/assignment-race/revocation/backend-authority/security tests |
| Ordinary 24h exact activation/restored expiry; manual typed/inherited/deleted-target protection; exact two-hour global gate | 6.6, 7.2, 9, 12 | `028`, `029`, restore `033` | `029` matrix/query; `033` blocker/replay/clock tests |
| Honest tracked-write boundary, layered enforcement, and capability-bound restore/automated recovery | 6.1–6.2, 6.6, 12, 15.2–15.3 | `028`; every writer; `033` | architecture/real-PG/capability negative tests |
| One commit-correct ChangeSet/global audit head, versioned snapshots, fixed list/render UI, audit-failure rollback | 6.1–6.3, 7.1/7.9, 18.7 | kernel `028`; payloads `029`–`032`; query/UI `033` | head/transaction proof `028`; UI/registry `033` |
| xmin tokens versus ExpectedTimelineGeneration and monotonic logical revisions/head/generation; grouped atomicity; no retry/merge | 6.2, 6.4, 7–8, 11–12 | `028`, every writer, `033` | affected/untouched stale-command, domain/head race, mock/HTTP parity and `033` ABA tests |
| Canonical Quran immutability/no projection, allowed noncanonical strings, stable IDs/import refusal before first FK | 4, 6.8, 15.1, 18.2/18.5 | safety `028`; FKs `031` | destructive-path/no-projection/FK tests |
| Exact reversible/permanent/outside/technical classes and unique adapter barrier | 6.7, 8, 12, 18.7 | adapters `029`–`031`; mixed rule `032`; planner `033` | persisted-type/event-kind uniqueness/round-trip/restore tests `033` |
| Persisted owner-bound one-time RestorePreview, exact status machine, current blockers, strict current-lineage replay, server hash/version preflight | 7.9, 11, 12 | `033` | transition/tamper/capability/protection-order/race/failure tests `033` |
| Irreversible full restore, append-only generation lineage, Pending forward invalidation, all personal states survive, old SafetyPoint objects ineligible, exact stabilization | 6.7, 7.9, 8, 10, 12 | root `028`; execution `033`; request contract `032` | multi-restore/state-by-state/SafetyPoint/controlled-clock proof `033` |
| Restore never rewinds permissions/owners/workspace/notifications/tokens; creates required notifications in transaction | 6.4–6.7, 8, 10, 12 | `033`; storage `028`; semantics `032` | `033` outside-state/notification/rollback proof |
| Durable per-item notification matrix, idempotent read state, no Outbox, read state outside audit/restore, live navigation | 7.8, 9–10, 14.4 | storage `028`; public/events `032`; restore `033`; live `034` | transaction/race/recipient tests `032`/`033`; reconnect/auth `034` |
| Shared frontend foundation only; per-domain ports/mocks/HTTP/mapping/parity/cache | 6.9, 14.1, 18 | `028` then each domain Spec Kit | architecture/parity checks in `028` and every domain |
| Reactive Forms first real use; Signals UI state; reusable Playwright and bounded spike | 5, 6.9, 14, 18.2–18.3 | shared foundation then real permission form `028`; later domain UI `029`–`033` | package/import check `028`; browser suite `028`/`034` |
| Generic versus domain cache ownership; commit-only publication; SignalR hint/gap/live-auth rules | 14.3–14.4, 18.8 | generic `028`; domains `029`–`033`; realtime `034` | stale/rollback tests per domain; gap/auth tests `034` |
| Sustained-work context/filter/focus/scroll preservation and explicit current/new/conflict display | 3.3, 14.2–14.4, 15.3 | domain UIs `029`–`033`; sweep `034` | per-domain and sustained-work Playwright journeys |
| Exact eight-Kit DAG, restore dependencies, safe parallelism, review-only final gate | 16–18 | planning workflow | doc consistency `027`; independent reviews outside implementation |
