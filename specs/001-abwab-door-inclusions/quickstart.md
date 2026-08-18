# Quickstart Validation Guide: Abwab Door Inclusions

This guide is for later implementation verification. The `/speckit-plan` run that created it does
not execute commands, generate/apply a migration, mutate a database, or authorize implementation.

## 1. Prerequisites and Authorization Checkpoints

- .NET 10 SDK, Node/npm dependencies, PostgreSQL, and local HTTPS certificates are available per
  `Backend/scripts/README.md`.
- The implementation is complete on a non-`main` feature branch.
- No new automated test has been added.
- Before any schema-dependent validation, the owner has separately authorized migration generation.
- Runtime/manual verification uses a local or disposable database on which an authorized operator
  has applied the migration. This guide does not authorize `Backend/scripts/update-db`.
- Never run `wipe-abwab`, `drop-db`, or `reset-db` as part of this guide.
- No command targets Railway production.

## 2. Generated Migration Checkpoint

Run only after explicit migration-generation authorization:

```bash
Backend/scripts/add-mig AddAbwabDoorInclusionSynchronization
```

Review the generated migration, designer, and model snapshot without hand-editing them. Confirm:

- two inclusion tables, restrictive foreign keys, indexes, and coherence checks match
  [data-model.md](./data-model.md);
- the linking contribution reference and check constraints accept the internal kind only with
  `DoorInclusionId IS NOT NULL` and `OperationId IS NULL`, while every public kind still requires
  `OperationId IS NOT NULL`;
- no synthetic `LinkingOperation` row or public token exists for an inclusion contribution;
- existing rows retain their operation owner, receive null `DoorInclusionId`, and require no data
  backfill; and
- the audited `wipe-abwab`/fixture reset closure cannot cascade into Quran or access data.

Do not apply the migration unless the owner gives a separate database-update instruction.

## 3. Backend and Schema Verification

From the repository root, after the authorized generated migration exists:

```bash
Backend/scripts/qd-build
Backend/scripts/check-pending-model --build
Backend/scripts/test-backend migration --no-build
Backend/scripts/test-backend gate-contract --no-build
Backend/scripts/test-backend tier-b --no-build
Backend/scripts/test-backend smoke --no-build
Backend/scripts/check-api-contract
```

Expected results:

- Backend builds successfully.
- Pending-model check reports no model drift.
- Existing migration and schema/catalogue gates pass with only minimal retained-protection updates.
- Existing tier-b and smoke lanes pass, including public topology and permission-classified writes.
- Swagger and retained generated frontend models match
  [contracts/http-api.md](./contracts/http-api.md).

`check-api-contract` may intentionally regenerate unstaged outputs and report drift on its first
run. Review only sanctioned generated model/spec paths; do not commit generated services that the
pipeline prunes.

## 4. Frontend Generation and Verification

From `Frontend/quran-dashboard-ui/`, independently and in this order:

```bash
npm run generate:permission-codes
npm run check:permission-catalogue
npm run check:no-unit-specs
npm run typecheck:app
npm run build:verify
npm run check:golden-ui
```

Expected results:

- Generated inclusion DTOs and the two new permission constants are current.
- Permission group/count/order checks pass.
- No Angular unit spec exists.
- Application typecheck and production build pass.
- Golden UI contract passes with no raw breakpoint, second gutter, prohibited elevation/effect,
  retired state adapter, or Quran-renderer boundary violation.

Only if the existing retained `e2e/abwab-permissions.e2e.ts` journey was minimally updated because
its protected subject changed:

```bash
npx playwright test e2e/abwab-permissions.e2e.ts --project=abwab --workers=1
```

This does not authorize a new Playwright journey or file.

## 5. Local Runtime Setup

After the local database is already at the authorized migration head:

```bash
Backend/scripts/qd-api
Backend/scripts/qd-ui
```

Use the existing supported authenticated fixture/session for write checks. Do not elevate an
identity, forge authentication, bypass permissions, or modify production/domain authorization to
obtain browser evidence. Public/read-only checks remain valid without a write-capable identity.

## 6. Target-First Management Flow

Validate against [contracts/inclusion-management-ui.md](./contracts/inclusion-management-ui.md):

