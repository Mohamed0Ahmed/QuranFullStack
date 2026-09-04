import { appendFileSync, symlinkSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

import { writeControlledBrowserEvidence } from './write-controlled-browser-evidence.mjs';

const outcome = process.argv[2];
appendFileSync(resolve(process.env.QDB_PR_OBSERVATION_RESULT_DIR, '..', '..', 'lifecycle-order.log'), `${process.env.QDB_NIGHTLY_ATTEMPT}:${outcome}\n`);

const status = outcome === 'failed' && process.env.QDB_NIGHTLY_ATTEMPT === 'primary' ? 'failed' : 'passed';
const directory = writeControlledBrowserEvidence(process.env.QDB_PR_OBSERVATION_RESULT_DIR, {
  status,
  evidenceRunId: outcome === 'run-id-mismatch' ? 'other-run' : undefined,
  inspection: {
    status: outcome === 'unsafe-screenshot' || outcome === 'invalid-diagnostics' ? 'failed' : 'passed',
    unsafeScreenshot: outcome === 'unsafe-screenshot',
    invalidDiagnosticFiles: outcome === 'invalid-diagnostics' ? ['diagnostics/probe/unapproved.json'] : [],
    removedRawFiles: [],
    rewrittenTextFiles: [],
    symlinks: [],
    unexpectedFiles: [],
  },
});
if (outcome === 'extra-file') writeFileSync(resolve(directory, 'unapproved.txt'), 'probe');
if (outcome === 'symlink') {
  symlinkSync('playwright-run.json', resolve(directory, 'unapproved-link'));
  symlinkSync('probe', resolve(process.env.QDB_PR_OBSERVATION_RESULT_DIR, 'playwright-evidence', 'unapproved-run-link'));
}
process.exit(status === 'passed' ? 0 : 7);
