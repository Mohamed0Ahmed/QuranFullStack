import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';

import { classifyFocusedPlaywrightSelector } from './canonical-playwright-runtime.mjs';

const [selector, ...extraArguments] = process.argv.slice(2);
if (!selector || extraArguments.length > 0) {
  throw new Error('Focused Playwright execution requires exactly one file:line selector.');
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

const tests = JSON.parse(discovery.stdout);
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
