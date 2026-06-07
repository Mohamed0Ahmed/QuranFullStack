# Feature Specification: Dashboard Layout & Foundation (Phase 0)

**Feature Branch**: `001-layout-foundation`
**Created**: 2026-06-07
**Status**: Draft
**Input**: User description: "Read Phase 0 from our plan docs/manhaj-qurani-layout-foundation-plan.md and, per GitHub Spec Kit best practices, create the spec (generation only). Implementation will be done by a cheaper model, so the specification must be super clear."

> **Clarity note for implementers**: This spec will be implemented by a less-capable model.
> Every requirement is written to be concrete, testable, and unambiguous. Where a choice was
> already made by the product owner it is recorded in **Locked Decisions** so the implementer
> never has to guess. Anything not stated here is **out of scope** for this phase.

---

## Clarifications

### Session 2026-06-07

- Q: Unknown / invalid route behavior → A: Redirect to the home page (`/dashboard`).
- Q: Footer health status refresh behavior → A: Fetch once on app load; manual retry on failure (no background polling).
- Q: Theme control states → A: Binary light ↔ dark toggle (no separate "system" mode in the control); OS preference governs only the initial state.
- Q: Home page overview cards → A: Mirror the 5 primary sections (excluding home): المصحف والآيات، الكلمات والجذور، التفاسير، الأبواب، المصادر.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Calm Arabic-first app shell (Priority: P1)

A curator (Arabic-speaking admin/teacher) opens the dashboard and immediately sees a calm,
scholarly, **right-to-left** workspace: a top navigation bar carrying the brand «المنهج
القرآني», and a footer, framing a main content area that shows a simple home/overview page.
The interface reads naturally in Arabic and does not look like a generic SaaS admin theme.

**Why this priority**: The shell is the visible foundation everything else lives inside.
Without it there is no product to navigate, theme, or extend. It is the minimum viable
deliverable on its own.

**Independent Test**: Launch the app; confirm it renders RTL with Arabic UI, shows the brand
wordmark in a top navbar and a footer, and displays a home page in the content area — with no
console errors and no horizontal scrolling.

**Acceptance Scenarios**:

1. **Given** the app is opened in a browser, **When** the home page loads, **Then** the page
   direction is right-to-left, the UI text is Arabic, and the brand «المنهج القرآني» appears
   in the top navbar.
2. **Given** the app is open, **When** the curator looks at the layout, **Then** they see
   exactly three shell regions — top navbar, main content area, footer — and **no** persistent
   left/right global navigation sidebar.
3. **Given** the home page is shown, **When** it renders, **Then** it presents a welcome
   heading, a short description, and overview cards linking to the main areas, with no
   fabricated counts or statistics.

---

### User Story 2 - Navigate sections from the top navbar (Priority: P2)

A curator uses the top navbar to move between the product's sections. Important sections are
visible directly in the bar; the rest are grouped under a «المزيد» (More) menu; Settings lives
in the user/actions area. Sections that are not built yet open a calm placeholder page instead
of an error. The current location is reflected in the address bar so refresh and back/forward
behave correctly.

**Why this priority**: Navigation makes the shell usable and establishes the stable routing
contract future features plug into. It depends on US1 but delivers independent value.

**Independent Test**: Click every navbar/More/Settings item; confirm each loads its route,
the active item is visually marked, the URL updates, refreshing keeps the same page, and
browser back/forward work.

**Acceptance Scenarios**:

1. **Given** the navbar is visible, **When** the curator selects a primary section, **Then**
   the content area shows that section's page and the address bar shows that section's stable
   route.
2. **Given** the curator opens the «المزيد» menu, **When** they choose a grouped section,
   **Then** that section's route loads the same way.
3. **Given** a section that is not implemented yet, **When** the curator opens it, **Then** a
   single shared placeholder page appears with calm, neutral wording (no "coming soon", no
   error), titled with that section's name.
4. **Given** the curator is on any section, **When** they refresh the browser or use
   back/forward, **Then** they remain on / return to the correct section.
5. **Given** the curator is on a section, **When** they view the navbar, **Then** the current
   section's nav item is shown as active.

---

### User Story 3 - Light / Dark theme for long focus (Priority: P3)

