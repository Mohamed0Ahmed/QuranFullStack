# Playwright Browser-E2E Bootstrap — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give agents and humans a browser flow-test layer that exercises every existing public
surface of the dashboard, runnable headless as a gate and headed/`--ui` for inspection.

**Architecture:** Frontend-owned Playwright project at `Frontend/quran-dashboard-ui/`
(`playwright.config.ts` + `e2e/`), booting **both** servers via a dual `webServer` block — the
Angular dev server on `https://localhost:4200` and the backend `https` launch profile on
`https://localhost:5015`, gated on `GET /api/health`. Specs are named `*.e2e.ts` so the Vitest
glob (`**/*.spec.ts`) never picks them up. The Logto IdP origin is stubbed per-page so a run makes
zero external network calls.

**Tech Stack:** `@playwright/test` (chromium only), Angular 20 dev server, .NET 10 API, local
PostgreSQL `quran_dashboard`, mkcert localhost certificates.

---

## Objective

One command runs browser flow tests across every public surface that exists today:
dashboard home, the Mushaf reader (incl. its tafsir / translation / full-إعراب / similar-ayahs /
متشابهات cards), the words hub, the five explorers, and the placeholder routes. Headless is the
default gate; `--headed` and `--ui` are the inspection modes.

## Scope

- Install `@playwright/test` + the chromium browser binary.
- `playwright.config.ts` with a dual `webServer` (Angular serve + backend https profile).
- The flow specs listed in **Flow inventory** below.
- Exactly three `data-testid` additions: navbar links, Mushaf prev/next buttons, reader page root.
  **Nothing else** — every other flow uses selectors that already exist.
- npm scripts: `e2e`, `e2e:headed`, `e2e:ui`.
- `.gitignore`: `playwright-report/`, `blob-report/`, `.playwright/`.
- Doc updates **in the same change**: `TESTING_STRATEGY.md` §3 Tier E rewrite + §4/§6 wiring
  (opt-in local gate, **not** a Tier C blocker), `Frontend/quran-dashboard-ui/CLAUDE.md:35`,
  `Frontend/quran-dashboard-ui/AGENTS.md:35`, Frontend `README.md` §Testing, root `CLAUDE.md`
  test-selection paragraph.

## Non-goals

- No auth flows, no sign-in, no token handling.
- No write flows — every assertion is over read-only GET data.
- No backend test host (`WebApplicationFactory`); this is not the §13 smoke tier and does not
  restore it. **§13 stays untouched.**
- No CI. No firefox/webkit. No mobile viewports.
- No application-code changes beyond the three testids: `devApiLatencyMs` stays 450 in dev, the
  environment files are untouched, no e2e-only build configuration.
- No exact-count assertions against the live DB.

## Locked decisions

| Decision | Value |
|---|---|
| Logto | stubbed via `page.route` on the IdP origin (discovery + jwks), zero external calls |
| Browsers | chromium only |
| Headed | headless default, `--headed` / `--ui` opt-in scripts |
| Retries | `0`; trace + screenshot on failure; video off |
| Servers | `reuseExistingServer: true` for both; backend readiness = `GET https://localhost:5015/api/health` (503 when the DB is down ⇒ boot fails loud) |
| TLS | `ignoreHTTPSErrors: true` (both servers are self-signed localhost) |
| Data | live local dev DB, read-only flows, loose count assertions. A dump-restored isolated DB is a recorded future step, not this feature |
| Spec naming | `*.e2e.ts`, never `*.spec.ts` |
| Types | own `e2e/tsconfig.e2e.json` with `@playwright/test` types |
| Location | `Frontend/quran-dashboard-ui/e2e/` + `playwright.config.ts`, cross-stack backend boot documented |
| Workers | `2` |
| Backend env | `Development` via the `https` launch profile — tradeoff accepted and documented |
| Timeouts | generous, for the 450 ms dev latency. Zeroing that latency is a **follow-up**, not this feature |

## Session-storage hygiene

**Every test gets a fresh browser context. Never introduce `storageState` reuse in this suite.**

Three stores would otherwise leak between tests:

- `qd-mushaf-reader-session` (sessionStorage, `src/app/features/mushaf/state/mushaf-reader-session.ts:17`)
  restores the last page / selected ayah / selected word whenever the reader is entered with no
  reader-owned query params (`isBareMushafEntry`, same file `:28-30`). A dirty session silently
  changes what `/dashboard/mushaf` shows, so "opens on page 1" would pass or fail depending on test
  order.
- `qd-theme` (localStorage, `src/app/core/theme/theme.service.ts:53`) decides `data-theme`.
- the words-explainer collapsed set (localStorage, `src/app/features/words/state/words-explainer-preference.ts:36`).

Playwright's default isolation (one context per test) resets all three. This is a stated invariant,
not an accident — record it in `e2e/README.md` (Task 4.5).

## Flow inventory

Selectors marked **NEW** are the three additions in Phase 2; everything else exists today.

| Spec file | Flow | Key selectors |
|---|---|---|
| `dashboard-home.e2e.ts` | home renders; `<html>` is `lang=ar dir=rtl` with a `data-theme` | `getByRole('heading')`, `html[dir]`, `html[lang]`, `html[data-theme]` |
| `shell-nav.e2e.ts` | navbar link → mushaf; navbar link → words hub | `nav-link--mushaf`, `nav-link--words` **NEW** |
| `mushaf-reader.e2e.ts` | reader opens on page 1; next→prev round-trip; `?page=5` deep-link hydration; reader `dir="rtl"`; Amiri on Quran text; Uthmani glyph testids | `mushaf-reader-page` **NEW**, `mushaf-next-page` / `mushaf-prev-page` **NEW**, `mushaf-page-area`, `mushaf-page-view`, `mushaf-page-jump-trigger`, `mushaf-page-surah-glyph`, `mushaf-page-juz-glyph` |
| `mushaf-ayah-study.e2e.ts` | click word → tafsir card; translation tab; full-إعراب tab; similar-ayahs tab; متشابهات tab; tafsir source switch | `[data-word-location]`, `selected-ayah-section`, `tafsir-card`/`tafsir-empty`, `translation-card`/`translation-empty`, `full-i3rab-card`/`full-i3rab-empty`, `ayah-tab-similar-ayahs`, `similar-ayahs-list`, `ayah-tab-mutashabihat`, `mutashabihat-groups-list`/`mutashabihat-empty`, `source-selector-trigger`, `source-selector-source-row` |
| `mushaf-word-analysis.e2e.ts` | click word → morphology summary + identity links; surah jump picker | `selected-word-section`, `word-morphology-summary`, `word-identity-summary`, `surah-jump-picker-trigger`, `surah-jump-picker-search`, `surah-jump-picker-row` |
| `words-hub.e2e.ts` | hub renders 5 cards; a card navigates | `words-hub-title`, `words-hub-card--*` |
| `words-explorers.e2e.ts` | one flow per explorer (roots, lemmas, stems, types, unique): open → search → click row → details panel shows the entity | `*-explorer-page-title`, `*-search-input`, `roots-table-root-button`, `lemmas-table-lemma-button`, `stems-table-stem-button`, `unique-words-table-word-button`, `[data-word-types-row]`, `*-details-panel-entity`, `word-drilldown-entity` |
| `placeholder-routes.e2e.ts` | `/mutashabihat` renders its placeholder sentence | heading text + `سيتم ربط هذا القسم ضمن خطة الميزات التالية.` |

