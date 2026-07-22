#!/usr/bin/env node
// FR-023 / FR-030 / SC-010 shared-frontend-foundation boundary gate (feature 028, US4/US5).
// The §14.1 foundation must stay GENERIC: the foundation primitives must not become a domain HTTP adapter,
// import @angular/forms, or reference any Abwab/Quran domain vocabulary. @angular/forms is LEGITIMATELY
// installed from US5 onward (the real Reactive-Forms grant/revoke feature imports it), so the gate no longer
// forbids the package — it only forbids Forms/HTTP/domain leakage INTO the foundation scope below. This is a
// pure source + manifest scan (no build/browser) so it runs anywhere CI runs, and it fails closed.

import { readdir, readFile, stat } from 'node:fs/promises';
import { join, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = join(dirname(fileURLToPath(import.meta.url)), '..');

// The exact source that US4 owns. The boundary is scoped to these paths so it never false-trips on
// legitimate domain features elsewhere in src/.
const foundationPaths = [
  'src/app/core/foundation',
  'src/app/core/caching/async-key-value-store.ts',
  'src/app/core/caching/indexed-db-key-value-store.ts',
  'src/app/core/caching/persistent-cache.ts',
  'src/app/core/data-access/signal-store.ts',
  'src/app/core/data-access/async-action.ts',
  'src/app/shared/concurrency',
];

// Domain vocabulary that must NOT leak into a generic primitive (case-insensitive, word-boundary).
const domainTokens = [
  'abwab', 'quran', 'ayah', 'ayat', 'surah', 'mushaf', 'tafsir', 'mutashabihat',
  'lemma', 'stem', 'i3rab', 'section', 'category', 'gate', 'attribution',
];
const domainPattern = new RegExp(`\\b(${domainTokens.join('|')})\\b`, 'i');

// A generic primitive is not an HTTP/domain adapter; HttpClient anywhere in the foundation fails the gate.
const httpPattern = /\b(HttpClient|@angular\/common\/http)\b/;

// The foundation stays Forms-free even though @angular/forms is installed for the US5 permissions feature.
// Matches a real module specifier (quoted import), not an incidental mention in a comment.
const formsPattern = /['"]@angular\/forms['"]/;

const violations = [];

async function collectFiles(target) {
  const info = await stat(target);
  if (info.isFile()) {
    return target.endsWith('.ts') ? [target] : [];
  }
  const entries = await readdir(target, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    const full = join(target, entry.name);
    if (entry.isDirectory()) {
      files.push(...(await collectFiles(full)));
    } else if (entry.name.endsWith('.ts')) {
      files.push(full);
    }
  }
  return files;
}

// Foundation primitives must be generic: no domain vocabulary, no HTTP adapter, no Forms import, no
// *.adapter.ts file. (@angular/forms may be installed as a package from US5 — it just must not leak here.)
for (const path of foundationPaths) {
  const absolute = join(projectRoot, path);
  let files;
  try {
    files = await collectFiles(absolute);
  } catch {
    continue; // a path may not exist yet during partial checkouts; other checks still run
  }
  for (const file of files) {
    const rel = relative(projectRoot, file);
    if (/\.adapter\.ts$/.test(file)) {
      violations.push(`${rel}: a domain/all-domain adapter must not live in the shared foundation.`);
    }
    const lines = (await readFile(file, 'utf8')).split('\n');
    lines.forEach((line, index) => {
      if (domainPattern.test(line)) {
        violations.push(`${rel}:${index + 1}: domain vocabulary in a generic primitive -> ${line.trim()}`);
      }
      if (httpPattern.test(line)) {
        violations.push(`${rel}:${index + 1}: HttpClient/HTTP adapter in the generic foundation -> ${line.trim()}`);
      }
      if (formsPattern.test(line)) {
        violations.push(`${rel}:${index + 1}: @angular/forms import in the generic foundation -> ${line.trim()}`);
      }
    });
  }
}

if (violations.length > 0) {
  console.error('Foundation boundary gate FAILED (FR-023 / SC-010):');
  for (const violation of violations) {
    console.error('  ' + violation);
  }
  process.exit(1);
}

console.log('Foundation boundary gate passed: foundation primitives are generic (Forms/HTTP/domain-free).');
