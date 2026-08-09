# Frontend Structure Guide

## Purpose

This document defines lightweight structure rules for the Quran Dashboard Angular
app, plus file size and responsibility thresholds.

Read this file **before** adding or changing:

- Angular components, services, or routes
- state / facade / store files
- feature folder structure
- frontend file organization

For visual / style-system rules (colors, themes, tokens, `qd-` classes, RTL),
read:

- `FRONTEND_UI_RULES.md` — the short mandatory rule set, first, for any UI-visible change
- `.architecture/UI_STYLE_SYSTEM.md` — the mechanics, §18 for the Golden foundation
- `.architecture/golden-ui/` — the permanent visual authority (contracts + acceptance boards)

For product and visual context:

- `../../PRODUCT.md`
- `../../DESIGN.md`

> Scope note: this is documentation/rules only. It does not create components,
> services, routes, or features — it defines how that work must be organized when
> it is explicitly requested. This file is intentionally minimal for now and will
> grow as the frontend foundation is built.

## Minimum Structure Guidance

Keep the structure simple and feature-first until there is a real reason to add
more.

- **Organize by feature**, not by technical type. Prefer
  `src/app/features/<feature>/` over global `components/`, `services/`, `models/`
  dumping folders.
- Keep truly shared, cross-feature building blocks in a small `src/app/shared/`
  (or `core/` for app-wide singletons). Do not let these become dumping grounds.
- **Components use separate `.html` and `.scss` files by default.** Do not inline
  templates in TypeScript except for a tiny inline component with explicit
  approval.
- Separate responsibilities:
  - **API services** call HTTP endpoints and map basic API responses.
  - **Facade / store / state services** own page/feature state and orchestration.
  - **Components** coordinate UI state and user interactions.
  - **Pure helpers** hold focused, side-effect-free logic near their feature.
- Routes/feature wiring should stay thin and predictable; do not hide business
  logic in route configuration.
- Follow `UI_STYLE_SYSTEM.md` for all visual styling — compose shared `qd-`
  classes and tokens instead of recreating styles per component.
- **Cross-cutting UI contracts live as data, not as duplicated constants.** The responsive
  bands are `src/app/shared/layout/breakpoints.contract.json`, read directly by
  `shared/layout/breakpoints.ts` and by `tailwind.config.js`, with
  `src/styles/_breakpoints.scss` as the Sass adapter. `scripts/check-golden-ui-contract.mjs`
  (`npm run check:golden-ui`) fails the build-time contract when any of the three drift, when a
  raw pixel threshold appears in a migrated file, or when the `qd-state` adapter gains a
  call site. Add a contract to that checker rather than to a second constants file.
- Arabic-first / RTL is the default; respect it in structure and layout.
- Quranic data safety: never invent Quranic text or labels; show missing data as a
  controlled state, never silently fabricated.

## File Size and Responsibility Guidelines

File size limits here are **review thresholds, not blind automatic failures**. A
file that exceeds a threshold is a strong signal that a component or service is
doing too much and should be reviewed and justified — not a number to satisfy
mechanically.

Principles:

- File size limits are review thresholds, not blind automatic failures.
- Do not generate huge Angular components or services.
- Never put HTML templates inside TypeScript files unless it is a tiny inline
  component with explicit approval.
- Angular components should use separate `.html` and `.scss` files by default.
- A component / service with thousands of lines is not acceptable.
- A 1000+ line Angular file usually means the component/service is doing too much.
- A 3000+ line service/component is not acceptable and must be split before
  completion.
- Split by feature, UI responsibility, state responsibility, or API
  responsibility.

### Frontend thresholds

Thresholds are line counts per file. A **soft** threshold means "review and
justify"; a **hard** threshold means "stop and split, or split immediately".

#### 1. Angular component TypeScript files

- Ideal: 150–250 lines
- Soft review threshold: 300 lines
- Hard review threshold: 400 lines

Rules:

- Component TS should coordinate UI state and user interactions only.
- Do not put large business logic, formatting pipelines, API orchestration, or
  repeated helpers inside the component.
- If it grows, split into child components, facade/store services, pure helpers, or
  feature services.

#### 2. Angular component HTML templates

- Ideal: 150–250 lines
- Soft review threshold: 300 lines
- Hard review threshold: 400 lines

Rules:

- If a template grows too large, split it into child components.
- Do not build full pages as one giant template.
- Keep repeated sections as reusable components.

