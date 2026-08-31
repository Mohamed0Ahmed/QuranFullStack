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
    check(job.policy?.blocking === false, `${job.id} must start as non-blocking.`);
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
    backend?.inputContract?.candidate === 'full-canonical',
    'The Backend candidate must remain explicitly full-canonical.',
  );
  check(
    backend?.inputContract?.compactFixtureException === null,
    'The Backend candidate must not silently claim a compact-fixture exception.',
  );
  check(
    hasCommand(backend, 'Backend/scripts/test-backend', ['pre-pr']),
    'The Backend job must execute the supported full pre-pr lane.',
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
    'The critical Chromium job must run sealed critical journeys.',
  );

  verifyObservationFailureContract();
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

function verifyObservationFailureContract() {
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
          blocking: false,
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
    const result = JSON.parse(readFileSync(resolve(resultsDirectory, 'job-result.json'), 'utf8'));
    check(result.status === 'failed', 'Observation mode must retain a failed job status.');
    check(
      result.firstAttemptStatus === 'failed',
      'Observation mode must retain the first-attempt failure.',
    );
    check(
      result.maxAttempts === 1 && result.attemptsExecuted === 1,
      'Observation mode must execute exactly one attempt.',
    );
    check(
      result.commands?.length === 1 && result.commands[0].exitCode === 7,
      'Observation evidence must retain the original command exit code without retry.',
    );
  } catch (error) {
    errors.push(`Observation runner failure probe failed: ${error.message}`);
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}
