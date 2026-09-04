import { createHash } from 'node:crypto';
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative, resolve, sep } from 'node:path';

export function findFiles(directory, basename) {
  return findMatchingFiles(directory, (entry) => entry.name === basename);
}

export function findFilesByExtension(directory, extension) {
  return findMatchingFiles(directory, (entry) => entry.name.endsWith(extension));
}

function findMatchingFiles(directory, predicate) {
  const matches = [];
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    if (entry.name === 'bin' || entry.name === 'obj') continue;
    const path = join(directory, entry.name);
    if (entry.isDirectory()) matches.push(...findMatchingFiles(path, predicate));
    else if (entry.isFile() && predicate(entry)) matches.push(path);
  }
  return matches.sort();
}

export function findAllFiles(directory) {
  const matches = [];
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) matches.push(...findAllFiles(path));
    else if (entry.isFile()) matches.push(path);
  }
  return matches.sort();
}

export function sha256Files(paths, repositoryRoot) {
  const hash = createHash('sha256');
  for (const path of [...paths].sort()) {
    hash.update(relative(repositoryRoot, path));
    hash.update('\0');
    hash.update(readFileSync(path));
    hash.update('\0');
  }
  return hash.digest('hex');
}

export function sha256Path(path) {
  const details = statSync(path);
  const paths = details.isDirectory() ? findAllFiles(path) : [path];
  const hash = createHash('sha256');
  for (const file of paths) {
    hash.update(relative(path, file));
    hash.update('\0');
    hash.update(readFileSync(file));
    hash.update('\0');
  }
  return hash.digest('hex');
}

export function controlledHarnessSourceFiles(frontendRoot) {
  const e2eRoot = resolve(frontendRoot, 'e2e');
  const browserSources = findAllFiles(e2eRoot).filter((path) =>
    path.endsWith('.e2e.ts')
      || path.startsWith(`${resolve(e2eRoot, 'fixtures')}${sep}`)
      || [
        resolve(e2eRoot, 'harness/controlled-execution-contract.mjs'),
        resolve(e2eRoot, 'harness/egress-guard.c'),
        resolve(e2eRoot, 'run-backend.mjs'),
        resolve(e2eRoot, 'run-canonical-backend.mjs'),
        resolve(e2eRoot, 'run-frontend.mjs'),
      ].includes(path),
  );
  return [
    ...browserSources,
    resolve(frontendRoot, 'package.json'),
    resolve(frontendRoot, 'playwright.config.ts'),
    resolve(frontendRoot, 'scripts/canonical-playwright-runtime.mjs'),
    resolve(frontendRoot, 'scripts/controlled-playwright-runtime.mjs'),
    resolve(frontendRoot, 'scripts/discover-playwright-journeys.mjs'),
    resolve(frontendRoot, 'scripts/discover-playwright-policies.mjs'),
    resolve(frontendRoot, 'scripts/provision-controlled-playwright.mjs'),
    resolve(frontendRoot, 'scripts/run-all-playwright.mjs'),
    resolve(frontendRoot, 'scripts/run-canonical-playwright.mjs'),
    resolve(frontendRoot, 'scripts/run-focused-playwright.mjs'),
    resolve(frontendRoot, 'scripts/run-interactive-playwright.mjs'),
    resolve(frontendRoot, 'scripts/run-stateful-playwright.mjs'),
    resolve(frontendRoot, 'scripts/stateful-playwright-runtime.mjs'),
    resolve(frontendRoot, 'scripts/structured-playwright-reporter.mjs'),
    resolve(frontendRoot, 'scripts/verify-egress-guard-runtime.mjs'),
  ].sort();
}
