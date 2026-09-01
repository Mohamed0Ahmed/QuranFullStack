import { appendFileSync, mkdirSync, symlinkSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const outcome = process.argv[2];
const directory = resolve(process.env.QDB_PR_OBSERVATION_RESULT_DIR, 'playwright-evidence', 'probe');
const runId = 'probe';
mkdirSync(directory, { recursive: true });
appendFileSync(resolve(process.env.QDB_PR_OBSERVATION_RESULT_DIR, '..', '..', 'lifecycle-order.log'), `${process.env.QDB_NIGHTLY_ATTEMPT}:${outcome}\n`);

const status = outcome === 'failed' && process.env.QDB_NIGHTLY_ATTEMPT === 'primary' ? 'failed' : 'passed';
const testStatus = status === 'passed' ? 'passed' : 'failed';
const evidenceRunId = outcome === 'run-id-mismatch' ? 'other-run' : runId;
writeFileSync(resolve(directory, 'playwright-results.json'), JSON.stringify({
  schemaVersion: 1,
  runId: evidenceRunId,
  status,
  declaredTestCount: 1,
  counts: { [testStatus]: 1 },
  tests: [{ id: 'mobile-probe', journey: 'mobile.probe', retry: 0, status: testStatus }],
}));
writeFileSync(resolve(directory, 'structured-results.json'), JSON.stringify({
  schemaVersion: 1,
  runId,
  status,
  phases: [
    { name: 'artifactProvisioning', status: 'passed', durationMs: 0 },
    { name: 'databasePreparation', status: 'passed', durationMs: 0 },
    { name: 'applicationStartup', status: 'passed', durationMs: 0 },
    { name: 'testExecution', status, durationMs: 0 },
  ],
}));
writeFileSync(resolve(directory, 'evidence-manifest.json'), JSON.stringify({
  schemaVersion: 1,
  runId,
  status,
  containsDatabaseDump: false,
  capturesRequestHeaders: false,
  capturesRequestBodies: false,
  traceFormat: 'sanitized-step-events-v1',
  screenshotPolicy: 'text-media-masked-v1',
  inspection: {
    status: outcome === 'unsafe-screenshot' || outcome === 'invalid-diagnostics' ? 'failed' : 'passed',
    unsafeScreenshot: outcome === 'unsafe-screenshot',
    invalidDiagnosticFiles: outcome === 'invalid-diagnostics' ? ['diagnostics/probe/unapproved.json'] : [],
  },
  files: outcome === 'extra-file'
    ? ['evidence-manifest.json', 'playwright-results.json', 'structured-results.json']
    : ['evidence-manifest.json', 'playwright-results.json', 'structured-results.json'],
}));
if (outcome === 'extra-file') writeFileSync(resolve(directory, 'unapproved.txt'), 'probe');
if (outcome === 'symlink') {
  symlinkSync('playwright-results.json', resolve(directory, 'unapproved-link'));
  symlinkSync('probe', resolve(process.env.QDB_PR_OBSERVATION_RESULT_DIR, 'playwright-evidence', 'unapproved-run-link'));
}
process.exit(status === 'passed' ? 0 : 7);
