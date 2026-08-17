# UI Contract: Target-First Door Inclusion Management

**Surface**: Existing Abwab page and a feature-owned wide inclusion modal

## Entry and Direction

1. The flow always starts from one aggregate target door.
2. Pointer users right-click the target in the live Abwab tree and select `تضمين الأبواب`.
3. Keyboard users invoke the same target context menu through the existing `ContextMenu` or
   `Shift+F10` path and select the same action.
4. There is no source-first entry and no flow that targets several aggregate doors.
5. Inclusion remains separate from semantic relations in labels, modal ownership, permissions, and
   data access.

The action opens for any reader because topology is public. Create/delete controls are independently
permission-gated. Archived targets can open read-only topology but cannot mutate.

## Modal Identity and Restoration

- Title: `تضمين الأبواب`.
- Fixed target label: `الباب الجامع` with target identity visible in the header/body.
- Use the existing wide `qd-modal-shell`: one body scroller, sticky header/footer, focus trap,
  labelled title, Escape handling, and focus return.
- Encode the target in modal URL state, for example `modal=inclusions-<doorId>`, so refresh/back and
  archived-door read-only restoration do not depend on the normal selected-door query parameter.
- Existing modal-restore behavior returns focus to a supported restore trigger.
- Opening or closing inclusion management does not change the main page selection, section, tree
  position, or content-panel mode.

## Topology Views

The modal shows two direct-topology views:

- `مصادر الباب`: active direct sources of the fixed target. Rows show door name and archive status.
  Detach appears only on these rows and only with inclusion-delete permission and a live target.
- `يُستخدم في أبواب جامعة`: active direct consumer doors. This view is read-only.

Archived participants remain visible with text/icon status and an explanation that existing
synchronized records remain present. Status never relies on color alone.

## Source Picker

Reuse `components/abwab-door-picker/*` and the same `liveRoots` snapshot rendered on the main Abwab
page.

Configuration:

```text
single = false
roots = current main-page liveRoots
excludedIds = [targetDoorId]
disabledIds = current direct source door IDs
```

Rules:

- Selecting one or multiple sources is supported in one draft.
- Sources may come from any section or tree position.
- The target is absent/unselectable.
- Already directly included sources remain visible but disabled where the existing picker contract
  supports disabled rows.
- Archived doors do not enter `liveRoots` and remain unselectable.
- Search, hierarchy expansion, checkbox selection, and keyboard navigation reuse existing behavior.
- The Frontend does not attempt authoritative cycle evaluation. Backend validation owns self,
  duplicate, stale, and cycle outcomes.
- No source count or graph-depth cap is applied.

## Add Interaction

1. Load topology and retain its latest `doorVersion` beside the selected target.
2. Open the source picker from the fixed target modal.
3. Select one or multiple eligible sources.
4. Submit one request containing the complete `sourceDoorIds[]` and latest
   `expectedTargetDoorVersion`.
5. Disable the submitting action without changing footer geometry.
6. On success, replace the held version, refresh topology, refresh the shared tree once, clear the
   source draft, announce success, and keep the modal on the same target.
7. On controlled failure, retain the user's selection where safe and show the localized error at
   the write origin. A conflict refreshes topology/version and never retries automatically.

The UI never submits one request per selected source and never reports success before the atomic
Backend action completes.

## Detach Interaction

1. Trigger detach from a direct source row.
2. Open the existing nested confirmation pattern with initial focus on Cancel.
3. Explain that the source door remains unchanged while synchronized target records owned by this
   inclusion are removed.
4. Submit the inclusion ID and latest target version as one request.
5. On success, use the returned version/removal count, refresh topology and the shared tree once,
   announce the outcome, and keep the modal target stable.
6. On conflict/failure, keep the modal open and show the controlled error near the action.

## Permissions

| Capability | Read topology | Add sources | Detach source |
| --- | --- | --- | --- |
| Public/anonymous reader | yes | hidden | hidden |
| Active user without inclusion permissions | yes | hidden | hidden |
| `abwab.inclusions.create` | yes | enabled for live target | according to delete permission |
| `abwab.inclusions.delete` | yes | according to create permission | enabled for live target |
| Owner | yes | enabled for live target | enabled for live target |
| Any reader on archived target | yes | disabled/hidden | disabled/hidden |

Do not reuse relation permissions. Existing link edit/delete permissions remain Owner-only.

## Async and Feedback Ownership

Use distinct owners; do not introduce a combined state adapter:

- Initial topology load: content-shaped skeleton.
- Refresh: existing content remains visible with refreshing indicator and `aria-busy`.
- Empty sources/consumers: explicit calm empty copy in the mounted modal shell.
- Read or write error: scoped error owner with Retry only for the failed read; write errors stay near
  the submitting action.
- Success/notice: polite live notice that clears on the next mutation.

The focused `abwab-inclusions.controller.ts` owns held topology, target version, source draft,
request generation/cancellation, refresh, write state, notices, and detach confirmation. The API
service owns HTTP only; the modal renders page-ready state; the Abwab page remains composition only.

## Accessibility and Visual Contract

- Arabic and RTL are the baseline; layout uses logical properties.
- Preserve picker tree roles, keyboard checkbox selection, focus-visible treatment, long-name
  disclosure, and minimum hit areas.
- Modal close returns focus to the context-menu trigger when it remains available.
- Announce selection count, add/detach outcomes, conflict refresh, and controlled errors through the
  existing Abwab announcer/notice vocabulary.
- Use existing tokens, flat surfaces, hairline borders, one primary action, and floating-layer
  shadow only on the modal/menu shell.
- Use Compact/Medium/Wide bands from the shared breakpoint contract; add no raw thresholds.
- No gradient, resting shadow, hover lift, decorative image, new font, or Quran renderer/style/
  animation change.

## Existing Link Content Boundary

Inclusion management never adds an origin badge, source label, sync-state field, link tab, effective
content mode, or new Quran-content component. Synchronized records continue through the current link
panel/list/record/editor/copy controls and stale-version refresh behavior.

## Required Composition Refactor

Current page/tree files are at their hard review boundary and the overlays controller is already
above its soft threshold. Before adding inclusion wiring:

- extract context-menu action composition from the tree rather than growing
  `abwab-tree.component.ts`;
- keep modal workflow state in `abwab-inclusions.controller.ts`, not
  `abwab-page-overlays.controller.ts`;
- render the inclusion modal as a focused child component so the page template gains composition,
  not the modal implementation; and
- do not modify existing door-link components unless stale-version orchestration demonstrably
  requires a focused compatibility change.
