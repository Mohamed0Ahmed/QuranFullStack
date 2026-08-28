# Mushaf Related-Ayah Linking — Implementation Plan

> Status: ready for phased implementation after owner approval
>
> Planning format: normal implementation plan; Spec Kit is intentionally not used
>
> Working branch: `unified-ayah-linking`
>
> Baseline commit: `66d2e9cc feat(mushaf): highlight mutashabihat phrases`
>
> Scope: linking entry points for Similar Ayahs and Mutashabihat inside the Mushaf reader only
>
> Lifecycle: this is a temporary feature plan. Delete this feature folder after the final engineering review passes and before merge, as required by `docs/README.md`.

## 1. How this plan must be executed

This plan is deliberately explicit so a smaller implementation model can execute one phase at a time without inventing product behavior or expanding the feature.

The implementer must follow these rules:

1. Execute exactly one phase per owner request.
2. Stop at every phase boundary. Do not begin the next phase until the owner requests it.
3. At the end of each phase, provide the review package defined in section 15.
4. Do not use Spec Kit commands or create `specs/` artifacts for this feature.
5. Do not create, update, or apply a database migration. The planned design requires no schema change.
6. Do not add tests unless the owner gives separate explicit approval. The Testing Decision is locked in section 14.
7. Do not stage, commit, push, open a PR, or deploy unless the owner separately requests that Git or deployment action.
8. Preserve unrelated working-tree changes and never broaden the phase to cleanup or refactoring.
9. Use code as implementation truth. This plan owns feature intent only while the feature is open.
10. If implementation truth conflicts with a locked contract below, stop and report the conflict instead of silently changing the contract.

Before changing Backend code, read the root router, `Backend/AGENTS.md`, and only the architecture sections triggered by that phase. Before changing Frontend code, read the root router, `Frontend/quran-dashboard-ui/AGENTS.md`, and only the architecture sections triggered by that phase. Every phase that selects or reports verification must follow `TESTING_CONSTITUTION.md`.

## 2. Required outcome

Add linking entry points to exactly two Mushaf study sources:

1. **Similar Ayahs** (`الآيات القريبة`)
2. **Mutashabihat** (`المتشابهات`)

For both sources, an authorized Owner can:

- choose specific results;
- select all available results;
- clear the current selection;
- add the assembled source to the persistent linking workspace;
- start the existing direct-link workflow from the assembled source.

The feature must reuse the existing linking engine. It must not introduce a second linking workflow, bypass preflight, bypass confirmation, or write directly to Abwab link records.

## 3. Scope boundaries

### 3.1 In scope

- Selection controls inside the Similar Ayahs card.
- Selection controls at Mutashabihat block and occurrence levels.
- A single assembled linking source per action.
- Automatic inclusion of the selected/original ayah for Similar Ayahs.
- Automatic seeding of canonical target Quran word IDs for selected Mutashabihat occurrences.
- Reuse of the existing `manual-mushaf-ayahs` linking source kind.
- Reuse of the existing direct-link workflow and persistent linking workspace.
- An optional manual-source context key so different Mushaf research contexts do not collide merely because they currently contain the same verse set.
- Atomic initialization of a newly added workspace source with its grouped shape, ayah selection, and selected Quran word IDs.
- Existing Owner authorization and linking permissions.
- Arabic-first RTL, keyboard access, responsive behavior, and protected Quran rendering.

### 3.2 Explicitly out of scope

- Quran phrase search, repetitions, manual search, or search-result linking.
- Linking entry points anywhere outside the Mushaf reader.
- Changes to the existing general manual Mushaf selection mode.
- New linking source kinds or new database tables.
- Persisting selected Mutashabihat block boundaries as independent backend units.
- Pairwise Similar-Ayah contributions.
- Automatic word matching for Similar Ayahs.
- Editing descriptions during source assembly.
- New routes, new top-level panels, or a new modal.
- Changes to tafsir, translation, i3rab, word analysis, doors, or Quran page rendering.
- New backend test classes or methods, frontend unit specs, or Playwright journeys without explicit owner approval.
- Database migration generation or application.
- Deployment.

## 4. Locked product behavior

### 4.1 Shared behavior

- Linking controls are visible only when `LinkingAccessService.canUseLinking()` is true.
- Read-only users continue to see the existing study results without selection controls or linking actions.
- Quran result text remains navigable exactly as it is now.
- Selection controls are separate interactive elements. Never nest a checkbox or button inside the existing Quran-text navigation button.
- The existing `QuranSourceLinkingActionsComponent` remains the shared owner of the two actions:
  - `إضافة للربط`
  - `ربط مباشر`
