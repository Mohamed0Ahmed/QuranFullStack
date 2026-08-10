import { readdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const sourceRoot = path.join(projectRoot, 'src');

const specFiles = (await readdir(sourceRoot, { recursive: true }))
  .filter((entry) => entry.endsWith('.spec.ts'))
  .map((entry) => path.join('src', entry).split(path.sep).join('/'))
  .sort();

if (specFiles.length > 0) {
  console.error(`FAIL check-no-unit-specs: found ${specFiles.length} prohibited unit spec(s):`);
  for (const file of specFiles) {
    console.error(`  - ${file}`);
  }
  process.exit(1);
}

console.log('Unit-spec freeze passed: no src/**/*.spec.ts files found.');
