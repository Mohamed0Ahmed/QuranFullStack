---
name: dependency-audit
description: Use when asked to audit Quran Dashboard NuGet or npm dependencies for vulnerabilities, advisories, transitive exposure, or staleness.
---

# Dependency Audit

## Responsibility

Scan and report on backend NuGet and frontend npm dependencies: known vulnerabilities
and advisories, direct vs transitive exposure with the likely parent of each transitive
advisory, staleness where useful, and the smallest safe remediation **option** per
finding. The read-only scans are this skill's own work; remediation is not.

**Not this skill's job:** editing package or lock files, restore/build/test/smoke runs,
advisory suppression (NuGetAudit suppressions, `npm audit fix`/`--force`, overrides), or
Git. Applying a remediation is a later, separately requested dependency-only change.

## Workflow

1. Inspect the real manifests first: the backend solution's package declarations
   (per-project `<PackageReference>` or `Directory.Packages.props` central management —
   that decides where a bump would go) and `Frontend/quran-dashboard-ui/package.json`
   plus its lockfile.
2. Backend scan: `dotnet list package --vulnerable --include-transitive` (the primary
   security signal).
3. Frontend scan: `npm audit`, with `--omit=dev` to separate deployed-runtime risk from
   build/test tooling risk; say which kind each finding is.
4. Classify each finding: direct or transitive; for transitive advisories identify the
   likely parent that brings it in.
5. Propose the smallest safe remediation per finding: prefer upgrading the parent over a
   stray direct pin (label any direct pin a deliberate override); patch/minor over
   major; flag any major bump as optional and breaking, for the user to decide.

## Conditional context

- `dotnet list package --outdated` / `npm outdated` — only when sizing how far behind a
  flagged package is, not a mandate to update everything.
- Dependency-graph inspection (`npm ls <pkg>`, NuGet dependency chains) — only when the
  parent of a transitive advisory is unclear.

## Output

1. **Verdict** — PASS / PASS WITH NOTES / CHANGES RECOMMENDED / BLOCKED.
2. **Scope scanned** — manifests and lockfiles inspected.
3. **Findings** — per package: severity, direct/transitive, likely parent, runtime vs
   dev-only.
4. **Remediation options** — smallest safe change per finding; majors flagged as
   optional/breaking.
5. **Risks / deferrals** — advisories with no safe fix, deferred majors, dev-only items.
6. **Next recommended action** — one short step (the user may then request the
   remediation as its own change, and a `deploy-smoke` afterward if wanted).

If a scan cannot run or its result is unknown, say unknown — never assume clean.
