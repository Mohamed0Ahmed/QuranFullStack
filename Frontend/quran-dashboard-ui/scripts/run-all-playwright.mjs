import { spawnSync } from 'node:child_process';
import { chmodSync, mkdirSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import { writeJson } from './controlled-playwright-runtime.mjs';

const [mode = '--full', ...extraArguments] = process.argv.slice(2);
if (!['--critical', '--full'].includes(mode) || extraArguments.length > 0) {
  throw new Error('Use --critical or --full for complete Playwright execution.');
}

const frontendRoot = resolve(import.meta.dirname, '..');
const runId = `playwright-${Date.now()}-${process.pid}`;
const observationDirectory = process.env.QDB_PR_OBSERVATION_RESULT_DIR?.trim();
const evidenceRoot = observationDirectory
  ? resolve(observationDirectory, 'playwright-evidence')
  : resolve(frontendRoot, '.playwright/evidence');
const aggregateDirectory = resolve(evidenceRoot, runId);
mkdirSync(aggregateDirectory, { recursive: true, mode: 0o700 });
chmodSync(aggregateDirectory, 0o700);
const startedAt = Date.now();
const report = {
  schemaVersion: 1,
  kind: 'complete-playwright',
  runId,
  status: 'passed',
  startedAt: new Date().toISOString(),
  completedAt: null,
  durationMs: null,
  children: [],
};

for (const child of [
  { kind: 'canonical-read', script: 'run-canonical-playwright.mjs', report: 'canonical/canonical-results.json' },
  { kind: 'stateful', script: 'run-stateful-playwright.mjs', report: 'stateful/stateful-results.json' },
]) {
  const childStartedAt = Date.now();
  const result = spawnSync(process.execPath, [resolve(import.meta.dirname, child.script), mode], {
    cwd: frontendRoot,
    env: {
      ...process.env,
      QDB_PLAYWRIGHT_AGGREGATE_DIRECTORY: aggregateDirectory,
    },
    stdio: 'inherit',
  });
  let childReport;
  try {
    if (result.error) throw result.error;
    childReport = readChildReport(resolve(aggregateDirectory, child.report), child.kind);
  } catch (error) {
    report.status = 'failed';
    report.failure = {
      child: child.kind,
      exitCode: result.status ?? 1,
      elapsedMs: Date.now() - childStartedAt,
      error: error instanceof Error ? error.message : `Controlled ${child.kind} child failed.`,
    };
    break;
  }
  report.children.push({
    ...childReport,
    report: child.report,
  });
  if (result.status !== 0 || childReport.status !== 'passed') {
    report.status = 'failed';
    report.failure = {
      child: child.kind,
      exitCode: result.status ?? 1,
      elapsedMs: Date.now() - childStartedAt,
    };
    break;
  }
}

report.completedAt = new Date().toISOString();
report.durationMs = Date.now() - startedAt;
writeJson(resolve(aggregateDirectory, 'playwright-run.json'), report);
console.log(
  `[e2e] controlled complete status=${report.status} children=${report.children.length} evidence=${aggregateDirectory}`,
);
process.exit(report.status === 'passed' ? 0 : 1);

function readChildReport(path, kind) {
  let child;
  try {
    child = JSON.parse(readFileSync(path, 'utf8'));
  } catch {
    throw new Error(`Controlled ${kind} execution returned no aggregate report.`);
  }
  if (
    child?.schemaVersion !== 1
    || child.kind !== kind
    || !['passed', 'failed'].includes(child.status)
    || !Number.isFinite(child.durationMs)
  ) {
    throw new Error(`Controlled ${kind} execution returned an invalid aggregate report.`);
  }
  return child;
}
