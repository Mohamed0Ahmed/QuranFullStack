import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  evaluateDependencyAdvisories,
  validateDependencyAdvisoryContract,
} from './dependency-advisory-contract.mjs';

const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const POLICY_PATH = resolve(REPOSITORY_ROOT, 'dependency-advisory-policy.json');
const WAIVERS_PATH = resolve(REPOSITORY_ROOT, 'dependency-advisory-waivers.json');

const policy = JSON.parse(readFileSync(POLICY_PATH, 'utf8'));
const waiversDocument = JSON.parse(readFileSync(WAIVERS_PATH, 'utf8'));

validateDependencyAdvisoryContract({
  policy,
  repositoryRoot: REPOSITORY_ROOT,
  waiversDocument,
});

assert.deepEqual(
  policy.triggers.map(({ id }) => id),
  ['weekly', 'lockfile-change', 'release'],
  'The provider-neutral contract must expose exactly the approved invocation triggers.',
);
assert.equal(policy.triggers[0].intervalDays, 7);
assert.deepEqual(
  policy.triggers[1].paths,
  ['Backend/**/packages.lock.json', 'Frontend/quran-dashboard-ui/package-lock.json'],
);
assert.equal(policy.triggers[2].requiredBeforePromotion, true);
assert.deepEqual(policy.excludedLanes, ['nightly']);

const emptyNugetReport = { version: 1, projects: [] };
const emptyNugetOutdatedReport = { version: 1, projects: [] };
const emptyNpmReport = emptyNpmAudit();
const packageLock = {
  lockfileVersion: 3,
  packages: {
    '': {
      dependencies: { '@angular/common': '20.3.26' },
      devDependencies: { '@angular/build': '20.3.29' },
    },
    'node_modules/@angular/common': { version: '20.3.26' },
    'node_modules/@angular/build': {
      version: '20.3.29',
      dependencies: { vite: '7.3.3' },
    },
    'node_modules/vite': { version: '7.3.3' },
  },
};

const clean = evaluateDependencyAdvisories({
  evaluatedAt: '2026-08-31T06:00:00.000Z',
  nugetLocks: {},
  nugetOutdatedReport: emptyNugetOutdatedReport,
  nugetReport: emptyNugetReport,
  npmAllReport: emptyNpmReport,
  npmPackageLock: packageLock,
  npmProductionReport: emptyNpmReport,
  policy,
  repositoryRoot: REPOSITORY_ROOT,
  trigger: 'weekly',
  waiversDocument,
});
assert.equal(clean.status, 'passed');
assert.equal(clean.blockingFindings.length, 0);

const highProductionNpmReport = npmAudit({
  '@angular/common': npmVulnerability({
    advisory: 'https://github.com/advisories/GHSA-test-high',
    direct: true,
    severity: 'high',
  }),
});
const unassessedProduction = evaluate({
  npmAllReport: highProductionNpmReport,
  npmProductionReport: highProductionNpmReport,
});
assert.equal(unassessedProduction.status, 'blocked');
assert.deepEqual(unassessedProduction.blockingFindings[0].dependencyPath, [
  'Frontend/quran-dashboard-ui/package.json',
  '@angular/common',
]);
assert.equal(unassessedProduction.blockingFindings[0].directness, 'direct');
assert.equal(unassessedProduction.blockingFindings[0].productionExposure, true);
assert.equal(unassessedProduction.blockingFindings[0].reachability, 'unassessed');
assert.equal(unassessedProduction.blockingFindings[0].blockReason, 'missing-production-waiver');

const acceptedUnreachable = evaluate({
  npmAllReport: highProductionNpmReport,
  npmProductionReport: highProductionNpmReport,
  waivers: [
    waiver({
      advisory: 'https://github.com/advisories/GHSA-test-high',
      approvedAt: '2026-08-01',
      dependencyPath: ['Frontend/quran-dashboard-ui/package.json', '@angular/common'],
      packageName: '@angular/common',
    }),
  ],
});
assert.equal(acceptedUnreachable.status, 'passed-with-notes');
assert.equal(acceptedUnreachable.findings[0].waiver.id, 'WAIVER-TEST');
assert.equal(acceptedUnreachable.findings[0].reachability, 'not-reachable');
assert.equal(acceptedUnreachable.findings[0].mitigation.available, true);
assert.match(acceptedUnreachable.findings[0].mitigation.recommendation, /Upgrade @angular\/common/);
assert.match(acceptedUnreachable.findings[0].mitigation.waiverPlan, /next compatible patch/);
assert.equal(acceptedUnreachable.blockingFindings.length, 0);