A curator working long review sessions switches between a light "parchment" theme and a dark
"ink" theme using a control in the top navbar. Their choice is remembered the next time they
open or refresh the app. Both themes are calm, readable, and restrained.

**Why this priority**: Comfort for long sessions is a product principle, but the app is usable
with a single default theme, so this ranks after navigation.

**Independent Test**: Toggle the theme; confirm the whole UI updates, the choice survives a
full page refresh, and both themes remain readable (sufficient contrast).

**Acceptance Scenarios**:

1. **Given** the app uses the default theme, **When** the curator activates the theme toggle,
   **Then** the entire interface switches to the other theme without a full reload.
2. **Given** the curator has chosen a theme, **When** they refresh or reopen the app, **Then**
   the previously chosen theme is applied.
3. **Given** the curator has never chosen a theme, **When** they first open the app, **Then**
   the theme follows their operating-system preference, defaulting to the light parchment theme
   if none is detectable.

---

### User Story 4 - Trustworthy live status and app metadata (Priority: P4)

The app proves it is genuinely connected to its backend: the home page shows real application
metadata (name, version, environment) and the footer shows a live service-health indicator.
When the backend cannot be reached, the app shows a calm, honest status instead of fabricating
data.

**Why this priority**: It validates the end-to-end data path the future features rely on and
reinforces trust ("structure you can trust"), but the shell and navigation work without it.

**Independent Test**: With the backend running, confirm real metadata and a healthy status
appear; stop the backend and confirm a calm error/unknown state appears with no invented
values.

**Acceptance Scenarios**:

1. **Given** the backend is available, **When** the home page loads, **Then** it displays the
   application name, version, and environment provided by the backend.
2. **Given** the backend is available, **When** the footer loads, **Then** it shows a live
   health indicator reflecting the backend's reported status, including whether the database
   dependency is healthy.
3. **Given** the backend is unreachable or returns an error, **When** the app requests status
   or metadata, **Then** the app shows a calm error/unknown state and never displays
   fabricated metadata or a false "healthy" status.

---

### User Story 5 - Consistent, safe backend boundary (Priority: P5)

Future feature work depends on a predictable, safe API. Every API response uses one consistent
envelope (success flag, human message, data, errors), user-facing messages are Arabic by
default, the health endpoint reports the database dependency, and no database credentials are
committed to source control.

**Why this priority**: This is foundational quality for everything that follows, but it is not
directly visible to a curator, so it ranks last among the slices.

**Independent Test**: Call the health and dashboard-info endpoints and confirm the consistent
envelope and Arabic default messages; trigger a server error and confirm the same envelope
shape with no leaked internal detail; inspect committed config and confirm no real password is
present.

**Acceptance Scenarios**:

1. **Given** any successful API call in this phase, **When** the response is inspected, **Then**
   it uses the envelope fields `isSuccess`, `message`, `data` (property names in English,
   message value in Arabic by default).
2. **Given** an unexpected server error, **When** the error response is inspected, **Then** it
   uses the same envelope with `isSuccess=false`, an Arabic safe message, and an `errors` list,
   and it does **not** leak stack traces, file paths, SQL, or connection details.
3. **Given** the health endpoint is called, **When** the database is reachable, **Then** the
   response reports overall and database-dependency health; **When** the database is
   unreachable, **Then** it reports an unhealthy database status without exposing connection
   details.
4. **Given** the committed configuration files, **When** they are inspected, **Then** they
   contain no real database password (a placeholder is used; the real value comes from local
   developer secrets or environment variables).

---

### Edge Cases

- **Backend down / slow**: Home metadata and footer status MUST show calm loading then
  error/unknown states; never a fabricated value or a false "healthy".
- **Unknown / mistyped route**: The app MUST redirect to the home page (`/dashboard`), never a
  blank screen, error, or crash.
- **Very small screens (down to 360px wide)**: The navbar MUST collapse into an accessible menu;
  there MUST be no horizontal scrolling and no clipped controls.
- **Long Arabic section labels**: Navbar and menu labels MUST remain readable and not break the
  layout.