- An action is not available until the source has a valid non-empty feature selection.
- Starting either action uses the exact same assembled source definition.
- Direct linking and workspace addition must produce the same label, membership, grouped shape, and selected-word seed.
- The source is a manual Mushaf source and defaults to one grouped linking unit.
- Users may change the grouped/manual configuration later through the existing linking editor. This feature only defines the initial configuration.

### 4.2 Similar Ayahs behavior

- The selected/original ayah is always included automatically.
- The original ayah is not duplicated inside the Similar Ayahs result list.
- The original ayah has no new checkbox in the selected-ayah header; it is locked into the assembled source.
- The user selects one or more related ayahs from the current Similar Ayahs result list.
- `Select all` selects every related result currently returned by the Similar Ayahs API.
- `Clear all` clears only the related-result selection. The original ayah remains implicitly included but the actions stay unavailable until at least one related ayah is selected.
- The source label is exactly:

```text
الآيات القريبة من الآية {ayahNumber} سورة {surahNameArabic}
```

- The source contains the original ayah plus the selected related ayahs, de-duplicated by verse key and ordered in Quran order by existing source normalization.
- Similar Ayahs contribute whole ayahs. Their initial `selectedWords` collection is empty.
- The initial manual link shape is `grouped`.
- The stable manual context key is:

```text
mushaf-similar-ayahs:{selectedVerseKey}
```

### 4.3 Mutashabihat behavior

- Every block exposes a block-level selection control.
- Selecting a block selects every occurrence in that block, including occurrences of the selected/original ayah.
- Clearing a fully selected block clears every occurrence in that block.
- When some but not all occurrences are selected, the block control is indeterminate.
- The user can expand a selected block and clear or reselect an individual occurrence.
- The user can select occurrences from multiple blocks before performing one action.
- All selected blocks and occurrences are assembled into one source and one grouped linking unit.
- Block boundaries are UI selection structure only. They are not persisted as separate source units.
- If the same ayah is selected more than once, the source contains that ayah once.
- If multiple selected occurrences target the same ayah, their canonical Quran word IDs are unioned and sorted.
- If the same canonical Quran word ID appears through more than one selected occurrence, it is persisted once.
- The selected/original ayah is treated like any other occurrence in Mutashabihat. It may be cleared from one block. If it remains selected through another block, the final source retains it with the union of the still-selected target words.
- Existing green target-word highlighting remains unchanged. Linking selection state must not replace or reinterpret that highlight.
- The source label is exactly:

```text
متشابهات الآية {ayahNumber} سورة {surahNameArabic}
```

- The initial manual link shape is `grouped`.
- The context key contains the selected ayah plus the sorted source group IDs that still have at least one selected occurrence:

```text
mushaf-mutashabihat:{selectedVerseKey}:groups:{sourceGroupId1},{sourceGroupId2},...
```

### 4.4 Selection reset behavior

- Similar-Ayah selection resets when the selected verse or accepted Similar Ayahs response changes.
- Mutashabihat selection resets when the selected verse or accepted Mutashabihat response changes.
- Selection does not survive navigation to another ayah.
- Expanding or collapsing a Mutashabihat block does not change its selection.
- A loading, error, empty, or stale response cannot retain an actionable source from the previous ayah.

## 5. Quran-data safety contracts

These rules are non-negotiable:

1. Never reconstruct or replace the visible ayah text from a different Quran table.
2. Continue rendering the current `textUthmani`/`displayText` path.
3. Do not split Quran text to infer canonical word identity.
4. Mutashabihat word selection is built only from canonical `quran_words.id` values returned by the Backend for the exact stored occurrence range.
5. Every returned target word must:
   - belong to the occurrence ayah;
   - be a non-marker Quran word;
   - have `word_number` inside the inclusive `[wordFrom, wordTo]` range.
6. The API must order `matchedQuranWordIds` by `word_number`.
7. The expected target count is `wordTo - wordFrom + 1`.
8. If a selected occurrence does not carry a complete canonical target set, fail closed: do not launch or persist the source. Never guess IDs or derive them from display text.
9. No Quran source resource, importer input, Quran word text, or Mutashabihat source table is mutated by this feature.

## 6. Current implementation findings

The following capabilities already exist and must be reused:

- `LinkingSourceDescriptor` already supports `manual-mushaf-ayahs`.
- Manual sources already resolve verse keys to canonical ayah IDs and Quran words.
- The direct-link workflow already supports:
  - selecting and clearing ayahs;
  - selecting canonical Quran words on manual sources;
  - `grouped` and `independent` manual shapes;
  - door selection, prepared preflight, confirmation, and execution.
