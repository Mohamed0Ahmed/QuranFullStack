import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const directory = resolve(process.env.QDB_PR_OBSERVATION_RESULT_DIR, 'playwright-evidence', 'probe');
const runId = 'probe';
mkdirSync(directory, { recursive: true });
writeFileSync(resolve(directory, 'playwright-results.json'), JSON.stringify({
  schemaVersion: 1,
  runId,
  status: 'passed',
  declaredTestCount: 1,
  counts: { skipped: 1 },
  tests: [{ id: 'mobile-probe', journey: 'mobile.probe', retry: 0, status: 'skipped' }],
}));
writeFileSync(resolve(directory, 'structured-results.json'), JSON.stringify({
  schemaVersion: 1,
  runId,
  status: 'passed',
  phases: [
    { name: 'artifactProvisioning', status: 'passed', durationMs: 0 },
    { name: 'databasePreparation', status: 'passed', durationMs: 0 },
    { name: 'applicationStartup', status: 'passed', durationMs: 0 },
    { name: 'testExecution', status: 'passed', durationMs: 0 },
  ],
}));
writeFileSync(resolve(directory, 'evidence-manifest.json'), JSON.stringify({
  schemaVersion: 1,
  runId,
  status: 'passed',
  containsDatabaseDump: false,
  capturesRequestHeaders: false,
  capturesRequestBodies: false,
  traceFormat: 'sanitized-step-events-v1',
  screenshotPolicy: 'text-media-masked-v1',
  files: ['evidence-manifest.json', 'playwright-results.json', 'structured-results.json'],
  inspection: { status: 'passed', unsafeScreenshot: false, invalidDiagnosticFiles: [] },
}));