**Why the study cards are driven through the reader:** `/tafsirs`, `/i3rab`, `/translations`,
`/mutashabihat` are placeholder routes (`src/app/app.routes.ts:10-19`,
`src/app/shared/ui/placeholder-page/placeholder-page.component.html:7`). The real tafsir /
translation / full-إعراب / متشابهات surfaces are cards inside the Mushaf reader, reached by
selecting an ayah. A single click on a non-marker word emits **both** `ayahSelect` and `wordSelect`
(`src/app/features/mushaf/components/mushaf-word/mushaf-word.component.ts:39-40`), so one click
opens both the ayah study and the word analysis.

**Why the ayah tabs are index-selected:** the tafsir / translation / full-إعراب tabs carry no
testid (`selected-ayah-section.component.html:31-63`) and adding some is out of the locked scope.
They are `role="tab"` children of `nav[aria-label="تبويبات دراسة الآية"]` in fixed order —
tafsir(0), translation(1), full-i3rab(2), similar-ayahs(3), mutashabihat(4) — and the same state is
reachable through the documented URL contract (`ayahTab=` in
`src/app/features/mushaf/models/mushaf.models.ts:223-235`). Specs use the URL for setup and one
click-through test for the tab bar itself.

## Runtime budget

Every API response is delayed 450 ms in dev (`src/environments/environment.development.ts:7`,
applied by `src/app/core/data-access/dev-latency.interceptor.ts`).

| | Target |
|---|---|
| Per simple test (home, hub, placeholder) | ≤ 5 s |
| Per reader / explorer test (3–8 chained requests) | 8–20 s |
| Full suite, servers already running (warm) | **≤ 4 min** |
| Full suite, cold start (Angular first build + backend boot) | **≤ 6 min** |
| Test count at completion | 28 tests across 8 files |

Config consequences: `timeout: 60_000` per test, `expect.timeout: 15_000`, Angular `webServer`
timeout `180_000`, backend `webServer` timeout `120_000`.

## Risks and stop conditions

| Risk | Detection | Stop condition |
|---|---|---|
| Logto stub leaks — a real request reaches `a8kvwi.logto.app` or any non-localhost host | the shared fixture records every request and asserts the leak list is empty after each test | any spec fails with `requests left localhost:` → fix the stub before adding more specs |
| App fails to bootstrap behind the stub (`withAppInitializerAuthCheck`, `src/app/app.config.ts:46`, blocks boot on OIDC discovery) | Task 1.6 renders the dashboard heading | if the heading never appears, the stub's discovery document is wrong — fix it there; do **not** proceed to Phase 2 |
| Vitest glob swallows e2e files (`angular.json` → `test.options.include = ["**/*.spec.ts"]`, project-root-relative) | `npm test` file/test count compared against the 169-file / 1,938-test baseline | any change in the file count → rename the offending file to `*.e2e.ts` |
| Angular dev-server cold start exceeds the wait | Playwright reports `Timed out waiting … 4200` | raise the Angular `webServer.timeout`; do not lower it below 180 s |
| mkcert certificates absent — `npm run start:https` dies and Playwright reports only a port timeout | `e2e/README.md` documents the prerequisite; Task 1.3 adds a `pretest`-style guard message | missing `localhost.pem` must fail with the mkcert instruction, never with a bare timeout |
| Backend up but DB unreachable | `/api/health` returns 503 (`Controllers/System/HealthController.cs:29-41`); Playwright only accepts <400 → boot fails loud | fix the DB/user-secrets before re-running; never weaken the readiness URL to `/swagger` |
| Live-DB count drift after a reseed | loose assertions only (`> 0`, regex, visibility) | any spec asserting an exact row count is a defect — rewrite it |

## Acceptance criteria

1. `npm run e2e` is green from a **cold start** (no servers running) — both servers boot, suite passes.
2. `npm run e2e` is green with **both servers already running** (reuse path).
3. `npm test` (Vitest) reports the **same file and test count** as before the change (169 files / 1,938 tests baseline, `TESTING_STRATEGY.md:23`).
4. `npm run e2e:headed` demonstrably opens a browser window.
5. Zero external network calls during a run — enforced by the fixture, not by inspection.
6. `TESTING_STRATEGY.md` describes the new reality: §3 Tier E no longer claims a browser E2E layer is absent, §4/§6 carry the new command, and the tier is documented as **opt-in**, not a Tier C blocker.
7. `Frontend/quran-dashboard-ui/CLAUDE.md`, `AGENTS.md`, `README.md`, and root `CLAUDE.md` no longer state that no browser E2E layer exists.

---

## File structure

**Created**

```
Frontend/quran-dashboard-ui/playwright.config.ts        dual webServer, chromium project, timeouts
Frontend/quran-dashboard-ui/e2e/tsconfig.e2e.json       @playwright/test types, isolated from tsconfig.spec.json
Frontend/quran-dashboard-ui/e2e/README.md               prerequisites, commands, invariants
Frontend/quran-dashboard-ui/e2e/fixtures/logto.ts       IdP route stub (discovery + jwks)
Frontend/quran-dashboard-ui/e2e/fixtures/app-test.ts    shared `test` — applies the stub, asserts no external requests
Frontend/quran-dashboard-ui/e2e/dashboard-home.e2e.ts
Frontend/quran-dashboard-ui/e2e/shell-nav.e2e.ts
Frontend/quran-dashboard-ui/e2e/mushaf-reader.e2e.ts
Frontend/quran-dashboard-ui/e2e/mushaf-ayah-study.e2e.ts
Frontend/quran-dashboard-ui/e2e/mushaf-word-analysis.e2e.ts
Frontend/quran-dashboard-ui/e2e/words-hub.e2e.ts
Frontend/quran-dashboard-ui/e2e/words-explorers.e2e.ts
Frontend/quran-dashboard-ui/e2e/placeholder-routes.e2e.ts
docs/feature-playwright-bootstrap/plan.md               this file
```

**Modified**

```
Frontend/quran-dashboard-ui/package.json                        devDependency + 3 scripts
.gitignore                                                      playwright-report/, blob-report/, .playwright/
Frontend/quran-dashboard-ui/src/app/core/layout/top-navbar/top-navbar.component.html
Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-header-navigation/mushaf-header-navigation.component.html
Frontend/quran-dashboard-ui/src/app/features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.html:1
TESTING_STRATEGY.md                                             §3 Tier E, §4, §6
Frontend/quran-dashboard-ui/CLAUDE.md:35
Frontend/quran-dashboard-ui/AGENTS.md:35
Frontend/quran-dashboard-ui/README.md                           §Testing
CLAUDE.md                                                       test-selection paragraph
```

One responsibility per spec file; fixtures hold the cross-cutting stub so no spec repeats it.

---

# Phase 1 — Foundation and the dual-server proof

Ends with one green spec that proves both servers boot and the Logto stub does not break bootstrap.

### Task 1.1: Install Playwright and chromium

**Files:**
- Modify: `Frontend/quran-dashboard-ui/package.json`

- [ ] **Step 1: Install the test runner**

```bash
cd Frontend/quran-dashboard-ui
npm install --save-dev @playwright/test
```

- [ ] **Step 2: Install the chromium binary only**

```bash
cd Frontend/quran-dashboard-ui
npx playwright install chromium
```

Expected: chromium downloads (or reports it is already installed). Do **not** pass
`--with-deps` (it runs `sudo apt-get`); if chromium reports missing system libraries, stop and
report the exact package list rather than installing anything.

- [ ] **Step 3: Add the three scripts**

In `package.json`, inside `"scripts"`, after the existing `"test"` entry:

