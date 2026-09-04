import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';

const [mode = '--full', ...extraArguments] = process.argv.slice(2);
if (!['--critical', '--full'].includes(mode) || extraArguments.length > 0) {
  throw new Error('Use --critical or --full for complete Playwright execution.');
}

const scripts = [
  'run-canonical-playwright.mjs',
  'run-stateful-playwright.mjs',
];
for (const script of scripts) {
  const result = spawnSync(process.execPath, [resolve(import.meta.dirname, script), mode], {
    cwd: resolve(import.meta.dirname, '..'),
    env: process.env,
    stdio: 'inherit',
  });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status ?? 1);
}
