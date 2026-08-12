# Abwab Linking Frontend Prototype V2 Report

## 1. Executive Summary

**Verdict: READY_FOR_V2_FRONTEND_PLAN**

The implemented frontend prototype has the right outer foundations: Owner-only access, a global workspace host, source descriptors and stable source keys, complete source resolvers, adaptive ayah inclusion, an Abwab Door picker, neutral ayah rendering, a mock command boundary, and app-level modal/inert composition. Those pieces make V2 a focused frontend reshape rather than a restart.

The V1 product model is nevertheless scalar in the places V2 now needs composition. The current Direct Link state and command carry one source; the workspace renders large independent-source cards; Mushaf Linking is attached to a selected word; sessionStorage is deliberately session-lived; and the review copy promises independent links. V2 must make the operation set-based while preserving each prepared source's independent configuration.

The clean direction is:

- retire only the Mushaf selected-word Linking seam and preserve all ordinary Mushaf word study;
- introduce a manual Mushaf ayah source, created through an Owner-only header mode named تحديد;
- replace workspace cards with a dense semantic source list;
- keep source-row selection, per-source ayah inclusion, and per-source word behavior as three orthogonal states;
- resolve selected sources independently, then derive a non-persisted MergedLinkingSelection by verseKey;
- use canonical QuranWord IDs where reads already provide them and a validated, presentation-only occurrence alignment where they do not;
- feed both a one-source Direct Link shortcut and workspace multi-source linking into the same Door, merge, review, and mock-confirm pipeline;
- place workspace persistence behind an actor-bound repository port, temporarily backed by versioned localStorage;
- preserve one primary Linking shell and exactly one vertical scroll owner for each surface, with only the shared remove-all alertdialog exception;
- keep the next implementation frontend-only and leave all real persistence, linking writes, authorization enforcement, grouping storage, approval, and audit design to a later backend phase.

This is a source-code audit only. No browser, DevTools, screenshots, runtime measurements, tests, production build, backend inspection beyond existing read-contract shapes, or implementation changes were used to reach the verdict.

## 2. Current Implemented Linking Architecture

### 2.1 Composition and ownership

The current feature is rooted in Frontend/quran-dashboard-ui/src/app/features/linking/.

- state/linking-access.service.ts exposes the existing fail-closed Owner gate.
- app.ts mounts LinkingWorkspaceHostComponent beside the app shell and Words entity-detail overlay. When Linking opens, the background shell and any underlying entity overlay are inert and aria-hidden.
- core/layout/top-navbar/top-navbar.component.ts and .html expose the Owner-only مساحة الربط action and workspace count.
- components/linking-workspace-host/ owns one qd-modal-shell, swaps the workspace and Direct Link bodies inside it, and keeps the modal at the sanctioned 80vw by 80dvh workspace geometry.
- The entire shell is currently inside @defer (when isOpen()) with no placeholder. From code, this leaves a possible first-open interval in which the app is inert but the dialog has not materialized.

The current composition is:

    Owner source action or Navbar
      -> LinkingWorkspaceStore
      -> global LinkingWorkspaceHost
         -> Workspace surface
         or
         -> scalar Direct Link workflow
            -> one source resolver
            -> one source ayah selection
            -> one Door
            -> review
            -> mock confirmation

### 2.2 Source descriptors and integrations

models/linking-source.models.ts defines the discriminated source union:

- mushaf-word;
- unique-word, with simple or tashkeel mode;
- root;
- lemma, including typeCode;
- stem, including typeCode;
- word-type, including word/root/stem/lemma selection kind and its full type/filter scope.

utils/linking-source-key.ts derives stable keys from identity-bearing descriptor fields and deliberately excludes display labels. This already prevents equivalent source rows from being added twice while allowing distinct modes/scopes to coexist.

components/quran-source-linking-actions/ is the reusable Owner-only action seam used by Words source pages and detail overlays. It currently offers إضافة للربط and ربط مباشر.

The source integrations are not centralized in one page. They are projected from:

- Unique Word drilldown and overlay adapters;
- Roots explorer page and root overlay adapter;
- Lemmas explorer page and lemma overlay adapter;
- Stems explorer page and stem overlay adapter;
- Word Type detail view-model, page, and overlay adapter;
- the Mushaf selected-word study section, which V2 must remove from Linking.

### 2.3 Resolution and neutral Quran models

data-access/linking-source-resolver.ts, linking-source-resolver.registry.ts, and the family-specific files under data-access/resolvers/ translate source read results into LinkingAyah[].

data-access/complete-paged-source.loader.ts is an important reusable boundary. It:

- loads all pages sequentially;
- validates successful envelopes;
- verifies page continuity, page size, total stability, and final item count;
- emits the aggregate only after the full result succeeds;
- does not publish a partial result.

models/linking-ayah.models.ts provides a neutral ayah shape. verseKey is universally populated. ayahId, surah display metadata, and canonicalQuranWordId are nullable. Each word currently has renderPosition, Uthmani text, marker state, and one scalar isSourceMatch flag.

Identity availability differs by resolver:

| Resolver family | Stable ayah identity | Canonical word identity today | Match truth today |
| --- | --- | --- | --- |
| Unique Word | verseKey | quranWordId is retained | matchedQuranWordIds / mapped match |
| Word Type | verseKey | quranWordId is retained | matchedWordIds / mapped match |
| Root | verseKey | not supplied in the neutral result | truthful isMatched flag |
| Lemma | verseKey | not supplied in the neutral result | truthful isMatched flag |
| Stem | verseKey | not supplied in the neutral result | truthful isMatched flag |
| Mushaf selected word | verseKey | only the analyzed selected occurrence is known | selected occurrence only; path is retired |

Within a single resolver, duplicate verseKey rows collapse only when the fully mapped ayah objects are identical; contradictory duplicates fail. Cross-source merge cannot reuse JSON equality because nullable metadata and available word IDs legitimately differ among source families.

### 2.4 Workspace state and persistence

models/linking-workspace.models.ts defines a V1 item containing:

- sourceKey and descriptor;
- adaptive ayah selection, represented as all-except or only;
- resultCount;
- one highlightSourceWords boolean.

utils/linking-selection.ts is reusable. It supports:

- all selected by default;
- explicit exclusion or explicit inclusion modes;
- select all and clear all;
- reconciliation with a refreshed complete universe;
- selection retention for rows hidden by local filtering;
- deterministic expansion to selected verse keys.

state/linking-workspace.store.ts owns the ordered workspace collection, modal surface, and one activeSourceKey. addOrFocus is idempotent by source key. The current تعديل اختيار الآيات workspace action only calls addOrFocus; no workspace editor consumes that focus, so the action has no visible functional effect.

state/linking-workspace-session.ts combines:

- a version-1 codec;
- actor binding;
- direct sessionStorage access under one global key;
- descriptor validation and invalid-item filtering.

It clears stored state when the actor changes or logs out. That matches a disposable prototype session, but not the newly locked “return another day” workspace behavior.

### 2.5 Current workflow and mock boundary

models/linking-workflow.models.ts and state/linking-workflow.facade.ts are single-source. The state carries one descriptor, source key, selection, highlight preference, and source load. The facade resolves that one source while loading the Door snapshot, then advances through Door, ayah, highlight, review, and result steps.

data-access/linking-command.port.ts is an injected replaceable boundary, but the current command is scalar and synchronous. It carries one source, one Door ID, selected verse keys, and one highlight boolean.

data-access/mock-linking-command.port.ts performs no write. Its current validation is narrower than the surrounding copy: it checks access, Door membership in snapshot.byId, and a non-empty selected-key array. snapshot.byId also contains archived nodes, whereas the picker renders the live tree. V2 must validate against the exact visible/selectable Door set.

The current review also states that each selected ayah is linked independently. That directly conflicts with the locked grouped-default behavior for a multi-ayah manual Mushaf source.

### 2.6 UI and scroll composition

The host sets qd-modal-shell to flushBody=true:

- shared/ui/modal-shell/modal-shell.component.scss gives the normal modal body overflow:auto;
- flush body changes that outer body to overflow:hidden;
- Workspace uses the shared .qd-details__body as its only vertical scroller;
- Direct Link uses .direct-link-workflow__body as its only vertical scroller;
- the Abwab picker, ayah-selection list, highlight list, and review list add no local overflow or max-height.

The committed Linking code therefore does not currently contain simultaneous outer and inner vertical scroll owners. The V2 risk is introducing one while adding source editors or manual word selection.

The workspace item is currently a bordered, rounded, multi-block card with three actions. This is structurally too large for the locked one-source-per-row composition and gives every row a competing primary Direct Link action.

## 3. Product Changes Since V1

| Concern | V1 implementation | Locked V2 product direction |
| --- | --- | --- |
| Mushaf source | One analyzed Mushaf word is a Linking source | Mushaf words are not Linking sources |
| Mushaf entry | Linking actions beside selected-word study | Owner-only تحديد in reader header |
| Mushaf unit | One selected word occurrence | One or several selected ayahs |
| Multi-ayah meaning | Review promises independent links | Multiple manual Mushaf ayahs default to grouped; explicit independent alternative remains possible |
| Workspace role | Storage basket of large source cards | Dense preparation and composition surface |
| Operation membership | Start Direct Link from one source | Check one or several prepared source rows |
| Ayah result | One source universe | Union of included ayahs from selected sources |
| Word result | One scalar source-match flag | Union of automatic/manual word contributions with provenance |
| Derived source words | One later highlight step | Per-source ON/OFF preference, default ON; no manual matching-word picker |
| Manual source words | Not supported | None, one, or many words per manually selected ayah |
| Persistence | actor-bound sessionStorage, cleared on logout | actor-bound durable-UX emulation, replaceable by future server repository |
| Workspace layout | nested cards | semantic dense result list / CSS Grid rows |
| Modal behavior | one shell, one owner per current surface | preserve one owner while adding editors and larger detail height |
| Core workflow | scalar source state and command | selected-source-set snapshot and shared merge/review pipeline |