```json
    "e2e": "playwright test",
    "e2e:headed": "playwright test --headed",
    "e2e:ui": "playwright test --ui",
```

- [ ] **Step 4: Verify the runner resolves**

```bash
cd Frontend/quran-dashboard-ui
npx playwright --version
```

Expected: a version line, e.g. `Version 1.x.x`.

### Task 1.2: Ignore Playwright output

**Files:**
- Modify: `.gitignore` (repo root)

- [ ] **Step 1: Append the three entries**

After the existing `Frontend/quran-dashboard-ui/test-results/` line:

```gitignore
Frontend/quran-dashboard-ui/playwright-report/
Frontend/quran-dashboard-ui/blob-report/
Frontend/quran-dashboard-ui/.playwright/
```

- [ ] **Step 2: Verify**

```bash
cd /projects/Dashboard/App
git check-ignore -v Frontend/quran-dashboard-ui/playwright-report/index.html
```

Expected: a line naming `.gitignore` and the new pattern.

### Task 1.3: Write the Playwright config

**Files:**
- Create: `Frontend/quran-dashboard-ui/playwright.config.ts`

- [ ] **Step 1: Write the config**

```ts
import { defineConfig, devices } from '@playwright/test';

const UI_ORIGIN = 'https://localhost:4200';
const API_HEALTH_URL = 'https://localhost:5015/api/health';

// Both servers are self-signed localhost, and the backend CORS policy admits exactly
// https://localhost:4200 (Backend/api/QuranDashboard.Api/appsettings.Development.json), so neither
// origin nor port is configurable here.
export default defineConfig({
  testDir: './e2e',
  testMatch: /.*\.e2e\.ts$/,
  fullyParallel: true,
  workers: 2,
  retries: 0,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: UI_ORIGIN,
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'npm run start:https',
      url: UI_ORIGIN,
      cwd: __dirname,
      reuseExistingServer: true,
      ignoreHTTPSErrors: true,
      timeout: 180_000,
      stdout: 'pipe',
      stderr: 'pipe',
    },
    {
      // /api/health is DbContext-backed and answers 503 when the database is unreachable, which
      // Playwright refuses to accept as ready — a broken DB fails the boot instead of producing a
      // suite of red UI tests.
      command:
        'dotnet run --project ../../Backend/api/QuranDashboard.Api --launch-profile https --no-build',
      url: API_HEALTH_URL,
      cwd: __dirname,
      reuseExistingServer: true,
      ignoreHTTPSErrors: true,
      timeout: 120_000,
      stdout: 'pipe',
      stderr: 'pipe',
    },
  ],
});
```

- [ ] **Step 2: Verify it parses and finds no tests yet**

```bash
cd Frontend/quran-dashboard-ui
npx playwright test --list
```

Expected: `Total: 0 tests in 0 files` (no config error).

### Task 1.4: Isolate e2e TypeScript from the Vitest tsconfig

**Files:**
- Create: `Frontend/quran-dashboard-ui/e2e/tsconfig.e2e.json`

- [ ] **Step 1: Write the tsconfig**

```json
{
  "extends": "../tsconfig.json",
  "compilerOptions": {
    "outDir": "../out-tsc/e2e",
    "module": "preserve",
    "moduleResolution": "bundler",
    "types": ["@playwright/test", "node"]
  },
  "include": ["**/*.ts", "../playwright.config.ts"]
}
```

Why a separate file: `tsconfig.spec.json` declares `types: ["vitest/globals"]` and includes only
`src/**/*.spec.ts`. Vitest's and Playwright's globals both declare `test` and `expect`; keeping the
two `include` sets disjoint keeps them from colliding.

- [ ] **Step 2: Verify it type-checks (no files yet is fine)**

```bash
cd Frontend/quran-dashboard-ui
npx tsc -p e2e/tsconfig.e2e.json --noEmit
```

Expected: exit 0.

### Task 1.5: Stub the Logto origin

**Files:**
- Create: `Frontend/quran-dashboard-ui/e2e/fixtures/logto.ts`
- Create: `Frontend/quran-dashboard-ui/e2e/fixtures/app-test.ts`

- [ ] **Step 1: Write the IdP stub**

`e2e/fixtures/logto.ts`:

```ts
import type { Page } from '@playwright/test';

// src/app/app.config.ts installs withAppInitializerAuthCheck(), so the app performs OIDC discovery
// against this origin before it renders — every page load, even though no flow here signs in.
// Serving a static discovery document locally keeps the boot path deterministic and offline.
export const LOGTO_ORIGIN = 'https://a8kvwi.logto.app';

const DISCOVERY_DOCUMENT = {
  issuer: `${LOGTO_ORIGIN}/oidc`,
  authorization_endpoint: `${LOGTO_ORIGIN}/oidc/auth`,
  token_endpoint: `${LOGTO_ORIGIN}/oidc/token`,
  userinfo_endpoint: `${LOGTO_ORIGIN}/oidc/me`,
  jwks_uri: `${LOGTO_ORIGIN}/oidc/jwks`,
  end_session_endpoint: `${LOGTO_ORIGIN}/oidc/session/end`,
  response_types_supported: ['code'],
  subject_types_supported: ['public'],
  id_token_signing_alg_values_supported: ['ES384'],
  scopes_supported: ['openid', 'offline_access', 'profile', 'email'],
  token_endpoint_auth_methods_supported: ['none'],
  code_challenge_methods_supported: ['S256'],
  grant_types_supported: ['authorization_code', 'refresh_token'],
};

export async function stubLogto(page: Page): Promise<void> {
  await page.route(`${LOGTO_ORIGIN}/**`, async (route) => {
    const path = new URL(route.request().url()).pathname;

    if (path.endsWith('/.well-known/openid-configuration')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(DISCOVERY_DOCUMENT),
      });
      return;
    }

    if (path.endsWith('/jwks')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ keys: [] }),
      });
      return;
    }

    await route.fulfill({ status: 404, contentType: 'application/json', body: '{}' });
  });
}
```

- [ ] **Step 2: Write the shared test fixture**

`e2e/fixtures/app-test.ts`:

```ts
import { test as base, expect } from '@playwright/test';

import { LOGTO_ORIGIN, stubLogto } from './logto';

export const test = base.extend({
  page: async ({ page }, use) => {
    const leaked: string[] = [];

    await stubLogto(page);

    page.on('request', (request) => {
      const url = request.url();
      if (url.startsWith('data:') || url.startsWith('blob:')) {
        return;
      }
      if (url.startsWith(LOGTO_ORIGIN)) {
        return;
      }
      if (new URL(url).hostname === 'localhost') {
        return;
      }
      leaked.push(url);
    });

    await use(page);

    expect(leaked, `requests left localhost: ${leaked.join(', ')}`).toEqual([]);
  },
});

export { expect } from '@playwright/test';
```

- [ ] **Step 3: Type-check**

```bash
cd Frontend/quran-dashboard-ui
npx tsc -p e2e/tsconfig.e2e.json --noEmit
```

Expected: exit 0.

### Task 1.6: First spec — dashboard home (proves the whole boot path)

**Files:**
- Create: `Frontend/quran-dashboard-ui/e2e/dashboard-home.e2e.ts`

- [ ] **Step 1: Write the failing spec**

```ts
import { expect, test } from './fixtures/app-test';

test('dashboard home renders the Arabic welcome heading and the surface cards', async ({ page }) => {
  await page.goto('/dashboard');

  await expect(
    page.getByRole('heading', { name: 'مرحباً بك في المنهج القرآني', level: 1 }),
  ).toBeVisible();
  await expect(page.getByRole('link', { name: 'المصحف والآيات' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'الكلمات والجذور' })).toBeVisible();
});

test('the document is Arabic, RTL and themed before paint', async ({ page }) => {
  await page.goto('/dashboard');

  const html = page.locator('html');
  await expect(html).toHaveAttribute('lang', 'ar');
  await expect(html).toHaveAttribute('dir', 'rtl');
  await expect(html).toHaveAttribute('data-theme', /^(light|dark)$/);
});

test('the root path redirects to the dashboard', async ({ page }) => {
  await page.goto('/');

  await expect(page).toHaveURL(/\/dashboard$/);
});
```

- [ ] **Step 2: Build the backend once (required by `--no-build`)**

```bash
cd /projects/Dashboard/App
dotnet build Backend/QuranDashboard.sln
```

Expected: `Build succeeded`.

- [ ] **Step 3: Run the spec cold — no servers running**

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm run e2e
```

Expected: both servers boot, `3 passed`. Cold run ≤ 3 min.

If the heading never appears: the app is stuck in `withAppInitializerAuthCheck`. Print the browser
console with `npm run e2e -- --headed --debug` and correct `DISCOVERY_DOCUMENT` in
`e2e/fixtures/logto.ts` until the dashboard renders. **Do not start Phase 2 until this passes.**

- [ ] **Step 4: Run it again with the servers still up (reuse path)**

Leave `qd-api` and `qd-ui` running in two terminals, then:

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm run e2e
```

Expected: `3 passed`, no server-boot lines, ≤ 30 s.

- [ ] **Step 5: Verify Vitest is unaffected**

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm test
```

Expected: **169 spec files, 1,938 tests**, unchanged from the `TESTING_STRATEGY.md:23` baseline.
Any other file count means the e2e files were swallowed by the `**/*.spec.ts` glob — stop and
rename.

- [ ] **Step 6: Commit**

```bash
cd /projects/Dashboard/App
git add Frontend/quran-dashboard-ui/package.json Frontend/quran-dashboard-ui/package-lock.json \
  Frontend/quran-dashboard-ui/playwright.config.ts Frontend/quran-dashboard-ui/e2e .gitignore
