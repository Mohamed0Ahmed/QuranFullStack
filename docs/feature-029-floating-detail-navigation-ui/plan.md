# Feature 029 — Floating detail navigation and frontend UI alignment

**Status:** read-only inspection and implementation plan; no implementation has been performed

**Planning date:** 2026-07-16

**Planning branch:** `plan/frontend-navigation-modal-ui`, cut from `dev` at `6f457a0`

**Inspected UI snapshot:** `restyle/flat-green-light` at `d7e6421`

**Implementation owner:** separate Fable workflow

**Backend/data scope:** none

## 1. Baseline and evidence boundary

The requested branch had to start at `dev`, while the flat parchment/scholarly-green restyle is still one commit ahead on `restyle/flat-green-light`. This report therefore lives on the requested `dev`-based branch but inspects the intended UI at `d7e6421`. Before implementation, Fable must first work from a branch that contains that restyle commit, whether it has been merged into `dev` or explicitly incorporated into the implementation branch. Otherwise the color tokens, design-preview files, and several cited SCSS lines will not match this report.

Relative file/line citations below refer to the inspected `d7e6421` snapshot unless explicitly noted. The restyle changes frontend SCSS and design documentation, not the relevant Angular TypeScript architecture, so routing, state, API, and cache findings are also valid at `dev`.

The inspection read the root and frontend `AGENTS.md`, `CODING_PRINCIPLES.md`, `PRODUCT.md`, `DESIGN.md`, `docs/design-preview/README.md`, the frontend architecture documents, the nearest frontend READMEs, and Feature 026's active specification/decision artifacts. The requested `frontend-design` skill is not installed in this workspace; the inspection used the closest available product-interface design workflow plus the repository's own authoritative design sources.

No tests were run because this was a static, read-only planning pass. The existing user-owned untracked file `docs/design-preview/decisions.html` was not changed and is not part of this plan's output.

## 2. Outcome summary

| Change | Current cause | Planned outcome | Main dependency/risk |
|---|---|---|---|
| A — ayah-card unification | Words, Similar Ayahs, and Mutashabihat use three different card treatments | One presentation-only shared ayah-card frame; each feature retains its existing Quran renderer and navigation behavior | Avoid moving Quran text normalization/highlighting into the shared primitive |
| B — floating-modal navigation | Details are owned by route-bound page facades and cross-links open new explorer tabs | One persistent, URL-driven overlay host with typed entity stack, browser-history parity, restore state, and route-independent detail controllers | Full cache identity, Word Type identity derivation, mobile nested-dialog behavior, and history provenance |
| U1 — loading shift | Responsive embedded rules reset the selected-word card's minimum size while the skeleton has fixed, smaller geometry | Reserve the last natural loaded block size, with a first-load responsive baseline and structurally matched skeleton | Real geometry cannot be proven by jsdom alone |
| U2 — count-range position | An expanding `<details>` is a wrapping flex sibling of the sort control | The shared filter host becomes a full-width second row on all four explorers | Keep it in normal flow and use logical sizing |
| U3 — Word Types tabs | Tabs are above the entire split layout, not the table column | Tabs become the first item in the table column, immediately above the table | Preserve mounted-shell and RTL keyboard order invariants |

## 3. Locked invariants and non-goals

1. Primary row selection on Roots, Lemmas, Stems, Unique Words, and Word Types keeps the current inline desktop side panel/current responsive drawer behavior. The global overlay is only for Mushaf entity links and links/actions launched from inside a detail.
2. Quran text rendering is unchanged. No normalization, token splitting, marker removal, font selection, text substitution, or matched-word calculation moves into a new shared card.
3. The global overlay never writes the existing explorer selection query keys and never reuses a route-bound singleton detail state instance.
4. No API, generated DTO, backend, import, database, or cache-key format changes are required.
5. URL links remain real, copyable anchors. Unmodified primary clicks open in-app; modifier clicks, middle-click, context-menu, and copied hrefs retain ordinary browser behavior.
6. All new UI is Arabic-first/RTL, keyboard operable, WCAG 2.1 AA, light/dark compatible, and reduced-motion safe.
7. The flat visual language remains quiet: parchment/surface tokens, hairline borders, and no card shadows. Only the floating dialog uses the existing floating shadow.
8. Table links that are outside a detail surface are not part of Change B and retain their current page-navigation behavior.

## 4. Current architecture evidence

### 4.1 Routes and persistent composition point

- `src/app/app.routes.ts:19-46` defines the lazy `/dashboard/mushaf` and `/dashboard/words` routes.
- `src/app/features/words/words.routes.ts:41-82` defines the Words hub, `unique/:mode`, Roots, Lemmas, Stems, and Word Types routes.
- `src/app/core/navigation/route-paths.ts:11-43` is the canonical path-builder boundary.
- `src/app/features/mushaf/mushaf.routes.ts:10-15` has one reader route.
- `src/app/app.ts:1-10` currently renders only `<qd-app-shell />`.
- The routed `<router-outlet>` is inside `src/app/core/layout/app-shell/app-shell.component.html:1-10`.

Therefore a modal mounted in any routed Words page would be destroyed during Words → Mushaf navigation. The persistent host must be a sibling of `qd-app-shell` at the application composition root. This also lets the shell become `inert` while the sibling dialog remains interactive without creating a core-to-feature import.

### 4.2 Existing primary-detail behavior

The side-panel behavior that must remain is already consistent:

- Roots binds the list and detail facades to the page route and renders the same panel inline on desktop or as a responsive dialog/drawer (`roots-explorer-page.component.ts:104-112`; `.html:75-131`).
- Lemmas and Stems follow the same route-bound facade plus inline/responsive panel pattern.
- Word Types binds both facades at `word-types-explorer-page.component.ts:215-237` and renders `qd-word-type-details-panel` at `.html:107-187`.
- Unique Words uses `WordDrilldownModalComponent` inline at desktop and dialog-style at smaller widths.

The existing `RootDetailsPanelComponent`, `LemmaDetailsPanelComponent`, `StemDetailsPanelComponent`, `WordTypeDetailsPanelComponent`, and `WordDrilldownModalComponent` are useful presentation surfaces. Their current route ownership is not reusable as global-overlay ownership.