- The persistent linking workspace already stores:
  - manual ayah membership;
  - ayah inclusion overrides;
  - selected Quran word IDs;
  - manual link shape;
  - source label and version.
- `QuranSourceLinkingActionsComponent` already owns direct-link and add-to-workspace actions for source-backed Quran UI.
- Mutashabihat already returns a canonical word-number range and now highlights that range in the existing visible text.

The missing capabilities are:

1. Similar and Mutashabihat cards do not expose linking selection controls.
2. The current source action accepts only a descriptor; it cannot carry initial grouped/manual selection configuration.
3. Workspace add creates a default manual source but cannot atomically seed selected words and grouped shape.
4. Mutashabihat occurrences do not currently return their canonical ayah ID or canonical matched Quran word IDs.
5. Manual source identity currently depends only on its verse set, so two distinct Mushaf research contexts with the same verse set can collide.

## 7. Target architecture

### 7.1 Frontend launch model

Introduce a Frontend-owned launch model in the linking feature. Use names equivalent to the following; exact file-local naming may vary only if it remains equally explicit:

```ts
interface LinkingSourceLaunch {
  readonly source: LinkingSourceDescriptor;
  readonly initialConfiguration: LinkingSourceInitialConfiguration | null;
}

interface LinkingSourceInitialConfiguration {
  readonly inclusionMode: 'all-except' | 'only';
  readonly ayahOverrideIds: readonly number[];
  readonly selectedWords: readonly {
    ayahId: number;
    quranWordId: number;
  }[];
  readonly automaticWordMatchesEnabled: boolean | null;
  readonly manualLinkShape: 'grouped' | 'independent' | null;
  readonly descriptions: readonly [];
}
```

For this feature, every launch uses:

```text
inclusionMode = all-except
ayahOverrideIds = []
automaticWordMatchesEnabled = null
manualLinkShape = grouped
descriptions = []
```

Similar Ayahs sends no selected words. Mutashabihat sends the de-duplicated canonical selected words.

Existing word/root/lemma/stem/word-type callers continue to launch from a descriptor with `initialConfiguration = null`, preserving current defaults.

### 7.2 Manual source context key

Extend only the `manual-mushaf-ayahs` descriptor with an optional `contextKey`.

```ts
type LinkingManualMushafAyahSource = {
  manualAyahs: readonly LinkingManualMushafAyahReference[];
  contextKey: string | null;
};
```

Backend domain and API equivalents must carry the same optional value.

Compatibility rules:

- Existing general manual Mushaf sources use `contextKey = null`.
- A null context key preserves the exact current source identity format.
- A non-null context key contributes to source identity using an explicit marker, then the normalized verse keys.
- Frontend `linkingSourceKey` and Backend `LinkingSourceIdentity.For` must produce equivalent logical identity parts.
- Store the context key in the existing linking source `ScopeJson` document.
- Decode and return it in workspace projections.
- Do not add a database column or migration.
- Validate it as a trimmed, non-blank, bounded opaque key when present. Do not use the user-facing Arabic label as identity.

Recommended identity shape:

```text
manual-mushaf-ayahs|context|{encodedContextKey}|{encodedVerseKey1}|...
```

### 7.3 Atomic workspace initialization

Extend `POST /api/linking/workspace/sources` with an optional `initialConfiguration` object that does not duplicate the source label.

Conceptual request:

```json
{
  "descriptor": {
    "kind": "manual-mushaf-ayahs",
    "label": "متشابهات الآية 23 سورة البقرة",
    "contextKey": "mushaf-mutashabihat:2:23:groups:17,44",
    "manualAyahs": [
      { "verseKey": "2:23" },
      { "verseKey": "10:38" }
    ]
  },
  "initialConfiguration": {
    "inclusionMode": "all_except",
    "ayahOverrides": [],
    "selectedWords": [
      { "ayahId": 23, "quranWordId": 337 }
    ],
    "automaticWordMatchesEnabled": null,
    "manualLinkShape": "grouped",
    "descriptions": []
  },
  "workspaceVersion": 12
}
```

The numeric IDs above are illustrative only. Never copy them into code or fixtures.

Backend behavior:

- `initialConfiguration = null` preserves current add-source behavior.
- A provided configuration is validated against the resolved descriptor membership before persistence.
- Source creation, manual membership rows, configuration rows, source/workspace version stamps, and transaction commit are atomic.
- Existing equivalent sources are never silently overwritten by a new launch. Preserve existing configuration and return the existing source behavior.
- A newly created source returns the configured shape and selected words in the same workspace response.

### 7.4 Mutashabihat response enrichment

Extend `MutashabihatOccurrenceDto` with:

