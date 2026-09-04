import { lstatSync, readFileSync, readdirSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { isAbsolute, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  CONTROLLED_RUNTIME_MARKER,
  CONTROLLED_RUNTIME_PREFIXES,
} from './controlled-playwright-runtime.mjs';

if (resolve(process.argv[1] ?? '') === fileURLToPath(import.meta.url)) {
  const [resultsDirectory, attempt, ...extras] = process.argv.slice(2);
  if (extras.length > 0) {
    throw new Error('Controlled cleanup accepts exactly one results directory and attempt identifier.');
  }
  const removed = cleanupControlledPlaywrightRuntime(resultsDirectory, attempt);
  console.log(`[e2e] controlled runtime cleanup verified removed=${removed}`);
}

export function cleanupControlledPlaywrightRuntime(resultsDirectory, attempt) {
  if (!resultsDirectory || !attempt || !/^[A-Za-z0-9._-]+$/.test(attempt)) {
    throw new Error('Controlled cleanup requires one results directory and one attempt identifier.');
  }
  const cleanupRoot = resolve(resultsDirectory, 'attempts', attempt, 'playwright-evidence');
  const candidates = ownedRuntimeDirectories(cleanupRoot);
  for (const candidate of candidates) rmSync(candidate, { force: true, recursive: true });
  const remaining = ownedRuntimeDirectories(cleanupRoot);
  if (remaining.length > 0) {
    throw new Error(`Controlled cleanup could not remove ${remaining.length} owned runtime directories.`);
  }
  return candidates.length;
}

function ownedRuntimeDirectories(ownerRoot) {
  return readdirSync(tmpdir(), { withFileTypes: true })
    .filter((entry) => entry.isDirectory()
      && CONTROLLED_RUNTIME_PREFIXES.some((prefix) => entry.name.startsWith(prefix)))
    .map((entry) => resolve(tmpdir(), entry.name))
    .filter((directory) => hasMatchingOwner(directory, ownerRoot));
}

function hasMatchingOwner(directory, ownerRoot) {
  try {
    const markerPath = resolve(directory, CONTROLLED_RUNTIME_MARKER);
    if (!lstatSync(markerPath).isFile()) return false;
    const marker = JSON.parse(readFileSync(markerPath, 'utf8'));
    if (marker?.schemaVersion !== 1 || typeof marker.cleanupOwner !== 'string') return false;
    const owner = resolve(marker.cleanupOwner);
    const nested = relative(ownerRoot, owner);
    return owner === ownerRoot || (nested !== '' && !nested.startsWith('..') && !isAbsolute(nested));
  } catch {
    return false;
  }
}
