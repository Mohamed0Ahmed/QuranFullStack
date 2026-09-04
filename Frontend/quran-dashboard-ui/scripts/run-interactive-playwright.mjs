import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';

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
const playwright = resolve(frontendRoot, 'node_modules/.bin/playwright');
const discovery = spawnSync(
  playwright,
  ['test', '--list', '--reporter=./scripts/discover-playwright-policies.mjs'],
  { cwd: frontendRoot, encoding: 'utf8', env: process.env, maxBuffer: 10 * 1024 * 1024 },
);
if (discovery.error) throw discovery.error;
if (discovery.status !== 0) {
  process.stderr.write(discovery.stderr ?? '');
  process.exit(discovery.status ?? 1);
}

const policy = classifyInteractivePlaywrightSelector(
  JSON.parse(discovery.stdout),
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
