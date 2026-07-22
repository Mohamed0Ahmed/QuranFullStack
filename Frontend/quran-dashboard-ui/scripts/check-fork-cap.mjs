#!/usr/bin/env node
// FR-003 Vitest fork-concurrency cap gate.
// vitest.config.ts is ignored by the @angular/build:unit-test builder, so the only place the fork
// cap can live is the package.json "test" script env prefix. This asserts it is still there; removing
// it silently regresses CI stability, so CI fails closed if the env vars go missing.

import { readFile } from 'node:fs/promises';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const pkg = JSON.parse(await readFile(join(projectRoot, 'package.json'), 'utf8'));
const testScript = pkg.scripts?.test ?? '';

const required = ['VITEST_MIN_FORKS=1', 'VITEST_MAX_FORKS=2'];
const missing = required.filter((token) => !testScript.includes(token));

if (missing.length > 0) {
  console.error('Fork-cap gate FAILED (FR-003): package.json "test" script must set ' + required.join(' '));
  console.error('  found: ' + JSON.stringify(testScript));
  process.exit(1);
}

console.log('Fork-cap gate passed: ' + required.join(' ') + ' present in the test script.');