```text
ayahId: integer
matchedQuranWordIds: integer[]
```

Do not add a full alternate word-rendering array. The response enrichment exists only to create canonical linking selections.

The existing fields remain unchanged:

- `textUthmani`
- `wordFrom`
- `wordTo`
- `phraseTextUthmani`
- `isSelectedAyah`
- navigation metadata

## 8. Expected file placement

The implementer must confirm paths with `rg --files` before editing. Do not invent a parallel folder.

### 8.1 Backend paths likely to change

```text
Backend/domain/QuranDashboard.Domain/Linking/LinkingSourceDescriptor.cs
Backend/application/QuranDashboard.Application.Abstractions/Linking/LinkingSourceIdentity.cs
Backend/application/QuranDashboard.Application.Abstractions/Linking/LinkingSourceDescriptorValidation.cs
Backend/application/QuranDashboard.Application.Abstractions/Linking/ILinkingWorkspaceWriter.cs
Backend/application/QuranDashboard.Application.Abstractions/Linking/LinkingWorkspaceConfigurationInput.cs
Backend/application/QuranDashboard.Application.Abstractions/Quran/MushafReader/Responses/AyahMutashabihatResponse.cs
Backend/application/QuranDashboard.Application/Linking/Commands/AddLinkingWorkspaceSource/
Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingSourceDescriptorBody.cs
Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingSourceDescriptorBodyMapper.cs
Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingWorkspaceBodies.cs
Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingWorkspaceConfigurationBodyMapper.cs
Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingWorkspaceController.cs
Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Linking/LinkingSourceStorage.cs
Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Linking/LinkingWorkspaceProjection.cs
Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfAyahMutashabihatReader.cs
Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingWorkspaceWriter.cs
Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingWorkspaceWriter.Configuration.cs
```

If atomic-initialization validation makes either writer file exceed its responsibility threshold, add a narrowly named partial such as:

```text
EfLinkingWorkspaceWriter.InitialConfiguration.cs
```

Do not add a generic helper or service without a concrete responsibility.

### 8.2 Frontend linking paths likely to change

```text
Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-manual-mushaf.models.ts
Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-source.models.ts
Frontend/quran-dashboard-ui/src/app/features/linking/models/linking-source-launch.models.ts
Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-key.ts
Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-descriptor-body.ts
Frontend/quran-dashboard-ui/src/app/features/linking/components/quran-source-linking-actions/
Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-operation-draft.store.ts
Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-inline-source-workflow.controller.ts
Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workflow.facade.ts
Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace-source-adder.ts
Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-workspace.store.ts
Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-workspace.repository.ts
Frontend/quran-dashboard-ui/src/app/features/linking/data-access/http-linking-workspace.repository.ts
```

### 8.3 Frontend Mushaf paths likely to change

```text
Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/
Frontend/quran-dashboard-ui/src/app/features/mushaf/components/similar-ayahs-card/
Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mutashabihat-groups-card/
Frontend/quran-dashboard-ui/src/app/features/mushaf/components/study-ayah-result/
Frontend/quran-dashboard-ui/src/app/features/mushaf/models/mushaf.models.ts
Frontend/quran-dashboard-ui/src/app/features/mushaf/utils/mushaf-related-linking-source.ts
```

The exact utility filename may change if an existing Mushaf utility already owns this responsibility. Do not place Mushaf-specific source assembly inside generic linking utilities.

### 8.4 Generated contract paths

```text
Frontend/quran-dashboard-ui/openapi/swagger.json
Frontend/quran-dashboard-ui/src/app/core/api/generated/models.ts
Frontend/quran-dashboard-ui/src/app/core/api/generated/models/linking-source-descriptor-body.ts
Frontend/quran-dashboard-ui/src/app/core/api/generated/models/linking-workspace-add-source-body.ts
Frontend/quran-dashboard-ui/src/app/core/api/generated/models/mutashabihat-occurrence-dto.ts
```

Never hand-edit generated TypeScript models.

## 9. Phase 1 — Backend contracts and atomic workspace initialization

### 9.1 Goal

Make the Backend capable of representing contextual manual Mushaf sources, returning canonical Mutashabihat selection IDs, and atomically creating a configured workspace source.

### 9.2 Tasks