### 4.3 Existing page URL state and cache identity

Page-owned selection state uses generic query keys, which is why the global overlay needs a separate namespace:

| Detail | Existing page selection keys | Identity/state that a modal frame must retain | Cache evidence |
|---|---|---|---|
| Root | `root`, `view`, `column`, `wordView`, `surahView`, `detailPage` (`roots.models.ts:96-126`) | root ID, view, word view, surah view, detail page | `RootsCacheKeys` at `roots-cache.ts:6-40` |
| Lemma | `lemma`, view/sub-view/page plus ayah `typeCode` | lemma ID, view, word view, surah view, detail page, type code | `LemmasCacheKeys.ayahs` includes ID, page, page size, and type code (`lemmas-cache.ts:19-42`) |
| Stem | `stem`, view/sub-view/page plus ayah `typeCode` | stem ID, view, word view, surah view, detail page, type code | `StemsCacheKeys.ayahs` has the same full identity (`stems-cache.ts:19-42`) |
| Unique word | route `mode`, then `word`, `view`, `ap` | mode, word ID, view, ayah page | `UniqueWordsCacheKeys` includes mode + ID and page (`unique-words-cache.ts:6-42`) |
| Word Type | full list scope plus `word`/dimension ID, `contextCode`, `detail*`, `view`, `detailPage`, `location`, `column` (`word-types.models.ts:159-203`) | tashkeel ID, context code, case, tense, voice, view, detail page; grouped selections also require dimension kind/ID and scope | `WordTypesCacheKeys` and `WordTypesCacheIdentity` at `word-types-cache.ts:13-48,81-94` |

Current URL parsers fail closed and current explorer updates use merged query params. `UniqueWordsDrilldownFacade` correctly treats `(mode, wordId, view, ayahPage)` as one identity (`unique-words-drilldown.facade.ts:162-185,282-292`). The modal codec must preserve the same principle: never compare or cache by numeric ID alone.

The shared `ApiResponseCache` already supplies successful-response reuse and in-flight de-duplication. The overlay should reuse the root-scoped feature API/cache services and their existing keys, not introduce a second cache.

### 4.4 Existing Mushaf URL hydration

- Mushaf owns `page`, `ayah`, `focusAyah`, `word`, `segment`, `panel`, the study tabs, and source keys (`mushaf.models.ts:223-235`).
- `parseMushafUrlParams` derives the selected ayah from `word` when necessary and strictly normalizes page/panel/tab state (`mushaf-url-sync.ts:68-88`).
- `buildMushafDeepLink` already expresses the required ayah destination (`mushaf-url-sync.ts:125-147`).
- `applyAuthoritativeUrlSnapshot` reloads only when selection/source identity changes (`mushaf-url-hydration.ts:46-81`).
- Mushaf's in-page URL patches merge unknown query keys (`mushaf-reader.facade.ts:497-511`), so a namespaced overlay stack can survive reader interaction.

One compatibility defect must be fixed with B: `mushaf-reader-session.ts:22-24` currently treats a URL as “bare” only when it has zero query keys. An overlay-only query would suppress Mushaf session restoration. Change bare detection to ignore overlay-owned keys and make the corresponding session-restore navigation merge query params, so restoring the base page does not remove the overlay.

### 4.5 Existing dialog and accessibility primitives

- `.qd-modal-backdrop` and `.qd-modal` already define the fixed overlay, surface, hairline border, large radius, and floating shadow (`src/styles/_components.scss:500-521`).
- Explorer-dialog sizing exists at `_components.scss:398-476`.
- `@angular/cdk` is already installed and current detail drawers use `cdkTrapFocus`, `cdkTrapFocusAutoCapture`, `role="dialog"`, `aria-modal="true"`, Escape handlers, and `ModalScrollLockDirective` in varying combinations.
- `ModalScrollLockDirective` is instance-local rather than reference-counted. A responsive explorer drawer underneath the global overlay can otherwise unlock body scroll when one of the two layers is destroyed.

Reuse the visual and CDK foundations, but consolidate the global shell's semantics and introduce reference-counted scroll locking rather than nesting the existing stateful dialogs.

## 5. Change B — floating-modal entity navigation

### 5.1 Exact target behavior

1. A primary explorer statistic/row selection continues to open only its existing page side panel/drawer.
2. A root, lemma, stem, type, or unique-word identity clicked in the Mushaf opens a global floating detail card over the reader.
3. A cross-entity link inside any detail opens the destination in the global overlay. From an explorer side panel this starts a new overlay stack without changing the panel's selection; from an existing overlay it appends to that stack.
4. Each entity append creates a browser-history entry. Browser Back/Forward and the dialog Back action restore the same frame sequence.
5. Internal tab/sub-view/pagination changes update the top frame in the current history entry; they do not masquerade as new entity pushes.
6. Ayah navigation updates/navigates the base Mushaf route while retaining the visible overlay stack.
7. Close, Escape, and backdrop dismissal keep the stack in the URL in a closed state. A fixed restore control appears at the physical top-left; restoring reopens the exact stack.
8. A fresh deep link hydrates the overlay and its stack over a valid current/canonical base page.

### 5.2 Current link inventory and migration boundary

| Source inside a detail | Current action | B action |
|---|---|---|
| Root → word | Unique Words explorer in a forced new tab (`root-words-list.component.ts:47-57`; HTML `:27-39`) | Push `unique` frame |
| Root → lemma | Lemma explorer in a forced new tab (`root-lemmas-list.component.ts:39-44`) | Push `lemma` frame |
| Root → stem | Stem explorer in a forced new tab (`root-stems-list.component.ts:39-44`) | Push `stem` frame |
| Lemma → word | Unique Words explorer (`lemma-words-list.component.ts:46-56`) | Push `unique` frame |
| Lemma → stem | Stem explorer (`lemma-stems-list.component.ts:38-45`) | Push `stem` frame |
| Stem → word | Unique Words explorer (`stem-words-list.component.ts:46-56`) | Push `unique` frame |
| Stem → lemma | Lemma explorer (`stem-lemmas-list.component.ts:38-45`) | Push `lemma` frame |
| Any explorer detail → ayah | Forced-new-tab Mushaf deep link (`ayah-matches-list.component.ts:41-52`; HTML `:42-51`) | Navigate the Mushaf; retain/promote overlay context as described below |
| Unique word detail → ayah | Same shared ayah list (`word-drilldown-modal.component.html:68-75`) | Navigate the Mushaf with the unique frame retained |
| Word Type grouped members | Display-only today (`word-type-grouped-words-list.component.html:36-48`) | Remains display-only; no invented row behavior |