const reachableHigh = evaluate({
  npmAllReport: highProductionNpmReport,
  npmProductionReport: highProductionNpmReport,
  waivers: [
    waiver({
      advisory: 'https://github.com/advisories/GHSA-test-high',
      dependencyPath: ['Frontend/quran-dashboard-ui/package.json', '@angular/common'],
      packageName: '@angular/common',
      reachability: 'reachable',
    }),
  ],
});
assert.equal(reachableHigh.status, 'blocked');
assert.equal(reachableHigh.blockingFindings[0].blockReason, 'confirmed-high-critical-production');

const expiredWaiver = evaluate({
  npmAllReport: highProductionNpmReport,
  npmProductionReport: highProductionNpmReport,
  waivers: [
    waiver({
      advisory: 'https://github.com/advisories/GHSA-test-high',
      approvedAt: '2026-08-01',
      dependencyPath: ['Frontend/quran-dashboard-ui/package.json', '@angular/common'],
      expiresAt: '2026-08-30',
      packageName: '@angular/common',
    }),
  ],
});
assert.equal(expiredWaiver.status, 'blocked');
assert.equal(expiredWaiver.blockingFindings[0].blockReason, 'expired-production-waiver');
assert.equal(expiredWaiver.summary.blocking, 1);

const devOnlyNpmReport = npmAudit({
  vite: npmVulnerability({
    advisory: 'https://github.com/advisories/GHSA-test-dev',
    direct: false,
    node: 'node_modules/vite',
    severity: 'high',
  }),
});
const developmentOnly = evaluate({ npmAllReport: devOnlyNpmReport });
assert.equal(developmentOnly.status, 'passed-with-notes');
assert.deepEqual(developmentOnly.findings[0].dependencyPath, [
  'Frontend/quran-dashboard-ui/package.json',
  '@angular/build',
  'vite',
]);
assert.equal(developmentOnly.findings[0].directness, 'transitive');
assert.equal(developmentOnly.findings[0].productionExposure, false);
assert.equal(developmentOnly.findings[0].scope, 'development');

const informationalDevelopment = evaluate({
  npmAllReport: npmAudit({
    'info-only': npmVulnerability({
      advisory: 'https://github.com/advisories/GHSA-test-info',
      direct: false,
      name: 'info-only',
      node: 'node_modules/info-only',
      severity: 'info',
    }),
  }),
  npmPackageLock: {
    lockfileVersion: 3,
    packages: {
      '': { devDependencies: { 'dev-parent': '1.0.0' } },
      'node_modules/dev-parent': {
        version: '1.0.0',
        optionalDependencies: { 'info-only': '1.0.0' },
      },
      'node_modules/info-only': { version: '1.0.0' },
    },
  },
});
assert.equal(informationalDevelopment.status, 'passed-with-notes');
assert.equal(informationalDevelopment.findings[0].severity, 'info');
assert.deepEqual(informationalDevelopment.findings[0].dependencyPath, [
  'Frontend/quran-dashboard-ui/package.json',
  'dev-parent',
  'info-only',
]);

const sharedNodeReport = npmAudit({
  shared: npmVulnerability({
    advisory: 'https://github.com/advisories/GHSA-test-shared',
    direct: true,
    name: 'shared',
    node: 'node_modules/shared',
    severity: 'high',
  }),
});
const sharedNodeProduction = evaluate({
  npmAllReport: sharedNodeReport,
  npmPackageLock: {
    lockfileVersion: 3,
    packages: {
      '': {
        dependencies: { 'production-parent': '1.0.0' },
        devDependencies: { shared: '1.0.0' },
      },
      'node_modules/production-parent': {
        version: '1.0.0',
        dependencies: { shared: '1.0.0' },
      },
      'node_modules/shared': { version: '1.0.0' },
    },
  },
  npmProductionReport: sharedNodeReport,
});
assert.equal(sharedNodeProduction.status, 'blocked');
assert.deepEqual(sharedNodeProduction.findings[0].dependencyPath, [
  'Frontend/quran-dashboard-ui/package.json',
  'production-parent',
  'shared',
]);
assert.equal(sharedNodeProduction.findings[0].directness, 'transitive');
assert.equal(sharedNodeProduction.findings[0].productionExposure, true);

