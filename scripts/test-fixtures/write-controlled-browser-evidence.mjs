import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const PASSED_INSPECTION = Object.freeze({
  status: 'passed',
  invalidDiagnosticFiles: [],
  removedRawFiles: [],
  rewrittenTextFiles: [],
  symlinks: [],
  unexpectedFiles: [],
  unsafeScreenshot: false,
});

export function writeControlledBrowserEvidence(root, options = {}) {
  const runDirectory = resolve(root, 'playwright-evidence', 'probe');
  const canonicalEvidenceDirectory = resolve(runDirectory, 'canonical/evidence');
  const statefulEvidenceDirectory = resolve(runDirectory, 'stateful/child-001/evidence');
  mkdirSync(canonicalEvidenceDirectory, { recursive: true });
  mkdirSync(statefulEvidenceDirectory, { recursive: true });

  const status = options.status ?? 'passed';
  const mobileStatus = options.mobileStatus ?? status;
  const canonicalTests = [{
    id: 'canonical-probe',
    journey: null,
    retry: 0,
    status: 'passed',
    durationMs: 1,
  }];
  const statefulTests = [{
    id: 'mobile-probe',
    journey: 'mobile.probe',
    retry: 0,
    status: mobileStatus,
    durationMs: 1,
  }];
  writeChildEvidence(
    resolve(canonicalEvidenceDirectory, 'playwright-results.json'),
    'canonical-child',
    'passed',
    canonicalTests,
  );
  writeChildEvidence(
    resolve(statefulEvidenceDirectory, 'playwright-results.json'),
    options.evidenceRunId ?? 'stateful-child',
    status,
    statefulTests,
  );

  const canonical = {
    schemaVersion: 1,
    kind: 'canonical-read',
    runId: 'canonical-child',
    status: 'passed',
    startedAt: '2026-01-01T00:00:00.000Z',
    completedAt: '2026-01-01T00:00:00.004Z',
    durationMs: 4,
    evidence: 'evidence/playwright-results.json',
    phases: phases(['capabilityInspection', 'applicationStartup', 'testExecution', 'applicationShutdown']),
    inspection: PASSED_INSPECTION,
  };
  const scenarioInspection = options.inspection ?? PASSED_INSPECTION;
  const scenario = {
    runId: 'stateful-child',
    selector: 'e2e/probe.e2e.ts:1',
    policy: 'mutating',
    fixtureProfile: 'probe',
    backgroundActivities: [],
    evidence: 'child-001/evidence/playwright-results.json',
    status,
    startedAt: '2026-01-01T00:00:00.000Z',
    completedAt: '2026-01-01T00:00:00.007Z',
    durationMs: 7,
    phases: phases([
      'lockAcquisition',
      'initialReset',
      'applicationStartup',
      'testExecution',
      'applicationShutdown',
      'finalReset',
      'lockRelease',
    ], status),
    cleanup: status === 'passed' ? 'passed' : 'failed',
    inspection: scenarioInspection,
  };
  const stateful = {
    schemaVersion: 1,
    kind: 'stateful',
    runId: 'stateful-aggregate',
    status,
    startedAt: '2026-01-01T00:00:00.000Z',
    completedAt: '2026-01-01T00:00:00.008Z',
    durationMs: 8,
    provisioningPhases: provisioningPhases(),
    scenarios: [scenario],
  };
  writeFileSync(resolve(runDirectory, 'canonical/canonical-results.json'), JSON.stringify(canonical));
  writeFileSync(resolve(runDirectory, 'stateful/stateful-results.json'), JSON.stringify(stateful));

  const tests = [...canonicalTests, ...statefulTests];
  writeFileSync(resolve(runDirectory, 'playwright-run.json'), JSON.stringify({
    schemaVersion: 1,
    kind: 'complete-playwright',
    runId: 'probe',
    status,
    startedAt: '2026-01-01T00:00:00.000Z',
    completedAt: '2026-01-01T00:00:00.009Z',
    durationMs: 9,
    provisioningPhases: provisioningPhases(),
    declaredTestCount: tests.length,
    counts: countStatuses(tests),
    tests,
    children: [
      { ...canonical, report: 'canonical/canonical-results.json' },
      { ...stateful, report: 'stateful/stateful-results.json' },
    ],
  }));
  return runDirectory;
}

function writeChildEvidence(path, runId, status, tests) {
  writeFileSync(path, JSON.stringify({
    schemaVersion: 1,
    runId,
    status,
    declaredTestCount: tests.length,
    counts: countStatuses(tests),
    tests,
  }));
}

function countStatuses(tests) {
  return tests.reduce((counts, test) => ({
    ...counts,
    [test.status]: (counts[test.status] ?? 0) + 1,
  }), {});
}

function phases(names, status = 'passed') {
  return names.map((name) => ({
    name,
    status: name === 'testExecution' ? status : 'passed',
    durationMs: 1,
  }));
}

function provisioningPhases() {
  return phases([
    'dependencyProvisioning',
    'chromiumProvisioning',
    'certificateProvisioning',
    'buildProvisioning',
  ]);
}