- **No stored theme + no OS preference**: The app MUST fall back to the light parchment theme.
- **Reduced-motion preference**: Any transitions (e.g., theme change, menu open) MUST be
  minimized; motion is only ever used to convey state.
- **Database missing but API up**: Health MUST report a degraded/unhealthy database while the
  API itself still responds with the standard envelope.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Application shell & layout
- **FR-001**: The application MUST present a single app shell composed of exactly three regions:
  a top navigation bar, a main content area, and a footer.
- **FR-002**: The application MUST NOT include a persistent global navigation sidebar; any
  sidebars are reserved for page-specific contextual panels in future features.
- **FR-003**: The interface MUST be right-to-left and Arabic-first by default, with no visible
  left-to-right flash on initial load.
- **FR-004**: The shell MUST contain only layout/presentation concerns and MUST NOT embed
  feature/business logic.

#### Branding
- **FR-005**: The brand MUST appear as the typographic wordmark «المنهج القرآني» (no logo image
  this phase) in the top navbar and in the footer.
- **FR-006**: All user-facing application naming MUST use «المنهج القرآني»; internal code
  identifiers and namespaces are unaffected by this requirement.

#### Navigation & routing
- **FR-007**: The top navbar MUST provide navigation to all product sections, with primary
  sections shown directly, secondary sections grouped under a «المزيد» (More) menu, and Settings
  placed in the user/actions area (see **Locked Decisions** for the exact grouping).
- **FR-008**: Every section MUST have a **stable route path** that does not change when its
  display label changes; navigation MUST link to routes, not to component identities.
- **FR-009**: Selecting any navigation item MUST load that section's route into the main content
  area and update the browser address bar.
- **FR-010**: The currently active section MUST be visually indicated in the navigation.
- **FR-011**: Navigating MUST preserve location across browser refresh and back/forward.
- **FR-012**: Sections that are not yet implemented MUST render **one shared placeholder page**
  showing the section's name and calm, neutral wording (no "coming soon", no error styling). The
  exact placeholder text is in **Locked Decisions**.
- **FR-013**: The home/overview section MUST be a real page (not the placeholder), presenting a
  welcome heading, a short description, and exactly **5 overview cards** that mirror the primary
  navbar sections (excluding home) — المصحف والآيات (`/mushaf`), الكلمات والجذور (`/words`),
  التفاسير (`/tafsirs`), الأبواب (`/gates`), المصادر (`/resources`) — each linking to its route.
- **FR-036**: Any unknown or invalid route MUST redirect to the home page (`/dashboard`); the
  app MUST NOT show a blank screen, raw error, or crash.

#### Theming
- **FR-014**: The application MUST support two themes — a light "parchment" theme and a dark
  "ink" theme — switched from a **binary toggle** (light ↔ dark) in the top navbar. The control
  MUST NOT offer a separate "system/auto" mode (OS preference governs only the initial state per
  FR-017).
- **FR-015**: Switching themes MUST update the entire interface without a full page reload.
- **FR-016**: The chosen theme MUST persist across refreshes and re-opening the app.
- **FR-017**: With no previously chosen theme, the app MUST follow the OS preference, defaulting
  to light parchment if none is available.
- **FR-018**: Both themes MUST use the restrained "parchment & ink" visual direction (warm,
  low-chroma neutrals; one muted accent used sparingly; depth via tonal layering and hairlines,
  not heavy shadows); pure black and pure white MUST NOT be used as the base neutrals.

#### Style system & typography
- **FR-019**: Reusable visual patterns (e.g., page, card, button, badge, navbar, footer,
  loading/empty/error states, text styles) MUST be provided as a **centralized, shared style
  system** consumed by all screens, not re-created per screen.
- **FR-020**: All reusable shared UI classes MUST use the project prefix `qd-`.
- **FR-021**: Themeable values (colors, surfaces, text, borders, accent, radius, spacing, focus
  ring) MUST be defined as central design tokens that components reference; components MUST NOT
  hardcode their own palette.
- **FR-022**: Content/heading text MUST use a scholarly Arabic naskh face and UI chrome MUST use
  a calm Arabic UI sans face; both fonts MUST be self-hosted (no external runtime font fetch).
  Exact faces are in **Locked Decisions**.