The earlier V1 report and plan remain historical evidence only. Their scalar workflow, Mushaf word source, independent-link copy, and session-lifetime persistence must not be treated as current product requirements.

## 4. Keep / Modify / Remove Matrix

| Current element | Decision | V2 treatment |
| --- | --- | --- |
| LinkingAccessService | Keep | Continue fail-closed Owner visibility and repeat the gate at every mutating entry point |
| Navbar workspace action/count | Keep | Preserve the global entry; count remains prepared workspace source count, not selected-operation count |
| App-level inert and overlay layering | Keep | Preserve background isolation; repair deferred-shell focus timing |
| One global Linking modal host | Keep | Continue swapping feature/editor surfaces in one primary shell; shared remove-all alertdialog is the only nested modal exception |
| qd-modal-shell wide geometry | Modify | Keep sanctioned custom-size mechanism and Compact sheet behavior; recommend one stable 80vw by 88dvh V2 surface |
| qd-details-workspace frame | Keep | Continue as workspace frame and scroll owner |
| QuranSourceLinkingActions on Words sources | Keep | Retain Add to Workspace and optional one-source Direct Link shortcut |
| Source descriptor discrimination | Modify | Remove mushaf-word; add manual-mushaf-ayahs; retain every automatic family discriminator |
| Stable source-key utility | Modify | Retain current automatic identities; add sorted manual verse-set identity |
| Adaptive linking-selection helper | Keep | Reuse independently for every prepared source's complete ayah universe |
| CompletePagedSourceLoader | Keep | Preserve strict all-pages loading and no-partial-result behavior |
| Automatic source resolvers | Keep with output enrichment | Keep reads/mapping; add contributor-aware merge inputs without fabricating IDs |
| Resolver registry/facade | Modify | Remove Mushaf word resolver; support selected-source-set coordination outside the registry |
| LinkingAyah / LinkingAyahWord | Modify | Preserve neutral Quran data; replace scalar match semantics with source-contributor-aware derived data |
| WorkspaceStore | Modify | Add source-row membership, real editor routing, per-source configurations, and repository hydration |
| LinkingWorkspaceSession | Replace behind a port | Split pure versioned codec from storage; use actor-bound localStorage adapter for V2 |
| LinkingWorkspaceComponent | Modify | Add compact top actions, selected count, dense list, empty state, and editor navigation |
| LinkingWorkspaceItemComponent | Replace/reshape | Make it a semantic one-row source item instead of a card |
| LinkingAyahSelectionComponent | Reuse with corrections | Preserve complete-universe selection/search/bulk behavior; correct list semantics, focus, announcements, and bounded rendering |
| LinkingAyahCardComponent | Reuse with enrichment | Preserve Quran text rendering; accept union highlighting and accessible match metadata |
| LinkingWorkflowFacade | Split and modify | Do not grow the existing 390-line facade; extract source-set resolution and pure merging |
| DirectLinkWorkflowComponent | Modify | Consume a one-or-many operation draft and shared final pipeline |
| LinkingDoorStep / Abwab picker | Reuse with correction | Keep picker; single selection must be select-only and validation must reject non-live/non-selectable Doors |
| LinkingCommandPort | Modify | Make operation set-aware, grouping-explicit, and async-ready while retaining an injected mock |
| MockLinkingCommandPort | Modify | Validate full frontend draft and return presentation success only; still perform no write |
| Selected Mushaf word Linking actions | Remove | Remove only the Linking action block and its descriptor computation |
| MushafWordLinkingSourceResolver | Remove | Delete the resolver and registry registration |
| Ordinary Mushaf word study | Keep | Preserve analysis, morphology, URL/session behavior, keyboard study navigation, and glyph/text/metric behavior; add only the explicit neutral selection-mode button state/ARIA seam |
| Current Linking labels | Modify | Reuse canonical Words terminology and expose scope/mode discriminators |

## 5. Workspace V2 Information Architecture

### 5.1 Surface hierarchy

The workspace should remain one large, quiet work surface:

1. modal title and close control, owned by qd-modal-shell;
2. a compact workspace summary/action bar;
3. a visual Wide-only column guide;
4. one dense row per prepared source;
5. a contextual empty state when no sources exist;
6. one short status/undo region for non-modal feedback.

The smallest useful top action set is:

- a live selected-source count;
- primary ربط المحدد;
- إلغاء التحديد only when at least one row is checked;
- danger إزالة الجميع, visually separated from the primary action.

Do not keep a solid-green Direct Link button in every row. Direct Link remains available at the source explorer as a shortcut; workspace composition uses the one primary ربط المحدد action.

### 5.2 Dense row anatomy

Use qdResultList / qdResultItem semantics with a CSS Grid presentation, not an HTML data table and not nested cards. Label the role=list from a real workspace/source-list heading. The Wide column guide is visual only; it does not claim table column-header relationships. Every repeated control remains independently source-qualified. A row contains:

| Visual column | Content and behavior |
| --- | --- |
| المصدر | Canonical source family, value, and any discriminator needed to distinguish its stable source key |
| عدد الآيات | Native button showing selected/total or selected count; opens that source's ayah editor |
| تحديد الكلمات | Native checkbox for automatic sources; explicit اختيار الكلمات (N) button for manual Mushaf sources |
| إزالة المصدر | Isolated danger/tertiary button with source-qualified accessible name |
| تحديد | Native checkbox controlling membership in the next operation |

The human-readable source value currently exists only in descriptor.label for Unique, Root, Lemma, Stem, and Word Type; the numeric identity/scope fields cannot reconstruct it. Retain label as a validated display snapshot, but never as identity or authoritative Quran data. A typed formatter combines that snapshot with the descriptor's stable family/mode/scope fields:

- كلمات فريدة بدون تشكيل — الرحمن;
- كلمات فريدة بالتشكيل — الرَّحْمَن;
- جذر — ر ح م;
- الصيغة المعجمية — …, with typeCode scope when present;
- الأصل الصرفي — …, with typeCode scope when present;
- نوع كلمة — …, with concise selection kind and active filter scope;
- سورة البقرة — آية 2;
- a concise Quran-ordered range/count label for a multi-ayah manual source.

The current Linking labels reverse lemma/stem terminology relative to models/words-shared.labels.ts. V2 must use the canonical Words terms rather than creating a second vocabulary.

When an equivalent descriptor is added again, retain the existing ayah/word configuration and row position and refresh non-identity display metadata from the newer descriptor. Focus the existing row only when the workspace is already visible or the user explicitly opens it. إضافة للربط from an explorer must retain focus in that source surface and use its existing polite feedback instead of opening a hidden workspace or stealing focus. The current addOrFocus retains a possibly stale label because label is intentionally excluded from sourceKey.

The count cell must expose source resolution truth:

- unresolved: show لم تُحمّل and let the editor/start action initiate loading;
- stale persisted hint: label it as a previous count, never as the current selected count;
- loading: show raw transport progress separately from the final count;
- error: show تعذر التحميل with a source-qualified retry;
- ready: show the authoritative selected unique-ayah count, optionally selected/total.

CompletePagedSourceLoader progress counts raw API rows, while a resolver may collapse identical duplicate verseKey rows. Raw loaded-row progress and the final unique-ayah total are different values and must not share one label or cause the displayed total to appear to decrease.

### 5.3 Editor navigation

The ayah count is the single entry point to a source's ayah-inclusion editor. The manual source word button is the entry point to its manual word editor. Both editors should replace the workspace body inside the existing global shell; neither should open a nested modal.

The host needs explicit surface states such as workspace, source-ayah-editor, manual-word-editor, and linking-flow. Entering an editor records the invoking control. Returning restores focus to that row control if the source still exists, otherwise to the workspace heading or primary action.

### 5.4 Removal behavior

For one row:

- use an always-visible, isolated danger action;
- name it with the source, for example إزالة المصدر جذر — ر ح م;
- remove immediately;
- show a short in-shell undo notice that restores the complete removed row configuration;
- if the removed source was checked or being edited, reconcile operation membership/editor state.

A confirmation dialog for every row would slow a reversible prototype action. The undo pattern is sufficient if the action is not adjacent to the row checkbox and remains keyboard accessible.

After removal, move focus deterministically to the next row's equivalent control, otherwise the previous row, otherwise the empty-workspace heading/action. Keep live undo text in the existing role=status region and place the interactive تراجع button adjacent to—not inside—that status. Undo restores the full row and moves focus to its restored row heading/control only when the user invoked Undo.

For إزالة الجميع:

- always use the existing qd-confirm-dialog;
- include the number of prepared sources;
- use danger tone;
- put initial focus on cancel, as the house primitive already does;
- clear prepared sources and checked membership only after confirmation;
- do not delete another actor's bucket.

qd-confirm-dialog renders its own qd-modal-shell, so this is an explicit nested alertdialog exception rather than another Linking editor surface. Render it as a sibling top layer. While it is open, make the lower Linking dialog inert/aria-hidden and suppress its Close, Escape, and backdrop dismissal; the current modal stack disables only the lower focus trap and is not sufficient by itself. On cancel, restore focus to إزالة الجميع. After confirm, focus the empty-workspace heading/action because the opener no longer exists.

### 5.5 Empty and disabled states

When the workspace is empty, explain where sources come from: Quran source explorers and the Mushaf تحديد mode. Keep the Navbar entry visible to the Owner so the durable workspace remains discoverable.

When sources exist but none are checked:

- disable ربط المحدد;
- expose a nearby status that a source row must be selected;
- do not silently fall back to all prepared sources.