1. Add optional `ContextKey` to `LinkingSourceDescriptor.ManualMushafAyahs`.
2. Normalize and validate the context key once in the domain/application boundary.
3. Preserve the old identity exactly when the context key is null.
4. Add the marked contextual identity form when the key is present.
5. Store/decode the context key through the existing `ScopeJson` schema.
6. Add nullable `ContextKey` to the API descriptor body and mapper.
7. Ensure workspace responses round-trip the same context key.
8. Add `AyahId` and `MatchedQuranWordIds` to `MutashabihatOccurrenceDto`.
9. Update `EfAyahMutashabihatReader` to select canonical Quran word IDs and derive the exact ordered range.
10. Keep visible text and `PhraseTextUthmani` behavior unchanged.
11. Add an optional `InitialConfiguration` body to workspace source creation.
12. Map it into a `LinkingWorkspaceConfigurationInput` whose label comes from the descriptor.
13. Pass the optional configuration through command, handler, writer interface, and writer implementation.
14. Resolve the descriptor under the current linking-data revision before validating configuration membership.
15. Validate:
    - descriptor/configuration kind coherence;
    - inclusion mode and overrides;
    - every referenced ayah belongs to the source;
    - every selected Quran word belongs to the referenced ayah and source;
    - no ayah-marker word is selected;
    - manual shape is present and automatic matching is null;
    - descriptions satisfy existing limits;
    - duplicate selected word IDs are normalized or rejected consistently with existing delta behavior.
16. Persist initial configuration only for a newly created source.
17. Keep duplicate-source behavior non-destructive.
18. Keep the whole add operation in the existing transaction.
19. Export Swagger and regenerate the Frontend API models.
20. Confirm no migration or model-snapshot change exists.

### 9.3 Required acceptance checks

- A legacy manual descriptor with null context key produces its old source identity.
- Two contextual descriptors with the same verse keys but different context keys produce different identities.
- Contextual descriptors round-trip through workspace storage/projection.
- A Mutashabihat occurrence returns an ayah ID and the exact ordered canonical target word IDs.
- The target ID count matches the inclusive range length for inspected live data.
- Adding a configured Similar-Ayah-style manual source creates a grouped source with no selected words.
- Adding a configured Mutashabihat-style manual source creates a grouped source with the supplied selected words.
- A selected word outside the source or owned by another ayah is rejected.
- Existing add-source clients that omit initial configuration retain current defaults.
- Backend build succeeds.
- OpenAPI export and Frontend API generation succeed.
- `git diff --check` succeeds.

### 9.4 Phase 1 stop boundary

Stop after the contracts, Backend implementation, generated API models, and Phase 1 verification are complete. Do not wire any Frontend action or selection UI in this phase.

## 10. Phase 2 — Frontend linking launch plumbing

### 10.1 Goal

Teach the existing linking actions, workspace add path, and direct-link path to consume the same optional initial configuration without changing existing callers.

### 10.2 Tasks

1. Add `LinkingSourceLaunch` and `LinkingSourceInitialConfiguration` feature models.
2. Add a helper that wraps an existing descriptor with `initialConfiguration: null`.
3. Extend manual Frontend descriptors with nullable `contextKey`.
4. Update descriptor guards, API mapping, and `linkingSourceKey`.
5. Preserve existing manual source keys when `contextKey` is null.
6. Update `QuranSourceLinkingActionsComponent` to receive one launch definition or an optional initial configuration while keeping current word-explorer callers source-compatible.
7. Ensure both buttons use the same immutable launch snapshot.
8. Update `LinkingWorkflowFacade.startFromSource` and `LinkingInlineSourceWorkflowController.start` to accept the launch.
9. Update `createInlineLinkingDraft` so a supplied configuration seeds:
   - inclusion selection;
   - selected word IDs by ayah ID;
   - manual link shape;
   - automatic-word state;
   - descriptions.
10. Preserve current defaults when configuration is absent.
11. Update `LinkingWorkspaceStore`, `LinkingWorkspaceSourceAdder`, repository interface, and HTTP repository to send the optional initial configuration.
12. Preserve current duplicate detection and feedback behavior.
13. Keep add operations serialized through the existing workspace sync queue.
14. Do not open the workspace automatically after `إضافة للربط`; preserve current action behavior.
15. Do not bypass direct-link source configuration, door selection, preflight, or confirmation.

### 10.3 Required acceptance checks

- Existing word/root/lemma/stem/word-type action callers compile without behavioral change.
- Existing general manual Mushaf selection compiles and retains independent/default behavior.
- A configured manual launch opens direct link with all source ayahs included, grouped shape selected, and seeded words already selected.
- The same launch sent to workspace addition contains the equivalent wire configuration.
- Source/action labels remain accurate.
- Frontend no-unit-spec gate, typecheck, and build all succeed as separate commands.
- `git diff --check` succeeds.

### 10.4 Phase 2 stop boundary

Stop after launch plumbing works through a controlled/manual invocation. Do not add selection controls to Similar Ayahs or Mutashabihat in this phase.

## 11. Phase 3 — Similar Ayahs selection and linking actions