- **FR-023**: Arabic text, including diacritics (tashkeel), MUST render correctly with
  comfortable line height for long reading.

#### Backend boundary & integration
- **FR-024**: All API responses in this phase MUST use a single consistent envelope with the
  English property names `isSuccess`, `message`, `data` for success and `isSuccess`, `message`,
  `errors` for failure.
- **FR-025**: User-facing API `message` values MUST be Arabic by default; the design MUST allow
  adding other languages later without restructuring.
- **FR-026**: The backend MUST expose an application-info capability returning the application
  name, version, and current environment.
- **FR-027**: The backend MUST expose a health capability that reports overall status and the
  database dependency's status.
- **FR-028**: Unexpected server errors MUST be converted centrally into the standard failure
  envelope with an Arabic safe message and MUST NOT leak stack traces, file paths, SQL, or
  connection details.
- **FR-029**: The frontend MUST consume the info and health capabilities to populate home
  metadata and the footer status, handling loading, success, and error states explicitly.
- **FR-030**: The frontend MUST NOT display any value it did not receive from the backend for
  metadata/status (no fabricated name, version, environment, counts, or "healthy" state).
- **FR-037**: Home metadata and the footer health status MUST be fetched once on app load; on
  failure the app MUST show an error state with a manual retry control. The app MUST NOT poll
  the backend automatically in the background.

#### Configuration & safety
- **FR-031**: No real database credentials MUST be committed to source control; committed
  configuration MUST use a placeholder, with the real value supplied via local developer secrets
  or environment variables, and the setup MUST be documented.
- **FR-032**: No Quranic text, ayah text, tafsir, translation, morphology, gate/topic names, or
  other religious content MUST be invented or displayed in this phase; missing data MUST be shown
  as a controlled state.

#### Accessibility & responsiveness
- **FR-033**: The layout MUST be responsive across desktop, tablet, and mobile; on small screens
  the navbar MUST collapse into an accessible menu with no horizontal overflow.
- **FR-034**: All interactive controls (nav items, More menu, theme toggle, mobile menu) MUST be
  keyboard operable with visible focus states and appropriate accessible labels.
- **FR-035**: Both themes MUST meet WCAG 2.1 AA contrast; meaning MUST NOT be conveyed by color
  alone; reduced-motion preferences MUST be respected.

### Key Entities

- **Navigation Item**: A selectable destination in the navbar. Attributes: stable key (English),
  Arabic label, English label, route path, and group (`primary` | `more` | `actions`).
- **App Metadata**: Backend-provided application descriptor. Attributes: application name,
  version, environment.
- **Health Status**: Backend-provided service status. Attributes: overall status and a set of
  dependency statuses (at least the database), without sensitive details.
- **API Response Envelope**: The shared response wrapper. Attributes: `isSuccess` (flag),
  `message` (Arabic-default human text), `data` (payload on success), `errors` (list on failure).
- **Theme Preference**: The user's selected theme (`light` | `dark`) and the rule for the initial
  value (stored choice → OS preference → light default).
- **Placeholder Section**: The single shared "not yet implemented" page, parameterized by the
  section's display name/title from its route.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On first load, the dashboard renders right-to-left with Arabic UI text and the
  «المنهج القرآني» wordmark, with **no** left-to-right flash and **zero** console errors.
- **SC-002**: A curator can reach **every** product section from the top navbar — primary
  sections in **1** click and «المزيد» sections in **2** clicks — and 100% of sections load a
  page (real or shared placeholder), never an error.
- **SC-003**: For any section, a browser refresh and back/forward return the curator to the
  correct section **100%** of the time.
- **SC-004**: The active section is visually indicated correctly for **100%** of sections.
- **SC-005**: Theme choice persists across a full page refresh **100%** of the time, and both
  themes pass WCAG 2.1 AA contrast on all foundation screens.
- **SC-006**: With the backend available, the home page shows the backend-provided application
  name, version, and environment, and the footer shows a live health status; **0** of these
  values are fabricated.
- **SC-007**: With the backend unavailable, the app shows a calm error/unknown state for
  metadata and status and displays **no** fabricated values and **no** false "healthy" status.
