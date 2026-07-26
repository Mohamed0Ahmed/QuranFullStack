# Contract: Template application to one real category

**Feature**: `030-abwab-relationships-templates` | **Source**: Master Plan §7.4, §9 (Apply template),
§8, §6.3, §11 — realizes §18.4 (Template workstream) only.

## Operation

Apply **one** template to **one** target real category. Requires `template.apply` **alone** (§5.2).
Envelope `ApiResponse<T>`; the command carries `ExpectedTimelineGeneration`, the expected
`TreeRevision`, the target category's expected `xmin`, and the template's expected `TemplateRevision`.

The real-category rows are written **only through the accepted `029` category writer** — this feature
adds **no second category writer** and **no second Category restore adapter**. Because
`CategoryContentHandler.AddAsync` runs one audited operation (and one `TreeRevision` bump) per call,
the writer is **extended behavior-preservingly**: its in-transaction creation core (normalization,
tree/name guards, protection gate, order allocation) is extracted into a grouped seam invoked by both
the existing single-add path and the application handler, so the whole application runs inside
**one** audited operation — one ChangeSet, one `TreeRevision` bump. The writer is never forked, and a
regression assertion proves `029` single-add behavior unchanged.

## What application does (§7.4)

- Creates **every template root as a direct child** of the target category.
- Recursively copies **only**: `Name`, `RepresentativeQuranExcerpt`, `Description`, aliases,
  order, and structure.
- Produces **independent real categories**, indistinguishable from hand-created ones, with their own
  fresh technical state.
- Is **one ChangeSet** and **one `TreeRevision`** bump for the whole application.

## Strict copy allowlist

The copy set above is an **allowlist**. Nothing else is copied — explicitly **not**: Surah/Ayah links,
ayah members, highlights, notes, requests, sources, decisions, notifications, audit or workflow history,
or technical revisions. The allowlist fails **closed**: a child family added by a later Spec Kit (e.g.
`031` links) is simply not copied, with no change required here.

## Revalidation, all inside one transaction

| Check | Behaviour on failure |
|---|---|
| destination **uniqueness** under the §5.1 normalized rule (root and sibling scopes) | `abwab.category_name_conflict` |
| manual **`InternalStructure`** protection on the target, direct or inherited (§9) | `abwab.manual_protection` |
| target category **still active** / parent chain valid | `abwab.category_unavailable` |
| expected `TreeRevision` | `abwab.tree_revision_stale` |
| expected target `xmin` | `abwab.row_stale` |
| expected `TemplateRevision` | `abwab.template_revision_stale` |
| `ExpectedTimelineGeneration` vs the locked generation | `abwab.timeline_generation_stale` |
| two-hour stabilization active | `abwab.stabilization_active` |
| the template is cyclic | `abwab.template_cycle` (a cyclic template can never be applied) |

Order allocation for the created children is computed and applied **within** the same transaction. Any
failure rolls the whole application back — **no partial tree**, no partial order change, no ChangeSet.

## Ordinary protection

Applying a template **does not create or restart** an ordinary 24-hour window on the target or on any
created category (§9). Manual protection and stabilization still apply.

## Audit (§6.3)

The application event stores and renders: the template identity and the **frozen template snapshot at
application time**, target and path, the complete created tree, all copied basic fields, and counts by
level. **Later template edits cannot change this rendering.** `030` publishes this payload shape; the
audit page/read model is `033`'s.

## Restore (§8)

Application creates ordinary Category aggregate rows, so its inversion uses the **single `029`
`CategoryRestoreAdapter`**. The versioned **application-event interpreter** maps the event to that
adapter and **registers no adapter of its own** — see
[`restore-adapters-contract.md`](./restore-adapters-contract.md).

## Tests

- Real PostgreSQL / API: roots created as **direct children**; **exactly 1** ChangeSet and **exactly 1**
  `TreeRevision` bump; uniqueness and protection revalidated **inside** the transaction (proven by a
  concurrent conflicting write); full rollback on each failure row above.
- Copy proof: for **every** forbidden family, the produced tree contains **0** copied rows; copied
  fields match the allowlist exactly, including nested aliases and order.
- Negative: applying a cyclic template is impossible; applying to a protected target is blocked;
  applying without `template.apply` is rejected even when `template.edit` is held.
- Audit: the stored snapshot is frozen — editing the template afterwards leaves the rendered application
  detail unchanged.
