import { spawn } from 'node:child_process';
import { mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';

const resultsDirectory = process.env.QDB_PR_OBSERVATION_RESULT_DIR;
const ownerRoot = resolve(resultsDirectory, 'playwright-evidence');
const runtime = mkdtempSync(resolve(tmpdir(), 'qdb-controlled-playwright-output-'));
writeFileSync(resolve(runtime, '.qdb-controlled-runtime-owner.json'), JSON.stringify({
  schemaVersion: 1,
  cleanupOwner: ownerRoot,
}));
writeFileSync(resolve(resultsDirectory, 'owned-runtime-path.txt'), runtime);

const descendant = spawn(process.execPath, ['-e', `
  process.on('SIGTERM', () => {});
  setInterval(() => {}, 1_000);
`], { detached: false, stdio: 'ignore' });
writeFileSync(resolve(resultsDirectory, 'descendant-pid.txt'), String(descendant.pid));

process.on('SIGTERM', () => process.exit(0));
setInterval(() => {}, 1_000);