- **SC-008**: Every API response observed in this phase uses the single consistent envelope, and
  triggered server errors expose **no** internal details (stack trace, path, SQL, connection
  string).
- **SC-009**: At a viewport width of **360px**, there is **no** horizontal scrolling and all
  navigation controls remain reachable via an accessible menu.
- **SC-010**: All interactive controls are reachable and operable by keyboard alone, with a
  visible focus indicator on **100%** of them.
- **SC-011**: Committed configuration contains **no** real database password.
- **SC-012**: **No** Quranic or religious content is fabricated or displayed anywhere in the
  foundation.
- **SC-013**: Both the frontend production build and the backend build complete successfully with
  no errors.

---

## Locked Decisions

These choices were made by the product owner during planning. Implement them exactly; do not
re-decide them.

- **Phase scope**: Frontend foundation + light backend polish only. No Quran feature data.
- **Navigation model**: Top navbar is the primary navigation. No global sidebar. Shell = top
  navbar + main content + footer.
- **Navbar grouping** (stable route keys → Arabic label → route path):
  - **Primary (shown in the bar)**:
    - `dashboard` → لوحة التحكم → `/dashboard` (home; `/` redirects here)
    - `mushaf` → المصحف والآيات → `/mushaf`
    - `words` → الكلمات والجذور → `/words`
    - `tafsirs` → التفاسير → `/tafsirs`
    - `gates` → الأبواب → `/gates`
    - `resources` → المصادر → `/resources`
  - **«المزيد» (More) menu**:
    - `i3rab` → الإعراب → `/i3rab`
    - `translations` → الترجمات → `/translations`
    - `audio` → الصوتيات → `/audio`
    - `mutashabihat` → المتشابهات → `/mutashabihat`
  - **User/actions area**:
    - `settings` → الإعدادات → `/settings`
- **Built page vs placeholder**: Only `dashboard` (home) is a real page this phase. All other
  sections render the single shared placeholder page.
- **Placeholder copy** (Arabic): «سيتم ربط هذا القسم ضمن خطة الميزات التالية.» Title is the
  section's Arabic label. No "coming soon" wording, no error styling.
- **Fonts (self-hosted)**: Content/headings = **Amiri** (naskh); UI chrome = **IBM Plex Sans
  Arabic**.
- **Palette**: "Parchment & ink" direction with one muted accent (per the design context).
  Concrete color values are finalized during implementation and reviewed in the running app.
- **Default theme**: Light parchment.
- **Brand**: Typographic wordmark only (no logo image this phase).
- **API envelope**: `isSuccess`, `message`, `data`, `errors` (English property names; Arabic
  default messages). Server errors return this failure envelope (not a raw problem-details body).
- **Endpoints used this phase**: application info and health (overall + database dependency).
- **DB secrets**: Removed from committed config; supplied via local developer secrets /
  environment variables; documented.

---

## Assumptions

- The audience is Arabic-speaking admins/teachers doing focused, long-session curation work; the
  UI is Arabic-only for this phase (no end-user language switcher yet).
- An existing backend service and an existing frontend application already exist and are the
  basis for this work; this phase extends them rather than starting from zero.
- A database is already configured for local development; its schema stays empty in this phase
  (no feature tables, no migrations).
- No authentication, authorization, or user management is required in this phase; a user/actions
  area is present as a placeholder host for Settings only.
- "Version" for app metadata is a foundation value (e.g., an early pre-1.0 value); exact value is
  not user-critical and may be sourced from build/config.
- Network access to external font CDNs is not assumed; fonts are self-hosted.
- Success is validated by build success plus the manual/observable checks in Success Criteria;
  no automated test suite currently exists and adding one is not required by this spec.

## Out of Scope

The following are explicitly **not** part of this phase and MUST NOT be implemented here:

- Any Quran feature data or screens: words, ayahs, tafsir content, translations content, i'rab
  data, morphology, mushaf reader, gates/topics data.
- Create/update/delete operations and data import of resources into the database.
- Authentication, authorization, roles, or admin/user management.
- A full localization/internationalization system or runtime end-user language switching.
- Page-specific contextual sidebars (these arrive with their owning features later).
- A finalized logo/brand mark, advanced theme customization, and production deployment.