### 11.1 Goal

Allow Owners to assemble the original ayah plus selected Similar Ayahs and hand the grouped source to either linking action.

### 11.2 State model

Use response-bound local state, preferably `linkedSignal`, keyed to the accepted Similar Ayahs response or selected verse key.

Store selected related verse keys only. Do not store the original verse key in the mutable set.

Derived values:

- selected related count;
- all-related-selected state;
- valid action state (`selectedRelatedCount > 0`);
- de-duplicated manual ayah references;
- source label;
- context key;
- immutable `LinkingSourceLaunch`.

### 11.3 Tasks

1. Pass the selected ayah metadata from `SelectedAyahSectionComponent` to `SimilarAyahsCardComponent`.
2. Add Owner-only selection controls and a selected-count summary.
3. Add `Select all` and `Clear all` controls.
4. Extend `StudyAyahResultComponent` with separate generic linking-selection inputs/outputs:
   - linking selection enabled;
   - linking selected state;
   - accessible selection label;
   - selection toggle output.
5. Keep the existing `selected` input dedicated to the original-ayah presentation; do not overload it with linking selection.
6. Keep the Quran text as its existing navigation button.
7. Build the source from:
   - the original `AyahCoreDto` reference;
   - selected `SimilarAyahItemDto` references.
8. De-duplicate by verse key.
9. Set the label and context key exactly as locked above.
10. Build the grouped initial configuration with no selected words.
11. Render the shared linking actions only when at least one related ayah is selected.
12. Preserve loading, error, and empty states.
13. Reset selection when the response changes or becomes unavailable.
14. Preserve keyboard navigation, clear focus indication, RTL order, and compact layouts.

### 11.4 Edge cases

- Ignore a malformed result whose `targetVerseKey` equals the original verse key when assembling the source.
- De-duplicate repeated target verse keys even if the read contract should already be unique.
- If response data changes while selected, rebuild state from the new response and remove stale keys.
- Do not launch when only the implicit original ayah remains.
- Navigation from a result must not toggle selection.
- Toggling selection must not navigate to the result.

### 11.5 Required acceptance checks

- One selected related ayah produces a two-ayah source including the original.
- Selecting all produces original plus all unique related ayahs.
- Clearing all hides/disables the actions and retains no actionable stale source.
- Direct link opens with grouped shape and all assembled ayahs selected.
- Workspace addition persists the same label, context, membership, and grouped shape.
- Similar-Ayah Quran text is unchanged.
- Non-Owner UI is unchanged.
- Frontend no-unit-spec gate, typecheck, and build succeed.
- Live browser verification succeeds on an ayah with Similar Ayahs.
- Console error count is zero for the tested path.
- `git diff --check` succeeds.

### 11.6 Phase 3 stop boundary

Stop after Similar Ayahs is complete and reviewed. Do not begin Mutashabihat selection in this phase.

## 12. Phase 4 — Mutashabihat block/occurrence selection and linking actions

### 12.1 Goal

Allow Owners to select complete blocks, refine individual occurrences, combine multiple blocks into one grouped source, and automatically seed every selected occurrence's canonical target words.

### 12.2 Stable selection identity

Define one occurrence selection key that cannot collide within a response. It must include at least:

```text
sourceGroupId
ayahId
wordFrom
wordTo
```

Do not key selection by verse key alone because one ayah may have multiple target ranges.

### 12.3 State model

Use response-bound local state, preferably `linkedSignal`, containing selected occurrence keys.

Derived per block:

- selected occurrence count;
- none selected;
- fully selected;
- indeterminate.

Derived for the assembled source:

- selected occurrence count;
- selected group IDs that still contain selected occurrences;
- unique ayahs by ayah ID/verse key;
- selected canonical word IDs grouped by ayah ID;
- complete canonical-word-seed validity;
- source label;
- context key;
- immutable `LinkingSourceLaunch`.

### 12.4 Tasks

1. Add an Owner-only overall selection toolbar with selected count, Select all, and Clear all.
2. Add a block-level checkbox/control to each block header.
3. Expose the indeterminate state accessibly and visually.
4. Selecting a collapsed block must select all of its occurrences without expanding it.
5. Keep selection intact across expand/collapse.
6. Add occurrence-level selection controls through `StudyAyahResultComponent`.
7. Keep occurrence text navigation separate from occurrence selection.
8. Preserve the existing selected-ayah badge.
9. Preserve the existing target-word coloring based on `wordFrom`/`wordTo`.
10. Assemble selected occurrences across all selected/partial blocks.
11. De-duplicate manual ayah references.
12. Union and sort canonical `matchedQuranWordIds` per ayah.
13. Reject launch assembly if any selected occurrence has:
    - missing ayah ID;
    - an empty target-word list;
    - a target count different from its inclusive word range.
