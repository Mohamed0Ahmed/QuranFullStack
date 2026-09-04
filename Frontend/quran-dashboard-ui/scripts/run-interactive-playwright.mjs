import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';

import {
  discoverControlledPlaywright,
  loadControlledProvisioningReceipt,
} from './controlled-playwright-runtime.mjs';
import { classifyInteractivePlaywrightSelector } from './stateful-playwright-runtime.mjs';

const [interactiveMode, requestedMode, selector, ...extraArguments] = process.argv.slice(2);
if (!['--headed', '--ui'].includes(interactiveMode)) {
  throw new Error('Interactive Playwright requires --headed or --ui.');
}
if (!['--read-only', '--mutating'].includes(requestedMode) || !selector || extraArguments.length > 0) {
  throw new Error(
    'Interactive Playwright requires exactly one --read-only or --mutating file:line selector.',
  );
}

const frontendRoot = resolve(import.meta.dirname, '..');
const repositoryRoot = resolve(frontendRoot, '../..');
const receipt = loadControlledProvisioningReceipt(frontendRoot, repositoryRoot);
const discovered = discoverControlledPlaywright(
  frontendRoot,
  receipt,
  './scripts/discover-playwright-policies.mjs',
  process.env,
);

const policy = classifyInteractivePlaywrightSelector(
  discovered,
  requestedMode.slice(2),
  selector,
);
const script = policy === 'canonical-read'
  ? 'run-canonical-playwright.mjs'
  : 'run-stateful-playwright.mjs';
const result = spawnSync(
  process.execPath,
  [resolve(import.meta.dirname, script), '--focused', selector, interactiveMode],
  { cwd: frontendRoot, env: process.env, stdio: 'inherit' },
);
if (result.error) throw result.error;
process.exit(result.status ?? 1);
