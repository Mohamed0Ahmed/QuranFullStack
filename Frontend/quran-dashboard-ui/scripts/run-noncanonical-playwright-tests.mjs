import { execFileSync, spawnSync } from 'node:child_process';
import { resolve } from 'node:path';

import { selectNonCanonicalPlaywrightTests } from './canonical-playwright-runtime.mjs';

const frontendRoot = resolve(import.meta.dirname, '..');
const playwright = resolve(frontendRoot, 'node_modules/.bin/playwright');
const discovery = execFileSync(
  playwright,
  ['test', '--list', '--reporter=./scripts/discover-playwright-policies.mjs'],
  {
    cwd: frontendRoot,
    encoding: 'utf8',
    env: process.env,
    maxBuffer: 10 * 1024 * 1024,
    stdio: ['ignore', 'pipe', 'inherit'],
  },
);
const selectors = selectNonCanonicalPlaywrightTests(JSON.parse(discovery));
if (selectors.length === 0) {
  throw new Error('Legacy Playwright execution discovered no non-canonical tests.');
}

const result = spawnSync(playwright, ['test', ...selectors, '--workers=1'], {
  cwd: frontendRoot,
  env: process.env,
  stdio: 'inherit',
});
if (result.error) throw result.error;
process.exit(result.status ?? 1);