1. On the main Abwab page, right-click one live door chosen as the aggregate target.
2. Select `تضمين الأبواب`; confirm the fixed target is labelled `الباب الجامع`.
3. Confirm keyboard `ContextMenu`/`Shift+F10` reaches the same action and focus returns on close.
4. Open the source picker and confirm it is the same live tree/list used by the main page.
5. Select one source, then multiple sources from different sections and unrelated tree positions.
6. Confirm the target, already directly included sources, and archived doors are unselectable.
7. Submit once and confirm the complete selection becomes one atomic request/action.
8. Confirm there is no source-first entry and no multi-target flow.
9. Open the same topology for an archived target and confirm it is read-only.

## 7. Manual Runtime Matrix

### Inclusion graph and initial sync

1. Include one live source containing independent and grouped records. The target's current link
   panel shows ordinary records with identical ayahs, selected words, descriptions, and grouping.
2. Include two sources from unrelated sections. Neither target nor sources move in the tree.
3. Submit multiple sources where one would make the batch invalid. Confirm no edge or clone from the
   batch commits.
4. Attempt self-inclusion, an active duplicate, repeated source ID, archived source, and a direct or
   transitive cycle. Confirm controlled rejection and zero partial state.
5. Confirm topology GET lists direct sources and consumers, including archived participants, and
   tree source/consumer counts update without changing existing count meanings.
6. Confirm a target/source pair may also retain an independent semantic relation.

### Projection and source propagation

7. Let source A select word X and source B select word Y in the same ayah. Target union contains X
   and Y once each.
8. Let two records supply X. Remove one and confirm X remains until the final supplier is removed.
9. Add a source record and confirm its target clone exists before source success is reported.
10. Edit selected words, descriptions, grouping, and ayah membership for an active source
    occurrence. Confirm the still-active clone changes without changing occurrence identity.
11. Delete an active source occurrence. Confirm its clone disappears without removing another
    supporting record.
12. Configure A includes B and B includes C. Add/edit/delete a C record and confirm propagation
    reaches B then A before the initiating action succeeds.
13. Force an expected synchronization conflict and confirm it returns a controlled Application
    outcome while the initiating source/inclusion action and all reachable target changes roll back
    together. Separately force an unexpected fault and confirm centralized middleware sanitizes it
    without exposing SQL, paths, content, or ledger details.
14. Exercise concurrent opposite-edge and link/topology attempts. Confirm no cycle, deadlock-created
    drift, or partial commit; changed door versions advance.

### Target-local override and suppression

15. In a chain where A includes B, edit selected words on a synchronized B record. Confirm only B's
    local clone changes, its source remains unchanged, the local override propagates to A, and later
    same-occurrence source edits advance only the observed fingerprint/audit and overwrite neither B
    nor A.
16. Delete the overridden source occurrence. Confirm the overridden target clone is removed.
17. In a chain where A includes B, delete an active synchronized B record. Confirm the source remains,
    B keeps a suppressed mapping with no target unit, and the target-visible removal reaches A before
    the action succeeds.
18. Edit the still-existing suppressed source occurrence. Confirm its observed fingerprint/audit
    advances while the target record stays absent and no downstream add occurs.
19. Run an internal one-to-one source-unit replacement. Confirm the same suppression/override state
    transfers before the old unit disappears.
20. Confirm an ambiguous internal split/merge cannot silently become a new occurrence or revive
    content; the mutation preserves logical occurrences or fails before commit.
21. Delete the suppressed source occurrence, commit, then explicitly link a new source occurrence.
    Confirm the old suppression retires and the fresh occurrence synchronizes.
22. Bulk-delete a mixed selection of direct and synchronized target records in a door that has a
    consumer. Confirm direct deletes, per-occurrence suppressions, and all downstream removals commit
    together while sources remain unchanged.

### Archive, detach, reattach, and direct records

23. Archive and restore a source. Existing target records/counts remain and no duplicate or
    suppression reset occurs.
24. Archive a target, change an included source, then restore the target. Internal synchronization
    stayed current while user mutations against the archived target were blocked.
25. Detach an edge containing active, overridden, and suppressed states. Confirm only edge-owned
    state disappears; source, target-direct, and other-edge records survive.
26. Reattach the pair. Confirm fresh sync and no retired suppression/override reuse.
27. Confirm target-direct records retain normal edit/delete behavior and survive source deletion,
    source archive, and detach.