git commit -m "test(e2e): bootstrap Playwright with dual webServer and a dashboard smoke"
```

---

# Phase 2 — Test hooks and the Mushaf reader flows

### Task 2.1: Add the navbar link testids

**Files:**
- Modify: `Frontend/quran-dashboard-ui/src/app/core/layout/top-navbar/top-navbar.component.html`
- Create: `Frontend/quran-dashboard-ui/e2e/shell-nav.e2e.ts`

Only the desktop nav is touched. The mobile drawer is behind `@if (mobileOpen)` (same file,
`:217`), so at the desktop viewport it is absent from the DOM and cannot produce duplicate testids.

- [ ] **Step 1: Write the failing spec**

`e2e/shell-nav.e2e.ts`:

```ts
import { expect, test } from './fixtures/app-test';

test('the navbar links reach the Mushaf reader', async ({ page }) => {
  await page.goto('/dashboard');

  await page.getByTestId('nav-link--mushaf').click();

  await expect(page).toHaveURL(/\/dashboard\/mushaf/);
  await expect(page.getByTestId('mushaf-page-area')).toBeVisible();
});

test('the words dropdown reaches the words hub', async ({ page }) => {
  await page.goto('/dashboard');

  await page.getByTestId('nav-words-trigger').click();
  await page.locator('#words-menu').getByRole('link', { name: 'الرئيسية' }).click();

  await expect(page).toHaveURL(/\/dashboard\/words$/);
  await expect(page.getByTestId('words-hub-title')).toBeVisible();
});

test('the more dropdown reaches a placeholder section', async ({ page }) => {
  await page.goto('/dashboard');

  await page.getByTestId('nav-more-trigger').click();
  await page.getByTestId('nav-menu-link--mutashabihat').click();

  await expect(page).toHaveURL(/\/mutashabihat$/);
});
```

- [ ] **Step 2: Run it to verify it fails**

```bash
cd Frontend/quran-dashboard-ui
npm run e2e -- e2e/shell-nav.e2e.ts
```

Expected: 3 failures, each timing out on a `getByTestId` locator.

- [ ] **Step 3: Add `data-testid` to the words dropdown trigger**

In `top-navbar.component.html`, the `<button>` opening at `:15`, after `[attr.aria-expanded]="wordsOpen"`:

```html
            [attr.aria-expanded]="wordsOpen"
            data-testid="nav-words-trigger"
```

- [ ] **Step 4: Add `data-testid` to the primary nav links**

Same file, the `<a>` at `:63`, after the `[routerLinkActiveOptions]` line:

```html
            [routerLinkActiveOptions]="{ exact: item.route === '/dashboard' }"
            [attr.data-testid]="'nav-link--' + item.key"
```

- [ ] **Step 5: Add `data-testid` to the more trigger and its menu links**

Same file, the more-dropdown `<button>` at `:75`, after `[attr.aria-expanded]="moreOpen"`:

```html
        [attr.aria-expanded]="moreOpen"
        data-testid="nav-more-trigger"
```

and the more-menu `<a>` at `:107`, after `routerLinkActive="active"`:

```html
                routerLinkActive="active"
                [attr.data-testid]="'nav-menu-link--' + item.key"
```

`NavItem.key` exists for every entry (`src/app/core/navigation/nav-items.ts:2-7`). The words
submenu is intentionally left without testids — `WordsNavItem` has no `key`
(`src/app/core/navigation/words-nav-items.ts:10-13`), and the spec selects those links by role and
Arabic label inside `#words-menu`.

- [ ] **Step 6: Run the spec to verify it passes**

```bash
cd Frontend/quran-dashboard-ui
npm run e2e -- e2e/shell-nav.e2e.ts
```

Expected: `3 passed`.

- [ ] **Step 7: Run the shell unit specs (Tier A — shared layout changed)**

```bash
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/core/layout/**/*.spec.ts"
```

Expected: all pass.

- [ ] **Step 8: Commit**

```bash
cd /projects/Dashboard/App
git add Frontend/quran-dashboard-ui/src/app/core/layout/top-navbar/top-navbar.component.html \
  Frontend/quran-dashboard-ui/e2e/shell-nav.e2e.ts
git commit -m "test(e2e): navbar navigation flow with stable link testids"
```

### Task 2.2: Add the reader testids and cover paging + deep linking

**Files:**
- Modify: `Frontend/quran-dashboard-ui/src/app/features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.html:1`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-header-navigation/mushaf-header-navigation.component.html:5-20`
- Create: `Frontend/quran-dashboard-ui/e2e/mushaf-reader.e2e.ts`

- [ ] **Step 1: Write the failing spec**

`e2e/mushaf-reader.e2e.ts`:

```ts
import { expect, test } from './fixtures/app-test';

test('the reader opens on page 1 and renders the Mushaf page', async ({ page }) => {
  await page.goto('/dashboard/mushaf');

  await expect(page.getByTestId('mushaf-reader-page')).toHaveAttribute('dir', 'rtl');
  await expect(page.getByTestId('mushaf-page-area')).toBeVisible();
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();
  await expect(page.getByTestId('mushaf-page-loading')).toHaveCount(0);
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');
});

