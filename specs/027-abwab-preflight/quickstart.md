# Quickstart: Validate the Abwab Preflight Freeze

**Feature**: `027-abwab-preflight` | **Date**: 2026-07-22

This guide validates that `027` faithfully froze the canonical Master Plan and produced no
implementation. It runs the documentation-consistency checks defined in
[`contracts/doc-consistency-checks.md`](./contracts/doc-consistency-checks.md) against the
artifacts described in [`data-model.md`](./data-model.md). All commands are read-only and
run from the repository root. `027` succeeds when every check reports IDENTICAL / zero
mismatch and the no-implementation guards hold (Master Plan §18.1 exit).

## Prerequisites

- Canonical source present: `docs/feature-abwab-management/MASTER_PLAN.md`
- Frozen copy present: `specs/027-abwab-preflight/spec.md`
- On branch `027-abwab-preflight`

## Validation scenarios

### DC-1/DC-2/DC-3/DC-5 — Catalogue & DAG fidelity (byte-for-code)

```bash
PLAN=docs/feature-abwab-management/MASTER_PLAN.md
SPEC=specs/027-abwab-preflight/spec.md

# DC-2 conflict codes
diff <(grep -oE 'abwab\.[a-z_]+' "$PLAN" | sort -u) \
     <(grep -oE 'abwab\.[a-z_]+' "$SPEC" | sort -u) && echo "DC-2 IDENTICAL"

# DC-3 DAG edges
diff <(grep -oE '0[0-9][0-9] -> 0[0-9][0-9]' "$PLAN" | sort -u) \
     <(grep -oE '0[0-9][0-9] -> 0[0-9][0-9]' "$SPEC" | sort -u) && echo "DC-3 IDENTICAL"

# DC-1 permission codes
diff <(grep -oE '`(category|section|protection|relationship|template|attribution|audit|safetyPoint|notification|permission)\.[a-zA-Z.]+`' "$PLAN" | tr -d '`' | sort -u) \
     <(grep -oE '`(category|section|protection|relationship|template|attribution|audit|safetyPoint|notification|permission)\.[a-zA-Z.]+`' "$SPEC" | tr -d '`' | sort -u) && echo "DC-1 IDENTICAL"

# DC-5 normalization Unicode ranges
diff <(grep -oE 'U\+[0-9A-F]{4,5}' "$PLAN" | sort -u) \
     <(grep -oE 'U\+[0-9A-F]{4,5}' "$SPEC" | sort -u) && echo "DC-5 IDENTICAL"
```

**Expected**: `DC-2 IDENTICAL`, `DC-3 IDENTICAL`, `DC-1 IDENTICAL`, `DC-5 IDENTICAL` with
no `diff` output. (Verified during authoring.)

### DC-7 — Traceability completeness

```bash
PLAN=docs/feature-abwab-management/MASTER_PLAN.md
SPEC=specs/027-abwab-preflight/spec.md
echo "plan §19 rows: $(sed -n '/## 19\./,/## 20\./p' "$PLAN" | grep -cE '^\| .* \| [0-9]')"
echo "spec Appendix B rows: $(sed -n '/## Appendix B/,$p' "$SPEC" | grep -cE '^\| .* \| `0|^\| .* \| [a-z]')"
```

**Expected**: equal counts (currently `29` → `29`). Then manually confirm each row has
exactly one implementation-owner set and at least one acceptance owner (SC-002).

### DC-8 — No-decision guard

```bash
grep -niE 'provisional|if needed|to be decided|\bTBD\b|\bTODO\b|NEEDS CLARIFICATION' \
  specs/027-abwab-preflight/spec.md || echo "DC-8 PASS (no provisional/undecided language)"
```

**Expected**: `DC-8 PASS`.

### DC-9 — No-implementation guard

```bash
# Only documentation artifacts under the feature dir; no source/migration/seed files
find specs/027-abwab-preflight -type f | sort
git diff --name-only HEAD -- Backend Frontend 2>/dev/null | grep . \
  && echo "DC-9 FAIL: source changed" || echo "DC-9 PASS (no Backend/Frontend changes)"
```

**Expected**: feature dir holds only `plan.md`, `spec.md`, `research.md`,
`data-model.md`, `quickstart.md`, `contracts/doc-consistency-checks.md`, and
`checklists/requirements.md`; `DC-9 PASS`.

### DC-10 — No-downstream-leak guard

Manual review: confirm `spec.md` records ownership/acceptance for `028`–`034` (Appendix B)
but performs none of their implementation (no foundation, core, links, workspace, restore,
or realtime work). Pass when only ownership is stated (§17, §18.2–§18.8).

## Done / acceptance

`027` is validated when **all** DC checks pass with zero mismatch, the traceability
record-set invariants hold, and no implementation artifact exists. Acceptance of `027` is
the sole DAG precondition for authorizing `028` (§16, §18.1 exit). Next planning command:
`/speckit-tasks`.