28. Copy a synchronized target record. Confirm the destination record is direct and carries no
    inclusion ownership.

### Contract, permissions, and UI compatibility

29. Confirm public/anonymous readers can read topology and see no create/delete controls.
30. Confirm an active user without inclusion permission receives `403`; create/delete permissions
    act independently; Owner succeeds. Existing link PATCH/bulk-delete remain Owner-only.
31. Confirm existing link responses/UI and linking preflight overlap labels expose no source door,
    inclusion ID, internal contribution, fingerprint, sync state, origin badge, or alternate content
    mode; authored request parsing rejects the internal kind without failing ordinary preflight.
32. Confirm synchronized records use existing link count/list/ayah/highlight/edit/delete/copy
    surfaces and stale-version recovery.
33. Confirm source and recursively changed target versions advance; a stale open panel refreshes
    rather than silently editing obsolete data.
34. Verify Compact, Medium, Wide, and Wide-plus structure, one body scroller, modal geometry, focus
    trap/return, keyboard picker selection, announcements, archive text/icon status, logical RTL
    layout, and no Quran text animation/style change.

## 8. Acceptance Record

Record later execution evidence with:

- branch/commit under review;
- generated migration authorization and generated file list;
- whether database application was separately authorized or skipped;
- each command and exit result, kept independent;
- any retained exact-contract files minimally updated;
- manual matrix pass/fail/blocked outcome with safe IDs/counts only;
- Golden browser evidence under a temporary path such as `/tmp/golden-ui-evidence/abwab-inclusions/`;
  and
- explicit confirmation that no Quran data, hierarchy, relation semantics, content attribution, or
  deployment boundary changed.

### Execution record — 2026-08-18

- Branch/current state: `feat/abwab-chapter-inclusion` at `e1126fcc` plus the current working-tree
  changes.
- The authorized generated migration is present as
  `20260817163513_AddAbwabDoorInclusionSynchronization`; migration generation was not repeated and
  `Backend/scripts/update-db` was not run. Playwright used its disposable database clone.
- Command results:
  - `Backend/scripts/qd-build`: passed with 0 errors and the existing `SSH.NET 2024.2.0` advisory
    warning. An initial sandboxed attempt failed before compilation because local process access was
    restricted; the unrestricted documented command passed.
  - `Backend/scripts/test-backend tier-b --no-build`: passed, 349/349.
  - `Backend/scripts/test-backend smoke --no-build`: failed, 88/90. The failures are outside this
    feature's allowed reconciliation scope: the unrelated
    `PATCH api/linking/workspace/sources/{id:long}/types` route lacks a smoke-catalog row, and the
    authentication-scheme baseline does not include `ApplicationAuthentication` and `DeviceSession`.
  - `npm run check:no-unit-specs`: passed.
  - `npm run typecheck:app`: passed.
  - `npm run build:verify`: passed with existing unrelated bundle/style budget warnings.
  - `npm run check:golden-ui`: passed.
  - `npx playwright test e2e/abwab-permissions.e2e.ts --project=abwab --workers=1`: passed, 2/2.
- Retained exact protection was minimally updated in `SmokeRouteBaselineTests.cs` and the existing
  `abwab-permissions.e2e.ts` journey; no test method, test class, unit spec, or Playwright journey was
  added. Test Guard found no issue in the changed Playwright assertions.
- Manual matrix result: blocked. The anonymous topology/hidden-control and anonymous-write subset
  passed through the retained Playwright journey, but the Owner mutations, archive/detach/reattach,
  propagation/fault/concurrency, existing-link compatibility, responsive viewport, focus, and RTL
  cases were not executed because no supported authenticated manual session or authorized fault
  injection was available. No pass is inferred for those cases.
- Golden browser evidence: not produced. The static Golden UI contract passed, but no authenticated
  responsive browser evidence was captured.
- Pre-delivery self-check result: failed. Comment policy, layer ownership, generated-file scope, and
  forbidden visual-effect checks were clean, but `abwab.labels.ts` is 477 lines and remains above
  the 300-line hard helper threshold.
- Final diff boundary check: passed statically. No Quran source/resource mutation, hierarchy or
  semantic-relation behavior change, source attribution in link content, hard graph/product cap,
  timing SLA, background propagation, database application, deployment change, or unauthorized new
  test was found.