test('next then previous returns to the starting page', async ({ page }) => {
  await page.goto('/dashboard/mushaf');
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');

  await page.getByTestId('mushaf-next-page').click();

  await expect(page).toHaveURL(/[?&]page=2/);
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('2');

  await page.getByTestId('mushaf-prev-page').click();

  // Page 1 is the default, so the reader drops the param instead of writing page=1
  // (src/app/features/mushaf/state/mushaf-reader-session.ts:35-37).
  await expect(page).not.toHaveURL(/[?&]page=/);
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');
});

test('a page deep link hydrates the reader', async ({ page }) => {
  await page.goto('/dashboard/mushaf?page=5');

  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('5');
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();
  await expect(page).toHaveURL(/[?&]page=5/);
});

test('the Mushaf renders Uthmani chrome glyphs and Amiri Quran text', async ({ page }) => {
  await page.goto('/dashboard/mushaf');
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();

  await expect(page.getByTestId('mushaf-page-surah-glyph').first()).toBeVisible();
  await expect(page.getByTestId('mushaf-page-juz-glyph').first()).toBeVisible();

  const words = page.locator('[data-word-location]');
  expect(await words.count()).toBeGreaterThan(0);

  // The reader must render in Amiri, never UthmanicHafs_V22, which mis-renders U+06DF
  // (Frontend/quran-dashboard-ui/README.md:58).
  const fontFamily = await words
    .first()
    .evaluate((element) => getComputedStyle(element).fontFamily);
  expect(fontFamily).toContain('Amiri');
});
```

- [ ] **Step 2: Run it to verify it fails**

```bash
cd Frontend/quran-dashboard-ui
npm run e2e -- e2e/mushaf-reader.e2e.ts
```

Expected: failures on `mushaf-reader-page`, `mushaf-next-page`, `mushaf-prev-page`.

- [ ] **Step 3: Add the reader page root testid**

`mushaf-reader-page.component.html:1` becomes:

```html
<div class="mushaf-reader" dir="rtl" data-testid="mushaf-reader-page">
```

- [ ] **Step 4: Add the prev/next testids**

In `mushaf-header-navigation.component.html`, the previous button (`:5-12`):

```html
        <button
          class="qd-btn qd-btn-ghost"
          type="button"
          data-testid="mushaf-prev-page"
          [disabled]="page().previousPageNumber === null"
          (click)="onPrevious()"
        >
          السابق
        </button>
```

and the next button (`:13-20`):

```html
        <button
          class="qd-btn qd-btn-ghost"
          type="button"
          data-testid="mushaf-next-page"
          [disabled]="page().nextPageNumber === null"
          (click)="onNext()"
        >
          التالي
        </button>
```

- [ ] **Step 5: Run the spec to verify it passes**

```bash
cd Frontend/quran-dashboard-ui
npm run e2e -- e2e/mushaf-reader.e2e.ts
```

Expected: `4 passed`, ≤ 60 s.

- [ ] **Step 6: Run the mushaf unit specs (Tier A)**

```bash
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/features/mushaf/**/*.spec.ts"
```

Expected: all pass.

- [ ] **Step 7: Commit**

```bash
cd /projects/Dashboard/App
git add Frontend/quran-dashboard-ui/src/app/features/mushaf Frontend/quran-dashboard-ui/e2e/mushaf-reader.e2e.ts
git commit -m "test(e2e): Mushaf reader paging and deep-link flows"
```

### Task 2.3: Ayah study flows — tafsir, translation, full-إعراب, similar ayahs, متشابهات, source switch

**Files:**
- Create: `Frontend/quran-dashboard-ui/e2e/mushaf-ayah-study.e2e.ts`

No app changes. A single click on a non-marker word selects both the ayah and the word
(`mushaf-word.component.ts:39-40`); markers are rendered as disabled buttons
(`mushaf-word.component.html:7`), so the `[data-is-marker="false"]` filter is required.

- [ ] **Step 1: Write the spec**

```ts
import type { Page } from '@playwright/test';

import { expect, test } from './fixtures/app-test';

const AYAH_TABS = 'nav[aria-label="تبويبات دراسة الآية"] [role="tab"]';

async function selectFirstWord(page: Page): Promise<void> {
  await page.goto('/dashboard/mushaf');
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();

  await page.locator('[data-word-location][data-is-marker="false"]').first().click();

  await expect(page.getByTestId('selected-ayah-section')).toBeVisible();
  await expect(page.getByTestId('ayah-study-loading')).toHaveCount(0);
}

test('selecting a word opens the ayah study on the tafsir tab', async ({ page }) => {
  await selectFirstWord(page);

  await expect(page).toHaveURL(/[?&]ayah=/);
  await expect(page.locator(AYAH_TABS).nth(0)).toHaveAttribute('aria-selected', 'true');
  await expect(
    page.getByTestId('tafsir-card').or(page.getByTestId('tafsir-empty')),
  ).toBeVisible();
});

test('the translation tab renders the translation card', async ({ page }) => {
  await selectFirstWord(page);

  await page.locator(AYAH_TABS).nth(1).click();

  await expect(page).toHaveURL(/[?&]ayahTab=translation/);
  await expect(
    page.getByTestId('translation-card').or(page.getByTestId('translation-empty')),
  ).toBeVisible();
});

test('the full-i3rab tab renders the i3rab card', async ({ page }) => {
  await selectFirstWord(page);

  await page.locator(AYAH_TABS).nth(2).click();

  await expect(page).toHaveURL(/[?&]ayahTab=full-i3rab/);
  await expect(
    page.getByTestId('full-i3rab-card').or(page.getByTestId('full-i3rab-empty')),
  ).toBeVisible();
});

test('the similar-ayahs tab lists similar ayahs or reports none', async ({ page }) => {
  await selectFirstWord(page);

  await page.getByTestId('ayah-tab-similar-ayahs').click();

  await expect(page.getByTestId('similar-ayahs-loading')).toHaveCount(0);
  await expect(
    page.getByTestId('similar-ayahs-list').or(page.getByTestId('similar-ayahs-empty')),
  ).toBeVisible();
});

test('the mutashabihat tab lists groups or reports none', async ({ page }) => {
  await selectFirstWord(page);

  await page.getByTestId('ayah-tab-mutashabihat').click();

  await expect(page.getByTestId('mutashabihat-loading')).toHaveCount(0);
  await expect(
    page.getByTestId('mutashabihat-groups-list').or(page.getByTestId('mutashabihat-empty')),
  ).toBeVisible();
});

test('switching the tafsir source writes the source into the URL', async ({ page }) => {
  await selectFirstWord(page);

  const selector = page.getByTestId('source-selector').first();
  await selector.getByTestId('source-selector-trigger').click();
  await expect(selector.getByTestId('source-selector-panel')).toBeVisible();

  const languageRows = selector.getByTestId('source-selector-language-row');
  if ((await languageRows.count()) > 0) {
    await languageRows.first().click();
  }
  await selector.getByTestId('source-selector-source-row').first().click();

  await expect(page).toHaveURL(/[?&](tafsirSource|translationSource|fullI3rabSource)=/);
});
```

- [ ] **Step 2: Run the spec**

```bash
cd Frontend/quran-dashboard-ui
npm run e2e -- e2e/mushaf-ayah-study.e2e.ts
```

Expected: `6 passed`, ≤ 2 min.

- [ ] **Step 3: Commit**

```bash
cd /projects/Dashboard/App
git add Frontend/quran-dashboard-ui/e2e/mushaf-ayah-study.e2e.ts
git commit -m "test(e2e): ayah study flows across tafsir, translation, i3rab, similar and mutashabihat"
```

### Task 2.4: Word analysis and surah jump

**Files:**
- Create: `Frontend/quran-dashboard-ui/e2e/mushaf-word-analysis.e2e.ts`

- [ ] **Step 1: Write the spec**

```ts
import { expect, test } from './fixtures/app-test';

