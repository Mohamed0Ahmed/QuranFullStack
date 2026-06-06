# Frontend API Integration Guidelines

## Purpose and Scope

This document defines how the Angular frontend integrates with backend APIs:
`ApiResponse<T>`, loading / error / empty states, API services, facades / stores,
DTOs, and view models.

Read this file **before** creating or changing:

- frontend API services
- data-access files
- facade / store services that call APIs
- `ApiResponse<T>` handling
- loading / error / empty state handling for API-backed screens
- API DTOs and frontend view models
- pagination / filter / search API integration
- error / message display from backend responses

This file does **not** implement API integration. It defines how future API
integration must be organized.

For related rules, also read:

- `.architecture/FRONTEND_STRUCTURE.md` — feature folders, data-access/state
  separation, file size thresholds, tabs and URL state
- `.architecture/UI_STYLE_SYSTEM.md` — shared state primitives (`qd-loading-state`,
  `qd-empty-state`, `qd-error-state`) and visual rules
- `Backend/.architecture/API_GUIDELINES.md` — backend API boundary, the
  `ApiResponse` shape, and localization rules

For product and visual context: `../../PRODUCT.md`, `../../DESIGN.md`.

> Scope note: this is documentation/rules only. It does not create API services,
> data-access files, facades, stores, or components — it defines how that work must
> be done when it is explicitly requested.

## Default API Flow

The default flow is:

```text
Routeable Page Component
  → Facade / Store
    → API Service
      → Backend
```

Rules:

- Routeable smart / page components should not call API services directly by
  default.
- Child / presentational components must not call backend API services directly.
- Components should consume page-ready state from a facade / store.
- Components should send user events / actions to a facade / store.
- The facade / store owns orchestration, loading state, selected filters, selected
  tab, pagination, errors, and messages.
- API services own HTTP calls and minimal response mapping only.

Allowed exception — a very small routeable component may call an API service
directly **only if**:

- the endpoint is simple
- there is no shared state
- there is no filter / pagination / tab URL sync
- there is no complex loading / error behavior
- the agent explicitly explains why a facade / store is unnecessary

For Quran Dashboard features, prefer a facade / store for all substantial pages.

## API Services

Rules:

- API services live in feature data-access folders:

  ```text
  src/app/features/<feature>/data-access/
  ```

- Truly shared API infrastructure can live in `core/` only if it is app-wide.
- API services should be named by backend resource or feature, for example:

  ```text
  words.api.ts
  mushaf-pages.api.ts
  gates.api.ts
  ```

- API services should mainly:
  - call HTTP endpoints
  - type route / query / body parameters
  - return typed `ApiResponse<T>`
  - perform minimal response mapping if needed
- API services must not:
  - own page state
  - own selected tabs
  - own modal state
  - own complex UI workflows
  - decide which toast or UI message to show
  - perform large formatting / transformation pipelines
  - invent fallback Quranic data
- Split API services by feature / resource when they grow or start mixing unrelated
  endpoints.

## ApiResponse&lt;T&gt; Handling

Backend preferred shape (see `Backend/.architecture/API_GUIDELINES.md`):

Success:

```json
{
  "isSuccess": true,
  "message": "تمت العملية بنجاح",
  "data": {}
}
```

Failure:

```json
{
  "isSuccess": false,
  "message": "حدث خطأ",
  "errors": []
}
```

Frontend rule: API services return `Observable<ApiResponse<T>>` (or the project
equivalent typed async result).

The facade / store is responsible for:

- checking `isSuccess`
- reading `data`
- reading `message`
- reading `errors`
- deciding what becomes page data
- deciding what becomes an inline error
- deciding what becomes a toast / notification later
- preserving the backend message when useful
- converting the backend response into page-ready state

Components should not repeatedly unwrap raw `ApiResponse<T>`. Components should
receive page-ready state, for example:

```ts
{
  data: ...,
  isLoading: false,
  errorMessage: null,
  errors: [],
  isEmpty: false
}
```

Rules:

- Keep the `ApiResponse<T>` type consistent across API integration.
- Do not create multiple incompatible response wrappers per feature.
- Do not ignore backend failure messages silently.
- Do not assume `data` exists when `isSuccess` is false.
- Do not show raw technical errors to users.

## Facade / Store Responsibilities

Facade / store services live in:

```text
src/app/features/<feature>/state/
```

They own:

- API orchestration
- loading state
- empty state
- error state
- selected filters
- selected tab / mode
- pagination
- selected item
- refresh / retry actions
- mapping `ApiResponse<T>` into page-ready state
- URL state coordination when needed

