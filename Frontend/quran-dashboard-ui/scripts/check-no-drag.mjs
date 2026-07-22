#!/usr/bin/env node
// FR-041 / SC-016 frontend no-drag SOURCE gate.
// Abwab tree reordering is explicitly NOT drag-and-drop (§3.2), so drag/drop must never enter the
// app source. This scans src/ and fails CI if any drag/drop package, directive, handle, or event
// wiring appears. It is a pure source scan (no build/browser) so it runs anywhere CI runs.

import { readdir, readFile } from 'node:fs/promises';
import { join, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const srcRoot = join(projectRoot, 'src');

// Each entry: a token that only appears when drag/drop is present. Kept specific so ordinary words
// (dropdown, backdrop, raindrop) never trip the gate; parenthesised forms match Angular outputs.
const forbidden = [
  { label: 'Angular CDK drag-drop import', pattern: /@angular\/cdk\/drag-drop/ },
  { label: 'DragDropModule', pattern: /\bDragDropModule\b/ },
  { label: 'cdkDrag* directive', pattern: /\bcdkDrag[A-Za-z]*\b/ },
  { label: 'cdkDropList* directive', pattern: /\bcdkDropList[A-Za-z]*\b/ },
  { label: 'draggable attribute/property', pattern: /\bdraggable\b/ },
  { label: 'Angular drag/drop output binding', pattern: /\((?:drag(?:start|end|enter|leave|over|exit)?|drop)\)/ },
  { label: 'DOM drag/drop handler property', pattern: /\bon(?:drag(?:start|end|enter|leave|over|exit)?|drop)\b/ },
  { label: 'DragEvent type', pattern: /\bDragEvent\b/ },
];

const scannedExtensions = new Set(['.ts', '.html', '.scss', '.css', '.json']);

async function collectFiles(dir) {
  const entries = await readdir(dir, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) {
      files.push(...(await collectFiles(full)));
    } else if (scannedExtensions.has(full.slice(full.lastIndexOf('.')))) {
      files.push(full);
    }
  }
  return files;
}

const violations = [];
for (const file of await collectFiles(srcRoot)) {
  const lines = (await readFile(file, 'utf8')).split('\n');
  lines.forEach((line, index) => {
    for (const { label, pattern } of forbidden) {
      if (pattern.test(line)) {
        violations.push(`${relative(projectRoot, file)}:${index + 1}: ${label} -> ${line.trim()}`);
      }
    }
  });
}

if (violations.length > 0) {
  console.error('No-drag source gate FAILED (FR-041): drag/drop is prohibited in src/.');
  for (const violation of violations) {
    console.error('  ' + violation);
  }
  process.exit(1);
}

console.log('No-drag source gate passed: no drag/drop tokens in src/.');