test('selecting a word renders its morphology summary and identity links', async ({ page }) => {
  await page.goto('/dashboard/mushaf');
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();

  await page.locator('[data-word-location][data-is-marker="false"]').first().click();

  await expect(page.getByTestId('selected-word-section')).toBeVisible();
  await expect(page.getByTestId('word-analysis-loading')).toHaveCount(0);
  await expect(page).toHaveURL(/[?&]word=/);
  await expect(page.getByTestId('word-morphology-summary')).toBeVisible();
  await expect(page.getByTestId('word-identity-summary')).toBeVisible();
});

test('the surah jump picker moves the reader to another page', async ({ page }) => {
  await page.goto('/dashboard/mushaf');
  await expect(page.getByTestId('mushaf-page-jump-trigger')).toHaveText('1');

  await page.getByTestId('surah-jump-picker-trigger').click();
  await expect(page.getByTestId('surah-jump-picker-panel')).toBeVisible();

  await page.getByTestId('surah-jump-picker-search').fill('البقرة');
  await page.getByTestId('surah-jump-picker-row').first().click();

  await expect(page.getByTestId('surah-jump-picker-panel')).toHaveCount(0);
  await expect(page.getByTestId('mushaf-page-jump-trigger')).not.toHaveText('1');
  await expect(page.getByTestId('mushaf-page-view')).toBeVisible();
});
```

- [ ] **Step 2: Run the spec**

```bash
cd Frontend/quran-dashboard-ui
npm run e2e -- e2e/mushaf-word-analysis.e2e.ts
```

Expected: `2 passed`.

- [ ] **Step 3: Run the whole suite so far**

```bash
cd Frontend/quran-dashboard-ui
npm run e2e
```

Expected: `18 passed` (3 + 3 + 4 + 6 + 2), ≤ 3 min warm.

- [ ] **Step 4: Commit**

```bash
cd /projects/Dashboard/App
git add Frontend/quran-dashboard-ui/e2e/mushaf-word-analysis.e2e.ts
git commit -m "test(e2e): word analysis and surah jump flows"
```

---

# Phase 3 — Hub, explorers, placeholder routes

### Task 3.1: Words hub

**Files:**
- Create: `Frontend/quran-dashboard-ui/e2e/words-hub.e2e.ts`

- [ ] **Step 1: Write the spec**

```ts
import { expect, test } from './fixtures/app-test';

test('the words hub renders its title and the explorer cards', async ({ page }) => {
  await page.goto('/dashboard/words');

  await expect(page.getByTestId('words-hub-title')).toBeVisible();
  await expect(page.getByTestId('words-hub-subtitle')).toBeVisible();
  expect(await page.locator('[data-testid^="words-hub-card--"]').count()).toBeGreaterThan(0);
});

