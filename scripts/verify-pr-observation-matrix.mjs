import { execFileSync } from 'node:child_process';
import {
  existsSync,
  mkdtempSync,
  readFileSync,
  rmSync,
  writeFileSync,
} from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadObservationMatrix } from './pr-observation-contract.mjs';

const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const MATRIX_PATH = resolve(REPOSITORY_ROOT, 'pr-observation-matrix.json');
const RUNNER_PATH = resolve(REPOSITORY_ROOT, 'scripts/run-pr-observation-job.mjs');

const errors = [];

if (!existsSync(MATRIX_PATH)) {
  errors.push('pr-observation-matrix.json is missing.');
}
if (!existsSync(RUNNER_PATH)) {
  errors.push('scripts/run-pr-observation-job.mjs is missing.');
}

if (errors.length === 0) {
  const matrix = loadObservationMatrix(MATRIX_PATH, REPOSITORY_ROOT);
  const expectedJobIds = [
    'backend-pr',
    'api-contract-model',
    'frontend-policy-build',
    'critical-chromium',
  ];

  check(
    JSON.stringify(matrix.durationComponents)
      === JSON.stringify([
        'provisioning',
        'databasePreparation',
        'applicationStartup',
        'testExecution',
      ]),
    'Observed duration must explicitly include provisioning, database, startup, and test execution.',
  );
  check(
    JSON.stringify(matrix.jobs?.map(({ id }) => id)) === JSON.stringify(expectedJobIds),
    `The matrix must contain exactly these four jobs: ${expectedJobIds.join(', ')}.`,
  );

  for (const job of matrix.jobs ?? []) {
    const expectedBlocking = job.id !== 'critical-chromium';
    check(
      job.policy?.blocking === expectedBlocking,
      `${job.id} blocking policy must match the adopted Local-first contract.`,
    );
    check(job.policy?.timeoutSeconds === 720, `${job.id} must have a 12-minute outer timeout.`);
    check(
      job.commands?.some(({ phase }) => phase === 'provisioning'),
      `${job.id} must include provisioning inside the observed duration.`,
    );
    check(
      job.commands?.some(({ phase }) => phase === 'execution'),
      `${job.id} must include execution inside the observed duration.`,
    );
  }

  const backend = findJob(matrix, 'backend-pr');
  check(
    backend?.inputContract?.candidate === 'quran_dashboard_test',
    'The Backend candidate must remain the persistent Test Database capability.',
  );
  check(
    backend?.inputContract?.compactFixtureException === undefined,
    'The Backend candidate must not declare a compact-fixture exception.',
  );
  check(
    hasCommand(backend, 'Backend/scripts/test-backend', ['pre-pr'])
      || hasCommand(backend, 'node', ['scripts/test', 'pre-pr']),
    'The Backend job must execute the supported pre-pr lane.',
  );

  const contract = findJob(matrix, 'api-contract-model');
  check(
    hasCommand(contract, 'npm', ['ci']),
    'The API contract job must install locked Frontend dependencies on its independent runner.',
  );
  check(
    hasCommand(contract, 'Backend/scripts/check-api-contract'),
    'The API contract job must check the generated contract.',
  );
  check(
    hasCommand(contract, 'Backend/scripts/check-pending-model', ['--build']),
    'The API contract job must check the pending EF model.',
  );

  const frontend = findJob(matrix, 'frontend-policy-build');
  check(
    hasNpmScript(frontend, 'test:pre-pr'),
    'The Frontend job must run policy, type, and build checks through test:pre-pr.',
  );
  check(
    hasNpmScript(frontend, 'e2e:typecheck'),
    'The Frontend job must type-check the Playwright layer.',
  );

  const browser = findJob(matrix, 'critical-chromium');
  check(
    hasNpmScript(browser, 'e2e:provision'),
    'The critical Chromium job must perform controlled provisioning.',
  );
  check(
    hasNpmScript(browser, 'e2e:critical'),
    'The critical Chromium job must run controlled critical journeys.',
  );
  check(
    JSON.stringify(browser?.policy?.journeyGroups) === JSON.stringify([
      {
        id: 'quran-fidelity',
        blocking: true,
        journeys: [
          'quran-fidelity.mushaf-font-rendering',
          'quran-fidelity.mushaf-mobile',
          'quran-location.sanitization',
        ],
      },
      {
        id: 'sessions-permissions',
        blocking: true,
        journeys: ['device-session.lifecycle', 'permission.lifecycle'],
      },
      {
        id: 'linking',
        blocking: true,
        journeys: ['linking.successful-owner', 'linking.successful-owner-mobile'],
      },
      {
        id: 'phrase-search',
        blocking: true,
        journeys: [
          'phrase-search.available-add-to-workspace',
          'phrase-search.canonical-read',
          'phrase-search.unavailable-stale',
        ],
      },
      {
        id: 'abwab-projection',
        blocking: true,
        journeys: [
          'abwab.inclusion-conflict-evidence',
          'abwab.inclusion-projection',
          'abwab.inclusion-revoked-permission',
        ],
      },
      {
        id: 'word-explorer',
        blocking: true,
        journeys: ['word-explorer.canonical-read'],
      },
    ]),
    'All five critical journey groups must be blocking after pilot #106.',
  );

  verifyBlockingFailureContract();
  verifyJourneyGroupEnforcementContract();
  verifyOrphanDescendantCleanupContract();
}