Mushaf morphology currently builds page deep links for root/lemma/stem (`selected-word-section.component.ts:39-67`) and unique identities (`:69-97`); their anchors force new tabs (`selected-word-section.component.html:84-123`). “Type” is plain text (`word-morphology-summary.component.html:1-5`). All five become modal-aware real anchors.

Table cross-links outside detail surfaces—such as Unique → root and Lemma/Stem table dimension links—remain page links. This preserves the user's explicit scope boundary.

### 5.3 Ownership and component structure

Use a small cross-cutting navigation layer plus a Words-owned renderer:

```text
App composition root
├── qd-app-shell                         routed base page; inert only while overlay is open
└── qd-entity-detail-modal-host          persistent across route changes
    ├── URL/history coordinator          authoritative stack and visibility
    ├── accessible modal shell           focus, Escape, Back, Close, restore button
    └── lazy entity adapter
        ├── root detail controller + existing panel content
        ├── lemma detail controller + existing panel content
        ├── stem detail controller + existing panel content
        ├── unique-word controller + existing drilldown content
        └── Word Type controller + existing detail content
```

Proposed file ownership:

- `src/app/core/navigation/detail-overlay/`
  - `detail-overlay.models.ts` — generic serializable frame union and transition types.
  - `detail-overlay-url-codec.ts` + spec — pure parse/serialize/canonicalize logic.
  - `detail-overlay-history.service.ts` + spec — URL-authoritative state, push/replace/back provenance, direct-link seeding, and href generation.
  - `detail-overlay-link.directive.ts` + spec — real href plus unmodified-click interception. Core owns app-wide navigation, not Words rendering.
- `src/app/shared/ui/detail-modal-shell/`
  - presentation-only dialog shell, Back/Close slots, focus trap, labels, backdrop, and restore button. It owns no entity/API state.
- `src/app/features/words/entity-detail-overlay/`
  - persistent host and small lazy entity adapters/controllers. Words owns all supported entity semantics and existing detail-component composition.
- `src/app/app.ts`
  - imports the persistent Words host beside `qd-app-shell` and binds `inert`/`aria-hidden` to the shell while the dialog is open.

Do not put Words models, APIs, labels, or selectors in `shared/`. Do not make `core` import a Words component. The root `App` is the allowed composition point for both.

Load only the small host/coordinator eagerly. Use Angular deferred/dynamic imports for entity adapters so the dashboard's initial bundle does not eagerly absorb all Words explorer detail code.

### 5.4 URL contract

Reserve two collision-resistant query keys:

- Repeated `qdDetail` values, ordered from bottom to top of the stack.
- `qdDetailOpen=1` when the dialog is visible; absent when the retained stack is closed/restorable.

Illustrative URL:

```text
/dashboard/mushaf?page=92&ayah=4:57&focusAyah=4:57&panel=ayah
  &qdDetail=v1~root~999~words~simple~mentioned~1
  &qdDetail=v1~lemma~555~ayahs~simple~mentioned~1~-
  &qdDetailOpen=1
```

Canonical version-1 frame grammars:

```text
v1~unique~<simple|tashkeel>~<id>~<view>~<ayahPage>
v1~root~<id>~<view>~<wordView>~<surahView>~<detailPage>
v1~lemma~<id>~<view>~<wordView>~<surahView>~<detailPage>~<typeCode|->
v1~stem~<id>~<view>~<wordView>~<surahView>~<detailPage>~<typeCode|->
v1~wordType~<tashkeelId>~<contextCode>~<case>~<tense>~<voice>~<view>~<detailPage>
```

Codec rules:

1. Encode every string field, validate positive IDs, and accept only closed enum values.
2. Serialize defaults explicitly. A future default change must not alter an old shared URL.
3. Use `ParamMap.getAll('qdDetail')` to retain stack order and `Router.serializeUrl` to generate hrefs. Do not extend `deepLinkToHref`; it only accepts scalar query values and manually joins them (`shared/url/deep-link-href.ts:1-20`).
4. Invalid first frame means no overlay; strip overlay keys with replace semantics. A malformed later frame truncates the stack immediately before it and canonicalizes once.
5. `qdDetailOpen=1` without a valid frame canonicalizes to closed/no overlay.
6. Cap at eight frames. Refuse a ninth append, leave the current state untouched, and announce a concise Arabic status; never silently drop the bottom frame.
7. Treat a push of the complete current top frame as a no-op. Equality includes every identity and view field.

Generated links retain the current base route and its existing query state. When a share link is built without a current base, use the corresponding unselected explorer as the canonical base: Roots, Lemmas, Stems, the appropriate Unique mode, or Word Types. Never populate the base page's existing selection keys merely to show the overlay.

### 5.5 State and history policy

The URL is authoritative:

```ts
type DetailOverlayUrlState = {
  visibility: 'open' | 'closed';
  stack: readonly DetailFrame[];
};
```

| Event | URL/history operation | Result |
|---|---|---|
| First entity open | append first frame; `qdDetailOpen=1`; push | Back returns to the same base with no overlay |
| Cross-entity click | append frame; push | Back returns to the parent card |
| Top card tab/sub-view/page | replace top frame; replace | URL stays shareable without polluting entity history |
| Dialog Back | browser Back only when owned provenance proves the immediate parent; otherwise replace with top frame removed | Always returns to the previous card; never exits to an unrelated site |
| Browser Back/Forward | parse the URL and cancel/synchronize the active controller | Browser and dialog navigation converge on one state machine |
| Close/Escape/backdrop | retain stack, remove `qdDetailOpen`; replace | Dialog disappears; restore button appears |
| Restore | add `qdDetailOpen=1`; push | Browser Back returns to the closed/restore state |
| New entity click while closed | start a new one-frame stack; push | Retained stack is reopened only by Restore, avoiding accidental append to hidden context |
| Ayah base-route change with an open stack | replace the current entry's base route and preserve its parent provenance | Browser Back and dialog Back still pop the entity frame together; the parent entry restores its historical base |