const unresolvedDevelopmentPath = evaluate({
  npmAllReport: npmAudit({
    orphan: npmVulnerability({
      advisory: 'https://github.com/advisories/GHSA-test-orphan',
      direct: false,
      name: 'orphan',
      node: 'node_modules/orphan',
      severity: 'low',
    }),
  }),
  npmPackageLock: {
    lockfileVersion: 3,
    packages: {
      '': { devDependencies: {} },
      'node_modules/orphan': { version: '1.0.0' },
    },
  },
});
assert.equal(unresolvedDevelopmentPath.status, 'passed-with-notes');
assert.equal(unresolvedDevelopmentPath.findings[0].dependencyPath, null);
assert.equal(unresolvedDevelopmentPath.findings[0].directness, 'unknown');
assert.equal(unresolvedDevelopmentPath.findings[0].blockReason, null);

const transitiveProduction = evaluate({
  npmAllReport: devOnlyNpmReport,
  npmPackageLock: {
    lockfileVersion: 3,
    packages: {
      '': { dependencies: { 'runtime-parent': '1.0.0' } },
      'node_modules/runtime-parent': {
        version: '1.0.0',
        dependencies: { vite: '7.3.3' },
      },
      'node_modules/vite': { version: '7.3.3' },
    },
  },
  npmProductionReport: devOnlyNpmReport,
});
assert.equal(transitiveProduction.status, 'blocked');
assert.deepEqual(transitiveProduction.blockingFindings[0].dependencyPath, [
  'Frontend/quran-dashboard-ui/package.json',
  'runtime-parent',
  'vite',
]);
assert.equal(transitiveProduction.blockingFindings[0].directness, 'transitive');
assert.equal(transitiveProduction.blockingFindings[0].productionExposure, true);

const nugetProject = 'Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj';
const nugetDevelopmentOnly = evaluate({
  nugetLocks: {
    [nugetProject]: {
      version: 1,
      dependencies: {
        'net10.0': {
          'SSH.NET': { type: 'Transitive', resolved: '2024.2.0' },
          Testcontainers: {
            type: 'Transitive',
            resolved: '4.4.0',
            dependencies: { 'SSH.NET': '2024.2.0' },
          },
          'Testcontainers.PostgreSql': {
            type: 'Direct',
            resolved: '4.4.0',
            dependencies: { Testcontainers: '4.4.0' },
          },
        },
      },
    },
  },
  nugetReport: {
    version: 1,
    projects: [
      {
        path: resolve(REPOSITORY_ROOT, nugetProject),
        frameworks: [
          {
            framework: 'net10.0',
            transitivePackages: [
              {
                id: 'SSH.NET',
                resolvedVersion: '2024.2.0',
                vulnerabilities: [
                  {
                    advisoryurl: 'https://github.com/advisories/GHSA-test-nuget',
                    severity: 'High',
                  },
                ],
              },
            ],
          },
        ],
      },
    ],
  },
});
assert.equal(nugetDevelopmentOnly.status, 'passed-with-notes');
assert.deepEqual(nugetDevelopmentOnly.findings[0].dependencyPath, [
  nugetProject,
  'Testcontainers.PostgreSql',
  'Testcontainers',
  'SSH.NET',
]);
assert.equal(nugetDevelopmentOnly.findings[0].productionExposure, false);

const nugetProductionProject = 'Backend/infrastructure/QuranDashboard.Infrastructure/QuranDashboard.Infrastructure.csproj';
const nugetProduction = evaluate({
  nugetLocks: {
    [nugetProductionProject]: {
      version: 1,
      dependencies: {
        'net10.0': {
          'Parent.Package': {
            type: 'Direct',
            resolved: '1.0.0',
            dependencies: { 'Vulnerable.Package': '2.0.0' },
          },
          'Vulnerable.Package': { type: 'Transitive', resolved: '2.0.0' },
        },
      },
    },
  },
  nugetReport: {
    version: 1,
    projects: [
      {
        path: resolve(REPOSITORY_ROOT, nugetProductionProject),
        frameworks: [
          {
            framework: 'net10.0',
            transitivePackages: [
              {
                id: 'Vulnerable.Package',
                resolvedVersion: '2.0.0',
                vulnerabilities: [
                  {
                    advisoryurl: 'https://github.com/advisories/GHSA-test-nuget-production',
                    severity: 'Critical',
                  },
                ],
              },
            ],
          },
        ],
      },
    ],
  },
  nugetOutdatedReport: {
    version: 1,
    projects: [
      {
        path: resolve(REPOSITORY_ROOT, nugetProductionProject),
        frameworks: [
          {
            framework: 'net10.0',
            transitivePackages: [
              {
                id: 'Vulnerable.Package',
                latestVersion: '2.1.0',
                resolvedVersion: '2.0.0',
              },
            ],
          },
        ],
      },
    ],
  },
});
assert.equal(nugetProduction.status, 'blocked');
assert.equal(nugetProduction.blockingFindings[0].severity, 'critical');
assert.deepEqual(nugetProduction.blockingFindings[0].dependencyPath, [
  nugetProductionProject,
  'Parent.Package',
  'Vulnerable.Package',
]);
assert.equal(nugetProduction.blockingFindings[0].directness, 'transitive');
assert.equal(nugetProduction.blockingFindings[0].productionExposure, true);
assert.equal(nugetProduction.blockingFindings[0].mitigation.available, 'candidate');
assert.equal(nugetProduction.blockingFindings[0].mitigation.latestVersion, '2.1.0');