## 6. Workspace Selection Levels

The three selection levels are independent and must never be inferred from one another.

| Level | Question answered | Owner | Default | Prototype persistence | Merge effect |
| --- | --- | --- | --- | --- | --- |
| Workspace source selection | Which prepared sources participate in this operation? | workspace composition state | unchecked | Transient; reset on hydration/re-login | Determines operation members only |
| Per-source ayah inclusion | Which resolved ayahs from this source participate? | prepared source item | all included | Persisted | Filters that source before union |
| Per-source word behavior | Which word occurrences does this source contribute? | prepared source item | automatic ON; manual none | Persisted | Adds highlights only; never adds/removes an ayah |

The source-row checked set should be transient operation intent. Persisting it across browser close could make a later click link stale, forgotten membership. Prepared sources and their configurations survive; the user deliberately checks the sources for each operation.

Required invariants:

- checking a source does not select or deselect any ayah;
- changing ayah inclusion does not check the source row;
- turning automatic word matching off does not remove included ayahs;
- choosing no manual words does not remove a manual ayah;
- removing a source removes its key from the checked set;
- refreshing a source result reconciles only that source's ayah overrides;
- local search changes visibility only, never selection;
- an item with zero included ayahs may remain prepared and checked, but is reported as non-contributing before review.

## 7. Multi-Source Merge Semantics

### 7.1 Deterministic operation

The operation should be a pure derivation:

    checked source keys
      -> capture immutable member snapshots
      -> resolve each member's complete ayah universe
      -> reconcile and apply that member's ayah inclusion
      -> derive that member's word contributions
      -> union included ayahs by verseKey
      -> union compatible word occurrences per merged ayah
      -> order by Quran order
      -> present one merged review

Individual workspace items are inputs. The merger must not rewrite their ayah overrides, word preferences, descriptors, or persisted counts.

### 7.2 Ayah identity and metadata

verseKey is the universal stable ayah merge key in every current resolver and in the Mushaf page DTO. It is sufficient for frontend V2 ayah deduplication.

The current isVerseKey guard is only syntactic/coarse: it accepts any surah 1–114 with any ayah 1–286, including impossible coordinates for shorter surahs. V2 must validate automatic verse keys through their successful source read and manual verse keys through the page/study read. Parsing a key yields numeric surah and ayah components; use those components for Quran ordering and manual source-key normalization, never lexical string sorting.

ayahId is optional and should be treated as corroborating metadata:

- attach a non-null value when another contribution has null;
- accept equal non-null values;
- fail the merge with a controlled source-data error when non-null values conflict.

Apply the same enrichment/fail-on-conflict rule to non-null surah number, ayah number, and exact Quran display metadata. Never silently choose between contradictory Quran data.

The final merged order is Quran order derived from validated verse keys, not workspace row order or API arrival order.

### 7.3 Word identity and union

There is no universal canonical word ID across current reads.

- Unique Word and Word Type contributions can use canonical quranWordId.
- Root, Lemma, and Stem contributions only expose truthful match flags in a complete ordered ayah.
- Mushaf page words expose wordLocation, not quranWordId.
- renderPosition, wordNumber, lineWordOrder, and Uthmani text alone are not durable identities.

For the frontend presentation merge:

1. Build one validated display-word sequence for each verse, excluding ayah markers from selectable occurrence slots.
2. Require contributing complete-ayah sequences to agree on marker-normalized word count, order, and exact Uthmani display text before aligning them.
3. Use a transient occurrence slot scoped by verseKey for rendering and contributor union.
4. Attach a canonical quranWordId if any compatible contribution provides one.
5. Map a manual wordLocation into that slot from the complete Mushaf ayah sequence, retaining wordLocation only as the manual prototype coordinate.
6. If alignment or non-null canonical identity contradicts another contribution, fail that source/merge visibly; do not guess.

This slot is presentation-only. It may deduplicate one displayed highlight and retain source provenance, but it must never be persisted as a canonical QuranWord identity or sent as if it were one.

### 7.4 Required edge behavior

| Edge case | Required result |
| --- | --- |
| Same ayah in two selected sources | One merged ayah keyed by verseKey; both source keys recorded |
| Same word occurrence matched twice | One rendered highlight; contributing source keys unioned |
| One source highlight ON, one OFF | OFF source may contribute the ayah but contributes no word matches |
| One source excludes the ayah | That source contributes nothing; another included contribution can still retain the ayah |
| All selected sources exclude the ayah | Ayah absent |
| Equivalent source added twice | One prepared row because stable sourceKey is idempotent |
| Manual Mushaf ayah overlaps automatic source | One ayah; manual locations and automatic matches are unioned |
| One checked source has zero included ayahs | Show a non-contributing warning; allow continuation only if another source contributes |
| All checked sources produce zero merged ayahs | Controlled empty composition; block Door/review/confirm and return to source configuration |
| No source rows checked | Disable start action and explain how to check sources |
| One resolver fails | Do not confirm a partial subset; retain the operation draft and expose retry/back |

Source resolution may run concurrently, but merged success must be published atomically only after every checked member has either succeeded or produced a surfaced blocking failure.

### 7.5 Reconciliation ownership

Resolution can reveal added/removed source ayahs, so the coordinator—not the pure merger—reconciles each captured LinkingSelection against the newly resolved universe.

- The operation always uses its captured, reconciled snapshot.
- Capture a local configuration revision with each member.
- After a complete successful source resolution, the coordinator may ask LinkingWorkspaceStore to write the reconciled selection and final unique-ayah count back to the prepared row only if that row still exists and its configuration revision is unchanged.
- If the row changed, was removed, or another state replaced it, do not overwrite newer workspace intent.
- A failed or partial read writes nothing back.

This keeps the merge side-effect-free while preventing stale exclusions/counts from remaining indefinitely after a successful refresh.

## 8. Proposed Derived Merge Model

The following is a conceptual frontend model, not code and not a backend entity design.

### 8.1 Prepared workspace row

| Conceptual value | Meaning | Classification |
| --- | --- | --- |
| sourceKey | Stable equivalence identity derived from the descriptor | Source truth; later server-backed |
| sourceDescriptor | Typed automatic source or manual Mushaf ayah set | Source truth; temporarily persisted |
| sourceDisplaySnapshot | Validated descriptor label carrying the current human-readable value | Source display snapshot; temporarily persisted; never identity |
| sourcePresentation | Canonical family/mode/scope combined with the display snapshot | Derived UI state; never identity |
| ayahInclusion | all-except or only overrides against this source's universe | Temporarily persisted; later server-backed |
| automaticWordMatchPreference | ON/OFF for an automatic source | Temporarily persisted; later server-backed |
| manualAyahWordSelection | wordLocations selected per manual verse | Temporarily persisted coordinate; later replaced/resolved to canonical IDs |
| multiAyahMode | grouped or independent preference for a manual multi-ayah source | Temporarily persisted; later server-backed intent |
| lastResolvedCount | Optional display hint, never authoritative | Optional prototype persistence |
| checkedForOperation | Membership in the next composition | Transient derived/UI state |
| currentLoad/editor/error state | Resolution and interaction status | Transient derived/UI state |

AutomaticWordMatchPreference and ManualAyahWordSelection are mutually exclusive variants selected by source kind; they should not be two simultaneously meaningful fields.

### 8.2 Operation snapshot

A SelectedWorkspaceSource member contains:

- sourceKey;
- immutable descriptor snapshot;
- reconciled ayah inclusion snapshot;
- the relevant automatic or manual word configuration;
- manual multi-ayah interpretation when applicable.

The selected members are kept in deterministic workspace order for attribution and review labels. That order does not affect Quran ordering or deduplication.

### 8.3 Derived merged selection

MergedLinkingSelection contains:

- the operation member snapshots;
- per-source resolution status and warnings;
- Quran-ordered MergedAyahSelection records;
- total unique ayah count;
- any non-contributing-source notices;
- explicit blocking identity/data errors;
- manual link-shape annotations required by the mock review.

Each MergedAyahSelection contains:

- verseKey as merge identity;
- compatible Quran display metadata;
- contributing source keys;
- one display-word sequence;
- a union of highlighted occurrence records.

Each merged highlighted occurrence contains:

- its transient verse-scoped display slot;
- exact Uthmani display text;
- optional canonical quranWordId;
- optional manual wordLocation coordinate;
- contributing source keys;
- provenance indicating canonical identity or presentation-only alignment.

### 8.4 Ownership and location

The derived types belong in the Linking feature, conceptually beside models/linking-merge.models.ts. The pure, side-effect-free aggregation belongs beside utils/linking-merge.ts. A focused state coordinator should:

- capture the selected member snapshot;
- invoke existing resolvers;
- manage cancellation/retry;
- call the pure merger;
- publish one atomic merged result.

Do not put merging into a component, a source resolver, LinkingWorkspaceStore, or the already 390-line LinkingWorkflowFacade. Do not persist MergedLinkingSelection.

## 9. Automatic Source Word Highlighting

For Unique Word, Root, Lemma, Stem, Word Type, and future automatic derived sources:

- the source-level تحديد الكلمات control is a native checkbox;
- the default is ON when a source is added;
- ON means every truthful resolver-reported matching occurrence in an included ayah contributes to the merged highlight union;
- OFF means the source still contributes its included ayahs but contributes zero highlighted occurrences;
- the user never manually picks individual matching words for these sources;
- the preference is stored per prepared source, not as a workflow-global highlight step.

V2 should remove the current separate scalar highlight decision from the final flow. The workspace row already owns the source's behavior. A one-source Direct Link shortcut can present the same preference while configuring its one ephemeral operation member.

Identity rules by family:

