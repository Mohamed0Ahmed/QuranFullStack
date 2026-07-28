# Browser E2E (Playwright)

**HOW rules:** `../../../TESTING_STRATEGY.md` §3 Tier E / §6. This file is the WHAT.

Chromium-only browser flow tests over the public browse surfaces: dashboard home, the app-shell
navbar and its dropdown menus, the Mushaf reader (paging, deep link, surah jump, fonts) with the
ayah study — tafsir / translation / full-إعراب / similar-ayahs / متشابهات tabs — and the word
analysis panel, the words hub, the five explorers (roots, lemmas, stems, word types, unique
words), and the placeholder / wildcard-fallback routes.

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
- **Read-only flows and loose count assertions only.** The suite reads the live local dev DB;
  exact row counts would break on the next reseed.
- Both servers boot with `reuseExistingServer`, and the backend readiness gate is
  `GET https://localhost:5015/api/health`, which answers 503 when the database is unreachable.
- **Never leave a server running on :4200 or :5015 outside Playwright's control.** A stray
  server is adopted by `reuseExistingServer` and silently poisons the whole run.

## Not this suite

This is not the backend route-smoke tier (`QuranDashboard.Tests.Smoke`,
`TESTING_STRATEGY.md` §3 Tier A/C and §5), and running it never substitutes for it: that tier
is a required gate for route, contract, auth, middleware, and binding changes, while this suite
is not a required gate at all — see §3 Tier E.