Each app-created push stores a small `NavigationExtras.state` provenance record containing the base-URL signature, parent-stack hash, current-stack hash, and transition kind. Top-frame replacements preserve that record. Dialog Back calls browser Back only when the provenance proves the immediately previous entry is its parent. A direct/shared URL or an intervening base-route navigation uses the deterministic replace fallback.

For a freshly loaded shared URL with no owned history provenance, materialize its prefixes once after initial Router stabilization: replace the current entry with the same base and no overlay, then add each stack prefix with `Location.go`, ending at the original URL. Mark the entries in `history.state` so reload does not seed them again. This makes browser Back pop a deep-linked stack instead of leaving the application immediately.

### 5.6 Route-independent detail state and cache reuse

`RootsDetailFacade`, `LemmasDetailFacade`, `StemsDetailFacade`, and `WordTypesDetailFacade` are `providedIn: 'root'` and bind directly to `ActivatedRoute`; using those singleton instances in the global host could alter the explorer beneath it. Refactor each into:

1. A route-independent detail controller that accepts a typed frame, owns its signal state/subscriptions/generation guard, and calls the existing API/cache/view-loader services.
2. A thin existing page facade/route adapter that parses today's page query and forwards it to its controller.
3. A component-scoped controller instance in each modal adapter.

`UniqueWordsDrilldownFacade.restoreFromUrl` is already close to the required route-free API, but its state must likewise be component scoped when used by the global overlay. Root-scoped API/cache services remain shared so the side panel and overlay de-duplicate the same reads.

Every identity transition cancels the prior subscription and carries a generation/frame signature so a late response cannot overwrite a newer top frame. Preserve distinct 404/not-found and transport/error states.

Render existing detail presentation in inline/content mode inside the new global dialog shell. Do not nest an existing backdrop, `role="dialog"`, focus trap, or shadow inside the global shell. If an existing component cannot expose content without dialog chrome, extract that content once and let the current responsive drawer and new global shell both compose it.

### 5.7 Word Type link without backend/API changes

The Mushaf DTO has `headPos`, `isVerb`, `verbTense`, `verbVoice`, `caseFeature`, and the unique tashkeel word ID, but no prebuilt Word Type identity (`core/api/generated/models/word-morphology-dto.ts:8-17`). Word Types has no standalone “type” entity; its word detail uses `(tashkeelWordId, contextCode, case, tense, voice)` (`word-types.models.ts:56-62`; `word-types-cache.ts:27-35`). Backend matching uses a verb tense context for verbs and `headPos` for non-verbs (`EfWordTypesReader.cs:413-424`; `WordTypeIdentityMatcher.cs:3-18`).

The frontend-only adapter is therefore:

```text
tashkeelWordId = analysis.identity.uniqueTashkeel.id
contextCode    = morphology.isVerb
                   ? (morphology.verbTense ?? "unspecified")
                   : morphology.headPos
case           = "all"
tense          = "all"
voice          = "all"
view           = "ayahs"
detailPage     = 1
```

Using `all` for the three optional filters opens the complete existing Word Type row represented by the chip rather than silently narrowing to one clicked occurrence. This is deterministic, uses the existing endpoint/cache contract, and obeys the no-backend constraint. Put the mapping in one pure Mushaf utility with verb/non-verb/unspecified tests. If the unique ID or required context cannot be formed, leave the label non-interactive and do not guess.

The current Arabic type label remains the visible anchor text. Its href contains the canonical Word Type modal frame over the current Mushaf base.

### 5.8 Ayah navigation continuity

Use the existing Mushaf deep-link builder for `page`, `ayah`, `focusAyah`, and `panel=ayah`, then add the untouched overlay keys:

- From an already-open global overlay, retain the current stack and `qdDetailOpen=1`.
- From an explorer side-panel ayah list with no overlay, promote the current detail selection to a one-frame overlay stack before navigating. This preserves the scholarly context over the Mushaf and makes the locked “keep the modal open” behavior true even when the click originated in a side panel.
- If a stack is already open, update/navigate to the Mushaf with replace semantics while preserving the current entry's parent-stack provenance. Browser Back and dialog Back then both return to the parent card and that parent's historical base URL; the ayah jump does not insert a non-entity history step between modal frames.
- If no stack exists and a side-panel frame is being promoted, push the first visible frame together with the Mushaf base so browser Back returns to the original side panel/base entry.
- If already on the Mushaf, retain its existing merge/replace reader behavior; the persistent host remains mounted.

The source page must provide its current typed frame as context to `AyahMatchesListComponent`; do not make the shared ayah-list infer a parent from generic route keys.

### 5.9 Accessible modal and restore control

The global shell must provide:

- `role="dialog"`, `aria-modal="true"`, `dir="rtl"`, and an entity-specific heading referenced by `aria-labelledby`.
- One active `cdkTrapFocus` with auto-capture. Initial focus is Back when depth is greater than one, otherwise Close; announce title changes in a polite live region.
- Escape and backdrop dismissal equivalent to Close. Back is a separate action and never closes a retained stack.
- `inert` and `aria-hidden="true"` on `qd-app-shell` only while open.
- Focus return to the invoking link when it remains connected. After cross-route navigation or Close, focus the restore button; on deep-link hydration focus the modal heading/first control.
- A restore button at physical top-left using logical `inset-block-start` + `inset-inline-end` in RTL. It has an explicit Arabic accessible name that includes the retained entity title.
- Logical Back icon/direction and Arabic text; no LTR-only transforms embedded in component logic.
- Reduced-motion behavior and no animation of Quran text.

On mobile, opening the global dialog over an existing explorer drawer must disable the underlying drawer's focus trap while the app shell is inert. Replace the instance-local body lock with a reference-counted service or CDK block-scroll strategy and test two simultaneous consumers.