- Unique Word and Word Type preserve current canonical QuranWord IDs and use them when available.
- Root, Lemma, and Stem preserve their resolver's truthful match flags. They must not invent numeric IDs.
- Contributor attribution uses sourceKey so a merged word can explain which selected sources matched it.
- Turning one contributor off removes only that contributor. If another ON source matches the same occurrence, the occurrence remains highlighted once.

Accessibility should be added at the ayah/card metadata level, such as a concise count or source summary. Do not inject spoken markers or extra elements into protected Quran glyph/text runs.

## 10. Manual Mushaf Ayah Word Selection

### 10.1 Manual source identity

Model a manual Mushaf selection as a distinct source kind, for example manual-mushaf-ayahs. It is not a derived word source.

Its descriptor should conceptually contain:

- one or more unique verse references;
- verseKey as the stable identity;
- page number(s) only as navigation/read context;
- sufficient display metadata to format the row, subject to refresh from Quran reads.

Equivalent manual sets should produce the same sourceKey from source-backed, deduplicated verseKeys normalized in numeric Quran order. Page number, label, current word selection, and grouped/independent preference are configuration or presentation and must not change identity.

Re-adding the exact same verse set targets the existing row and preserves its ayah overrides, manual words, and grouping preference; it refreshes read/display metadata only. Focus that row only when the workspace is visible; otherwise retain focus in the originating Words/Mushaf surface and announce that it already exists. It must not silently replace or merge configuration. A different configuration is made by editing that row, or by removing it before adding the set again.

### 10.2 Grouped-default contract

Persist one minimal manual preference:

- grouped;
- independent.

Derive the effective shape:

- one currently included ayah -> single;
- two or more currently included ayahs -> the explicit preference;
- a newly created multi-ayah manual source -> grouped by default.

Do not infer backend grouping from array length and do not fabricate a group ID. The frontend mock carries the explicit intent and displays it; a real durable group identity will be server-issued later.

Place the grouped/independent two-option control in the manual source's ayah editor when at least two ayahs are included. The review displays the current choice and offers a return to that editor. When inclusion temporarily drops to one ayah, show the effective single interpretation without erasing the stored multi-ayah preference.

### 10.3 Word-selection draft

The safest current manual coordinate is wordLocation from MushafWordDto, scoped and validated against verseKey. It is a stable Quran occurrence coordinate for the prototype, but not the canonical numeric database identity.

The generated frontend contracts make the boundary explicit:

- core/api/generated/models/mushaf-word-dto.ts exposes verseKey and wordLocation for every rendered page token but no quranWordId;
- core/api/generated/models/word-occurrence-dto.ts exposes quranWordId on the later word-analysis occurrence path;
- core/api/generated/models/ayah-core-dto.ts exposes authoritative pageFrom, pageTo, and wordsCount through the ayah-study path.

Persist conceptually:

- verseKey;
- zero, one, or many selected wordLocations for that verse.

An empty location set means the ayah remains included but contributes no manual word highlight. It never means all words or unresolved. Temporarily excluding that ayah from the source retains its manual word configuration, while the excluded ayah contributes neither itself nor its words to the current merge.

Do not persist renderPosition, wordNumber, lineWordOrder, an array index, or text as identity. Do not call word analysis for every click: the current analysis runner is single-flight and cancels its previous request, so N manual selections would produce N heavy requests and still not form a reliable batch.

The future backend/read contract should either return quranWordId with complete ayah/page words or batch-resolve submitted wordLocations.

### 10.4 Complete-ayah requirement

AyahCoreDto exposes pageFrom, pageTo, and wordsCount through features/mushaf/data-access/mushaf-ayah-study.api.ts and its existing getAyahStudy path, so an ayah may span pages. MushafPagesApi.getPage returns a page response without that authoritative range/count metadata. A descriptor's saved page numbers are navigation/cache hints only and must not prove completeness.

The frontend-only editor may reuse the study read plus Mushaf page reads to aggregate every authoritative page in the range and then extract the full verse. Before enabling word selection it must prove:

- the current AyahCoreDto loaded and its verseKey matches;
- every page from pageFrom through pageTo loaded successfully;
- tokens are ordered by numeric page number, containing line number, and lineWordOrder;
- non-marker wordNumber values are contiguous from 1 through wordsCount;
- the non-marker count equals wordsCount;
- every non-marker wordLocation is unique and belongs to the verse.

If completeness cannot be proven from current reads, show a controlled incomplete-source state and block the word editor/merged confirmation for that manual source. Never offer a partial ayah as if it were complete.

### 10.5 Scalable multi-ayah editor

Do not render every word of every selected ayah at once. Use one manual-word editor surface inside the existing shell:

- a compact ayah chooser/list with per-ayah selected-word counts;
- one active ayah's complete word buttons;
- clear words for this ayah;
- optional next/previous ayah navigation;
- an overall summary;
- back/save behavior that updates the prepared source configuration.

The list and active word area expand inside the editor's one body scroller. Do not add a nested modal or a second scrollable word pane.

Load complete word data lazily for only the active ayah. Reuse the existing shared page cache, deduplicate required reads by page number across ayahs, and use bounded request concurrency. Switching ayahs must cancel or ignore stale active-view publication without discarding already cached complete pages.

## 11. Mushaf Ayah Selection Mode

### 11.1 Exact integration seam

The correct visual seam is components/mushaf-header-navigation/, mounted before the protected page view by components/mushaf-page-area/mushaf-page-area.component.html.

Add a neutral projected header-action slot through MushafPageAreaComponent. The reader page/coordinator contributes the Owner-gated Linking action; the generic header and MushafWordComponent should not import Linking workflow state.

The action:

- is named exactly تحديد;
- sits near السابق, التالي, and سورة;
- uses aria-pressed to expose mode state;
- is absent for non-Owners;
- broadens the header's current navigation-only accessible label.

Header navigation currently uses a tight two-column grid, and page-area geometry reserves measured header space. The later implementation must preserve Quran width/line metrics and re-check the responsive header allowance after adding the action; this report makes no runtime measurement.

### 11.2 State ownership

Selection mode is an ephemeral, Owner-gated draft orthogonal to MushafReaderFacade's URL-backed ayah, word, panel, and study state. It contains:

- inactive or active status;
- selected verseKeys in Quran order;
- relevant page context for later complete reads;
- a status/count message;
- no Linking Door or workflow state.

Every mutation repeats the Owner gate. Losing access, leaving the reader, or explicit cancel discards the draft safely.

MushafPageArea currently unmounts both header and page content during page loading, error, or empty states. Keep a compact active-mode status/actions owner at MushafReaderPage level, outside that load-state conditional, so count, clear, cancel, and completion do not disappear during navigation. When the header action unmounts, move focus to that persistent owner; on successful page load, restore focus only when appropriate. Adding to the workspace remains disabled while any selected ayah lacks complete metadata.

### 11.3 Interaction flow

1. Activate تحديد. Move focus to the mode instruction/status and announce that clicking an ayah toggles it.
2. Clicking any non-marker word in an ayah toggles that verseKey once.
3. Keep the mode active while selecting one or several ayahs.
4. Keep السابق, التالي, and سورة navigation usable; preserve the draft and total count across Mushaf page navigation and restore selected styling when another fragment of an ayah enters view.
5. Mark every currently mounted non-marker word whose verseKey is selected, without implying that an unmounted cross-page fragment is visible and without changing Quran text, glyphs, word spacing, line grouping, or page metrics.
6. Offer مسح التحديد, إلغاء, and إضافة إلى مساحة الربط.
7. Adding creates or focuses one manual source descriptor for the selected verse set.
8. One ayah receives effective single interpretation. Two or more receive grouped preference by default.
9. Return to ordinary Mushaf interaction only after explicit completion or cancel.

The active header action and persistent selection status/count are the primary mode cues. Mounted word buttons may reuse the existing no-metric accent treatment used by mushaf-word--highlighted-ayah, generalized to multiple selected verse keys. Do not add an ayah-wide background wash or a single selection thread: the current DOM has no ayah wrapper, one line can contain several ayahs, and one ayah can cross lines/pages. The persistent mushaf-word--selected-word background/ring must keep visual precedence.

In selection mode, each non-marker word button changes action from studying a word to selecting/deselecting its ayah. Give every such button an action-specific accessible name including select/deselect and verseKey, plus accurate pressed/selected state; retaining only the spoken word text would be misleading.

### 11.4 Preserve word study behavior

The current event chain emits verseKey and then wordLocation from MushafWordComponent through line, page view, and page area to MushafReaderPage.

At the reader-page dispatch boundary:

- normal mode remains unchanged: ayahSelect selects/studies the ayah and wordSelect selects/studies the word;
- ayah-selection mode consumes ayahSelect as a verse toggle and ignores the immediately following wordSelect;
- document ArrowLeft/ArrowRight word-study navigation is gated while selection mode is active;
- the mode does not synchronously exit on first click, which would allow the following wordSelect event to reopen ordinary word study.

MushafWordComponent's glyph text, metrics, order, marker behavior, and normal emission contract remain intact, as do the line/page renderers, study panels, word analysis API/runner/cache, URL/session state, and Golden UI mushaf-word selector. Only the neutral selection-mode button class, state, accessible name, and reader-page dispatch branch may change.

Manual word choice happens later from the workspace row's اختيار الكلمات entry point, not beside selected-word study and not during the initial ayah-selection mode.

## 12. Direct Link vs Workspace Multi-Source Linking

### 12.1 One shared operation pipeline

Both entry paths should produce the same operation draft:

    Direct Link shortcut
      -> one ephemeral configured source member
      -> shared selected-source operation

    Workspace ربط المحدد
      -> one or many captured prepared-source members
      -> shared selected-source operation

    shared operation
      -> resolve every member
      -> reconcile member selections
      -> derive merged selection
      -> choose one live Door
      -> review merged ayahs and source/group intent
      -> mock confirm