14. Derive the context key from only group IDs that still contribute at least one selected occurrence.
15. Set the exact source label locked above.
16. Build the grouped initial configuration with selected words flattened as `{ ayahId, quranWordId }` pairs.
17. Render the shared linking actions only for a valid non-empty assembled source.
18. Keep current block counts, expansion controls, result navigation, and responsive flow.

### 12.5 Selection algorithms

#### Select one block

```text
next = current selected occurrence keys
for each occurrence in block:
  add stable occurrence key
```

#### Clear one block

```text
next = current selected occurrence keys
for each occurrence in block:
  remove stable occurrence key
```

#### Toggle an occurrence

```text
if key selected: remove it
else: add it
```

#### Build source membership

```text
selected occurrences
  -> validate canonical target IDs
  -> group by ayahId
  -> keep one manual ayah reference per ayah
  -> union matched word IDs per ayah
  -> sort word IDs
  -> sort/normalize verse membership through existing descriptor rules
```

### 12.6 Edge cases

- The same ayah may occur in multiple selected blocks.
- The same ayah may carry different selected ranges in different blocks.
- The same target word may be reached through more than one occurrence.
- A block may become indeterminate after one row is cleared.
- A fully cleared block must disappear from the context-key group list.
- A group selected through Select all can still be refined occurrence by occurrence.
- Selection must reset if the response reloads for another ayah.
- A selected occurrence hidden by collapse remains selected.
- A stale response must never launch against the new selected ayah.

### 12.7 Required acceptance checks

- Selecting one block selects every occurrence in it.
- Clearing one occurrence makes the block indeterminate.
- Selecting two blocks creates one source, not two sources.
- The source label uses the selected ayah, not a representative occurrence.
- Duplicate ayahs are stored once.
- Canonical target word IDs are unioned correctly per ayah.
- Direct link opens with the same preselected target words shown by the Mushaf target ranges.
- Workspace addition persists the same grouped shape and selected words atomically.
- Editing the persisted source in the existing linking editor shows the words selected.
- Target coloring and visible Quran text remain unchanged.
- Non-Owner UI is unchanged.
- Frontend no-unit-spec gate, typecheck, and build succeed.
- Live browser verification succeeds on an ayah with multiple Mutashabihat blocks.
- Console error count is zero for the tested path.
- `git diff --check` succeeds.

### 12.8 Phase 4 stop boundary

Stop after Mutashabihat is complete and reviewed. Do not perform unrelated UI polish or broaden the feature to Quran search.

## 13. Phase 5 — Integrated hardening and final feature verification

### 13.1 Goal

Verify both sources through both linking entry points, confirm compatibility with the existing linking system, and prepare the feature for formal engineering review.

### 13.2 Required live scenarios

#### Similar Ayahs — direct link

1. Open a Mushaf ayah with Similar Ayahs.
2. Select two related ayahs.
3. Start direct link.
4. Confirm the original ayah is included.
5. Confirm all three ayahs are selected.
6. Confirm grouped shape is active.
7. Select a door.
8. Reach prepared preflight.
9. Do not confirm the write unless the owner explicitly authorizes that external mutation for the verification session.

#### Similar Ayahs — workspace

1. Assemble a different valid selection.
2. Add it to the workspace.
3. Open the source editor.
4. Confirm label, context, membership, and grouped shape.
5. Confirm no manual words are selected.

#### Mutashabihat — direct link

1. Open an ayah with at least two blocks.
2. Select one whole block.
3. Clear one occurrence.
4. Select another block.
5. Start direct link.
6. Confirm one source is opened.
7. Confirm selected ayahs match the refined selection.
8. Confirm every expected target word is selected.
9. Confirm duplicates are not repeated.
10. Reach prepared preflight without confirming unless separately authorized.

#### Mutashabihat — workspace

1. Assemble a valid multi-block source.
2. Add it to the workspace.
3. Open the source editor.
4. Confirm grouped shape.
5. Confirm selected words survive persistence and reload.

#### Compatibility

1. Confirm an existing word/root/lemma/stem action still starts direct linking.
2. Confirm the existing general manual Mushaf selection still adds/launches with its old defaults.
3. Confirm a read-only/non-Owner session has no new linking controls.
4. Confirm the Quran search UI has no new linking control or behavior.
5. Confirm navigation by clicking result text still works.

### 13.3 Final static verification

Run these commands separately:

```bash
dotnet build Backend/QuranDashboard.sln --disable-build-servers -m:1 -p:BuildInParallel=false -v minimal
```

