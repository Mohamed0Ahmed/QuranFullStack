# Dependency advisory evaluation

`dependency-advisory-policy.json` and `dependency-advisory-waivers.json` are the provider-neutral
contract for risk-based NuGet and npm advisory evaluation. The repository runner performs locked
NuGet restore, scans direct and transitive NuGet packages plus available upgrade candidates, compares
production-only and complete npm audits, reconstructs dependency paths from the committed locks, and
writes structured evidence.

This contract does not select a CI provider and does not update dependencies. Provider adapters must
invoke it in these three cases:

| Trigger | Provider responsibility | Invocation |
| --- | --- | --- |
| Weekly | Allocate a run at least once every seven days. | `node scripts/run-dependency-advisory-evaluation.mjs --trigger weekly` |
| Lockfile change | Run when any `Backend/**/packages.lock.json` or `Frontend/quran-dashboard-ui/package-lock.json` path changes. | `node scripts/run-dependency-advisory-evaluation.mjs --trigger lockfile-change` |
| Release | Require a passing result before release promotion. | `node scripts/run-dependency-advisory-evaluation.mjs --trigger release` |

Dependency evaluation is not part of the nightly lane. The runner rejects `--trigger nightly`, and a
provider must not hide one of the three required invocations inside nightly orchestration.

## Risk decision

NuGet projects are classified explicitly in the policy. The API, application/domain/infrastructure,
shared library, AccessAdmin, and DataImporter graphs are production surfaces. The xUnit project and
TestRuntime and the Tests project are development/test surfaces. For npm, presence in `npm audit --omit=dev` is the
production signal; the complete audit retains build/test-only findings as notes.

Every finding records:

- ecosystem, package, resolved version, advisory, and severity;
- direct or transitive exposure and one exact path reconstructed from the committed lock;
- production or development scope and reachability decision;
- whether the scanner reports a fix, plus the smallest parent-upgrade direction where known.

If an exact production path cannot be reconstructed, evaluation fails closed instead of inventing a
direct path. An unresolved development-only path stays visible as controlled evidence because the
production-only audit has already excluded that install occurrence. NuGet's advisory report does not
identify the first fixed version, so an outdated scan runs only when a NuGet advisory needs remediation
sizing. Its result is retained only as an upgrade candidate and never claimed as confirmed fixed. npm
fix objects preserve their reported version and mark semantic-major candidates as optional/breaking.

A development/test-only advisory stays visible but does not block automatically. A production finding
fails closed until an exact waiver supplies the human reachability and mitigation assessment. A valid
waiver can accept a limited or unreachable exposure until its expiry. A high or critical production
finding assessed as reachable always blocks, even if a waiver record exists. Missing and expired
production waivers also block, so absence of analysis cannot be mistaken for safety.

The tracked waiver file starts empty. Add a waiver only after reviewing the exact result path. Each
entry must contain all of this evidence:

```json
{
  "id": "DEP-2026-001",
  "ecosystem": "npm",
  "advisory": "https://github.com/advisories/GHSA-example",
  "package": "example-package",
  "dependencyPath": [
    "Frontend/quran-dashboard-ui/package.json",
    "direct-or-parent-package",
    "example-package"
  ],
  "reachability": "not-reachable",
  "rationale": "Why the vulnerable operation cannot be reached in the deployed surface.",
  "owner": "named-maintainer",
  "mitigation": "The concrete upgrade or compensating-control plan.",
  "expiresAt": "2026-09-30",
  "approvedBy": "security-maintainer",
  "approvedAt": "2026-08-31"
}
```

`reachability` is one of `reachable`, `limited`, or `not-reachable`. Matching is exact on ecosystem,
advisory, package, and the complete dependency path, so a waiver for one parent or project cannot
silently cover another path.

## Running and evidence

Inspect the commands without accessing advisory services:

```bash
node scripts/run-dependency-advisory-evaluation.mjs --trigger weekly --dry-run
node scripts/verify-dependency-advisory-contract.mjs
```

Run an evaluation with an explicit provider artifact directory:

```bash
node scripts/run-dependency-advisory-evaluation.mjs \
  --trigger release \
  --results-dir /path/to/dependency-advisory-results
```

The runner inherits ordinary NuGet/npm cache variables, so isolated runners may supply
`NUGET_PACKAGES` and `npm_config_cache`. It starts no application, database, container, or listening
service. Advisory services require network access. The locked restore is CI input provisioning for
this evaluation contract; it is not package remediation and does not expand the read-only audit
skill's responsibility.

The results directory contains normalized `nuget.json`, `nuget-outdated.json`,
`npm-production.json`, `npm-all.json`, and `evaluation.json`. The evaluation records its trigger and
timestamp, all findings, exact paths, preserved scanner mitigation evidence, waiver plans, expired
waivers, summary counts, and blocking reasons. Scan/parse failure also produces a blocked evaluation.
Exit code `0` means `passed` or `passed-with-notes`; exit code `1` means blocked; invocation/contract
errors use exit code `2`.