Direct Link remains a convenience, not a second architecture. It should not require adding the source to the durable workspace unless the user explicitly chooses إضافة للربط.

### 12.2 Shared configuration and review

For one automatic source, Direct Link may expose:

- all-source ayah inclusion editor;
- the same automatic-word preference, default ON.

For a manual Mushaf source, normal entry is through the workspace; if a future Direct Link shortcut is exposed, it must still use the same manual grouped/word configuration contract.

The final review shows:

- one target Door;
- all operation source labels and contribution status;
- one Quran-ordered, deduplicated ayah list;
- union word highlighting;
- warnings for checked sources that contribute zero ayahs;
- explicit grouped/independent intent for manual multi-ayah sources;
- a visible write/mock error at the review step.

### 12.3 Door selection

Reuse the existing Abwab picker and snapshot loading once per operation. Correct two current behaviors:

- single=true has radio semantics, so choosing the selected Door must not toggle it to null;
- selection and mock confirmation must validate against the exact live/selectable tree supplied to the picker, not merely snapshot.byId, which also contains archived nodes.

The selected Door is transient operation state and is never stored in the prepared workspace.

### 12.4 Completion behavior

Mock confirmation performs no HTTP write and creates no history. On success:

- show a presentation-only success result;
- retain the prepared sources and their configurations;
- clear the transient checked-source set and operation draft on explicit finish;
- do not claim that a durable link, group, request, audit record, or workspace save occurred.

The command boundary should be async-ready even though the V2 adapter remains a frontend mock.

Treat the result as terminal: offer return to workspace/close or start another operation, not Back to the same review where the mock could be reconfirmed as though it represented a reversible persisted write.

### 12.5 Source-origin handoff

Preserve the current retained-overlay handoff: a Direct Link launched inside the global Words entity overlay captures its source/origin, closes the overlay into its retained-history state, and activates Linking only after the overlay is closed.

The widened pipeline must carry a transient focus-origin token:

- workspace entry -> invoking workspace row/top action;
- inline Words page/panel entry -> connected action, then stable containing row/panel opener fallback;
- entity-overlay entry -> retained overlay frame plus stable source action key;
- Navbar entry -> Navbar Linking trigger.

On initial flow-body readiness, focus the flow heading/first step. On dismissal from a retained entity-overlay origin, restore the retained overlay when the route/frame is still valid and focus the regenerated Direct Link source action after render. If it cannot be restored, use the originating page row/panel opener, then the Navbar trigger as final connected fallback. Never ask qd-modal-shell to focus a destroyed overlay button.

## 13. Workspace Persistence During Frontend Prototype

### 13.1 Storage decision

**Recommendation: actor-bound, versioned localStorage behind an injected workspace repository port.**

| Option | Browser/tab close | Logout/login | Actor isolation | Replaceability | Decision |
| --- | --- | --- | --- | --- | --- |
| Current sessionStorage | Fails browser-close requirement | Current implementation deletes on logout | Actor checked inside one envelope | Storage and codec are coupled | Do not retain for V2 |
| In-memory store | Fails refresh and close | Fails | Process-local only | Simple but does not emulate product | Reject |
| Direct localStorage calls in store | Survives | Can survive | Possible | Couples UI state to browser API | Reject direct coupling |
| Repository port + actor-bound localStorage adapter | Survives | Survives for same actor | Key and payload can both bind actor | Server adapter can replace it | Use for V2 |

This is a UX emulator, not the final persistence design and not a security boundary.

### 13.2 Repository boundary

Separate three responsibilities:

1. A pure versioned codec validates serialized prepared-workspace data.
2. A LinkingWorkspaceRepository port loads, saves, and invalidates one authenticated actor's workspace.
3. A LocalStorageLinkingWorkspaceRepository adapter implements that port for V2.

Components talk only to LinkingWorkspaceStore. The store talks only to the repository port. A later authenticated HTTP/server adapter should replace the local adapter without changing row components, editors, selection helpers, or merge logic.

The port should be async-ready even if localStorage resolves immediately, because its eventual server implementation will not be synchronous.

Persist after every durable workspace mutation; do not add a misleading Save button. Serialize writes so a slower earlier save cannot overwrite a newer in-memory revision, and surface adapter failure without discarding the working in-memory state.

### 13.3 Actor binding and lifecycle

Use an actor-specific key such as qd-linking-workspace:v2:<encoded actor sub>. The payload also repeats:

- schema version;
- actor subject;
- ordered prepared items.

Required behavior:

- do not compute or read a bucket before authentication is resolved;
- hydrate only when the actor is an allowed Owner;
- require the payload actor to exactly match the active subject;
- on mismatch or malformed data, ignore and remove only the active actor's invalid bucket;
- on logout, close Linking, cancel active resolution, and clear in-memory state, but preserve the actor's local bucket;
- on login as the same subject, rehydrate that subject's prepared workspace;
- on login as another subject, read only that subject's key and show none of the previous actor's state;
- if access is lost, fail closed and do not expose or mutate the retained bucket;
- storage denial/quota errors must not crash the app; expose one non-blocking persistence warning while keeping the in-memory workspace usable.

Because any same-origin script can read localStorage, this does not enforce confidentiality against a compromised client. Real per-user isolation and authorization remain server responsibilities.

### 13.4 Versioning and invalidation

Use a new V2 key/version and do not semantically migrate the V1 session envelope. V1 contains the retired mushaf-word meaning, scalar highlight assumptions, no checked-source contract, and different lifecycle semantics. Safe invalidation is clearer than pretending those meanings are equivalent.

Strict decoding should:

- reject unknown top-level versions;
- validate every descriptor and configuration;
- recompute sourceKey and reject a mismatch;
- sort/deduplicate manual verse and word coordinates;
- drop only invalid items when safe to retain independent valid items;
- reject duplicate source keys after the first valid item;
- bound item/count/string sizes;
- never trust persisted resultCount as source truth.

Codec validation is structural only. A decoded verseKey is not accepted for an operation until a successful source/page/study read proves actual Quran membership; final keys must also be members of the resolved selected universe.

### 13.5 Persisted and transient boundaries

Persist:

- stable source descriptor and sourceKey;
- per-source ayah inclusion overrides;
- automatic-word ON/OFF preference;
- manual wordLocations per verse;
- manual grouped/independent preference;
- ordered prepared-source position;
- optionally a clearly stale last-resolved count hint.

Do not persist:

- loaded Quran DTOs or Uthmani text;
- resolved ayah universes;
- merged ayahs or presentation occurrence slots;
- source-row checked membership;
- current search queries;
- modal/editor/step state;
- loading/progress/errors;
- selected Door;
- mock result;
- focus targets;
- entity-overlay state.

On hydration, rows are unchecked, data is unresolved, and counts are refreshed through the existing read path before a source participates in an operation.

### 13.6 Same-actor multi-tab policy

Actor-specific localStorage is shared by tabs. V2 deliberately provides no live storage-event merge or cross-tab locking: the last successfully persisted payload wins, and another open tab does not update until a later hydration. Same-tab writes remain serialized as described above. This is an explicit prototype limitation, not the future server concurrency contract.

## 14. Workspace Row UI / Responsive Behavior

### 14.1 Golden UI composition

Reuse shared/ui/result-list/result-list.directive.ts inside the existing qd-details-workspace. The result-list primitive gives flat sibling rows and a visual selected class, avoiding the current card-inside-card appearance.

Its selected input adds the visual .qd-is-selected class only; it does not set aria-selected. The native source-membership checkbox is the sole semantic selected-state control.

The list remains Arabic-first and RTL:

- quiet parchment surfaces;
- one green selection thread rather than elevation;
- no shadows, gradients, hover-only actions, or decorative chips;
- static kind/scope metadata uses text or a non-interactive badge, not qd-chip;
- long values use min-inline-size:0 and controlled wrapping;
- Quran/source numeric values use the project's direction isolation helpers.

### 14.2 Wide, Medium, and Compact

Use the established responsive bands:

| Band | Recommended presentation |
| --- | --- |
| Wide, 1080px and above | One visual CSS Grid row with the five named columns; source is flexible and actions remain compact |
| Medium, 768–1079px | Two-band row: source identity/scope across the first band; count, word control, remove, and checkbox in a clear second action band |
| Compact, up to 767px | One semantic list item stacked as identity, metadata/count, word behavior, then remove and source checkbox actions |

Do not create horizontal page/list scrolling and do not squeeze five tiny columns onto Compact.

At every band, especially Compact, separate the danger remove action from the membership checkbox with distinct action groups, spacing, and ordering cues; they must not become adjacent tap targets. The DOM order below is a reading-order inventory, not permission to visually cluster the final two controls.

The DOM reading order remains:

1. source identity;
2. ayah count/editor;
3. word behavior;
4. remove;
5. source selection.

CSS Grid may reposition these visually without changing the accessible order.

### 14.3 Control semantics

| Control | Element and accessible behavior |
| --- | --- |
| Ayah count | Native button; visible selected count, optionally selected/total; accessible name includes source and count; opens source editor |
| Automatic word behavior | Native checkbox with visible ON/OFF Arabic copy; label includes source; not a decorative custom slider |
| Manual word behavior | Native button labelled اختيار الكلمات (N), with source-qualified accessible name |
| Remove | Native tertiary/danger button; visible text or icon plus text; accessible name includes source |
| Source membership | Native checkbox; row label associates source identity; checked state is not conveyed by color alone |

Use at least the shared medium control target or a 44px hit target on Wide/Medium, and the existing 48px Compact convention. The current shared small action is only 32px outside Compact and is too small for repeated row controls.

