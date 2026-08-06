---
name: dependency-audit
description: >-
  Dependency and security audit for the Quran Dashboard (المنهج القرآني) fullstack
  workspace — backend NuGet (.NET) and frontend npm (Angular). Use this skill whenever the
  user wants to check dependencies for known vulnerabilities or staleness, asks "are our
  packages safe / up to date", mentions a CVE / advisory / `npm audit` / `dotnet list
  package --vulnerable`, wants a periodic security-hygiene pass, or is about to bump
  dependencies and wants to know the smallest safe change first — even if they only say
  "audit our deps", "check for vulnerable packages", or "is this package safe to upgrade".
  It is an audit / report skill first: it scans, separates direct from transitive issues,
  identifies the likely parent for a transitive advisory, and proposes the smallest safe
  remediation with verification commands. It does NOT perform package upgrades unless the
  user explicitly asks, never does major upgrades by default, never suppresses advisories
  without explicit approval, and never mixes dependency cleanup with feature or performance
  code changes. Not for implementing features, not a full engineering/performance review,
  and not a build/migration runtime smoke (recommend deploy-smoke afterward).
---

# Dependency Audit

An audit-first pass over backend NuGet and frontend npm dependencies: what is vulnerable or
outdated, whether it is a direct or transitive dependency, and the **smallest safe** change
that clears it. The output is findings and a remediation proposal — not an upgrade. Upgrades
happen only when the user explicitly asks, as a separate, dependency-only change.

## When to use

- A security-hygiene pass ("check our packages", "any vulnerable deps?").
- A CVE / advisory landed and you need to know if the workspace is affected and how deep.
- Before deliberately bumping a package — find the minimal safe target first.
- Periodically, or before a release, to catch drift.

## What this skill is NOT

- **Not an auto-upgrade skill.** Do not run package upgrades unless the user explicitly asks.
- **No major upgrades by default** — majors carry breaking changes; propose them, flag the
  risk, and let the user decide.
- **No advisory suppression** (NuGetAudit suppressions, `npm audit` overrides/`nsp` ignores,
  `--force`) without explicit user approval — silencing a warning is a decision, not a fix.
- **No mixing** dependency cleanup with feature or performance code changes; a dependency fix
  is its own change so it stays reviewable and revertible.
- Not a runtime smoke (that is `deploy-smoke`) and not a code review (that is
  `engineering-review` / the `performance-*` skills).

## Before running: inspect, don't assume

Learn how the workspace declares dependencies before naming commands:

- **Backend:** locate the solution (e.g. `QuranDashboard.sln`) under `Backend/`; note whether
  it uses central package management (`Directory.Packages.props`) or per-project
  `<PackageReference>`s — that decides *where* a version bump goes. Identify the projects that
  actually reference a flagged package.
- **Frontend:** read `Frontend/quran-dashboard-ui/package.json` (`dependencies` vs
  `devDependencies`) and confirm which lockfile exists (`package-lock.json`), which decides
  `npm install` vs `npm ci`.

## Backend checks (NuGet)

- **Vulnerable, including transitive:** `dotnet list package --vulnerable --include-transitive`.
  This is the primary security signal.
- **Outdated (where useful):** `dotnet list package --outdated` to see how far behind direct
  references are — use it to judge how big a bump the fix needs, not as a mandate to update
  everything.
- **Direct vs transitive:** for each finding, state whether the flagged package is a **direct**
  `<PackageReference>` or pulled in **transitively**.
- **Find the parent:** for a transitive advisory, identify the likely **parent** package that
  brings it in (the dependency chain), so the fix targets the right place.
- **Prefer upgrading the parent** over pinning a random direct reference to the transitive
  package — a stray direct pin masks the real dependency graph and drifts later. Add a direct
  pin only when no parent upgrade clears the advisory, and call it out as a deliberate override.
- **Verify after any remediation:** `dotnet restore`, `dotnet build`, the relevant tests, and
  re-run `dotnet list package --vulnerable --include-transitive` to confirm the advisory is gone.
- **Swagger / OpenAPI smoke** only when API-documentation packages changed (e.g. Swashbuckle /
  Microsoft.OpenApi) — confirm the docs still generate; skip otherwise.

## Frontend checks (npm)

- **Audit:** `npm audit`, or `npm audit --omit=dev` when you need to separate deployed-runtime
  risk from build-only tooling risk.
- **Outdated (where useful):** `npm outdated` to size the gap between installed and latest.
- **Runtime vs dev:** distinguish `dependencies` (ship to users) from `devDependencies`
  (build/test tooling). A high-severity advisory in a dev-only tool is real but lower urgency
  than the same in a runtime dependency — say which it is.
- **Avoid `npm audit fix`** (and never `--force`) unless the user explicitly approves — it can
  make sweeping, unsafe, or major bumps and mix unrelated changes.
- **Propose minimal safe bumps** — the smallest version that clears the advisory.
- **Verify after any remediation:** `npm install` (or `npm ci` for a clean lockfile-faithful
  install), `npm run build`, and the test lanes `TESTING_STRATEGY.md` §5 requires for the
  changed scope (`npm run test:*`; `Backend/scripts/test-backend …` for NuGet bumps).

## Remediation principle: smallest safe change

- **Patch / minor** bumps that clear the advisory are usually safe — propose them directly.
- **Major** bumps are risky (breaking changes); propose as an option with the risk called out,
  never as the default, and never applied without explicit approval.
- Fix the **parent** for transitive issues; a direct pin is a last resort and must be labeled.
- One dependency concern per proposed change so review and rollback stay clean.

## Examples

**1 — Backend transitive advisory (parent upgrade):**
`dotnet list package --vulnerable --include-transitive` flags `Microsoft.OpenApi` as
*transitive*. It is not a direct reference; it is pulled in by **Swashbuckle.AspNetCore**.
Smallest safe fix: bump the **Swashbuckle** parent to a version whose dependency graph
resolves `Microsoft.OpenApi` to the patched version — not a new direct `Microsoft.OpenApi`
`<PackageReference>`. Verify with restore/build, then re-scan; if Swagger packages moved,
smoke that the OpenAPI docs still generate.

**2 — Frontend dev-only advisory:**
`npm audit` reports a high-severity issue, but `npm audit --omit=dev` comes back clean →
the advisory is in a **devDependency** (build/test tooling), not shipped to users. Report it
as dev-only (lower urgency), propose the minimal patch/minor bump of that tool, and do **not**
run `npm audit fix` to sweep it.

**3 — Safe vs risky bump:**
A patch bump `1.4.2 → 1.4.5` that clears the advisory → propose and apply on request. A major
`1.x → 3.0` "fix" → flag as breaking, present as an option with the migration risk, and leave
the decision to the user.

## Cross-skill guidance

- If remediation includes EF migrations or you want a runtime build/boot check afterward, run
  **`deploy-smoke`** next.
- If remediation touches frontend test execution, follow
  `.claude/skills/test-guard/references/frontend-test-harness-constraints.md` for the
  Vitest/jsdom command and cap — do not reinvent it.
- If remediation affects Quran data, importers, or source packages, apply the shared safety
  rules: `.claude/skills/engineering-review/references/quran-data-safety.md`. A dependency
  change must never weaken source integrity or provenance.

## Output format

Return the result in this structure:

1. **Verdict** — one of: `PASS` / `PASS WITH NOTES` / `CHANGES RECOMMENDED` / `BLOCKED`.
2. **Scope checked** — backend / frontend; which manifests and lockfiles were scanned.
3. **Backend dependency findings** — vulnerable/outdated NuGet packages with severity.
4. **Frontend dependency findings** — `npm audit` / `npm outdated` results with severity.
5. **Direct / transitive analysis** — per finding: direct or transitive, and the likely parent.
6. **Proposed remediation** — the smallest safe change per finding (parent bump preferred),
   with major upgrades flagged as optional/risky.
7. **Verification commands to run after remediation** — restore/build/test + re-scan (backend),
   install/ci + build + test (frontend).
8. **Risks / deferrals** — majors deferred, advisories with no safe fix yet, dev-only items,
   anything intentionally not changed.
9. **Next recommended action** — one short, direct step (e.g. "apply the Swashbuckle minor
   bump then re-scan", "defer the major to its own change", "run deploy-smoke after the bump").

## Guardrails

- Audit and propose; do not upgrade unless explicitly asked, and never a major by default.
- Never suppress an advisory without explicit approval.
- Never mix dependency changes with feature/performance edits.
- Prefer parent upgrades over stray direct pins for transitive issues.
- Prefer project scripts; inspect manifests/lockfiles before naming exact commands.
- If a scan cannot run or its result is unknown, say unknown — do not assume clean.
- Do not commit, stage, or push (that is `commit-workflow`).