#### 3. Angular component SCSS

- Ideal: under 150 lines
- Soft review threshold: 200 lines
- Hard review threshold: 300 lines

Rules:

- Component SCSS should stay local and small.
- Repeated visual patterns must move to the global style system using `qd-`
  classes.
- Do not create a component-level design system.
- Do not redefine global cards / buttons / inputs / tables / modals inside
  component SCSS.

#### 4. Frontend API services

- Ideal: 100–200 lines
- Soft review threshold: 250 lines
- Hard review threshold: 350 lines

Rules:

- API services should mainly call HTTP endpoints and map basic API responses.
- They must not own page state, complex UI logic, business workflows, or formatting
  logic.
- Split by backend resource / feature when needed.

#### 5. Frontend facade / store / state services

- Ideal: 200–350 lines
- Soft review threshold: 400 lines
- Hard review threshold: 600 lines

Rules:

- State / facade services can be larger than API services, but must remain
  cohesive.
- Split by workflow, state slice, or feature area when they become too broad.
- Avoid oversized stores that own unrelated modals, filters, data loading, selection,
  drag/drop, and persistence all in one file.

#### 6. Frontend utility / helper files

- Ideal: under 150 lines
- Soft review threshold: 200 lines
- Hard review threshold: 300 lines

Rules:

- Helpers must be pure and focused.
- Do not create generic dumping files like `helpers.ts` or `utils.ts` with
  unrelated functions.
- Put helper functions near the feature unless truly shared.

### Frontend review behavior

If a frontend file is expected to exceed its **soft** threshold, the agent must:

- mention it in the plan or final response
- explain why the size is justified
- explain why splitting is not better

If a frontend file is expected to exceed its **hard** threshold, the agent must:

- stop and propose a split before implementing, or
- split the file immediately into cohesive smaller files

If a frontend file would exceed **1000 lines**:

- do not proceed without explicit human approval
- propose a concrete split plan

## Routeable Smart Components

Distinguish two kinds of components.

### 1. Routeable smart / page components

Top-level screens or feature entry points that a user can open directly. They
should have **stable routes**.

Examples:

- Mushaf pages viewer
- Surahs page
- Words explorer
- Gates management / viewer
- Tafsir resources
- Audio resources
- Settings pages

### 2. Child / presentational components

Internal UI pieces used inside a page. They do **not** get routes by default.

Examples:

- side nav
- toolbar
- ayah card
- filter panel
- selected word panel
- modal / dialog
- table component
- reusable card / list component

### Rules

- Every routeable smart / page component must have a stable route.
- Child components must not get routes unless they become a real standalone screen.
- Do not create routes for every small component.
- Routeable smart components should stay focused and compose child components.
- Dynamic navigation must link to **route definitions**, not directly to component
  classes.
- Route labels can be changed by the owner / admin later, but route paths / keys
  should remain stable.
- Do not couple navbar text to component names.

### Navigation item example

```ts
{
  key: "mushaf-pages",
  labelAr: "صفحات المصحف",
  labelEn: "Mushaf Pages",
  route: "/mushaf/pages"
}
```

Clarifications:

- The owner / admin may rename the navigation label.
- The route remains the stable contract.
- Navigation configuration should point to route keys / paths, not component class
  names.

## Tabs and URL State

Important tabs that change the **main content** of a page must be represented in
the URL.

This allows:

- refresh to preserve the selected tab
- browser back / forward to work correctly
- sharing direct links to the same tab
- dynamic navigation shortcuts to open a specific tab
- future breadcrumbs / analytics / permissions to understand the user location

### Rules

- Do not keep important page tabs only in in-memory component state.
- Refreshing the page should return the user to the same important tab when
  practical.
- Internal tabs inside small modals or minor panels may remain local state if they
  are not meaningful as a direct URL.
- Use stable **tab keys**, not display labels.
- Arabic / English labels may change, but tab keys should stay stable.
- Avoid tab keys based on translated text.

### Choosing URL style by tab importance

#### 1. Child routes

Use child routes when each tab is a major section or meaningful destination.

Examples:

- `/words/ordered`
- `/words/unique`
- `/words/roots`
- `/gates/:id/ayahs`
- `/gates/:id/articles`
- `/roots/:root/ayahs`
- `/roots/:root/morphology`

#### 2. Query params

