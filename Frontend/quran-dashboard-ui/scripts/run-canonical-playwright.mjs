import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  buildCanonicalPlaywrightEnvironment,
  selectCanonicalCriticalJourneys,
  selectCanonicalPlaywrightTests,
} from './canonical-playwright-runtime.mjs';

const scriptPath = fileURLToPath(import.meta.url);
const frontendRoot = resolve(dirname(scriptPath), '..');
const repositoryRoot = resolve(frontendRoot, '../..');
const playwright = resolve(frontendRoot, 'node_modules/.bin/playwright');
const testRuntime = resolve(
  repositoryRoot,
  'Backend/tools/QuranDashboard.TestRuntime/bin/Debug/net10.0/QuranDashboard.TestRuntime.dll',
);
const [mode = '--full', selector, interactiveMode, ...extraArguments] = process.argv.slice(2);

if (!['--critical', '--focused', '--full'].includes(mode)) {
  throw new Error('Use --critical, --focused, or --full for canonical Playwright execution.');
}
if (mode === '--focused' && (!selector || extraArguments.length > 0)) {
  throw new Error('--focused requires exactly one Playwright file:line selector.');
}
if (
  mode === '--focused'
  && interactiveMode !== undefined
  && !['--headed', '--ui'].includes(interactiveMode)
) {
  throw new Error('Focused canonical Playwright supports only --headed or --ui interactive mode.');
}
if (
  mode !== '--focused'
  && (selector !== undefined || interactiveMode !== undefined || extraArguments.length > 0)
) {
  throw new Error(`${mode} does not accept a Playwright selector.`);
}

const environment = buildCanonicalPlaywrightEnvironment(process.env);
const selectors = mode === '--critical'
  ? selectCanonicalCriticalJourneys(discover('./scripts/discover-playwright-journeys.mjs'))
  : selectCanonicalPlaywrightTests(
      discover('./scripts/discover-playwright-policies.mjs'),
      mode === '--focused' ? selector : undefined,
    );

if (selectors.length === 0) {
  throw new Error('Canonical Playwright execution discovered no canonical-read tests.');
}
if (!existsSync(testRuntime)) {
  throw new Error(`Canonical Playwright execution requires built TestRuntime output: ${testRuntime}`);
}

run('dotnet', [testRuntime, 'inspect'], environment);
const playwrightArguments = ['test', ...selectors, `--workers=${interactiveMode ? 1 : 2}`];
if (interactiveMode) playwrightArguments.push(interactiveMode);
run(playwright, playwrightArguments, environment);

function discover(reporter) {
  const result = spawnSync(
    playwright,
    ['test', '--list', `--reporter=${reporter}`],
    {
      cwd: frontendRoot,
      encoding: 'utf8',
      env: environment,
      maxBuffer: 10 * 1024 * 1024,
    },
  );
  if (result.error) throw result.error;
  if (result.status !== 0) {
    process.stderr.write(result.stderr ?? '');
    process.exit(result.status ?? 1);
  }
  try {
    return JSON.parse(result.stdout);
  } catch (error) {
    throw new Error(`Playwright discovery returned invalid JSON: ${error.message}`);
  }
}

function run(executable, arguments_, env) {
  const result = spawnSync(executable, arguments_, {
    cwd: frontendRoot,
    env,
    stdio: 'inherit',
  });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status ?? 1);
}
