# Contract: Sections API

**Feature**: `029-abwab-core` | **Source**: Master Plan §11 (Sections), §7.1, §9, §18.3. Realizes
§18.3 only.

## Operations

`read` / `add` / `edit` / `reorder` / `delete-empty`. Envelope `ApiResponse<T>`. Every mutation
DTO carries `ExpectedTimelineGeneration` (and expected `xmin`/`TreeRevision` where structural).
Verb→code mapping is mechanical (`view`/`add`/`edit`/`reorder`/`delete`) per §11; no verb silently
authorizes another. No drag semantics — reorder is an explicit action.

## Rules (from §7.1 / §9)

- Active **normalized** section names are unique.
- Exactly **one permanent default section** (`أبواب غير مصنفة`, `IsPermanentDefault`): it may be
  **reordered** but **not renamed, deleted, or duplicated**.
- A **non-default** section may be deleted **only when it has no active root categories**. Root
  reassignment is an explicit category-move command, never a hidden side effect.
- Section add/edit/reorder and delete-empty are **not** ordinary 24-hour actions and carry **no**
  category manual target; **stabilization blocks** them (owned by `028`/enforced here).

## Conflict codes (exact — §11)

| Code | When |
|---|---|
| `abwab.section_name_conflict` | active normalized Section name uniqueness fails |
| `abwab.section_not_empty` | delete attempted while a non-default Section still has an active root |
| `abwab.permanent_default_section` | rename/delete/duplicate of the permanent default section |
| `abwab.stabilization_active` | any section add/edit/reorder/delete attempted before the exact two-hour end (§9 "Two-hour stabilization: Blocked") |
| `abwab.timeline_generation_stale` / `abwab.row_stale` | stale generation / `xmin` |

## Tests

- Name-conflict, non-empty-delete, and permanent-default races map to the **exact** codes above,
  **identically across API, core mock/HTTP, frontend, and contract tests** (0 drift).
- Permanent default: reorder succeeds; rename/delete/duplicate fail with
  `abwab.permanent_default_section`.
- Real-PostgreSQL uniqueness + race tests; mock ≡ HTTP parity.
