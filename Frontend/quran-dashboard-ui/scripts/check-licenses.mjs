#!/usr/bin/env node
// FR-042 license gate (§15.2 gate 8).
// Fails if any direct production dependency ships under a strong-copyleft license the project cannot
// distribute. Runtime deps only (dependencies, not devDependencies) — the shipped SPA bundle is what
// matters. Kept dependency-free (reads installed package.json license fields) so it runs anywhere.

import { readFile } from 'node:fs/promises';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const pkg = JSON.parse(await readFile(join(projectRoot, 'package.json'), 'utf8'));

const forbidden = [/^AGPL/i, /^GPL/i, /^SSPL/i, /^CC-BY-NC/i];

async function licenseOf(name) {
  try {
    const depPkg = JSON.parse(
      await readFile(join(projectRoot, 'node_modules', name, 'package.json'), 'utf8'),
    );
    if (typeof depPkg.license === 'string') return depPkg.license;
    if (depPkg.license?.type) return depPkg.license.type;
    if (Array.isArray(depPkg.licenses)) return depPkg.licenses.map((l) => l.type).join(' OR ');
    return null;
  } catch {
    return null;
  }
}

const violations = [];
for (const name of Object.keys(pkg.dependencies ?? {})) {
  const license = await licenseOf(name);
  if (license && forbidden.some((rule) => rule.test(license))) {
    violations.push(`${name}: ${license}`);
  }
}

if (violations.length > 0) {
  console.error('License gate FAILED (FR-042): forbidden strong-copyleft license in a production dependency.');
  for (const violation of violations) {
    console.error('  ' + violation);
  }
  process.exit(1);
}

console.log('License gate passed: no forbidden production-dependency licenses.');
