# Contract: Core audit render payloads (§6.3)

**Feature**: `029-abwab-core` | **Source**: Master Plan §6.3, §18.3 step 4. `029` **publishes** the
core category render payloads over the `028` append-only audit engine. It does **not** build the
main audit page, pagination/filters, or the restore/decision/link/template payloads — those are
owned by `033` (audit read model) and the respective domain Kits. Detail below is owned by §6.3.

## Payloads published by `029`

| Payload | Contents (per §6.3) |
|---|---|
| **Category create** | complete new state — name, representative excerpt, description, aliases, section, parent, full path, and every order — empty values shown as `غير محدد` |
| **Category edit** | same complete-field component; old state right, new state left; changed value marked with green **plus a non-color marker**; never hides unchanged/empty fields |
| **Bulk move** (one ChangeSet) | selected root count and, per selected root, name, historical section/path/order before+after, moved-descendant count, expandable moved subtree; descendants **nested** not reported as independent moves; sibling-order side effects grouped by affected parent/order scope |
| **Subtree delete / operation-restore** (one ChangeSet) | selected root, `DeletionOperationId`, complete affected subtree, dormant attached-state counts, historical/current paths+orders, any personal items notified; attached state labelled **dormant**, never falsely shown as deleted |
| **Manual-protection** | target, each type, scope, actor/time, before/after, and every direct/inherited protection whose effective result changed |

## Rules

- **No standalone "ordering" render component.** §6.3 defines no separate reorder payload: ordering
  data is rendered **within** the **bulk-move** payload (sibling-order side effects grouped by
  affected parent/order scope) and the **category-edit** payload (order fields shown by the
  complete-field component). §18.3 step 4's "ordering ... as defined in §6.3" is satisfied by this
  fold-in — ordering data IS shown, just not as its own payload.
- Historical section/path at operation time is **immutable**; current name/path/deletion-state is
  fetched live on open (§6.3).
- Non-color diff markers accompany color (RTL, scholarly, accessible presentation).
- These renders are **presentation of already-audited `029` operations**; they add no new audit
  eligibility rules (main-log eligibility/filters are `033`).

## Tests

- Each payload renders the complete field set with the locked empty/`غير محدد`, dormant, and
  non-color-marker treatments; parity across backend DTO, core mock, HTTP mapping, and UI.
- Bulk move renders one ChangeSet with nested descendants; subtree delete/restore renders one
  ChangeSet with dormant counts; manual-protection renders changed direct/inherited effects.