const wrongNugetFramework = evaluate({
  nugetLocks: {
    [nugetProductionProject]: {
      version: 1,
      dependencies: {
        'net10.0': {
          'Parent.Package': {
            type: 'Direct',
            resolved: '1.0.0',
            dependencies: { 'Vulnerable.Package': '2.0.0' },
          },
          'Vulnerable.Package': { type: 'Transitive', resolved: '2.0.0' },
        },
      },
    },
  },
  nugetReport: {
    version: 1,
    projects: [
      {
        path: resolve(REPOSITORY_ROOT, nugetProductionProject),
        frameworks: [
          {
            framework: 'net9.0',
            transitivePackages: [
              {
                id: 'Vulnerable.Package',
                resolvedVersion: '2.0.0',
                vulnerabilities: [
                  {
                    advisoryurl: 'https://github.com/advisories/GHSA-wrong-framework',
                    severity: 'High',
                  },
                ],
              },
            ],
          },
        ],
      },
    ],
  },
});
assert.equal(wrongNugetFramework.status, 'blocked');
assert.equal(wrongNugetFramework.blockingFindings[0].dependencyPath, null);
assert.equal(wrongNugetFramework.blockingFindings[0].blockReason, 'unresolved-dependency-path');

const majorFixReport = npmAudit({
  '@angular/common': npmVulnerability({
    advisory: 'https://github.com/advisories/GHSA-test-major',
    direct: true,
    fixAvailable: {
      isSemVerMajor: true,
      name: '@angular/common',
      version: '21.0.0',
    },
    severity: 'high',
  }),
});
const majorFix = evaluate({
  npmAllReport: majorFixReport,
  npmProductionReport: majorFixReport,
});
assert.equal(majorFix.findings[0].mitigation.changeType, 'major-breaking');
assert.match(majorFix.findings[0].mitigation.recommendation, /Optional breaking upgrade/);

assert.throws(
  () => validateDependencyAdvisoryContract({
    policy,
    repositoryRoot: REPOSITORY_ROOT,
    waiversDocument: {
      schemaVersion: 1,
      waivers: [{
        advisory: 'https://github.com/advisories/GHSA-incomplete',
        ecosystem: 'npm',
        id: 'INCOMPLETE',
      }],
    },
  }),
  /package/,
  'Waivers without the required package/path evidence must fail contract validation.',
);

assert.throws(
  () => validateDependencyAdvisoryContract({
    policy,
    repositoryRoot: REPOSITORY_ROOT,
    today: '2026-08-31',
    waiversDocument: {
      schemaVersion: 1,
      waivers: [waiver({
        advisory: 'https://github.com/advisories/GHSA-invalid-date',
        dependencyPath: ['Frontend/quran-dashboard-ui/package.json', '@angular/common'],
        expiresAt: '2026-02-31',
        packageName: '@angular/common',
      })],
    },
  }),
  /real calendar date/,
);

assert.throws(
  () => validateDependencyAdvisoryContract({
    policy,
    repositoryRoot: REPOSITORY_ROOT,
    today: '2026-08-31',
    waiversDocument: {
      schemaVersion: 1,
      waivers: [waiver({
        advisory: 'https://github.com/advisories/GHSA-future-approval',
        approvedAt: '2026-09-01',
        dependencyPath: ['Frontend/quran-dashboard-ui/package.json', '@angular/common'],
        packageName: '@angular/common',
      })],
    },
  }),
  /approvedAt must not be in the future/,
);