### 5.10 B affected files/components

Create the core/shared/feature files described in §5.3, including unit specs. Modify:

- `src/app/app.ts`.
- Existing root/lemma/stem/unique/Word Type detail facades/controllers and their specs.
- Existing detail panel/drilldown content only as needed to expose dialog-free content mode.
- `selected-word-section.component.{ts,html,spec.ts}`.
- `word-morphology-summary.component.{ts,html,scss,spec.ts}`.
- `root-words-list`, `root-lemmas-list`, `root-stems-list`, `lemma-words-list`, `lemma-stems-list`, `stem-words-list`, and `stem-lemmas-list` component TS/HTML/spec files.
- `ayah-matches-list.component.{ts,html,spec.ts}`; its card styling is owned by Change A.
- `mushaf-reader-session.ts`, `mushaf-reader.facade.ts`, and their URL/session specs.
- `modal-scroll-lock.directive.ts` plus a new reference-count/multi-consumer spec, or replace it with the chosen CDK strategy.
- `src/styles/_components.scss` and `_explorer-detail-lists.scss` for generic overlay/restore geometry only.
- `src/app/core/README.md`, `src/app/shared/README.md`, Words `README.md`, Mushaf `README.md`, and `UI_STYLE_SYSTEM.md` because app-wide URL/navigation/dialog contracts change.

No generated API file changes belong in this phase.

### 5.11 B risks and mitigations

| Risk | Mitigation/acceptance gate |
|---|---|
| Global controller mutates side-panel state | Component-scope every overlay controller; invariant test proves page selection/query/list requests do not change |
| Same numeric ID cross-serves wrong data | Compare/serialize complete frames; retain Unique mode and full Word Type composite identity |
| Stale async response overwrites a later frame | Cancel subscriptions plus generation/frame signature |
| Deep link Back exits instead of popping | One-time prefix history materialization and Back/Forward integration tests |
| URL growth | Explicit fields, eight-frame cap, no embedded summaries/text; test serialized maximum length |
| Eager bundle regression | Persistent host stays small; lazy-load each entity adapter; compare production build chunks |
| Double modal focus/scroll lock on mobile | Inert shell, disable underlying trap, reference-counted scroll lock, two-consumer tests |
| Overlay-only Mushaf URL blocks session restore | Ignore overlay-owned keys in bare-state detection and merge during restoration |
| Type chip opens semantically wrong row | One pure, locked identity adapter and backend-contract-shaped fixtures; never infer from localized label |
| Shareable anchors regress browser affordances | Directive intercepts only unmodified primary click; href/modifier tests |

### 5.12 B tests to add/update

1. Pure codec tests for all frame kinds, explicit defaults, repeated order, encoding, invalid version/ID/enum, later-frame truncation, cap, and closed retained stacks.
2. History service tests for first open, append, top replace, close, restore, Back/Forward, direct-link prefix seeding, reload idempotence, and provenance fallback.
3. Link directive tests for canonical href, unmodified interception, and untouched Ctrl/Cmd/Shift/middle/context-menu behavior.
4. Controller tests for cache identity, same ID/different sub-state, stale cancellation, 404, transport error, and reuse of existing cache keys.
5. Explorer invariant test: side-panel selection/query/list request count is unchanged while opening/navigating/closing a global stack.
6. Mushaf type-adapter tests for verb tense, null/unspecified verb tense, non-verb head POS, missing identity, and fixed `all` scope.
7. Same-page and cross-page ayah tests retaining/promoting the stack, plus the overlay-only session-restoration case.
8. Dialog tests for naming, trap, inert shell, Escape, backdrop, Back, Close, Restore, opener/fallback focus, RTL placement/direction, and reduced motion.
9. Mobile nested-layer test with two scroll-lock consumers and only the top focus trap active.
10. Router integration: Words primary side panel → modal lemma → modal stem → ayah/Mushaf → Close → Restore → browser Back/Forward → refresh/share.

### 5.13 B implementation phase order

1. **B0 — contract fixtures:** encode the frame union, frontend-only Word Type mapping, canonical base routes, eight-frame cap, and side-panel/ayah promotion rules as tests before UI work.
2. **B1 — codec:** implement strict parse/serialize/canonicalize helpers and maximum-length/corruption coverage.
3. **B2 — history coordinator:** implement push/replace/close/restore, provenance, PopState synchronization, direct-link prefix seeding, and real href generation.
4. **B3 — shell:** mount the persistent root host, add inert/focus/ARIA/RTL/restore behavior, and make scroll locking multi-consumer safe.
5. **B4 — route-independent details:** extract/scoped controllers and dialog-free reusable content for Root, Lemma, Stem, Unique, and Word Type while proving primary panels unchanged.
6. **B5 — cross-entity links:** migrate detail-list anchors and Mushaf root/lemma/stem/unique identity anchors, preserving modifier-click browser semantics.
7. **B6 — type entity:** add the pure Word Type identity adapter and type anchor using the existing frontend contract only.
8. **B7 — ayah continuity:** retain/promote stacks through same-page and cross-page Mushaf navigation; fix overlay-only session hydration.
9. **B8 — hardening:** integration, mobile nested-layer, Back/Forward/refresh/share, accessibility, bundle, README, full test, and build gates.

## 6. Change A — ayah-card style unification

### 6.1 Current state and evidence

All Words explorer “الآيات” views already funnel through `AyahMatchesListComponent`:

- Roots at `roots-explorer-page.component.html:181-189,232-239`.
- Lemmas at `lemmas-explorer-page.component.html:220-227,264-270`.
- Stems at `stems-explorer-page.component.html:232-238,276-282`.
- Word Types at `word-types-explorer-page.component.html:128-133,165-172`.
- Unique Words at `word-drilldown-modal.component.html:68-75`.

Current Words cards are local `qd-card` articles with surface/alternating backgrounds (`ayah-matches-list.component.html:1-75`; `.scss:17-26,59-63`), and explorer-specific global selectors further recolor/flatten them (`src/styles/_explorer-detail-lists.scss:315-321,379-386,416-447`).