Apply the same 44px Wide/Medium hit-target rule to the retained إضافة للربط / ربط مباشر source actions in Words surfaces, which currently request size=sm. Give their polite feedback a generated stable ID or remove the unused ID; do not derive DOM IDs from raw, possibly duplicate or whitespace-containing descriptor labels.

Repeated buttons must have unique accessible names or aria-describedby references to a stable row heading. تعديل, إزالة, and ربط alone are insufficient in a repeated list.

### 14.4 Selected and status states

A checked source row uses:

- native checked state;
- quiet green background tint;
- a 2px logical inline-start green thread;
- optional concise selected text for non-visual reinforcement;
- no shadow, lift, or full solid-green row.

The selected-source count and bulk-selection changes should be announced politely without repeatedly interrupting Quran reading. Step changes in the subsequent flow use aria-current=step and focus the new step heading after render.

### 14.5 Result volume

The current complete loader accumulates all rows, and the ayah selection/review templates render every visible card. Broad Roots or Word Type scopes can make that expensive.

The V2 baseline should keep complete result loading and client-local search but render a bounded client page of ayah cards. Page controls remain inside the sole body scroller, and selection continues to target the complete universe through LinkingSelection. This is lower-risk than introducing a virtual viewport into the compound modal before runtime evidence exists.

Client paging bounds DOM work only. The current complete loader still requests and accumulates every API page and repeatedly copies the aggregate array; that request/memory cost is an accepted V2 prototype risk because complete-universe selection is locked. Preserve visible raw progress, cancellation or stale-generation protection, retry, and full-failure states. Do not describe client paging as bounding network or in-memory volume.

If later evidence requires virtualization, the virtual viewport must replace the surface body as the single vertical owner for that step; it must not be nested inside an already scrolling body.

## 15. Modal Size and Single-Scroll-Owner Audit

### 15.1 Current code audit

| Surface/layer | Current vertical behavior | Evidence | Current verdict |
| --- | --- | --- | --- |
| Document background | Locked while dialog is open | shared/ui/modal-scroll-lock/scroll-lock.service.ts | Keep locked |
| qd-modal-shell frame | Clips the envelope | shared/ui/modal-shell/modal-shell.component.scss | Not a scroll owner |
| qd-modal-shell default body | overflow:auto normally | modal-shell.component.scss | Disabled for Linking by flushBody |
| Linking shell body | overflow:hidden through flushBody=true | linking-workspace-host.component.html | Correct compound-shell boundary |
| Workspace | .qd-details__body has overflow:auto | src/styles/_components.scss | Current sole workspace owner |
| Direct Link | .direct-link-workflow__body has overflow:auto | direct-link-workflow.component.scss | Current sole flow owner |
| Abwab picker | No component-local vertical overflow/max-height | Abwab picker styles | Expands into flow owner |
| Linking ayah selection | No component-local vertical overflow/max-height | linking-ayah-selection component | Expands into flow owner |
| Highlight/review ayah lists | No component-local vertical overflow/max-height | direct-link-workflow template/styles | Expand into flow owner |

No current Linking feature component adds a second vertical scrollbar under the flush shell. The source audit therefore does not reproduce a committed-code nested-scroll defect. V2 must preserve this topology while adding editors.

### 15.2 V2 owner rule by surface

| V2 modal surface | Final vertical owner | Required non-owners |
| --- | --- | --- |
| Workspace source list | .qd-details__body | modal body and qdResultList rows |
| Source ayah editor | one editor body in the same shell | search header, bulk bar, pager, and ayah list wrappers |
| Manual word editor | one editor body in the same shell | ayah chooser and active word area |
| Door/merge/review/mock flow | one common workflow body, evolved from .direct-link-workflow__body | picker, ayah cards, warnings, and review list |
| Remove-all confirmation | qd-confirm-dialog's short body; normally no vertical overflow | explicit sibling alertdialog exception; implementation must inert/aria-hide and suppress dismissal on the lower Linking shell |

For every surface:

- the shell header and close control remain outside the owner;
- any surface header/progress and footer remain fixed by the surface grid;
- search, list, empty/error, and pagination content flow naturally within the owner;
- no inner max-height or overflow:auto is added to an ayah list;
- horizontal scrolling is not introduced.

### 15.3 Geometry

Keep one qd-modal-shell and the shared wide variant. A coherent V2 target is:

- 80vw inline size on Wide/Medium;
- one stable 88dvh block size for Workspace, editors, and linking flow, rather than resizing between surfaces;
- the shared Compact near-full-height behavior, capped at 94dvh;
- readable ayah-card measure constrained inside content, not by shrinking the shell.

