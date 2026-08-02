# Browser E2E (Playwright)

**HOW rules:** `../../../TESTING_STRATEGY.md` §3 Tier E / §6. This file is the WHAT.

Chromium-only browser flow tests over the public browse surfaces: dashboard home, the app-shell
navbar and its dropdown menus, the Mushaf reader (paging, deep link, surah jump, fonts) with the
ayah study — tafsir / translation / full-إعراب / similar-ayahs / متشابهات tabs — and the word
analysis panel, the words hub, the five explorers (roots, lemmas, stems, word types, unique
words), and the placeholder / wildcard-fallback routes — **plus the Abwab doors/sections flows**
(`abwab-structure.e2e.ts`, `abwab-operations.e2e.ts`, `abwab-archive.e2e.ts`,
`abwab-url-and-a11y.e2e.ts`, `abwab-global-order.e2e.ts`), which are the one deliberate exception
to the read-only invariant below.

## Commands

```bash
npm run e2e                                        # headless (the gate) — two sequential runs, see below
npm run e2e:headed                                 # visible browser
npm run e2e:ui                                      # Playwright UI mode
npx playwright test e2e/mushaf-reader.e2e.ts        # one flow file, any worker count
npm run e2e:typecheck                               # tsc over e2e/ + playwright.config.ts
```

`npm run e2e` runs two Playwright projects in sequence: `default` (every non-Abwab spec, 2
workers) then `abwab` (the five `abwab-*.e2e.ts` specs, 1 worker — see the parallelism note
below). Both share the same `webServer` config and `reuseExistingServer`, so the second
invocation does not re-pay startup cost when the first left the servers up. To run only one
group at a custom worker count: `npx playwright test --project=abwab --workers=1`.

`npm run e2e:headed` and `npm run e2e:ui` do **not** apply the split — both run every project at
the top-level `workers: 2`, which puts the Abwab specs back under the parallelism hazard below.
They are debug commands, not the gate; to debug an Abwab flow specifically, scope both the
project and the worker count yourself: `npx playwright test --project=abwab --workers=1 --headed`.

## Prerequisites

- mkcert certificates in the project root (`mkcert -install && mkcert localhost`) — without
  `localhost.pem` / `localhost-key.pem` the Angular dev server never starts, and Playwright
  reports it only as a port timeout.
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
  test whose browser context talked to a non-localhost host.
- **The five Abwab specs run single-worker, in their own `abwab` Playwright project.** A `Global`
  reorder resequences `global_order_value` on **every live root in the database**, not just the
  sandbox's own doors — so two Abwab specs racing in different workers can both hold a version of
  a root the other worker's write (or teardown archive) just invalidated, and the second one either
  `409`s or, worse, computes a target position against a live-root list that changed underneath it
  between the read and the write. Retry-on-409 is not the fix: this feature's own policy is that
  `409`s are always surfaced, never swallowed or auto-retried (`features/abwab/README.md`).
  Measured directly: at the default 2 workers, `abwab-global-order.e2e.ts` failed on a
  wrong-position assertion (not even a `409` — a silently different result) after another worker's
  teardown resequenced the global order mid-test; at 1 worker, all 20 Abwab tests pass repeatably.
  `playwright.config.ts` therefore splits `projects` into `default` (`testIgnore` on
  `abwab-*.e2e.ts`, 2 workers) and `abwab` (`testMatch` on `abwab-*.e2e.ts`, 1 worker), and
  `npm run e2e` runs both in sequence. This costs real wall-clock time (the full gate went from
  47 tests/~1.6 m to 48 tests/~2.7 m — one new flow plus a serialized phase that used to overlap
  with everything else) in exchange for not shipping a suite that can pass or fail depending on
  scheduling luck.
- **A `Global` reorder's residue reaches outside the sandbox, and that is accepted.** Every root
  in the local dev database gets renumbered by `global_order_value`, sandbox or not — resequencing
  is order-preserving for untouched rows, and teardown removes the sandbox's own roots again
  afterward, so the residue is a permutation of nothing observable. It is still a write outside the
  sandbox's blast radius, same class as the archived-doors residue below, and is accepted on the
  same terms: a local, disposable dev DB, not a shared one.
- **Read-only flows and loose count assertions, except for Abwab.** Every suite but Abwab reads
  the live local dev DB without writing to it; exact row counts would break on the next reseed.
  The five Abwab specs are a deliberate, named exception (`docs/feature-abwab-doors/plan-slice-b2.md`
  §2, and `TESTING_STRATEGY.md` §6, which this amendment mirrors): each test creates its own
  uniquely-named sandbox section over the API (`fixtures/abwab.ts`) and drives real writes
  against it through the UI. Teardown archives **every live door in that section** — swept from
  the tree by `sectionId`, not just the ids the fixture handed out, because flows also create
  doors through the UI — and then deletes the now-empty section. That order is forced, not
  stylistic: section delete `409`s while live doors remain. Each archive re-reads the door's
  current version first, since every write resequences the scope and bumps its siblings'
  `xmin`; archiving from one up-front snapshot succeeds once and then `409`s silently for the
  rest, which is what used to leave live sandbox doors and undeleted sandbox sections behind.
  Teardown is best-effort and never masks a test's own failure (R19), and no Abwab test asserts
  a global count — each one only ever asserts on the ids its own sandbox produced (R18).
  **The residue that remains is archived doors, and it is permanent, not "self-cleaning":**
  there is no hard delete and no section restore, so every run leaves its sandbox doors
  **archived** in the local dev DB forever, and any future restore of one is refused until the
  user names a live destination section, since the one it belonged to is gone. What must **not**
  remain after a run is any live `e2e-sandbox-*` door or any `e2e-sandbox-*` section — either one
  is a teardown bug, not accepted residue. This is accepted on a local dev DB with loose, id-scoped
  assertions; it would need to be revisited (per the same `TESTING_STRATEGY.md` §6 note) before
  this suite runs anywhere but a disposable local database.
- Both servers boot with `reuseExistingServer`, and the backend readiness gate is
  `GET https://localhost:5015/api/health`, which answers 503 when the database is unreachable.
- **Never leave a server running on :4200 or :5015 outside Playwright's control.** A stray
  server is adopted by `reuseExistingServer` and silently poisons the whole run.

## Not this suite

This is not the backend route-smoke tier (`QuranDashboard.Tests.Smoke`,
`TESTING_STRATEGY.md` §3 Tier A/C and §5), and running it never substitutes for it: that tier
is a required gate for route, contract, auth, middleware, and binding changes, while this suite
is not a required gate at all — see §3 Tier E.