Mushaf Similar Ayahs uses `study-card qd-card` (`similar-ayahs-card.component.html:19-44`) with `--qd-section-bg` (`_study-card.shared.scss:1-9`). Mutashabihat's comparable ayah unit is each occurrence (`mutashabihat-groups-card.component.html:41-95`), currently recessed and borderless except when selected (`.scss:55-75`). The two Mushaf sources are not themselves identical today. The flat restyle preview resolves the intended common frame at `docs/design-preview/assets/preview.css:579-605`: surface background, 1px hairline, control radius, compact padding, and no shadow.

### 6.2 Exact target and approach

Create a presentation-only `src/app/shared/ui/ayah-card/` component. It owns only:

- `--qd-surface` background.
- `1px solid var(--qd-border)` frame.
- `var(--qd-radius-sm)` control radius.
- compact logical padding/gap consistent with `qd-card--mini`.
- no box shadow and no alternating row fill.
- projected metadata/text/action content.

It accepts no Quran/domain model, text, word array, match ID, formatter, route, or output. It does not set a Quran font and does not make the whole card clickable.

Use it for:

1. Loaded and loading cards inside `AyahMatchesListComponent`.
2. Every Similar Ayahs item.
3. Every Mutashabihat occurrence, while the outer group header/expand state remains feature-owned.

Callers retain semantic list/article wrappers as appropriate. Preserve the selected Mutashabihat state as an additional accent/border modifier layered on the common frame.

The Quran integrity boundary remains:

- Words keeps `HighlightedAyahComponent`, which filters markers, calculates the matched ID set, and renders untouched `textUthmani` spans (`highlighted-ayah.component.ts:13-26`; HTML `:1-10`; SCSS `:1-16`).
- Similar/Mutashabihat keep their current `toStudyAyahDisplayText`/verse-key display mapping and whole-string interpolation.
- Change A alone is presentation-only. Change B separately changes the link destination while leaving the renderer and visible text untouched.

One existing correctness defect should be fixed while changing the loop host: Word Type ayah mapping supplies `ayahId: 0`, while the list currently tracks by `ayahId`. Track by stable `verseKey` and add a multi-row Word Type regression. Do not invent missing labels or edit generated DTOs.

### 6.3 A affected files/components

Create:

- `src/app/shared/ui/ayah-card/ayah-card.component.{ts,html,scss,spec.ts}`.

Modify:

- `features/words/components/ayah-matches-list/*`.
- `features/mushaf/components/similar-ayahs-card/*`.
- `features/mushaf/components/mutashabihat-groups-card/*`.
- `src/styles/_explorer-detail-lists.scss` to remove selectors that override the shared frame.
- Existing Roots/Lemmas/Stems page specs that assert old `.ayah-matches-list__card` implementation classes; replace with semantic/test-id behavior assertions.
- Shared, Words, and Mushaf READMEs plus `UI_STYLE_SYSTEM.md` to record the primitive and sacred rendering boundary.

Leave `_study-card.shared.scss` intact for other study cards. Remove only the Similar Ayahs dependency that becomes redundant after migration.

### 6.4 A risks and tests

Risks:

- Accidentally changing Uthmani content/marker stripping while extracting the frame.
- Losing the Words matched-word accent or moving its font to the generic component.
- Nested borders when Mutashabihat outer groups and occurrences both use card chrome.
- Old global detail-list selectors overriding the new component in inline versus modal contexts.
- Duplicate Angular tracking keys in Word Types.

Tests:

1. Shared component: content projection, frame classes/tokens, flat/no-shadow contract, and no domain/text API.
2. Words: exact word sequence and `textUthmani` values, marker exclusion, only supplied Quran word IDs highlighted, analysis action retained, stable `verseKey` tracking, and loading frame parity.
3. Similar Ayahs: same display text, metadata, and `ayahNavigate` output before/after wrapper migration.
4. Mutashabihat: grouping, selected occurrence, collapse/expand, display text, and `ayahNavigate` remain unchanged.
5. Visual/manual: all five Words detail consumers plus both Mushaf sources in RTL/light/dark, checking surface, hairline, radius, padding, focus, and no shadow.

### 6.5 A phase list

1. **A1:** add and test the presentation-only shared frame.
2. **A2:** migrate Similar Ayahs and Mutashabihat occurrences without touching their renderers.
3. **A3:** migrate Words ayah matches, remove alternate/global overrides, and switch tracking to `verseKey`.
4. **A4:** run Quran-rendering regressions, update READMEs/style-system documentation, and perform visual parity checks.

## 7. Change U1 — selected-word loading layout reservation

### 7.1 Current state and resolved target

Inspection resolves “word-detail card” to the Mushaf `SelectedWordSectionComponent`, not the generic Unique Words drilldown:

- Loading and loaded branches share the component shell at `selected-word-section.component.html:15-126`.
- The card has `min-height: 14rem`, but embedded responsive rules reset host, card, and content minimums to `auto` (`selected-word-section.component.scss:13-33`; global `_components.scss:643-656`).
- The next divider/selected-ayah section therefore moves when the selected-word content changes (`study-context-section.component.html:11-39`).
- The skeleton always renders three segment cards (`selected-word-section.component.ts:37`), while loaded segment count is dynamic.
- Skeleton and loaded segment cards share a minimum, but real content can wrap; morphology skeleton spacing is also smaller than the loaded summary (`selected-word-section.component.scss:79-158`; `segment-data-rows.component.scss:1-23`; `word-morphology-summary.component.scss:1-21`).
- The current spec compares individual segment minimum heights but not the total shell geometry (`selected-word-section.component.spec.ts:362-391`).

The Unique Words drilldown's outer desktop/dialog geometry is already fixed by `_words-explorer-layout.scss:89-123` and `_components.scss:433-442`, so it does not match the reported reflow cause.

### 7.2 Exact approach

Keep the existing structured skeleton and never retain hidden old Quran DOM. Add a guarded, component-local natural-size reservation:

1. Observe the successfully loaded `.selected-word-section` block size with `ResizeObserver` when available. Retain only numeric block size and previous segment count, never prior text/DOM.
2. On the next loading transition, apply the greater of the last successful block size and a responsive first-load baseline through a logical CSS custom property/minimum such as `--qd-selected-word-reserved-block-size` and `min-block-size`.
3. Render the last successful segment count as anonymous skeleton cards; retain the existing safe fallback count for first load.
4. Match loaded header, segment-card, morphology-card, identity-row padding/gaps in the skeleton so the reservation represents natural layout rather than an unrelated blank block.
5. Re-measure on width changes. Keep separate/current responsive measurements so a wide desktop value is not blindly imposed on a phone layout.
6. Remove the temporary loading reservation after success/error/empty. Loaded content determines its own natural height.
7. Browser/test guard `ResizeObserver` and direct DOM access. Do not expand the generic shared skeleton API for this one dynamic content surface.

Before choosing the first-load CSS baseline value, Fable must reproduce the issue with representative one-, three-, and maximum-observed-segment fixtures at desktop/tablet/phone widths and record the largest loaded natural size per responsive band. That measurement, not an invented number, becomes the documented baseline.

### 7.3 U1 affected files/components

- `selected-word-section.component.ts` — measurement metadata/lifecycle.
- `selected-word-section.component.html` — loading modifier, measurement target, dynamic anonymous placeholders.
- `selected-word-section.component.scss` — logical reservation and matched loading geometry.
- `selected-word-section.component.spec.ts` — state/measurement/no-stale-Quran tests.
- Mushaf `README.md` — stable selected-word shell behavior. Update `UI_STYLE_SYSTEM.md` only if the measurement pattern is deliberately generalized later.

### 7.4 U1 risks and tests

Risks:

- Observer loops or stale desktop measurements on resize.
- Retaining old Quran glyphs to fake stability; explicitly forbidden.
- An arbitrary fixed height creating excess blank space or clipping dynamic content.
- Unit tests giving false confidence about browser geometry.

Tests:

1. With a fake observer, record a successful 420px render, transition to loading, and assert the loading shell reserves at least 420px while old word/segment/morphology/identity DOM is absent.
2. Assert a successful next render clears the temporary reservation and updates numeric size/segment-count metadata.
3. Assert resize invalidates/re-measures the correct responsive value.
4. Preserve existing tests that loading never exposes the previous Quran word and that structured skeletons—not one overlay block—are rendered.
5. Browser/manual with delayed uncached word-analysis response: the following divider's top coordinate differs by no more than 1px immediately before versus during loading, and the loaded transition does not clip. Record this at desktop/tablet/phone widths.
6. Optional browser `PerformanceObserver` observation may report CLS, but coordinate stability is the primary acceptance gate because click-triggered shifts can be excluded from CLS by recent input.

No Playwright/browser harness is installed. Do not add a dependency solely for this change; keep real geometry as a documented manual acceptance check unless the project separately authorizes a browser-test harness.

### 7.5 U1 phase list

1. **U1.1:** reproduce and measure natural loaded geometry across representative content/widths.
2. **U1.2:** add responsive baseline plus last-loaded numeric reservation and structurally matched skeleton.
3. **U1.3:** add guarded observer/state tests and preserve no-stale-Quran invariants.
4. **U1.4:** run delayed-response browser geometry acceptance and document the measured baseline.

## 8. Change U2 — count-range panel below the toolbar row

### 8.1 Current state and cause

- `.qd-explorer-controls-secondary` is a wrapping flex row with `align-items: center` (`src/styles/_words-explorer-layout.scss:192-200`).
- `ExplorerCountRangeFilterComponent` has a block host and a normal-flow `<details>` (`explorer-count-range-filter.component.scss:1-9`; HTML `:1-89`).
- Opening it reveals a wide six-column/nowrap body (`.scss:56-69`), increasing the sibling's intrinsic dimensions and re-centering/rewrapping the sort.
- The same shared component follows the sort on Roots, Lemmas, Stems, and Unique Words (`roots...html:29`; `lemmas...html:42`; `stems...html:54`; `unique...html:54`).
- The approved restyle preview already shows the intended rule: the filter takes `flex: 1 1 100%` as its own row (`docs/design-preview/assets/preview.css:424`), matching Feature 026's “collapsible row under the toolbar” contract (`specs/026-words-explorers-enhancements/tasks.md:159-163`).

### 8.2 Exact target and approach

Change only the shared filter host to:

```scss
:host {
  display: block;
  flex: 1 1 100%;
  min-inline-size: 0;
}
```

The sort remains in the first secondary-control row. The closed summary and expanded panel occupy a stable full-width second row on all four pages. Keep native `<details>` semantics and normal document flow; do not use an absolute popover and do not edit four page templates.

### 8.3 U2 affected files, risks, and tests

Affected:

- `explorer-count-range-filter.component.scss`.
- Its component spec only if a host-class/contract assertion is useful; do not pretend jsdom validates flex geometry.
- Words `README.md` to state that the shared filter is a full-width row below sort.

Risks:

- A page-specific selector overriding the host flex basis.
- Phone layout regressions; keep logical sizing and the existing column flow.
- Converting it to an overlay and losing the approved in-flow design.

Tests/acceptance:

1. Existing filter input, chip, custom-range, disabled, clear, and ARIA tests remain green.
2. Manual geometry on all four explorers: record the sort control's top/inline-start coordinates before and after opening; both must be unchanged (≤1px tolerance).
3. Check open/closed panel at desktop/tablet/phone widths in RTL/light/dark.

### 8.4 U2 phase list

1. **U2.1:** apply the shared host full-row rule.
2. **U2.2:** run the component/Words focused tests.
3. **U2.3:** verify unchanged sort coordinates on all four pages and update the Words README.

## 9. Change U3 — Word Types view tabs directly above the table

### 9.1 Current state and evidence

`word-types-explorer-page.component.html:58-73` currently renders scope counts and then the four tabs inside the top container. The split layout begins later at `:75`, so the tabs span above both the table and detail-panel columns rather than sitting directly above the table.

The existing option order is `كلمات | جذور | أصول | صيغ` (`word-types.labels.ts:32-37`). In RTL that renders physically as the requested `صيغ / أصول / جذور / كلمات`. Reversing the array would break DOM, roving-tabindex, and Feature 026 contract order.