Use query params when the tab is a lighter view mode, display mode, or filter-like
state.

Examples:

- `/words?tab=unique-tashkeel`
- `/mushaf/pages/5?mode=study`
- `/translations?language=en&source=daryabadi`

#### 3. Local state only

Allowed only for minor UI states that do not need direct linking or refresh
preservation.

Examples:

- tabs inside a small modal
- temporary filter panel section
- purely visual toggle that does not represent a meaningful location

### Decision rule

- If the owner / admin may link to it from navigation, sidebar, shortcut, or saved
  menu item, it must have URL state.
- If the user would be frustrated after refresh because the tab resets, it should
  have URL state.
- If the tab represents a major page section, prefer a **child route**.
- If the tab represents a display mode / filter, prefer a **query param**.

## Route Structure Guidance

Rules:

- Keep routes readable and predictable.
- Prefer feature-owned route files when a feature grows.
- Lazy-load feature routes where appropriate.
- Do not hide business logic in route config.
- Route data may include stable metadata such as `titleKey`, `navKey`,
  `permissionKey` later.
- Do not put large navigation configuration directly inside components.
- Do not hardcode navbar items directly inside the navbar component when dynamic
  navigation is planned.

Example route metadata shape only:

```ts
{
  path: "mushaf/pages",
  loadComponent: () =>
    import("./features/mushaf/pages/mushaf-pages/mushaf-pages.component")
      .then(m => m.MushafPagesComponent),
  data: {
    navKey: "mushaf-pages",
    titleKey: "navigation.mushafPages"
  }
}
```

Note: this is an example shape only. Do not implement it now.

## Agent Behavior for New Screens

When adding a new frontend screen later, the agent must state:

- whether it is a routeable smart / page component or a child component
- its route if it is routeable
- whether it has important tabs
- how the tab state is represented in the URL
- whether navigation metadata is needed

If a feature has tabs but no URL state, the agent must explain why.

## Feature Folder Structure

Features must be organized by domain / feature, not by technical dumping folders.

Recommended feature structure:

```text
src/app/features/<feature>/
  pages/
  components/
  data-access/
  state/
  models/
  <feature>.routes.ts
```

Rules:

- Do not create empty folders in advance.
- Add a folder only when the feature actually needs it.
- `pages/` contains routeable smart / page components.
- `components/` contains feature-owned child / presentational components.
- `data-access/` contains API clients and backend communication for this feature.
- `state/` contains facade / store / state services for this feature.
- `models/` contains feature-owned frontend DTOs / view models / types when needed.
- `<feature>.routes.ts` contains feature-owned route definitions when the feature
  has multiple routes or child routes.
- Do not create global `components/` / `services/` / `models/` dumping folders.
- Truly reusable components may live in `shared/`, but only when they are genuinely
  reused across features.
- App-wide singleton services may live in `core/`, but `core` must not become a
  dumping ground.

Example:

```text
features/words/
  pages/
    words-explorer-page/
      words-explorer-page.component.ts
      words-explorer-page.component.html
      words-explorer-page.component.scss
  components/
    words-tabs/
    words-search-bar/
    ordered-words-list/
    unique-words-list/
    roots-list/
    word-card/
    root-card/
    word-detail-panel/
    morphology-summary/
    i3rab-preview/
  data-access/
    words.api.ts
  state/
    words-explorer.facade.ts
    words-explorer.store.ts
  models/
    words.models.ts
  words.routes.ts
```

This is an example shape only; do not implement it now.

## Smart Page Components as Shells

Routeable smart / page components must act as **page shells / orchestrators**.
They should not contain the entire feature UI, all tabs, all panels, all modals,
all state, and all API logic in one huge component.

Rules:

- A routeable smart / page component should compose smaller child components.
- It may read route params and query params.
- It may connect the page to a facade / store.
- It may coordinate page-level mode / tab selection.
- It may pass data into child components and handle output events.
- It must not perform direct API orchestration when a facade / store / data-access
  layer is appropriate.
- It must not contain large business / data transformation logic.
- It must not contain huge HTML templates for every tab / panel / modal.
- It must not own unrelated modals, filters, selections, audio state, display
  settings, and navigation all in one file.
- A full feature must not be implemented inside one routeable component.

If a page contains multiple major areas, tabs, panels, or workflows, split them
into:

- child components
- facade / store state services
- API / data-access services
- focused helpers or mappers

