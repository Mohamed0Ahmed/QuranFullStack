import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';

import { classifyFocusedPlaywrightSelector } from './canonical-playwright-runtime.mjs';
import {
  discoverControlledPlaywright,
  loadControlledProvisioningReceipt,
} from './controlled-playwright-runtime.mjs';

const [selector, ...extraArguments] = process.argv.slice(2);
if (!selector || extraArguments.length > 0) {
  throw new Error('Focused Playwright execution requires exactly one file:line selector.');
}

const frontendRoot = resolve(import.meta.dirname, '..');
const repositoryRoot = resolve(frontendRoot, '../..');
const receipt = loadControlledProvisioningReceipt(frontendRoot, repositoryRoot);
const tests = discoverControlledPlaywright(
  frontendRoot,
  receipt,
  './scripts/discover-playwright-policies.mjs',
  process.env,
);
const partition = classifyFocusedPlaywrightSelector(tests, selector);
const script = partition === 'canonical-read'
  ? 'run-canonical-playwright.mjs'
  : 'run-stateful-playwright.mjs';
const result = spawnSync(process.execPath, [resolve(frontendRoot, 'scripts', script), '--focused', selector], {
  cwd: frontendRoot,
  env: process.env,
  stdio: 'inherit',
});
if (result.error) throw result.error;
process.exit(result.status ?? 1);