test('a hub card opens its explorer', async ({ page }) => {
  await page.goto('/dashboard/words');

  await page.getByTestId('words-hub-card--roots').click();

  await expect(page).toHaveURL(/\/dashboard\/words\/roots/);
  await expect(page.getByTestId('roots-explorer-page-title')).toBeVisible();
});
```

- [ ] **Step 2: Confirm the card key before running**

```bash
cd Frontend/quran-dashboard-ui
grep -n "words-hub-card--\|key:" src/app/features/words/pages/words-hub-page/words-hub-page.component.html src/app/features/words/models/*.labels.ts | head -20
```

Use the real `card.key` value for roots in the spec if it is not literally `roots`.

- [ ] **Step 3: Run the spec**

```bash
cd Frontend/quran-dashboard-ui
npm run e2e -- e2e/words-hub.e2e.ts
```

Expected: `2 passed`.

- [ ] **Step 4: Commit**

```bash
cd /projects/Dashboard/App
git add Frontend/quran-dashboard-ui/e2e/words-hub.e2e.ts
git commit -m "test(e2e): words hub flow"
```

### Task 3.2: The five explorers

**Files:**
- Create: `Frontend/quran-dashboard-ui/e2e/words-explorers.e2e.ts`

Roots, lemmas and stems share the search → row → panel shape, so they are data-driven. Word Types
and Unique Words differ (no search row on types; a drilldown modal on unique), so each gets its own
test rather than a forced abstraction.

- [ ] **Step 1: Write the spec**

```ts
import { expect, test } from './fixtures/app-test';

const SEARCHABLE_EXPLORERS = [
  {
    name: 'roots',
    path: '/dashboard/words/roots',
    title: 'roots-explorer-page-title',
    search: 'roots-search-input',
    rowButton: 'roots-table-root-button',
    panelEntity: 'root-details-panel-entity',
    query: 'كتب',
  },
  {
    name: 'lemmas',
    path: '/dashboard/words/lemmas',
    title: 'lemmas-explorer-page-title',
    search: 'lemmas-search-input',
    rowButton: 'lemmas-table-lemma-button',
    panelEntity: 'lemma-details-panel-entity',
    query: 'كتاب',
  },
  {
    name: 'stems',
    path: '/dashboard/words/stems',
    title: 'stems-explorer-page-title',
    search: 'stems-search-input',
    rowButton: 'stems-table-stem-button',
    panelEntity: 'stem-details-panel-entity',
    query: 'كتاب',
  },
] as const;

for (const explorer of SEARCHABLE_EXPLORERS) {
  test(`${explorer.name} explorer: search then open a row shows the details panel`, async ({
    page,
  }) => {
    await page.goto(explorer.path);
    await expect(page.getByTestId(explorer.title)).toBeVisible();

    await page.getByTestId(explorer.search).fill(explorer.query);
    await expect(page).toHaveURL(/[?&]search=/);

    const rows = page.getByTestId(explorer.rowButton);
    await expect(rows.first()).toBeVisible();
    expect(await rows.count()).toBeGreaterThan(0);

    await rows.first().click();

    await expect(page.getByTestId(explorer.panelEntity)).toBeVisible();
    await expect(page.getByTestId(explorer.panelEntity)).not.toBeEmpty();
  });
}

test('word-types explorer: rows render and a row opens the details panel', async ({ page }) => {
  await page.goto('/dashboard/words/types');

  await expect(page.getByTestId('word-types-table-loading')).toHaveCount(0);
  const rows = page.locator('[data-word-types-row]');
  await expect(rows.first()).toBeVisible();

  await rows.first().click();

  await expect(page.getByTestId('word-type-details-panel-entity')).toBeVisible();
});

test('unique-words explorer: search then open a word shows the drilldown', async ({ page }) => {
  await page.goto('/dashboard/words/unique/tashkeel');

  await expect(page.getByTestId('unique-words-page-title')).toBeVisible();
  await expect(page.getByTestId('unique-words-loading')).toHaveCount(0);

  await page.getByTestId('unique-words-search-input').fill('الله');
  const words = page.getByTestId('unique-words-table-word-button');
  await expect(words.first()).toBeVisible();

  await words.first().click();

  await expect(page.getByTestId('word-drilldown-entity')).toBeVisible();
});

test('the unique-words route redirects to the tashkeel mode', async ({ page }) => {
  await page.goto('/dashboard/words/unique');

  await expect(page).toHaveURL(/\/dashboard\/words\/unique\/tashkeel/);
});
```

- [ ] **Step 2: Run the spec**

```bash
cd Frontend/quran-dashboard-ui
npm run e2e -- e2e/words-explorers.e2e.ts
```

Expected: `6 passed`, ≤ 2 min. If a search query returns no rows against the local DB, replace it
with a term that does — never relax the assertion to "zero rows is fine".

- [ ] **Step 3: Commit**

```bash
cd /projects/Dashboard/App
git add Frontend/quran-dashboard-ui/e2e/words-explorers.e2e.ts
git commit -m "test(e2e): explorer flows for roots, lemmas, stems, types and unique words"
```

### Task 3.3: Placeholder route

**Files:**
- Create: `Frontend/quran-dashboard-ui/e2e/placeholder-routes.e2e.ts`

- [ ] **Step 1: Write the spec**

```ts
import { expect, test } from './fixtures/app-test';

test('an unbuilt section renders its placeholder sentence', async ({ page }) => {
  await page.goto('/mutashabihat');

  await expect(page.getByRole('heading', { name: 'المتشابهات', level: 1 })).toBeVisible();
  await expect(
    page.getByText('سيتم ربط هذا القسم ضمن خطة الميزات التالية.'),
  ).toBeVisible();
});

test('an unknown route falls back to the dashboard', async ({ page }) => {
  await page.goto('/definitely-not-a-route');

  await expect(page).toHaveURL(/\/dashboard$/);
});
```

- [ ] **Step 2: Run the whole suite**

```bash
cd Frontend/quran-dashboard-ui
npm run e2e
```

Expected: `28 passed` (18 after Phase 2, plus hub 2 + explorers 6 + placeholder 2), ≤ 4 min warm.

- [ ] **Step 3: Verify the headed mode opens a browser**

```bash
cd Frontend/quran-dashboard-ui
npm run e2e:headed -- e2e/dashboard-home.e2e.ts
```

Expected: a visible chromium window; `3 passed`.

- [ ] **Step 4: Verify Vitest is still untouched**

```bash
cd Frontend/quran-dashboard-ui
npm test
```

Expected: 169 files / 1,938 tests, unchanged.

- [ ] **Step 5: Commit**

```bash
cd /projects/Dashboard/App
git add Frontend/quran-dashboard-ui/e2e/placeholder-routes.e2e.ts
git commit -m "test(e2e): placeholder route and wildcard fallback flows"
```

---

# Phase 4 — Documentation wiring

Every doc that currently asserts "no browser E2E layer exists" becomes false the moment Phase 1
lands; this phase is not optional polish.

### Task 4.1: Rewrite `TESTING_STRATEGY.md` §3 Tier E

**Files:**
- Modify: `TESTING_STRATEGY.md:122-124`

- [ ] **Step 1: Replace the paragraph**

Current text:

```markdown
There is **no browser E2E layer in this tree** — no Playwright dependency, config, or
`e2e` npm script exists. Do not cite an E2E run as release evidence; the release gate is
the full Backend and Frontend suites plus both production builds.
```

Replacement:

```markdown
A browser E2E layer exists (`Frontend/quran-dashboard-ui/playwright.config.ts` + `e2e/`,
chromium only, `npm run e2e`). It is an **opt-in local gate, not part of any required tier**:
it is not required for Tier C and not required for this release gate, which remains the full
Backend and Frontend suites plus both production builds. Promoting it into a required tier is a
separate decision, to be made only after it has proven stable across several runs. An E2E run
MAY be reported as supplementary evidence, and MUST then state that it is supplementary.
```

- [ ] **Step 2: Verify no other line still denies the layer**

```bash
cd /projects/Dashboard/App
grep -rn "no browser E2E\|no Playwright" TESTING_STRATEGY.md CLAUDE.md Frontend/quran-dashboard-ui/CLAUDE.md Frontend/quran-dashboard-ui/AGENTS.md Frontend/quran-dashboard-ui/README.md
```

Expected after Tasks 4.2–4.4: no matches.

### Task 4.2: Wire §4 and §6

**Files:**
- Modify: `TESTING_STRATEGY.md` §4 matrix, §6 Frontend command catalog

- [ ] **Step 1: Add a §4 matrix row**

Append to the change-to-tier table:

```markdown
| Frontend routing, app shell, or a public browse surface (optional extra confidence) | A | C (E2E optional, never a blocker) | No |
```

- [ ] **Step 2: Add the §6 commands**

Append to the Frontend command catalog code block:

```bash
# Browser E2E — opt-in, chromium only, boots both servers (see e2e/README.md):
npm run e2e                       # headless
npm run e2e:headed                # visible browser
npm run e2e:ui                    # Playwright UI mode
npm run e2e -- e2e/mushaf-reader.e2e.ts   # one flow file
```

- [ ] **Step 3: Add the prose note under §6**

```markdown
The E2E suite boots the Angular dev server **and** the backend `https` launch profile
(`ASPNETCORE_ENVIRONMENT=Development`), so it reads the real local `quran_dashboard` database.
Every flow is read-only and every count assertion is loose; do not add write flows to it without
first moving it onto an isolated database. It requires `dotnet build Backend/QuranDashboard.sln`
beforehand (the backend boots with `--no-build`) and mkcert certificates in the frontend project
root. It is **not** the §13 route-smoke tier and does not restore it.
```

- [ ] **Step 4: Confirm §13 is untouched**

```bash
cd /projects/Dashboard/App
git diff TESTING_STRATEGY.md | grep -c "^[+-].*Tests.Smoke"
```

Expected: `0`.

### Task 4.3: Invert the frontend instruction lines

**Files:**
- Modify: `Frontend/quran-dashboard-ui/CLAUDE.md:35`
- Modify: `Frontend/quran-dashboard-ui/AGENTS.md:35`

- [ ] **Step 1: Replace the bullet in both files**

Current (identical in both):

```markdown
- There is no browser E2E layer (no Playwright dependency, config, or `e2e` script). Never
```

Replacement (keep the surrounding bullet structure of each file intact):

```markdown
- A browser E2E layer exists: Playwright (chromium only) at `playwright.config.ts` + `e2e/`,
  run with `npm run e2e`. It is opt-in and is NOT a required gate — never cite it in place of
  the Vitest suite or a build, and never let an E2E run substitute for Tier C evidence. Specs
  are named `*.e2e.ts`; a `*.spec.ts` under `e2e/` would be swallowed by the Vitest glob.
```

- [ ] **Step 2: Verify both files changed**

```bash
cd /projects/Dashboard/App
grep -n "browser E2E" Frontend/quran-dashboard-ui/CLAUDE.md Frontend/quran-dashboard-ui/AGENTS.md
```

Expected: one match per file, describing the layer as existing.

### Task 4.4: Frontend README §Testing and root `CLAUDE.md`

**Files:**
- Modify: `Frontend/quran-dashboard-ui/README.md` (§Testing)
- Modify: `CLAUDE.md` (Test selection paragraph)

- [ ] **Step 1: Extend the README §Testing section**

Append to the existing bullet list:

```markdown
- **Browser E2E (opt-in):** `npm run e2e` (headless), `npm run e2e:headed`, `npm run e2e:ui`.
  Chromium only. It boots the Angular dev server *and* the backend `https` profile, so it needs
  mkcert certificates, a migrated local `quran_dashboard`, and a prior
  `dotnet build Backend/QuranDashboard.sln`. Specs live in `e2e/` and MUST be named `*.e2e.ts` —
  a `*.spec.ts` there would be collected by the Vitest builder. See `e2e/README.md`.
```

- [ ] **Step 2: Extend the root `CLAUDE.md` test-selection paragraph**

Append to the "Test selection" section, after the two "facts the strategy fixes" bullets:

```markdown
A third fact, new: a browser E2E layer now exists
(`Frontend/quran-dashboard-ui/e2e/`, `npm run e2e`), but it is **opt-in and not a required
tier** — do not present an E2E run as a Tier C or release gate, and do not confuse it with the
still-absent backend route-smoke tier (§13).
```

- [ ] **Step 3: Verify the denial is gone everywhere**

```bash
cd /projects/Dashboard/App
grep -rn "no browser E2E\|no Playwright dependency" --include="*.md" . --exclude-dir=node_modules
```

Expected: no matches outside `docs/feature-playwright-bootstrap/plan.md`.

### Task 4.5: Write `e2e/README.md`

**Files:**
- Create: `Frontend/quran-dashboard-ui/e2e/README.md`

- [ ] **Step 1: Write the file**

```markdown
# Browser E2E (Playwright)

**HOW rules:** `../../../TESTING_STRATEGY.md` §3 Tier E / §6. This file is the WHAT.

Chromium-only browser flow tests over the public browse surfaces: dashboard home, the Mushaf
reader (incl. tafsir / translation / full-إعراب / similar-ayahs / متشابهات cards), the words hub,
the five explorers, and the placeholder routes.

## Commands

```bash
npm run e2e                              # headless (the gate)
npm run e2e:headed                       # visible browser
npm run e2e:ui                           # Playwright UI mode
npm run e2e -- e2e/mushaf-reader.e2e.ts  # one flow file
```

## Prerequisites

- mkcert certificates in the project root (`mkcert -install && mkcert localhost`) — without
  `localhost.pem` / `localhost-key.pem` the Angular dev server never starts.
- A migrated local `quran_dashboard` database with the DB password in backend user-secrets.
  Nothing migrates on startup.
- `dotnet build Backend/QuranDashboard.sln` first — the backend boots with `--no-build`.

## Invariants

- **Specs are `*.e2e.ts`, never `*.spec.ts`.** The Angular unit-test builder collects
  `**/*.spec.ts` from the project root, so a `.spec.ts` here would be run by Vitest.
- **Fresh context per test — never add `storageState` reuse.** `qd-mushaf-reader-session`
  (sessionStorage) restores the last reader page on a bare entry, and `qd-theme` decides the
  theme; leaking either between tests makes results order-dependent.
- **Zero external network calls.** `fixtures/app-test.ts` stubs the Logto origin and fails any
  test whose page talked to a non-localhost host.
- **Read-only flows and loose count assertions only.** The suite reads the live local dev DB;
  exact row counts would break on the next reseed.
- Both servers boot with `reuseExistingServer`, and the backend readiness gate is
  `GET https://localhost:5015/api/health`, which answers 503 when the database is unreachable.

