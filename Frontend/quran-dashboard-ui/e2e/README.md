# Browser E2E (Playwright)

**HOW rules:** `../../../TESTING_STRATEGY.md` §3 Tier E / §6. This file is the WHAT.

Chromium-only browser flow tests over the public browse surfaces: dashboard home, the app-shell
navbar and its dropdown menus, the Mushaf reader (paging, deep link, surah jump, fonts) with the
ayah study — tafsir / translation / full-إعراب / similar-ayahs / متشابهات tabs — and the word
analysis panel, the words hub, the five explorers (roots, lemmas, stems, word types, unique
words), and the placeholder / wildcard-fallback routes — **plus the Abwab doors/sections flows**
(`abwab-structure.e2e.ts`, `abwab-operations.e2e.ts`, `abwab-archive.e2e.ts`,
`abwab-url-and-a11y.e2e.ts`), which are the one deliberate exception to the read-only invariant
below.

## Commands

```bash
npm run e2e                              # headless (the gate)
npm run e2e:headed                       # visible browser
npm run e2e:ui                           # Playwright UI mode
npm run e2e -- e2e/mushaf-reader.e2e.ts  # one flow file
npm run e2e:typecheck                    # tsc over e2e/ + playwright.config.ts
```

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
- **Read-only flows and loose count assertions, except for Abwab.** Every suite but Abwab reads
  the live local dev DB without writing to it; exact row counts would break on the next reseed.
  The four Abwab specs are a deliberate, named exception (`docs/feature-abwab-doors/plan-slice-b2.md`
  §2, and `TESTING_STRATEGY.md` §6, which this amendment mirrors): each test creates its own
  uniquely-named sandbox section over the API (`fixtures/abwab.ts`), drives real writes against
  it through the UI, and tears down by archiving every door it created and then deleting the
  now-empty section — the only lawful order, since section delete `409`s while live doors remain.
  Teardown is best-effort and never masks a test's own failure (R19), and no Abwab test asserts a
  global count — each one only ever asserts on the ids its own sandbox produced (R18).
  **The residue is real and permanent, not "self-cleaning":** there is no hard delete and no
  section restore, so every run leaves its sandbox doors **archived** in the local dev DB forever,
  and any future restore of one reports `detachedFromArchivedSection: true` since its section is
  gone. This is accepted on a local dev DB with loose, id-scoped assertions; it would need to be
  revisited (per the same `TESTING_STRATEGY.md` §6 note) before this suite runs anywhere but a
  disposable local database.
- Both servers boot with `reuseExistingServer`, and the backend readiness gate is
  `GET https://localhost:5015/api/health`, which answers 503 when the database is unreachable.
- **Never leave a server running on :4200 or :5015 outside Playwright's control.** A stray
  server is adopted by `reuseExistingServer` and silently poisons the whole run.

## Not this suite

This is not the backend route-smoke tier (`QuranDashboard.Tests.Smoke`,
`TESTING_STRATEGY.md` §3 Tier A/C and §5), and running it never substitutes for it: that tier
is a required gate for route, contract, auth, middleware, and binding changes, while this suite
is not a required gate at all — see §3 Tier E.
