# Phase 1 Data Model: Layout & Foundation

No persistent (database) entities are introduced this phase — `QuranDashboardDbContext` stays
empty. The "data model" here is the set of **transport and UI types** the foundation defines.
TypeScript shapes are the frontend contract; C# shapes are the backend contract. They MUST stay
in sync via the envelope contract in `contracts/`.

---

## 1. ApiResponse&lt;T&gt; (response envelope)

The single wrapper for every API response this phase.

| Field | Type | Present when | Notes |
|-------|------|--------------|-------|
| `isSuccess` | boolean | always | `true` for success, `false` for failure |
| `message` | string \| null | always (recommended) | Human, **Arabic by default** |
| `data` | T \| null | success | The payload; `null`/absent on failure |
| `errors` | string[] \| null | failure | List of error strings; `null`/absent on success |

- **Rule**: property names are English; only `message` is localized.
- **Rule**: a failure MUST NOT include fabricated `data`; a success MUST NOT include `errors`.

## 2. AppInfo (data of `GET /api/dashboard/info`)

| Field | Type | Rules |
|-------|------|-------|
| `appName` | string | MUST equal «المنهج القرآني» |
| `version` | string | App version (e.g. `"0.1.0"`); sourced from the running app, never invented |
| `environment` | string | Runtime environment name (e.g. `Development`) |

## 3. HealthStatus (data of `GET /api/health`)

| Field | Type | Rules |
|-------|------|-------|
| `status` | string enum: `healthy` \| `unhealthy` (\| `degraded`) | Overall aggregate status |
| `checks` | HealthCheckItem[] | MUST include the `database` check |

### HealthCheckItem

| Field | Type | Rules |
|-------|------|-------|
| `name` | string | e.g. `database` |
| `status` | string enum: `healthy` \| `unhealthy` (\| `degraded`) | Per-dependency status |

- **Rule**: output MUST NOT contain connection strings, hosts, credentials, SQL, or exception
  detail.

## 4. NavItem (frontend navigation config)

Defined once in `core/navigation/nav-items.ts`; drives navbar + routes.

| Field | Type | Rules |
|-------|------|-------|
| `key` | string (stable, English) | Unique; never changes when label changes |
| `labelAr` | string | Arabic display label |
| `labelEn` | string | English label (for future use / a11y) |
| `route` | string | Stable absolute route path (e.g. `/mushaf`) |
| `group` | enum: `primary` \| `more` \| `actions` | Placement in the navbar |

**Canonical list** (see `contracts/ui-navigation.md` for the full table):
`dashboard, mushaf, words, tafsirs, gates, resources` (primary) ·
`i3rab, translations, audio, mutashabihat` (more) · `settings` (actions).

## 5. ThemePreference (frontend, persisted)

| Aspect | Value |
|--------|-------|
| Type | enum: `light` \| `dark` |
| Storage | `localStorage` key `qd-theme` |
| Applied via | `data-theme` attribute on `<html>` |
| Default | light parchment |

**Resolution (on app init)**:

```text
stored qd-theme present?  → use it
else prefers-color-scheme: dark?  → "dark"
else  → "light"
```

**State transition**: `toggle()` flips `light ↔ dark`, updates `data-theme`, and writes
`localStorage`. There is no `system` state in the toggle (OS preference only seeds the initial
value).

## 6. PlaceholderRouteData (route `data` for the shared placeholder)

| Field | Type | Rules |
|-------|------|-------|
| `titleAr` | string | The section's Arabic label (shown as the placeholder title) |

The placeholder body text is fixed: «سيتم ربط هذا القسم ضمن خطة الميزات التالية.»

## 7. Design tokens (CSS custom properties)

Single source of truth in `_tokens.scss`; overridden per theme in `_themes.scss`. Components
reference these and never hardcode values. Concrete color values are chosen during
implementation (warm-tinted OKLCH; no pure black/white).

| Token | Category | Purpose |
|-------|----------|---------|
| `--qd-bg` | color | App background (parchment / ink) |
| `--qd-surface` | color | Cards/panels surface |
| `--qd-surface-elevated` | color | Raised surface (menus) |
| `--qd-text` | color | Primary text (ink / parchment) |
| `--qd-text-muted` | color | Secondary/meta text |
| `--qd-border` | color | Hairline dividers/borders |
| `--qd-accent` | color | The single muted accent (sparing use) |
| `--qd-danger` / `--qd-warning` / `--qd-success` | color | Status colors (calm) |
| `--qd-focus-ring` | color | Visible focus indicator |
| `--qd-radius-sm` / `-md` / `-lg` | radius | Corner radii |
| `--qd-space-1 … --qd-space-6` | spacing | Spacing scale |
| `--qd-shadow` / hairline | elevation | Used only as a state response, not ambient |

## 8. Reusable `qd-*` classes (UI contract surface)

Built this phase (full list in `contracts/ui-design-tokens.md`): `qd-shell`, `qd-navbar`,
`qd-container`, `qd-footer`, `qd-page`, `qd-page-header`, `qd-card`, `qd-btn` (+ `-primary`,
`-secondary`, `-ghost`), `qd-badge`, `qd-input`, `qd-empty-state`, `qd-loading-state`,
`qd-error-state`, and text classes `qd-page-title`, `qd-section-title`, `qd-card-title`,
`qd-text`, `qd-text-muted`, `qd-text-meta`.