Feature 026 requires semantic order `filters → scope summary → tabs → table` and a mounted table shell (`docs/feature-026-words-explorers-enhancements/plan.md:317-328`). The move must preserve both.

### 9.2 Exact target and approach

1. Move the existing conditional `qd-word-type-table-view-tabs` block to be the first direct child of `.word-types-page__layout`, immediately before `<main>`.
2. Do not put it inside `.qd-explorer-layout__table`: at desktop the shared rule gives every direct child of that main wrapper the full table-card block size (`_words-explorer-layout.scss:120-127`).
3. Give the tabs a page-specific class and, at desktop only, place:
   - tabs: grid column 1, row 1;
   - table main: grid column 1, row 2;
   - detail panel: grid column 2, row 2.
4. Below desktop, remove explicit placement and use DOM flow: tabs → table → panel.
5. Keep the current tree-loaded condition and component instance/mounted-shell behavior through loading, prompt, empty, error, and view changes. No facade, labels, tab implementation, or URL-state change is needed.

### 9.3 U3 affected files, risks, and tests

Affected:

- `word-types-explorer-page.component.html`.
- `word-types-explorer-page.component.scss`.
- `word-types-explorer-page.component.spec.ts`.
- Words `README.md` to record table-local placement.

Risks:

- Applying desktop grid rows at tablet/phone widths.
- Aligning the detail panel to the tabs instead of the table.
- Increasing total viewport height without reviewing the explorer chrome/table-body calculation.
- Reversing the option array to chase physical RTL order.

Tests:

1. Assert tabs are the layout's first child and immediately precede `main` in DOM order.
2. Preserve current tests for tab order, RTL roving focus, selection output, URL delegation, active view across scope changes, and mounted table/detail hosts.
3. Verify tabs remain present in loading, select-prompt, empty, error, and success states once the tree is available.
4. Manual desktop: tabs align only with the table column and the panel top aligns with the table, not the tabs. Tablet/phone: tabs → table → panel, no overlap or horizontal overflow.

### 9.4 U3 phase list

1. **U3.1:** move the existing mounted tab host into the split layout.
2. **U3.2:** add desktop-only grid placement and responsive reset.
3. **U3.3:** update DOM-order/mounted-shell tests and verify RTL/light/dark responsive layout.

## 10. Integrated implementation order for Fable

The changes can be reviewed independently, but A and B both touch `AyahMatchesListComponent`. Use this order to avoid conflicting rewrites:

1. **P0 — baseline:** start from `dev` plus the flat-green restyle; reproduce A/U1/U2/U3 and save before screenshots/coordinates. Add no backend/generated changes.
2. **P1 — U2 and U3:** land the isolated shared-filter and Word Types placement fixes with focused tests.
3. **P2 — A:** introduce/migrate the shared ayah-card frame and lock Quran-rendering regressions.
4. **P3 — B0–B3:** URL/frame tests, history coordinator, persistent accessible shell, and safe scroll/focus ownership.
5. **P4 — B4–B6:** route-independent controllers plus entity/cross-link/type migrations; prove side panels remain unchanged.
6. **P5 — B7–B8:** Mushaf ayah/session continuity, browser integration, accessibility, responsive nesting, bundle, and documentation.
7. **P6 — U1:** measure after the final modal/card CSS has settled, then implement natural-size reservation and run geometry acceptance.
8. **P7 — final guard:** clean-code self-check, test-code self-check, focused suites, full frontend suite, production build, `git diff --check`, and manual RTL/light/dark keyboard/history matrix.

No phase should change the backend or generated API. Keep commits/PR decisions outside this plan; the current task authorizes none.

## 11. Verification matrix for the implementation workflow

From `Frontend/quran-dashboard-ui`:

```bash
# Focused component/feature suites during phases
npm test -- --include="src/app/shared/ui/ayah-card/**/*.spec.ts"
npm test -- --include="src/app/core/navigation/detail-overlay/**/*.spec.ts"
npm test -- --include="src/app/features/words/**/*.spec.ts"
npm test -- --include="src/app/features/mushaf/**/*.spec.ts"

# Final automated gates
npm test -- --watch=false
npm run build -- --output-path=/tmp/qdb-feature-029-verification
git diff --check
```

Manual/browser acceptance matrix:

| Surface | Desktop | Tablet | Phone | Keyboard/history | Light/dark |
|---|---:|---:|---:|---:|---:|
| Words ayah cards across five detail consumers | yes | yes | yes | focus/activation | both |
| Mushaf Similar/Mutashabihat cards | yes | yes | yes | focus/activation | both |
| Modal from Mushaf root/lemma/stem/type/unique | yes | yes | yes | Tab/Shift+Tab/Escape | both |
| Cross-link from each explorer side panel | yes | yes | yes | focus return, side panel unchanged | both |
| Two-plus-level stack | yes | yes | yes | dialog Back + browser Back/Forward | both |
| Deep link/refresh/invalid URL/closed restore | yes | yes | yes | URL canonicalization | both |
| Ayah navigation on/off Mushaf | yes | yes | yes | retained/promoted stack | both |
| Count filter opening on four pages | yes | yes | yes | native details | both |
| Word Types tabs/table/panel | yes | yes | yes | RTL roving focus | both |
| Selected-word delayed loading | yes | yes | yes | divider coordinate ≤1px | both |

Quran-specific manual comparison must verify identical visible Uthmani text, marker behavior, word order, font, line height, and matched-word set before/after A and B.

## 12. Open decisions

There are no backend or data decisions; those are excluded. One frontend semantic policy should be confirmed before B is considered acceptance-complete:

1. **Word Type aggregation scope:** this plan recommends the complete existing type row (`contextCode` derived from verb tense/head POS with `case=tense=voice=all`). If product intent is the exact clicked occurrence's case/tense/voice instead, the modal URL and cache identity must carry those normalized feature values. Do not mix the two meanings.

All other implementation choices in this report are resolved defaults: eight-frame cap, closed stack retained in URL, restore as a history push, side-panel ayah context promotion, ayah base replacement while a stack is open, component-owned U2 sizing, and DOM/semantic tab order unchanged in RTL.