## Child and Presentational Components

Rules:

- Child components live under the owning feature's `components/` folder unless they
  are truly shared.
- They should focus on rendering one UI area or one reusable piece.
- They should receive data through inputs and emit user events through outputs
  where practical.
- They should not call backend API services directly.
- They should not read global route state directly unless that is explicitly part
  of their job.
- They should not know about navbar configuration or app-wide navigation unless
  they are navigation components.
- They should not duplicate shared `qd-` visual primitives.
- Complex child components can have their own smaller children if needed.

Examples:

- `ayah-card`
- `word-card`
- `selected-word-panel`
- `filter-panel`
- table component
- modal / dialog
- toolbar
- side nav

## Data Access and State Separation

Rules:

- API services in `data-access/` should mainly call HTTP endpoints and do minimal
  response mapping.
- API services must not own page state, selected tabs, filters, modal state, or
  complex UI workflows.
- Facade / store / state services in `state/` own feature / page state and
  orchestration.
- Facade / store services may call data-access services.
- Components should normally talk to the facade / store rather than directly
  orchestrating multiple API calls.
- Keep API DTOs, frontend view models, and UI state types clear and intentional.
- Avoid mixing API response shape directly with complex UI state when a view model
  is needed.

## Large Page Split Examples

Pages like **Words Explorer** and **Mushaf Reader** can become huge if not split
early.

### Example 1 — Words Explorer

Do not build one giant component that owns:

- ordered words
- unique words
- roots
- stems
- lemmas
- i3rab
- morphology
- filters
- search
- pagination
- selected word details
- all tab rendering

Recommended split:

```text
features/words/
  pages/words-explorer-page/
  components/words-tabs/
  components/words-search-bar/
  components/ordered-words-list/
  components/unique-words-list/
  components/roots-list/
  components/word-card/
  components/root-card/
  components/word-detail-panel/
  components/morphology-summary/
  components/i3rab-preview/
  data-access/words.api.ts
  state/words-explorer.facade.ts
  state/words-explorer.store.ts
  models/words.models.ts
  words.routes.ts
```

### Example 2 — Mushaf Reader

Do not build one giant component that owns:

- mushaf rendering
- toolbar
- page navigation
- word selection
- ayah selection
- audio playback
- display settings
- surah index modal
- ayah doors modal
- gates highlighting
- reader state
- API calls

Recommended split:

```text
features/mushaf/
  pages/mushaf-reader-page/
  components/mushaf-toolbar/
  components/mushaf-page-view/
  components/mushaf-line/
  components/mushaf-word/
  components/ayah-marker/
  components/selected-word-panel/
  components/selected-ayah-panel/
  components/reader-audio-controls/
  components/display-settings-popover/
  components/surah-index-modal/
  components/ayah-doors-modal/
  components/gates-highlight-panel/
  data-access/mushaf-pages.api.ts
  data-access/mushaf-audio.api.ts
  state/mushaf-reader.facade.ts
  state/mushaf-selection.store.ts
  state/mushaf-audio.store.ts
  state/mushaf-display.store.ts
  state/mushaf-gates.store.ts
  models/mushaf.models.ts
  mushaf.routes.ts
```

These are examples and not a command to implement now. They show how to avoid
oversized routeable components.

## Splitting Decision Rules

A smart / page component should be split when:

- it has multiple independent visual regions
- it has major tabs with different content
- it owns modals or panels that have their own behavior
- it has repeated template sections
- its HTML approaches the soft threshold
- its TypeScript approaches the soft threshold
- it mixes API loading, state management, rendering, and formatting
- a child section can be understood and tested independently
- the owner / admin may navigate to or configure part of it separately

Rules:

- Split by responsibility, not by arbitrary line count only.
- Prefer meaningful child components over tiny useless fragments.
- Do not create excessive micro-components for every small div.
- Do not move logic into generic helpers just to reduce line count; split around
  real responsibilities.
- Keep related files near the feature that owns them.

## Agent Behavior for Large Pages

When implementing a large page later, the agent must state:

- the routeable smart / page component
- child components created
- data-access services created
- facade / store services created
- where URL tab state lives
- why the split keeps each file under the review thresholds

If the agent chooses to keep a large page in one component, it must explicitly
justify why splitting is not better.

If the component would exceed the hard threshold, the agent must stop and propose a
split.