The custom inputs do not retain the shared Wide defaults: 80vw replaces the normal 52rem inline fallback (with only the shell's 100% clamp), and 88dvh replaces the normal min(92dvh, 44rem) block size/max. Compact media rules still replace these with the shared 94dvh cap. Moving the current Linking token from 80dvh to 88dvh therefore requires explicit contract/visual approval; this report recommends it for V2 capacity, not as a new modal variant or framework.

### 15.4 Focus and deferred content

Render the lightweight qd-modal-shell synchronously and defer only heavy inner surfaces. This removes the code-inferred first-open state where app.ts has made the page inert but @defer has not produced a dialog focus target.

The eager shell may initially focus its Close control before the deferred body is ready. When the first Workspace/flow body reports ready, perform one explicit post-render focus handoff to that surface's heading or primary entry control; do not leave initial focus on Close by accident.

The shell remains continuously open during Workspace/editor/flow transitions. Because its normal initial-focus and return-focus effects do not rerun when an inner @if subtree is destroyed, the host/coordinator must explicitly:

- capture the invoking row/action;
- move focus after render to the new surface heading or first relevant control;
- announce the surface/step change;
- restore focus on back when the invoker still exists;
- use the explicit source-origin fallback chain for Direct Link, or the workspace heading/action for workspace/editor transitions, when it does not.

Outer close means exit Linking. Inner previous/next means step navigation. Avoid multiple controls all labelled رجوع for different outcomes.

## 16. Existing Source Integration Impact

| Source family | Current creation seam | Current identity/match quality | V2 impact |
| --- | --- | --- | --- |
| Unique without tashkeel | word-drilldown-modal and unique detail-overlay adapter | mode=simple and wordId distinguish identity; canonical word IDs retained | Keep; display بدون تشكيل; automatic words default ON |
| Unique with tashkeel | same seams | mode=tashkeel and wordId distinguish identity; canonical word IDs retained | Keep; display بالتشكيل; automatic words default ON |
| Root | roots explorer page and root overlay adapter | rootId stable; complete resolver supplies truthful match flags, not neutral canonical IDs | Keep; display جذر; do not fabricate IDs |
| Lemma | lemmas explorer page and lemma overlay adapter | lemmaId plus nullable typeCode; truthful flags, no neutral canonical IDs | Keep; use الصيغة المعجمية and show relevant typeCode scope |
| Stem / sarfi | stems explorer page and stem overlay adapter | stemId plus nullable typeCode; truthful flags, no neutral canonical IDs | Keep; use الأصل الصرفي and show relevant typeCode scope |
| Word Type word | word-types detail-panel view-model/page; word-kind overlay seam | tashkeelWordId/context/scope stable; canonical word IDs retained | Keep; display main/child type and active case/tense/voice scope concisely |
| Word Type grouped root/stem/lemma | word-types detail-panel view-model/page | selected member ID plus full Word Type scope | Keep; formatter must expose selection kind/scope so distinct rows do not look identical |
| Mushaf selected word | selected-word-section descriptor/actions and Mushaf resolver | analyzed quranWordId for one occurrence | Remove from Linking only |
| Manual Mushaf ayah(s) | Not implemented | page payload has verseKey/wordLocation but no quranWordId; single-occurrence analysis exists but is intentionally not a batch strategy | Add as distinct manual source with grouped-default and manual word configuration |

Important exact creation paths include:

- features/words/components/word-drilldown-modal/word-drilldown-modal.component.ts and .html;
- features/words/pages/roots-explorer-page/roots-explorer-page.component.ts and .html;
- features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.ts;
- features/words/pages/stems-explorer-page/stems-explorer-page.component.ts;
- features/words/pages/word-types-explorer-page/word-types-detail-panel.view-model.ts and word-types-explorer-page.component.html;
- features/words/entity-detail-overlay/adapters/*-detail-overlay-adapter.component.ts and .html;
- features/words/components/details-panel-shell/details-panel-shell.component.html, which owns the shared qdDetailsActions projection.

These Words seams should continue producing typed descriptors and rendering QuranSourceLinkingActionsComponent. They do not need to know about workspace rows, merge state, persistence, or Doors.

## 17. State and Contract Changes

The following are the minimal conceptual frontend contract changes.

### 17.1 Source contract

| Current | V2 |
| --- | --- |
| LinkingSourceKind includes mushaf-word | Remove mushaf-word; add manual-mushaf-ayahs |
| Descriptor has one analyzed Mushaf word | Manual descriptor has unique Quran-ordered verse references and page read context |
| label is the only human-readable value snapshot for most automatic descriptors | Keep a validated label snapshot and combine it with typed canonical family/mode/scope; never use it for identity |
| Source key includes quranWordId/location for Mushaf | Manual source key includes only sorted unique verseKeys |

### 17.2 Workspace item contract

| Current | V2 |
| --- | --- |
| descriptor, one LinkingSelection, resultCount, highlight boolean | descriptor, ayah inclusion, source-kind-specific word behavior, manual multi-ayah preference, optional count hint |
| scalar activeSourceKey | transient checkedSourceKeys plus explicit editor target |
| sessionStorage state | repository-hydrated actor workspace |
| addOrFocus masquerades as edit | explicit openAyahEditor / openManualWordEditor surface intents |

checkedSourceKeys should preserve workspace order when materialized, reconcile on remove/hydration, and never contain a key not present in prepared items.

The store also needs a local per-item configuration revision for conditional reconciliation write-back. It is concurrency bookkeeping, not Quran/source identity; it may restart on hydration for the frontend prototype.

### 17.3 Neutral resolution and merge contract

| Current | V2 |
| --- | --- |
| LinkingAyahWord has scalar isSourceMatch | Resolver result or adapter retains per-source match contribution for the operation |
| renderPosition is the only neutral slot | Transient validated verse occurrence slot; optional canonical ID and manual location |
| one sourceLoad | keyed per-source load/progress/error plus atomic merged state |
| one source selection | member-specific inclusion snapshots applied before union |

Source resolvers should remain responsible only for translating one source read. They must not know which other sources are selected or mutate workspace state.

### 17.4 Workflow contract

Replace scalar workflow assumptions with:

- an ordered one-or-many operation-member snapshot;
- keyed source resolution states;
- one derived merged selection;
- one selected live Door;
- shared review/mock result;
- visible error channels for resolution, Door loading/validity, identity conflict, and mock failure.

The current global highlight step becomes unnecessary because word behavior is already configured per member. The workflow may still show a read-only highlight summary or allow returning to the relevant source editor.

### 17.5 Command/mock contract

The frontend mock command should conceptually carry:

- ordered source member snapshots or stable source/config summaries;
- explicit selected live Door ID;
- deduplicated final verseKeys;
- per-source contribution/highlight configuration;
- manual wordLocation selections clearly marked non-canonical;
- explicit manual effective link shape;
- enough merge provenance to validate the review.

Actor identity/access is adapter-owned live context, not caller-supplied trusted command data. The mock continues injecting LinkingAccessService and rechecking it at execute time; a future HTTP adapter relies on authenticated transport/server context.

The mock validates:

- Owner access at execution time;
- every source resolved completely;
- final merged ayahs are non-empty and unique;
- verse keys are from the resolved selected universes;
- the Door remains visible/live/selectable;
- manual grouped intent is explicit;
- no presentation slot is claimed as canonical.

It then returns a presentation result only. The port should use an asynchronous result shape so a future adapter can replace it without another component contract rewrite.

### 17.6 Access and state reset

Every mutating method—add, remove, clear, check, edit inclusion, edit manual words, open Direct Link, select Door, and mock confirm—must repeat the fail-closed Owner check. Template visibility alone is insufficient.

Closing the modal cancels transient workflow/editor work but does not clear the prepared workspace. Logout clears memory and active work but not the same actor's local bucket.

## 18. Components to Reuse

### Shared UI

- shared/ui/modal-shell/: dialog semantics, focus trap, close, backdrop/Escape, Compact sizing, and focus return.
- shared/ui/confirm-dialog/: remove-all confirmation with cancel-first focus.
- shared/ui/result-list/result-list.directive.ts: dense sibling source rows and selected treatment.
- the shared qd-details-workspace / .qd-details__body composition: workspace frame and sole scroll owner.
- existing error, empty, action, direction-isolation, and scroll-stability primitives documented by the shared/Golden UI catalog.

### Abwab

- the existing Abwab hierarchy/Door picker in single-selection mode;
- AbwabSnapshotFacade and its live tree;
- existing Door label/tree rendering.

The caller must correct select-only behavior and validate the exact selectable live set.

### Linking

- LinkingAccessService;
- QuranSourceLinkingActionsComponent for Words sources;
- LinkingSelection and its all-except/only helpers;
- CompletePagedSourceLoader;
- LinkingSourceResolver abstraction and automatic family resolvers;
- stable automatic source-key rules;
- LinkingAyahCardComponent's exact Quran display treatment;
- LinkingAyahSelectionComponent's complete-universe selection, local search, select-all, and clear-all concepts;
- global host and one-primary-shell composition;
- injected command boundary, after widening it to the new operation contract.

### Words and Mushaf reads

- all existing Words descriptor creation seams and read APIs;
- current Mushaf page reads and stable verseKey/wordLocation fields, where they can prove a complete ayah;
- the existing Mushaf word/line/page render chain and click event propagation;
- current normal Mushaf analysis/study state and components.

Reuse means preserving the responsibility, not freezing every template or type signature. Accessibility, set-awareness, and contributor metadata changes are still required where identified.

## 19. Components to Modify or Remove

### 19.1 Remove the Mushaf word Linking seam

Remove only:

- features/mushaf/components/selected-word-section/selected-word-section.component.ts:
  - Linking imports;
  - QuranSourceLinkingActionsComponent registration;
  - linkingSource computed descriptor.
- selected-word-section.component.html:
  - the Linking action block containing إضافة للربط / ربط مباشر.
- selected-word-section.component.scss:
  - the linking-actions wrapper style.
- features/linking/models/linking-source.models.ts:
  - mushaf-word kind, descriptor member, and validator branch.
- features/linking/models/linking.labels.ts:
  - Mushaf word source label.
- features/linking/utils/linking-source-key.ts:
  - Mushaf word key branch.
- the entire data-access/resolvers/mushaf-word-linking-source.resolver.ts.
- Mushaf resolver import, injection, and registration in data-access/linking-source-resolver.registry.ts.

Preserve selected-word analysis/study markup and all non-Linking styles. Preserve the Golden UI mushaf-word selector because it refers to the protected renderer, not the retired Linking source kind.

### 19.2 Modify Linking contracts/state/data access

- models/linking-source.models.ts: add the manual ayah descriptor and strict guards.
- models/linking-workspace.models.ts: source-kind-specific configuration and operation membership boundary.
- models/linking-ayah.models.ts: contributor-aware merge representation.
- models/linking-workflow.models.ts: selected member set, per-source status, merged state, and explicit mock result/errors.
- models/linking.labels.ts: grouped/manual/row/flow copy and canonical Words terminology.
- utils/linking-source-key.ts: manual set identity.
- state/linking-workspace.store.ts: checked keys, real editor routing, repository hydration, safe actor lifecycle, remove undo.
- state/linking-workspace-session.ts: replace/split into codec plus repository adapter; retire direct sessionStorage ownership.
- state/linking-workflow.facade.ts: reduce to orchestration over extracted selected-source coordinator and pure merger.
- data-access/linking-command.port.ts and mock-linking-command.port.ts: set-aware async-ready mock contract and exact live-Door validation.
- data-access/linking-source-resolver.registry.ts: remove Mushaf word registration; retain automatic registry.

Add focused feature-owned merge models/utility and a source-set resolution coordinator rather than enlarging the existing facade.

### 19.3 Modify Linking UI

- components/linking-workspace-host/: eagerly mount the shell, route explicit surfaces, own transition focus, and keep one geometry.
- styles/_tokens.scss: if this report's geometry recommendation is accepted, change --qd-linking-workspace-modal-block-size from the current 80dvh to 88dvh.
- components/linking-workspace/: dense list, top actions, empty/status/undo behavior.
- components/linking-workspace-item/: reshape into a result-list row or replace it with a purpose-named row component.
- components/linking-ayah-selection/: valid list semantics, explicit focus/announcements, bounded client rendering, and editor reuse.
- components/linking-ayah-card/: merged contributor/highlight input and accessible match summary without changing Quran text.
- components/direct-link-workflow/: accept one-or-many operation draft, remove scalar highlight step, share merged review, show errors where they occur, and clarify navigation.
- components/linking-door-step/: select-only radio behavior and exact live selection.
- components/quran-source-linking-actions/: preserve Words behavior, add 44px Wide/Medium hit targets, use stable feedback identity, capture the mandatory connected/retained-overlay focus origin, and hand one-source Direct Link to the common pipeline.

### 19.4 Add the Mushaf ayah-selection seam

Modify, with neutral selection-state inputs only below the page boundary:

- features/mushaf/pages/mushaf-reader-page/: Owner gate, selection-mode dispatch, draft lifecycle, and ArrowLeft/Right gating.
- features/mushaf/components/mushaf-page-area/: projected header action and neutral selected-verse state propagation.
- features/mushaf/components/mushaf-header-navigation/: action slot, aria-pressed context, accessible header label, responsive geometry.
- features/mushaf/components/mushaf-page-view/ and mushaf-line/: forward neutral selection state.
- features/mushaf/components/mushaf-word/: apply selected-verse state to the existing button/class/ARIA only where required.

Do not change Quran glyph text, font, word order, line grouping, marker rules, spacing metrics, normal click emission order, or study behavior.

In selection mode, the neutral word-button contract must provide an action-specific select/deselect-ayah accessible name or description containing verseKey and accurate state. Repeating aria-pressed on buttons still named only by Quran word text is not sufficient.

Add a thin Linking-owned Mushaf selection coordinator/store and workspace manual-source/editor UI. Keep Linking knowledge out of the glyph and line components.

## 20. Frontend-Only V2 Scope

The next implementation may build only:

- retirement of selected Mushaf word Linking UI/model/resolver paths;
- Owner-only Mushaf تحديد mode for one/many ayahs;
- creation and formatting of a manual Mushaf ayah workspace source;
- grouped-by-default manual multi-ayah preference with explicit independent alternative;
- complete-ayah manual word editor using temporary wordLocation coordinates;
- dense responsive workspace source list;
- source-row operation selection;
- real per-source ayah editor with complete load, local search, select all, clear all, and retained hidden selections;
- automatic-source word ON/OFF and manual-source word selection;
- pure selected-source ayah/word merge and contributor-aware review;
- shared one-source Direct Link and multi-source workspace pipeline;
- one live Door selection and frontend mock confirmation;
- actor-bound versioned localStorage repository adapter;
- one-primary-shell, remove-all alertdialog, one-scroll-owner, focus, responsive, accessible, and bounded-rendering corrections;

No automated tests are to be added, removed, or modified. Later implementation verification is limited to:

- code review;
- typecheck;
- production build;
- existing Golden UI/static guards;
- targeted Chrome/manual verification only after implementation.

No browser/manual verification belongs to this report activity.

## 21. Explicitly Out of Scope

The following remain out of scope for V2:

- backend Linking implementation;
- database tables or workspace/link entities;
- migrations;
- EF entities/configurations;
- backend repositories;
- write endpoints or detailed API route design;
- real link/group persistence;
- server workspace synchronization;
- cross-device behavior beyond documenting the future requirement;
- server-issued group identity;
- approval or request workflows;
- audit/event persistence;
- transaction boundaries;
- Admin Linking permission or fake Admin mode;
- changes to existing Quran source data;
- fabricated ayah or QuranWord IDs;
- changes to Mushaf Quran glyph text/DOM, fonts, word/marker order, line grouping, spacing, or metrics; neutral selection inputs/button class/ARIA are explicitly in scope;
- new global Quran search;
- link history;
- offline synchronization/conflict resolution;
- deployment;
- commits, pushes, or PR work;
- automated test changes.

Existing read APIs may be reused. A missing canonical identity may be surfaced or handled presentation-only under the explicit rules above; it does not authorize a backend change in V2.

## 22. Future Backend Contract Implications

The approved UX will later require backend contracts capable of representing:

- an authenticated actor's ordered persistent workspace;
- stable typed source identities and complete source scopes;
- per-source included/excluded ayah state;
- automatic source-word match preference;
- manual ayah sources and their explicit grouped/independent intent;
- canonical QuranWord selections resolved from manual word locations;
- one operation containing one or several selected sources;
- canonical verse and word deduplication;
- one validated live Door target;
- server-side authorization independent of frontend visibility;
- server-issued durable link/group identity;
- concurrency/version behavior for a workspace used on multiple devices;
- clear validation/error reporting when a source or Door becomes invalid.

Manual-location resolution must validate that every location is non-marker, belongs to its declared verseKey, remains inside an included ayah, and retains source/ayah provenance. It must not return an unscoped flat ID set that loses membership context.

The backend report will need to decide how a grouped manual source interacts with automatic-source contributions in one mixed operation and how canonical manual word resolution is exposed. The frontend should preserve the intent and provenance now without pre-empting that design.

This report intentionally does not specify tables, columns, migrations, entity relationships, repositories, endpoints, transactions, approvals, requests, or audit implementation.

## 23. Risks / Gaps / Open Decisions

### 23.1 Risks with a bounded V2 policy

| Risk/gap | Current evidence | V2 containment |
| --- | --- | --- |
| Universal canonical word identity is absent | Root/Lemma/Stem expose flags; Mushaf page exposes wordLocation | Canonical IDs where present; validated presentation slots elsewhere; never persist/send slots as canonical |
| Current verse-key guard accepts impossible per-surah ayahs | isVerseKey checks only 1–114 and 1–286 | require successful source/study membership and numeric Quran-order parsing |
| A selected ayah can span pages | AyahCoreDto study read has pageFrom/pageTo/wordsCount; page payload does not | Require authoritative metadata, complete ordered page aggregation, and count/contiguity proof |
| Large source results have DOM and all-page memory/request cost | Complete loader fetches/accumulates every page; templates render every visible card | Client paging bounds DOM only; accept all-page prototype cost with progress, cancellation/stale protection, retry, and failure state |
| Partial multi-source success could be mistaken for full success | Current workflow has one source only | Atomic merged publish; any selected-source failure blocks confirmation |
| Archived Door can pass current check | picker uses liveRoots; validation uses byId | Validate exact live/selectable picker set at selection and confirmation |
| Current shell deferral may create focus vacuum | app becomes inert before deferred shell exists | Mount shell eagerly; defer inner body only |
| Surface swaps can lose focus | shell stays open while inner subtree is destroyed | Explicit capture/focus/restore protocol |
| Mushaf header disappears during page loading/error | PageArea mounts header only for a successful page | Keep persistent reader-page selection status/actions and an explicit focus fallback |
| Mushaf has no ayah-level DOM wrapper | Lines are flat words; ayahs and lines/pages cross each other | Do not add an ayah thread/wrapper; mark only mounted matching word buttons |
| Ayah-wide wash conflicts with existing selected-word truth | Mushaf styling deliberately keeps selected word as the persistent wash | Reuse a no-metric accent state, keep selected-word ring precedence, and verify the state contract before implementation completion |
| localStorage can be stale/tampered | client-owned browser storage | strict actor/version codec; re-resolve source truth; treat adapter as UX only |
| Same actor opens two tabs | both tabs share one localStorage key | explicit last-writer-wins prototype policy; no live cross-tab merge |
| Manual and automatic Quran sequences can disagree | nullable metadata/identity varies by resolver | enrich compatible data, fail visibly on contradiction |
| Current morphology labels can misidentify rows | Linking lemma/stem labels conflict with Words labels | reuse canonical Words terminology and typed formatter |

### 23.2 Non-blocking product/architecture decisions made by this report

Unless the product owner chooses otherwise before planning:

- source-row checked membership is transient and resets on hydration;
- a single row removes immediately with in-shell undo;
- إزالة الجميع always confirms with qd-confirm-dialog;
- client paging is the initial high-volume rendering strategy, not virtualization;
- one stable 80vw by 88dvh Wide/Medium shell is used across Linking surfaces;
- Direct Link does not implicitly persist its source;
- a manual source's grouped/independent preference persists even when ayah inclusion temporarily reduces its effective shape to single.

### 23.3 Genuinely deferred decisions

These do not block the frontend V2 plan:

1. **Mixed-operation durable grouping.** The frontend can retain manual group intent and show one merged review, but the later backend design must decide how that group partitions durable links when automatic sources contribute additional or overlapping ayahs.
2. **Canonical manual word resolution contract.** The later backend/read design must choose between returning quranWordId in complete ayah/page reads and batch-resolving wordLocations.
3. **Cross-device conflict semantics.** The product requires a server workspace later, but merge/overwrite/version behavior belongs to that backend report.

Post-implementation browser verification must verify and fix any deviation in DOM/focus order, focus restoration, the accepted V2 geometry, and selected-word precedence. Row density and responsive header layout may be tuned within the locked rendering/target/scroll constraints. No browser was opened and no runtime measurement was taken for this report.

## 24. Inputs for the V2 Frontend Implementation Plan

The later plan should use the following as bounded capability inputs, not as task IDs, estimates, or an ordered plan:

### Contract and identity boundary

- retire mushaf-word and introduce manual-mushaf-ayahs;
- define typed source presentation;
- define source-kind-specific word behavior;
- define operation member and pure merged-selection contracts;
- document canonical versus presentation-only identities;
- replace coarse verse validation/order with source-backed membership and numeric Quran ordering.

### Persistence boundary

- split codec and repository port;
- implement actor-bound V2 localStorage adapter;
- define login/logout/access-loss hydration behavior;
- invalidate V1 safely;
- keep loaded Quran/operation state transient;
- lock the explicit last-writer-wins/no-live-multi-tab prototype policy.

### Dense workspace boundary

- replace cards with qdResultList rows;
- add checked-source membership and top actions;
- wire real ayah/manual-word editor navigation;
- implement removal undo/focus and the remove-all nested-alertdialog inert/dismissal exception;
- implement Wide/Medium/Compact behavior and accessible control names.

### Existing source-resolution boundary

- preserve automatic Words entry seams and strict paged loader;
- remove only the Mushaf word resolver;
- retain truthful automatic match data;
- add per-source resolution status and atomic selected-set coordination;
- conditionally write reconciled state back only against an unchanged item revision;
- bound visible ayah DOM rendering without claiming to bound all-page request/memory cost.

### Pure merge boundary

- apply each member's reconciled ayah inclusion;
- union by verseKey in Quran order;
- enrich compatible Quran metadata and reject contradictions;
- union canonical/manual/presentation-only word contributions with provenance;
- cover all zero-source, zero-contribution, overlap, OFF-highlight, failure, and retry states in production behavior.

### Shared linking-flow boundary

- feed one-source Direct Link and workspace selected-set entry into one pipeline;
- remove the scalar global highlight step;
- reuse one live Door picker;
- correct radio and archived-Door validation;
- show merged review, manual link intent, visible command errors, and presentation-only mock success;
- preserve inline/retained-overlay entry state and deterministic initial/return focus.

### Mushaf ayah-selection boundary

- remove selected-word Linking actions without touching study;
- add projected Owner-only تحديد in the reader header;
- keep an orthogonal ayah-selection draft;
- intercept ayah/word event dispatch only while the mode is active;
- keep persistent active-mode controls outside PageArea loading/error replacement;
- propagate neutral selected-verse button state/ARIA while preserving glyph text/DOM, fonts, ordering, spacing, and metrics;
- add the selected verse set to the workspace with grouped default;
- use getAyahStudy metadata plus lazy, cached, deduplicated page reads to prove word count/order completeness, or block incomplete ayahs.

### Modal, focus, and accessibility boundary

- eagerly mount the shell;
- keep one stable geometry;
- enforce one explicit scroll owner per surface;
- own initial deferred-body focus plus surface/step/origin transitions;
- correct semantic list/control relationships, target sizes, aria-current, live status, and source-qualified repeated actions.

### Verification boundary

- make no automated test changes;
- use only the permitted later code review, typecheck, production build, existing guards, and post-implementation manual verification.

The implementation plan should preserve these boundaries and stop before backend contracts, schema, persistence APIs, real writes, permissions beyond Owner, automated test work, or Quran glyph/text/metric changes beyond the explicit neutral selection-state seam.
