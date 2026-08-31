import { execFileSync, spawnSync } from 'node:child_process';
import { resolve } from 'node:path';

import { COMPACT_ARTIFACT_IDS } from '../e2e/harness/artifact-contract.mjs';

const playwright = resolve(process.cwd(), 'node_modules/.bin/playwright');
const discovery = execFileSync(
  playwright,
  ['test', '--list', '--reporter=./scripts/discover-playwright-journeys.mjs'],
  { cwd: process.cwd(), encoding: 'utf8', stdio: ['ignore', 'pipe', 'inherit'] },
);
const journeys = JSON.parse(discovery);
if (!Array.isArray(journeys) || journeys.length === 0) {
  throw new Error('Critical Playwright execution received an empty discovery selection.');
}
const supportedArtifacts = new Set(COMPACT_ARTIFACT_IDS);
if (journeys.some((journey) => !supportedArtifacts.has(journey.artifact))) {
  throw new Error('Critical Playwright execution found an unsupported artifact selection.');
}

const selectors = [
  ...new Set(journeys.map((journey) => `e2e/${journey.file}:${journey.line}`)),
];
const result = spawnSync(
  playwright,
  ['test', ...selectors, '--workers=1'],
  { cwd: process.cwd(), env: process.env, stdio: 'inherit' },
);
if (result.error) {
  throw result.error;
}
if (result.status !== 0) {
  process.exit(result.status ?? 1);
}