## Not this suite

This is not the backend route-smoke tier (`TESTING_STRATEGY.md` §13), and running it does not
restore that tier. It is also not a required gate — see §3 Tier E.
```

- [ ] **Step 2: Final full verification**

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm run e2e
npm test
npm run build
```

Expected: E2E all passed; Vitest 169 files / 1,938 tests; production build succeeds.

- [ ] **Step 3: Commit**

```bash
cd /projects/Dashboard/App
git add TESTING_STRATEGY.md CLAUDE.md Frontend/quran-dashboard-ui/CLAUDE.md \
  Frontend/quran-dashboard-ui/AGENTS.md Frontend/quran-dashboard-ui/README.md \
  Frontend/quran-dashboard-ui/e2e/README.md docs/feature-playwright-bootstrap/plan.md
git commit -m "docs: wire the browser E2E layer into the testing strategy and instruction files"
```

---

## Follow-ups (recorded, not in scope)

1. **Zero the dev API latency for E2E.** `devApiLatencyMs: 450` costs the suite real wall-clock.
   An e2e build configuration or a runtime override would cut the runtime materially. Out of scope
   because it touches the environment files, which the `Environment` model deliberately keeps in
   lockstep (`src/environments/environment.model.ts:1-15`).
2. **Isolated database.** `resources/db-dumps/quran-canonical/` exists and is currently orphaned —
   no test or script reads it (`TESTING_STRATEGY.md:111-114`). Restoring it into a dedicated e2e
   database would decouple the suite from the working dev DB and unlock exact-count assertions and
   write flows.
3. **Promotion decision.** After the suite has been stable across several runs, decide whether it
   becomes a required Tier C gate for frontend routing / shell changes.
4. **Testids for the tafsir / translation / full-إعراب tabs**, if the index-based tab selection
   proves brittle.
5. **The words-dropdown hover/click defect** (deviation 2 below) is a real user-facing bug, not a
   test-infrastructure item: a pointer user who hovers "الكلمات والجذور" and then clicks its label
   sees the menu snap shut, because the button's `(click)="toggleWords()"` inverts the state
   `mouseenter` just set. Keyboard activation is unaffected. It needs its own decision and its own
   unit coverage; the e2e spec hovers instead, with a comment pinning why.

---

## Deviations found during implementation

The task bodies above are left as written; these are the premises implementation disproved.

1. **The reader does not drop `page=1` after paging back.** `changePage` writes the page key
   unconditionally (`src/app/features/mushaf/data-access/mushaf-reader.facade.ts`); only session
   restoration omits the default. The spec asserts `page=1`.
2. **The words dropdown does not open on `.click()`.** The item opens on `mouseenter` and the
   button's own click handler toggles it shut, so the spec hovers instead. **This is a real UX
   defect in the app** — a pointer user who hovers "الكلمات والجذور" then clicks its label sees
   the menu snap shut; keyboard activation is unaffected. Not fixed here (application changes
   were limited to testids), and it is worth its own decision.
3. **Word-types rows are not clickable**, and they appear only after a subtype is chosen. The
   spec picks a subtype first, then clicks a row's count chip — the chips are a row's only
   interactive elements.
4. **Lemma search must query `كتب`, not `كتاب`.** Lemma text is Uthmani, and the backend
   normalizer strips the superscript alef (U+0670) rather than folding it, so a plain-alef query
   cannot match.
5. **The source-selector test skips when the DB seeds a single tafsir source**, and proves the
   switch by picking a row with `aria-selected="false"` — the trigger label unmounts during the
   reload, so it is not a usable witness.

---

## Phase count summary

| Phase | Tasks | What lands | Verification |
|---|---|---|---|
| 1 — Foundation | 6 | dependency, chromium, config, e2e tsconfig, gitignore, Logto stub + shared fixture, dashboard smoke | cold `npm run e2e` (3 passed), warm re-run, `npm test` count unchanged |
| 2 — Reader | 4 | navbar/prev-next/reader-root testids + shell nav, reader paging & deep link, ayah study (6 flows), word analysis & surah jump | `npm run e2e` (18 passed), mushaf + layout Vitest specs |
| 3 — Explorers | 3 | words hub, five explorer flows, placeholder + wildcard | `npm run e2e` (28 passed), `npm run e2e:headed`, `npm test` count unchanged |
| 4 — Docs | 5 | TESTING_STRATEGY §3/§4/§6, frontend CLAUDE.md + AGENTS.md, frontend README, root CLAUDE.md, `e2e/README.md` | grep proves no doc still denies the layer; §13 diff is empty; full e2e + Vitest + build |

**4 phases, 18 tasks, 8 spec files, 28 tests, 7 `data-testid` attributes** across the three
elements the scope named (four navbar bindings, reader prev/next, reader page root).