They may call:

- data-access API services
- pure mappers / helpers near the feature

They must not:

- contain HTML / template logic
- directly manipulate the DOM
- define global styles
- become oversized stores with unrelated workflows
- own multiple unrelated feature areas in one file

If a facade / store approaches the size thresholds in `FRONTEND_STRUCTURE.md`:

- split by state slice or workflow
- for example: selection store, audio store, display store, gates store

## DTOs, View Models, and State Models

Rules:

- API DTOs represent backend response / request shapes.
- View models represent what the UI actually needs.
- UI state models represent loading / error / selection / filter state.
- Do not expose backend DTOs directly to complex UI templates when transformation
  is needed.
- Do not mix `ApiResponse<T>`, backend DTOs, and UI state into one unclear object.
- Keep models near the feature:

  ```text
  src/app/features/<feature>/models/
  ```

- Truly shared models must be genuinely cross-feature.
- Avoid generic global `models.ts` dumping files.

Examples:

- `MushafPageDto` — backend response shape.
- `MushafPageViewModel` — page-ready UI shape.
- `MushafReaderState` — loading / selection / audio / display state.

## Loading, Empty, and Error States

Rules:

- Every API-backed page must define loading, empty, and error behavior.
- Loading state should be owned by the facade / store.
- Empty state should be explicit and not confused with loading.
- Error state should preserve a useful backend message when safe.
- Error states should be calm and clear, not aggressive.
- Use shared UI style primitives from `UI_STYLE_SYSTEM.md`:

  ```text
  qd-loading-state
  qd-empty-state
  qd-error-state
  ```

- Do not leave pages blank during loading or failure.
- Do not silently swallow API failures.
- Do not fabricate Quranic data when data is missing.

## Messages and Notifications

Rules:

- Backend user-facing message values are localized; Arabic is the default.
- The frontend should preserve backend messages when useful.
- The facade / store decides whether a message is:
  - an inline page message
  - a field / form validation message
  - a toast / notification later
  - ignored because it is not relevant to the UI
- Components should not hardcode repeated success / error messages.
- Avoid scattering Arabic / English messages across components.
- If message keys / resources are introduced later, keep them feature-owned unless
  truly shared.
- Technical errors must not be shown raw to users.

## Pagination, Filters, Search, and URL State

Rules:

- Pagination state belongs in the facade / store.
- Filter / search state belongs in the facade / store.
- Important filter / search / tab state should be represented in the URL when
  refresh / back / share behavior matters.
- Follow the **Tabs and URL State** rules in `FRONTEND_STRUCTURE.md`.
- The API service receives typed query params; it does not own the filter UI.
- Components emit filter / search changes to the facade / store.
- Debouncing / search timing decisions belong in the facade / store or focused
  helpers, not inside large templates.
- Do not make each child component call the API independently for its own piece of
  the same page without a clear reason.

## HTTP Errors vs Backend Failure Responses

There are two failure categories:

1. **Backend-controlled failure response** — the HTTP request succeeds but
   `ApiResponse.isSuccess` is false. Examples:
   - validation failure
   - not found handled by backend
   - business conflict

2. **Transport / unexpected HTTP error** — the HTTP request itself fails. Examples:
   - network error
   - server unavailable
   - unhandled 500
   - timeout

Rules:

- The facade / store must handle both categories.
- Do not treat every HTTP 200 as success; check `ApiResponse.isSuccess`.
- Do not treat every failure as a crash.
- Keep user-facing error messages safe and localizable.
- Unexpected errors should produce a controlled page error state.
- Technical details should stay out of the UI.

## Quranic Data Safety in API Integration

Rules:

- Do not invent Quranic text, ayah text, word text, roots, tafsir, translations,
  i3rab, or gates in frontend fallback logic.
- Do not silently "fix" Quranic data in the frontend.
- If API data is missing, show a controlled missing / empty state.
- Any fallback label must be clearly UI-only and not presented as verified Quranic
  data.
- Preserve traceability where the backend provides source / resource metadata.
- Do not replace backend validation with frontend assumptions for Quranic data.

## Definition of Done for API Integration Changes

Any future API integration change should report:

- API services added / changed
- facade / store files added / changed
- DTO / view model / state model files added / changed
- endpoints consumed
- `ApiResponse<T>` handling
- loading / empty / error behavior
- URL state impact
- message / notification impact
- Quranic data safety impact
- build status
- test status if tests exist or were added