From `Frontend/quran-dashboard-ui/`, run in this order:

```bash
npm run check:no-unit-specs
npm run typecheck:app
npm run build:verify
```

Then run:

```bash
git diff --check
```

Also confirm:

- no migration or model-snapshot diff;
- no Quran source-resource diff;
- no code-area README;
- no `*.spec.ts` file;
- no new Playwright journey;
- no search-feature file change unless it is a shared linking type that is demonstrably required and behavior-neutral;
- changed files remain within structure/responsibility thresholds or are split before delivery.

### 13.4 Phase 5 stop boundary

Stop with the full review package. Do not delete this plan yet. The plan is deleted only after the final engineering review passes and the owner requests the final pre-merge cleanup.

## 14. Testing Decision

**Decision: no new automated tests.**

Reason:

- The repository Test Freeze is active.
- Frontend unit specs are prohibited.
- No owner approval has been given for a new Backend test method/class or Playwright journey.
- The feature can be verified through Backend build, Frontend gates, targeted contract inspection, and live browser checks of the existing direct/workspace workflows.

Do not claim coverage. Do not add a test merely because a DTO, component, endpoint body, or state class changed.

If the owner later explicitly approves retained protection for the atomic workspace write, pause and use the test-review route before writing it. That approval is not implied by this plan.

## 15. Mandatory phase review package

At the end of every phase, the implementer must stop and report all of the following:

1. **Phase completed:** exact phase number and name.
2. **Outcome:** concise behavior now implemented.
3. **Changed files:** exact root-relative paths grouped by Backend, Frontend, generated, and docs.
4. **Contract changes:** request/response/domain/state changes introduced in that phase.
5. **Data safety:** confirmation that no Quran text/resource was mutated or reconstructed.
6. **Scope check:** confirmation that Quran search and future phases were not implemented.
7. **Verification:** every command run and its pass/fail result.
8. **Live evidence:** exact route, selected ayah, selection made, and observed result when the phase has UI behavior.
9. **Warnings/uncertainty:** anything not proven, skipped, or environment-blocked.
10. **Diff hygiene:** `git diff --check` result, unexpected files, and current Git status.
11. **Next phase:** name only; do not start it.

The owner will request a separate review after each phase. The implementer must leave the work in a reviewable, buildable state.

## 16. Final acceptance checklist

The feature is complete only when every item below is true:

- [ ] Similar Ayahs supports individual selection.
- [ ] Similar Ayahs supports Select all and Clear all.
- [ ] Similar Ayahs always includes the original ayah in an actionable source.
- [ ] Similar Ayahs launches one grouped direct-link source.
- [ ] Similar Ayahs adds the equivalent grouped source to workspace.
- [ ] Mutashabihat supports block selection.
- [ ] Mutashabihat block state becomes indeterminate after partial refinement.
- [ ] Mutashabihat supports occurrence-level clearing/reselection.
- [ ] Multiple blocks assemble into one source.
- [ ] Block boundaries are not persisted as separate units.
- [ ] Duplicate ayahs are de-duplicated.
- [ ] Target Quran word IDs are canonical, complete, and de-duplicated.
- [ ] Target words are preselected in direct link.
- [ ] Target words are persisted atomically in workspace addition.
- [ ] Existing target-word coloring remains unchanged.
- [ ] Existing Quran visible text remains unchanged.
- [ ] Existing result navigation remains functional.
- [ ] Existing general manual Mushaf selection remains functional.
- [ ] Existing lexical linking actions remain functional.
- [ ] Read-only users see no linking controls.
- [ ] Quran search has no new linking behavior.
- [ ] No database migration exists.
- [ ] No Quran source resource changed.
- [ ] No unauthorized automated test was added.
- [ ] Backend build passes.
- [ ] Frontend no-unit-spec gate passes.
- [ ] Frontend typecheck passes.
- [ ] Frontend production build passes.
- [ ] Live browser scenarios pass without console errors.
- [ ] Every phase received an owner-requested review before the next phase began.
- [ ] Final engineering review passes.
- [ ] This temporary feature-plan folder is deleted before merge.

## 17. Non-negotiable implementation order

The order is:

```text
Phase 1: Backend contracts and atomic workspace initialization
    ↓ owner review
Phase 2: Frontend linking launch plumbing
    ↓ owner review
Phase 3: Similar Ayahs selection and linking actions
    ↓ owner review
Phase 4: Mutashabihat block/occurrence selection and linking actions
    ↓ owner review
Phase 5: Integrated hardening and final verification
    ↓ final engineering review
Plan cleanup before merge
```

Do not combine phases to save time. The review boundary is part of the feature's safety design.