if (errors.length > 0) {
  console.error(errors.map((error) => `ERROR: ${error}`).join('\n'));
  process.exit(1);
}

console.log('PR observation matrix contract passed.');

function check(condition, message) {
  if (!condition) {
    errors.push(message);
  }
}

function findJob(matrix, id) {
  return matrix.jobs?.find((job) => job.id === id);
}

function hasCommand(job, executable, requiredArguments = []) {
  return job?.commands?.some(
    (command) =>
      command.executable === executable
      && requiredArguments.every((argument) => command.arguments?.includes(argument)),
  );
}

function hasNpmScript(job, script) {
  return hasCommand(job, 'npm', ['run', script]);
}

function verifyBlockingFailureContract() {
  const temporaryRoot = mkdtempSync(resolve(tmpdir(), 'qdb-pr-observation-'));
  const fixturePath = resolve(temporaryRoot, 'matrix.json');
  const resultsDirectory = resolve(temporaryRoot, 'results');
  const fixture = {
    schemaVersion: 1,
    id: 'failure-contract-probe',
    scheduling: 'parallel',
    durationScope: 'probe duration with queue time excluded',
    durationComponents: ['provisioning', 'testExecution'],
    jobs: [
      {
        id: 'failure-probe',
        title: 'Failure probe',
        policy: {
          blocking: true,
          maxAttempts: 1,
          timeoutSeconds: 5,
          queueTimeIncluded: false,
        },
        commands: [
          {
            id: 'fail-once',
            phase: 'execution',
            cwd: '.',
            executable: process.execPath,
            arguments: ['-e', 'process.exit(7)'],
          },
        ],
      },
    ],
  };

  try {
    writeFileSync(fixturePath, `${JSON.stringify(fixture, null, 2)}\n`);
    let exitCode = 0;
    try {
      execFileSync(
        process.execPath,
        [
          RUNNER_PATH,
          '--matrix',
          fixturePath,
          '--job',
          'failure-probe',
          '--results-dir',
          resultsDirectory,
        ],
        { cwd: REPOSITORY_ROOT, stdio: 'pipe' },
      );
    } catch (error) {
      exitCode = error.status ?? 1;
    }
    const result = JSON.parse(readFileSync(resolve(resultsDirectory, 'job-result.json'), 'utf8'));
    check(exitCode === 1, 'A failed blocking regression job must return exit code 1.');
    check(result.mode === 'blocking', 'The result must identify the regression job as blocking.');
    check(result.status === 'failed', 'Blocking mode must retain a failed job status.');
    check(
      result.firstAttemptStatus === 'failed',
      'Blocking mode must retain the first-attempt failure.',
    );
    check(
      result.maxAttempts === 1 && result.attemptsExecuted === 1,
      'Blocking mode must execute exactly one attempt.',
    );
    check(
      result.commands?.length === 1 && result.commands[0].exitCode === 7,
      'Blocking evidence must retain the original command exit code without retry.',
    );
    let reuseExitCode = 0;
    try {
      execFileSync(
        process.execPath,
        [
          RUNNER_PATH,
          '--matrix',
          fixturePath,
          '--job',
          'failure-probe',
          '--results-dir',
          resultsDirectory,
        ],
        { cwd: REPOSITORY_ROOT, stdio: 'pipe' },
      );
    } catch (error) {
      reuseExitCode = error.status ?? 1;
    }
    check(
      reuseExitCode === 2,
      'The runner must refuse a non-empty results directory rather than reuse stale evidence.',
    );
  } catch (error) {
    errors.push(`Blocking runner failure probe failed: ${error.message}`);
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}

function verifyJourneyGroupEnforcementContract() {
  const temporaryRoot = mkdtempSync(resolve(tmpdir(), 'qdb-pr-journey-enforcement-'));
  const fixturePath = resolve(temporaryRoot, 'matrix.json');
  const probePath = resolve(temporaryRoot, 'write-playwright-evidence.mjs');
  const fixture = {
    schemaVersion: 1,
    id: 'journey-enforcement-probe',
    scheduling: 'parallel',
    durationScope: 'probe duration with queue time excluded',
    durationComponents: ['provisioning', 'testExecution'],
    jobs: [
      {
        id: 'critical-probe',
        title: 'Critical journey probe',
        policy: {
          blocking: false,
          maxAttempts: 1,
          timeoutSeconds: 5,
          queueTimeIncluded: false,
          journeyGroups: [
            {
              id: 'quran-fidelity',
              blocking: true,
              journeys: ['quran-fidelity.reader'],
            },
            {
              id: 'sessions-permissions',
              blocking: false,
              journeys: ['device-session.lifecycle'],
            },
          ],
        },
        commands: [
          {
            id: 'critical-journeys',
            phase: 'execution',
            cwd: '.',
            executable: process.execPath,
            arguments: [probePath, '{SCENARIO}'],
          },
        ],
      },
    ],
  };
  const probe = `
import { mkdirSync, mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
const scenario = process.argv[2];
const evidence = resolve(process.env.QDB_PR_OBSERVATION_RESULT_DIR, 'playwright-evidence', 'probe');
const ownedRuntime = mkdtempSync(resolve(tmpdir(), 'qdb-controlled-playwright-output-'));
writeFileSync(resolve(ownedRuntime, '.qdb-controlled-runtime-owner.json'), JSON.stringify({
  schemaVersion: 1,
  cleanupOwner: evidence,
}));
writeFileSync(resolve(process.env.QDB_PR_OBSERVATION_RESULT_DIR, 'owned-runtime-path.txt'), ownedRuntime);
if (scenario === 'missing-evidence') process.exit(7);
const childEvidence = resolve(evidence, 'canonical', 'evidence');
mkdirSync(childEvidence, { recursive: true });
const quranStatus = scenario === 'blocking-failure' ? 'failed' : 'passed';
const quranRetry = scenario === 'retried-blocker' ? 1 : 0;
const sessionRetry = scenario === 'retried-observation' ? 1 : 0;
const tests = [
  { id: 'quran', journey: 'quran-fidelity.reader', status: quranStatus, durationMs: 5, retry: quranRetry },
  ...(scenario === 'missing-observation'
    ? []
    : [{ id: 'session', journey: 'device-session.lifecycle', status: 'failed', durationMs: 7, retry: sessionRetry }]),
];
const counts = tests.reduce((result, test) => ({
  ...result,
  [test.status]: (result[test.status] ?? 0) + 1,
}), {});
writeFileSync(resolve(childEvidence, 'playwright-results.json'), JSON.stringify({
  schemaVersion: 1,
  runId: 'child-run',
  status: 'failed',
  declaredTestCount: 2,
  counts,
  tests,
}));
const inspection = {
  status: scenario === 'failed-inspection' ? 'failed' : 'passed',
  invalidDiagnosticFiles: scenario === 'failed-inspection' ? ['unsafe.txt'] : [],
  removedRawFiles: [],
  rewrittenTextFiles: [],
  symlinks: [],
  unexpectedFiles: [],
  unsafeScreenshot: false,
};
const child = {
  schemaVersion: 1,
  kind: 'canonical-read',
  runId: 'child-run',
  status: 'failed',
  startedAt: '2026-01-01T00:00:00.000Z',
  completedAt: '2026-01-01T00:00:00.004Z',
  durationMs: 4,
  evidence: 'evidence/playwright-results.json',
  phases: [
    { name: 'capabilityInspection', status: 'passed', durationMs: 1 },
    { name: 'applicationStartup', status: 'passed', durationMs: 1 },
    { name: 'testExecution', status: 'failed', durationMs: 1 },
    { name: 'applicationShutdown', status: 'passed', durationMs: 1 },
  ],
  inspection,
};
writeFileSync(resolve(evidence, 'canonical', 'canonical-results.json'), JSON.stringify(child));
writeFileSync(resolve(evidence, 'playwright-run.json'), JSON.stringify({
  schemaVersion: 1,
  kind: 'complete-playwright',
  runId: 'probe',
  status: 'failed',
  startedAt: '2026-01-01T00:00:00.000Z',
  completedAt: '2026-01-01T00:00:00.005Z',
  durationMs: 5,
  provisioningPhases: [
    'dependencyProvisioning',
    'chromiumProvisioning',
    'certificateProvisioning',
    'buildProvisioning',
  ].map((name) => ({ name, status: 'passed', durationMs: 1 })),
  declaredTestCount: 2,
  counts,
  tests,
  children: [{ ...child, report: 'canonical/canonical-results.json' }],
}));
process.exit(7);
`;

  try {
    writeFileSync(probePath, probe);
    for (const scenario of [
      'observation-failure',
      'blocking-failure',
      'retried-blocker',
      'retried-observation',
      'missing-observation',
      'failed-inspection',
      'missing-evidence',
    ]) {
      const resultsDirectory = resolve(temporaryRoot, scenario);
      const scenarioFixture = structuredClone(fixture);
      scenarioFixture.jobs[0].commands[0].arguments[1] = scenario;
      writeFileSync(fixturePath, `${JSON.stringify(scenarioFixture, null, 2)}\n`);
      let exitCode = 0;
      try {
        execFileSync(
          process.execPath,
          [
            RUNNER_PATH,
            '--matrix',
            fixturePath,
            '--job',
            'critical-probe',
            '--results-dir',
            resultsDirectory,
          ],
          { cwd: REPOSITORY_ROOT, stdio: 'pipe' },
        );
      } catch (error) {
        exitCode = error.status ?? 1;
      }

      const result = JSON.parse(readFileSync(resolve(resultsDirectory, 'job-result.json'), 'utf8'));
      const ownedRuntime = readFileSync(resolve(resultsDirectory, 'owned-runtime-path.txt'), 'utf8');
      const shouldBlock = scenario !== 'observation-failure';
      check(result.status === 'failed', `${scenario} must retain the failed full-catalogue status.`);
      check(
        result.runtimeCleanup?.status === 'passed' && !existsSync(ownedRuntime),
        `${scenario} must verify cleanup of its controlled private runtime.`,
      );
      check(
        result.enforcementStatus === (shouldBlock ? 'failed' : 'passed'),
        `${scenario} must report the blocking journey decision separately.`,
      );
      check(
        exitCode === (shouldBlock ? 1 : 0),
        `${scenario} must return the blocking journey decision to the provider.`,
      );
      check(
        result.journeyGroups?.find(({ id }) => id === 'quran-fidelity')?.status
          === (scenario === 'missing-evidence'
            ? 'not-run'
            : scenario === 'blocking-failure' || scenario === 'retried-blocker'
              ? 'failed'
              : 'passed'),
        `${scenario} must retain Quran-fidelity group evidence.`,
      );
      check(
        result.journeyGroups?.find(({ id }) => id === 'sessions-permissions')?.status
          === (scenario === 'missing-evidence' || scenario === 'missing-observation'
            ? 'not-run'
            : 'failed'),
        `${scenario} must retain non-blocking session failure evidence.`,
      );
      if (scenario === 'failed-inspection') {
        check(
          result.journeyContractErrors?.includes('controlled-evidence-inspection-invalid'),
          'A failed controlled-evidence inspection must be retained as the blocking reason.',
        );
      }
    }
  } catch (error) {
    errors.push(`Journey-group enforcement probe failed: ${error.message}`);
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}

function verifyOrphanDescendantCleanupContract() {
  const temporaryRoot = mkdtempSync(resolve(tmpdir(), 'qdb-pr-orphan-cleanup-'));
  const fixturePath = resolve(temporaryRoot, 'matrix.json');
  const resultsDirectory = resolve(temporaryRoot, 'results');
  const fixture = {
    schemaVersion: 1,
    id: 'orphan-cleanup-probe',
    scheduling: 'parallel',
    durationScope: 'probe duration with queue time excluded',
    durationComponents: ['testExecution'],
    jobs: [{
      id: 'critical-probe',
      title: 'Critical cleanup probe',
      policy: {
        blocking: false,
        maxAttempts: 1,
        timeoutSeconds: 1,
        queueTimeIncluded: false,
        journeyGroups: [{ id: 'probe', blocking: true, journeys: ['probe.journey'] }],
      },
      commands: [{
        id: 'orphan-probe',
        phase: 'execution',
        cwd: '.',
        executable: process.execPath,
        arguments: ['scripts/test-fixtures/pr-orphan-descendant-probe.mjs'],
      }],
    }],
  };
  try {
    writeFileSync(fixturePath, `${JSON.stringify(fixture, null, 2)}\n`);
    let exitCode = 0;
    try {
      execFileSync(process.execPath, [
        RUNNER_PATH,
        '--matrix', fixturePath,
        '--job', 'critical-probe',
        '--results-dir', resultsDirectory,
      ], { cwd: REPOSITORY_ROOT, stdio: 'pipe' });
    } catch (error) {
      exitCode = error.status ?? 1;
    }
    const result = JSON.parse(readFileSync(resolve(resultsDirectory, 'job-result.json'), 'utf8'));
    const descendantPid = Number(readFileSync(resolve(resultsDirectory, 'descendant-pid.txt'), 'utf8'));
    const runtime = readFileSync(resolve(resultsDirectory, 'owned-runtime-path.txt'), 'utf8');
    check(exitCode === 1 && result.status === 'timed-out', 'The orphan probe must time out.');
    check(result.runtimeCleanup?.status === 'passed', 'PR timeout cleanup must be verified.');
    check(!existsSync(runtime), 'PR timeout cleanup must remove the owned private runtime.');
    let descendantAlive = true;
    try {
      process.kill(descendantPid, 0);
    } catch (error) {
      descendantAlive = error?.code !== 'ESRCH';
    }
    check(!descendantAlive, 'PR timeout cleanup must follow the process-group SIGKILL sweep.');
  } catch (error) {
    errors.push(`PR orphan-descendant cleanup probe failed: ${error.message}`);
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}