assert.throws(
  () => validateDependencyAdvisoryContract({
    policy,
    repositoryRoot: REPOSITORY_ROOT,
    today: '2026-08-31',
    waiversDocument: {
      schemaVersion: 1,
      waivers: [waiver({
        advisory: 'https://github.com/advisories/GHSA-reversed-dates',
        approvedAt: '2026-08-20',
        dependencyPath: ['Frontend/quran-dashboard-ui/package.json', '@angular/common'],
        expiresAt: '2026-08-19',
        packageName: '@angular/common',
      })],
    },
  }),
  /approvedAt must be on or before expiresAt/,
);

const runnerPath = resolve(REPOSITORY_ROOT, 'scripts/run-dependency-advisory-evaluation.mjs');
const dryRun = spawnSync(process.execPath, [runnerPath, '--trigger', 'weekly', '--dry-run'], {
  cwd: REPOSITORY_ROOT,
  encoding: 'utf8',
});
assert.equal(dryRun.status, 0, dryRun.stderr);
const dryRunContract = JSON.parse(dryRun.stdout);
assert.equal(dryRunContract.trigger, 'weekly');
assert.deepEqual(
  dryRunContract.scans.map(({ id }) => id),
  [
    'locked-nuget-restore',
    'nuget-advisories',
    'nuget-upgrade-candidates',
    'npm-production-advisories',
    'npm-all-advisories',
  ],
);
assert.equal(
  dryRunContract.scans.find(({ id }) => id === 'nuget-upgrade-candidates').condition,
  'nuget-findings',
);

const nightly = spawnSync(process.execPath, [runnerPath, '--trigger', 'nightly', '--dry-run'], {
  cwd: REPOSITORY_ROOT,
  encoding: 'utf8',
});
assert.equal(nightly.status, 2);
assert.match(nightly.stderr, /nightly is excluded/);

console.log('Dependency advisory contract passed.');

function evaluate(overrides = {}) {
  return evaluateDependencyAdvisories({
    evaluatedAt: '2026-08-31T06:00:00.000Z',
    nugetLocks: overrides.nugetLocks ?? {},
    nugetOutdatedReport: overrides.nugetOutdatedReport ?? emptyNugetOutdatedReport,
    nugetReport: overrides.nugetReport ?? emptyNugetReport,
    npmAllReport: overrides.npmAllReport ?? emptyNpmReport,
    npmPackageLock: overrides.npmPackageLock ?? packageLock,
    npmProductionReport: overrides.npmProductionReport ?? emptyNpmReport,
    policy,
    repositoryRoot: REPOSITORY_ROOT,
    trigger: 'weekly',
    waiversDocument: {
      schemaVersion: 1,
      waivers: overrides.waivers ?? [],
    },
  });
}

function emptyNpmAudit() {
  return npmAudit({});
}

function npmAudit(vulnerabilities) {
  return {
    auditReportVersion: 2,
    metadata: {
      vulnerabilities: {
        critical: 0,
        high: 0,
        info: 0,
        low: 0,
        moderate: 0,
        total: Object.keys(vulnerabilities).length,
      },
    },
    vulnerabilities,
  };
}

function npmVulnerability({
  advisory,
  direct,
  fixAvailable = true,
  name = null,
  node = null,
  severity,
}) {
  const packageName = name ?? node?.split('/').at(-1) ?? '@angular/common';
  return {
    effects: [],
    fixAvailable,
    isDirect: direct,
    name: packageName,
    nodes: [node ?? `node_modules/${packageName}`],
    range: '<99.0.0',
    severity,
    via: [
      {
        dependency: packageName,
        name: packageName,
        severity,
        source: 123,
        title: 'Synthetic advisory',
        url: advisory,
      },
    ],
  };
}

function waiver({
  advisory,
  approvedAt = '2026-08-31',
  dependencyPath,
  expiresAt = '2026-09-30',
  packageName,
  reachability = 'not-reachable',
}) {
  return {
    advisory,
    approvedAt,
    approvedBy: 'security-maintainer',
    dependencyPath,
    ecosystem: 'npm',
    expiresAt,
    id: 'WAIVER-TEST',
    mitigation: 'Upgrade at the next compatible patch and keep the affected path disabled meanwhile.',
    owner: 'frontend-maintainer',
    package: packageName,
    rationale: 'The vulnerable operation is not reachable in the deployed browser application.',
    reachability,
  };
}
